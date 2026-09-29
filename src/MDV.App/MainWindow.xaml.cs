using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using MDV.App.Services;
using MDV.Core.Models;
using MDV.Core.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MDV_App;

/// <summary>
/// The application window。メニューバー・WebView2本文・ステータスバーの骨組み。
/// </summary>
public sealed partial class MainWindow : Window
{
    private string? _currentPath;
    private ThemeMode _currentTheme = ThemeMode.System;
    private string _template = "";
    private bool _webReady;
    private AppSettings _settings = new();
    // WinUI3のWebView2 XAMLコントロールはCoreWebView2Controller/ZoomFactorを公開していないため、
    // CSS(zoom)経由でWebView2側に倍率を適用し、現在値はこのフィールドで保持する
    private double _zoomFactor = 1.0;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        this.Closed += (s, e) => PersistSettings();
    }

    // WebView2を初回利用時に1回だけ初期化し、vendorフォルダを仮想ホストにマッピングする
    private async Task EnsureWebViewAsync()
    {
        if (_webReady) return;

        EncodingDetector.RegisterProviders();
        // WebView2の既定UI(右クリックメニュー等)をアプリの表示言語に合わせるため、言語を指定した環境で初期化する
        var env = await CoreWebView2Environment.CreateWithOptionsAsync(null, null,
            new CoreWebView2EnvironmentOptions { Language = Localizer.Get("WebViewLanguage") });
        await ContentView.EnsureCoreWebView2Async(env);

        var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        // http://vendor/... を Assets/vendor フォルダにマッピング
        ContentView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "vendor", Path.Combine(assetsDir, "vendor"),
            CoreWebView2HostResourceAccessKind.Allow);

        _template = LocalizeTemplate(File.ReadAllText(Path.Combine(assetsDir, "template.html")));

        // 外部リンク(http/https)はアプリ内WebView2で遷移させず、既定ブラウザで開く
        ContentView.CoreWebView2.NewWindowRequested += (s, e) =>
        {
            e.Handled = true;
            LaunchExternal(e.Uri);
        };
        ContentView.CoreWebView2.NavigationStarting += (s, e) =>
        {
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https") &&
                uri.Host != "vendor")            // 同梱資産(http://vendor/...)の取得は遮らない
            {
                e.Cancel = true;
                LaunchExternal(e.Uri);
            }
        };

        // 右クリックの既定メニューを閲覧向けに整理する（保存/検証などを除去し、コピー・印刷・再読み込みを残す）
        ContentView.CoreWebView2.ContextMenuRequested += OnContextMenuRequested;

        // ブラウザ既定のショートカット(Ctrl+P/F5/Ctrl+F/Ctrl+±等)を無効化し、アプリのメニュー動作に一本化する
        ContentView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        // WebView2内でのショートカット押下(template.htmlのkeydown→postMessage)をホストで受ける
        ContentView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        _webReady = true;

        LoadSettings();
    }

    // template.html 内のUI文字列プレースホルダ(コピーボタン)を表示言語の文字列に置き換える。
    // JSの文字列リテラルとして埋め込むため、JSONエンコード(引用符・</script>等をエスケープ)して渡す。
    // 本文(Markdown)を埋め込む前のテンプレートに対して行うので、本文中の同じ文字列は置換されない。
    private static string LocalizeTemplate(string template) => template
        .Replace("{{COPY_LABEL}}", System.Text.Json.JsonSerializer.Serialize(Localizer.Get("CopyButton")))
        .Replace("{{COPIED_LABEL}}", System.Text.Json.JsonSerializer.Serialize(Localizer.Get("CopyButtonDone")));

    // 外部URIを既定のブラウザ（既定アプリ）で開く
    private static async void LaunchExternal(string uri)
    {
        if (Uri.TryCreate(uri, UriKind.Absolute, out var u))
            await Windows.System.Launcher.LaunchUriAsync(u);
    }

    // WebView2の右クリックメニューを閲覧向けに整理する
    private void OnContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs e)
    {
        // 閲覧に必要な既定項目のみ残す（コピー・切り取り・貼り付け・全選択・印刷）。
        // 名前(Name)は言語非依存の識別子のため、日本語表示でもこの判定は有効。
        var keep = new System.Collections.Generic.HashSet<string>
        {
            "copy", "cut", "paste", "pasteAndMatchStyle", "selectAll", "print"
        };
        for (int i = e.MenuItems.Count - 1; i >= 0; i--)
        {
            var item = e.MenuItems[i];
            if (item.Kind == CoreWebView2ContextMenuItemKind.Separator || !keep.Contains(item.Name))
                e.MenuItems.RemoveAt(i);
        }

        // 「再読み込み」はカスタム項目として追加し、ファイルを再読込する。
        // ※WebView2既定のReloadはNavigateToStringページを空白化してしまうため使わない。
        if (_currentPath != null)
        {
            if (e.MenuItems.Count > 0)
                e.MenuItems.Add(sender.Environment.CreateContextMenuItem(
                    "", null, CoreWebView2ContextMenuItemKind.Separator));

            var reload = sender.Environment.CreateContextMenuItem(
                Localizer.Get("ContextMenuReload"), null, CoreWebView2ContextMenuItemKind.Command);
            reload.CustomItemSelected += (s2, a2) =>
                DispatcherQueue.TryEnqueue(async () =>
                {
                    if (_currentPath != null) await OpenFileAsync(_currentPath);
                });
            e.MenuItems.Add(reload);
        }
    }

    // WebView2内で押されたショートカット(template.htmlから通知)を対応するメニュー動作へ振り分ける
    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string msg;
        try { msg = e.TryGetWebMessageAsString(); }
        catch { return; }
        // コードブロックの「コピー」ボタン: 受け取った本文をクリップボードへ設定する
        if (msg != null && msg.StartsWith("copy:"))
        {
            var pkg = new DataPackage();
            pkg.SetText(msg.Substring(5));
            Clipboard.SetContent(pkg);
            return;
        }
        switch (msg)
        {
            case "open": OnOpenClick(this, null!); break;
            case "print": OnPrint(this, null!); break;
            case "zoomIn": OnZoomIn(this, null!); break;
            case "zoomOut": OnZoomOut(this, null!); break;
            case "zoomReset": OnZoomReset(this, null!); break;
            case "reload": OnReload(this, null!); break;
            case "about": OnAboutClick(this, null!); break;
        }
    }

    // [ファイル]→[印刷]: 印刷プレビュー(ダイアログ)を表示する。
    // WinUIのWebView2射影は ShowPrintUI を公開していないため、JSの window.print() を用いる。
    private async void OnPrint(object s, RoutedEventArgs e)
    {
        if (_webReady)
            try { await ContentView.CoreWebView2.ExecuteScriptAsync("window.print()"); }
            catch { /* 印刷UIの表示失敗は無視 */ }
    }

    // 文字サイズ(ズーム)をWebView2へ適用する
    // WinUI3のWebView2 XAMLコントロールはCoreWebView2Controller(ZoomFactor)を公開していないため、
    // CSS(zoom)をJS経由で適用する（template.html側のwindow.__setZoomフック）
    private async void ApplyZoom(double factor)
    {
        _zoomFactor = factor;
        if (_webReady)
            try
            {
                await ContentView.CoreWebView2.ExecuteScriptAsync(
                    $"window.__setZoom && window.__setZoom('{_zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)}')");
            }
            catch
            {
                // WebView2が利用不可などのスクリプト実行失敗は無視（テーマ/ズームの見た目のみの影響）
            }
    }

    // 保存済み設定を読み込み、各UIへ適用する（読み込み時は永続化しない）
    private void LoadSettings()
    {
        try { _settings = SettingsService.Load(); } catch { _settings = new AppSettings(); }
        ApplyZoom(_settings.ZoomFactor);
        StatusBar.Visibility = _settings.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;
        SetTheme(_settings.Theme);   // テーマを適用する（永続化はしない）
    }

    // 現在のテーマ・文字サイズ・ステータスバー表示状態を設定として保存する
    private void PersistSettings()
    {
        _settings.Theme = _currentTheme;
        _settings.ZoomFactor = _zoomFactor;
        _settings.ShowStatusBar = StatusBar.Visibility == Visibility.Visible;
        try { SettingsService.Save(_settings); } catch { /* 保存失敗は無視 */ }
    }

    // ファイルを読み込み、MarkdownをHTMLに変換してWebView2に表示し、ステータスバーを更新する
    public async Task OpenFileAsync(string path)
    {
        await EnsureWebViewAsync();
        try
        {
            var doc = FileService.Load(path);
            _currentPath = path;

            var baseUri = new Uri(Path.GetDirectoryName(path)! + Path.DirectorySeparatorChar).AbsoluteUri;
            var html = MarkdownRenderer.BuildHtml(doc.Text, _currentTheme, _template, baseUri);
            ContentView.CoreWebView2.NavigateToString(html);
            // ナビゲーションでCSS状態がリセットされるため、文字サイズを再適用する
            ApplyZoom(_zoomFactor);

            EncodingText.Text = doc.EncodingDisplay;
            // 「混在」だけは言語依存の語のため、表示言語の文字列に差し替える（CRLF/LF/CRは共通表記）
            LineEndingText.Text = doc.LineEnding == LineEndingKind.Mixed
                ? Localizer.Get("LineEndingMixed")
                : LineEndingDetector.ToDisplay(doc.LineEnding);
            // ウィンドウ名(タスクバー)と可視タイトルバーの両方を更新する
            var title = $"MDV — {Path.GetFileName(path)}";
            this.Title = title;
            AppTitleBar.Title = title;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex);
        }
    }

    // 起動時パス（コマンドライン引数/ファイル関連付け）を受け取り、指定があれば開く
    public async Task OpenInitialAsync(string? path)
    {
        await EnsureWebViewAsync();
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            await OpenFileAsync(path!);
    }

    // ファイル読み込みエラーをダイアログで通知する
    private async Task ShowErrorAsync(Exception ex)
    {
        var dialog = new ContentDialog
        {
            Title = Localizer.Get("OpenErrorTitle"),
            Content = ex is FileNotFoundException
                ? Localizer.Get("OpenErrorNotFound")
                : $"{Localizer.Get("OpenErrorGeneric")}\n{ex.Message}",
            CloseButtonText = Localizer.Get("DialogClose"),
            XamlRoot = this.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    // [ヘルプ]→[このアプリについて]: アプリ情報とサードパーティライセンスをダイアログ表示する
    private async void OnAboutClick(object s, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = Localizer.Get("AboutTitle"),
            Content = BuildAboutContent(),
            CloseButtonText = Localizer.Get("DialogClose"),
            XamlRoot = this.Content.XamlRoot,
            // ダイアログをウィンドウの現在テーマに合わせる（ライト/ダーク両対応）
            RequestedTheme = (this.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default
        };
        await dialog.ShowAsync();
    }

    // Aboutダイアログの中身を組み立てる（アプリ名・版・著作権・MIT・サードパーティ一覧）
    private static FrameworkElement BuildAboutContent()
    {
        // アプリ名・バージョンをパッケージ情報から動的取得（非パッケージ実行時はアセンブリ情報へフォールバック）
        string appName = "MDV";
        string version = "1.0.0.0";
        try
        {
            var pkg = Windows.ApplicationModel.Package.Current;
            appName = string.IsNullOrWhiteSpace(pkg.DisplayName) ? "MDV" : pkg.DisplayName;
            var v = pkg.Id.Version;
            version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        }
        catch
        {
            var asmVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (asmVersion != null) version = asmVersion.ToString();
        }

        // サードパーティ表記を Assets から読み込む（ビルド時にコピー済み。無ければメッセージを出す）
        string notices;
        try
        {
            notices = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "THIRD-PARTY-NOTICES.md"));
        }
        catch
        {
            notices = Localizer.Get("AboutThirdPartyLoadError");
        }

        var panel = new StackPanel { Spacing = 6 };

        panel.Children.Add(new TextBlock
        {
            Text = string.Format(Localizer.Get("AboutVersion"), appName, version),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 18
        });
        panel.Children.Add(new TextBlock
        {
            Text = Localizer.Get("AboutDescription"),
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8
        });
        panel.Children.Add(new TextBlock { Text = "Copyright (c) 2026 kajiyajp", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock
        {
            Text = Localizer.Get("AboutLicense"),
            TextWrapping = TextWrapping.Wrap
        });

        // GitHubリンク（クリックで既定ブラウザが開く。オフラインでも下のライセンス本文は読める）。
        // 注: WinUI3デスクトップでは HyperlinkButton.NavigateUri が自動でブラウザを開かないため、
        //     アプリ本体と同じ Launcher.LaunchUriAsync 経由(LaunchExternal)で明示的に開く。
        var githubLink = new HyperlinkButton { Content = Localizer.Get("AboutGitHubLink"), Padding = new Thickness(0) };
        githubLink.Click += (s, e) => LaunchExternal("https://github.com/kajiyajp/MDV");
        panel.Children.Add(githubLink);

        panel.Children.Add(new TextBlock
        {
            Text = Localizer.Get("AboutThirdPartyHeader"),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 15,
            Margin = new Thickness(0, 6, 0, 0)
        });

        // 通知本文は等幅フォントで表示。テーブルの整列を保つため折り返さず「横スクロールのみ」。
        // 縦スクロールは外側のScrollViewerに委ねる（内側の縦スクロールを無効化しネスト縦スクロールを回避）。
        var noticesText = new TextBlock
        {
            Text = notices,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.NoWrap,
            IsTextSelectionEnabled = true
        };
        var noticesScroll = new ScrollViewer
        {
            Content = noticesText,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollMode = ScrollMode.Disabled
        };
        // 枠線はテーマリソースを用い、両テーマで視認できるようにする（未定義時は灰色にフォールバック）
        Microsoft.UI.Xaml.Media.Brush strokeBrush =
            Application.Current.Resources.TryGetValue("CardStrokeColorDefaultBrush", out var b) && b is Microsoft.UI.Xaml.Media.Brush br
                ? br
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
        panel.Children.Add(new Border
        {
            Child = noticesScroll,
            BorderBrush = strokeBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8)
        });

        // ダイアログ全体を縦スクロール可能にし、画面が小さくても内容が溢れず
        // 「閉じる」ボタンが隠れないようにする（高さ・幅を上限で制約）。
        return new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 460,
            MaxWidth = 560
        };
    }

    // [ファイル]→[開く]: ファイルピッカーでMarkdownファイルを選択して開く
    private async void OnOpenClick(object s, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        // WinUI3ではHWND初期化が必要
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        foreach (var ext in new[] { ".md", ".markdown", ".mdown", ".mkd" })
            picker.FileTypeFilter.Add(ext);

        var file = await picker.PickSingleFileAsync();
        if (file != null) await OpenFileAsync(file.Path);
    }

    // ルートGridへのドラッグ中: コピー操作を許可する
    private void OnDragOver(object s, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    // ルートGridへのドロップ: 最初のファイルを開く
    private async void OnDrop(object s, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var file = items.OfType<StorageFile>().FirstOrDefault();
            if (file != null) await OpenFileAsync(file.Path);
        }
    }

    private void OnExitClick(object s, RoutedEventArgs e) { PersistSettings(); Application.Current.Exit(); }

    // テーマを切り替え、ウィンドウ・トグルアイコン・WebView2本文へ反映する
    private async void SetTheme(ThemeMode mode)
    {
        _currentTheme = mode;
        ThemeService.ApplyToWindow(this, mode);
        ThemeToggle.Content = ThemeService.ToggleIcon(mode);
        // WebView2側のCSSも切替
        if (_webReady)
            try
            {
                await ContentView.CoreWebView2.ExecuteScriptAsync(
                    $"window.__setTheme && window.__setTheme('{ThemeService.ToCssClass(mode)}')");
            }
            catch
            {
                // WebView2が利用不可などのスクリプト実行失敗は無視（テーマ/ズームの見た目のみの影響）
            }
    }

    private void OnThemeLight(object s, RoutedEventArgs e) { SetTheme(ThemeMode.Light); PersistSettings(); }
    private void OnThemeDark(object s, RoutedEventArgs e) { SetTheme(ThemeMode.Dark); PersistSettings(); }
    private void OnThemeSystem(object s, RoutedEventArgs e) { SetTheme(ThemeMode.System); PersistSettings(); }
    private void OnThemeToggle(object s, RoutedEventArgs e)
    {
        SetTheme(_currentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark);
        PersistSettings();
    }

    private void OnZoomIn(object s, RoutedEventArgs e)
    {
        if (_webReady)
        {
            ApplyZoom(Math.Min(3.0, _zoomFactor + 0.1));
            PersistSettings();
        }
    }

    private void OnZoomOut(object s, RoutedEventArgs e)
    {
        if (_webReady)
        {
            ApplyZoom(Math.Max(0.5, _zoomFactor - 0.1));
            PersistSettings();
        }
    }

    private void OnZoomReset(object s, RoutedEventArgs e)
    {
        if (_webReady)
        {
            ApplyZoom(1.0);
            PersistSettings();
        }
    }

    // [表示]→[再読み込み]: 現在開いているファイルを再読込する
    private async void OnReload(object s, RoutedEventArgs e)
    {
        if (_currentPath != null) await OpenFileAsync(_currentPath);
    }

    private void OnToggleStatusBar(object s, RoutedEventArgs e)
    {
        StatusBar.Visibility = StatusBar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        PersistSettings();
    }
}
