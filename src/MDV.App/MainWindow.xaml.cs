using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
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

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
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

    private void OnExitClick(object s, RoutedEventArgs e) { Application.Current.Exit(); }
    private void OnThemeLight(object s, RoutedEventArgs e) { }
    private void OnThemeDark(object s, RoutedEventArgs e) { }
    private void OnThemeSystem(object s, RoutedEventArgs e) { }
    private void OnThemeToggle(object s, RoutedEventArgs e) { }
    private void OnZoomIn(object s, RoutedEventArgs e) { }
    private void OnZoomOut(object s, RoutedEventArgs e) { }
    private void OnZoomReset(object s, RoutedEventArgs e) { }

    // [表示]→[再読み込み]: 現在開いているファイルを再読込する
    private async void OnReload(object s, RoutedEventArgs e)
    {
        if (_currentPath != null) await OpenFileAsync(_currentPath);
    }

    private void OnToggleStatusBar(object s, RoutedEventArgs e) { }
}
