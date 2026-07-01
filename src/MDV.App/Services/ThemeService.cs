using Microsoft.UI.Xaml;
using MDV.Core.Models;

namespace MDV.App.Services;

/// <summary>ウィンドウ全体のテーマ適用（WebView2側はMainWindowがJSで切替）</summary>
public static class ThemeService
{
    public static void ApplyToWindow(Window window, ThemeMode mode)
    {
        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = mode switch
            {
                ThemeMode.Light => ElementTheme.Light,
                ThemeMode.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

    public static string ToCssClass(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => "theme-light",
        ThemeMode.Dark => "theme-dark",
        _ => "theme-system"
    };

    public static string ToggleIcon(ThemeMode mode) => mode == ThemeMode.Dark ? "☀️" : "🌙";
}
