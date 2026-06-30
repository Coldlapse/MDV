using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MDV_App;

/// <summary>
/// The application window。メニューバー・WebView2本文・ステータスバーの骨組み。
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
    }

    // 各ハンドラはTask 9以降で中身を実装する。まずビルドを通す。
    private void OnOpenClick(object s, RoutedEventArgs e) { }
    private void OnExitClick(object s, RoutedEventArgs e) { Application.Current.Exit(); }
    private void OnThemeLight(object s, RoutedEventArgs e) { }
    private void OnThemeDark(object s, RoutedEventArgs e) { }
    private void OnThemeSystem(object s, RoutedEventArgs e) { }
    private void OnThemeToggle(object s, RoutedEventArgs e) { }
    private void OnZoomIn(object s, RoutedEventArgs e) { }
    private void OnZoomOut(object s, RoutedEventArgs e) { }
    private void OnZoomReset(object s, RoutedEventArgs e) { }
    private void OnReload(object s, RoutedEventArgs e) { }
    private void OnToggleStatusBar(object s, RoutedEventArgs e) { }
}
