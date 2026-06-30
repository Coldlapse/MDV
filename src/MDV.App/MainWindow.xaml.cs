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
        await ContentView.EnsureCoreWebView2Async();

        var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        // http://vendor/... を Assets/vendor フォルダにマッピング
        ContentView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "vendor", Path.Combine(assetsDir, "vendor"),
            CoreWebView2HostResourceAccessKind.Allow);

        _template = File.ReadAllText(Path.Combine(assetsDir, "template.html"));
        _webReady = true;

        LoadSettings();
    }

    // 文字サイズ(ズーム)をWebView2へ適用する
    // WinUI3のWebView2 XAMLコントロールはCoreWebView2Controller(ZoomFactor)を公開していないため、
    // CSS(zoom)をJS経由で適用する（template.html側のwindow.__setZoomフック）
    private async void ApplyZoom(double factor)
    {
        _zoomFactor = factor;
        if (_webReady)
            await ContentView.CoreWebView2.ExecuteScriptAsync(
                $"window.__setZoom && window.__setZoom('{_zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)}')");
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
            LineEndingText.Text = LineEndingDetector.ToDisplay(doc.LineEnding);
            this.Title = $"MDV — {Path.GetFileName(path)}";
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
            Title = "ファイルを開けません",
            Content = ex is FileNotFoundException
                ? "指定されたファイルが見つかりませんでした。"
                : $"ファイルの読み込み中にエラーが発生しました。\n{ex.Message}",
            CloseButtonText = "閉じる",
            XamlRoot = this.Content.XamlRoot
        };
        await dialog.ShowAsync();
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
            await ContentView.CoreWebView2.ExecuteScriptAsync(
                $"window.__setTheme && window.__setTheme('{ThemeService.ToCssClass(mode)}')");
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
