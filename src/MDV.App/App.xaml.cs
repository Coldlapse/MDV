using System;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppLifecycle;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MDV_App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();

        string? path = null;
        // ファイル関連付け起動（"AppInstance" は Windows.ApplicationModel にも存在するため完全修飾する）
        var activated = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
        if (activated.Kind == ExtendedActivationKind.File &&
            activated.Data is IFileActivatedEventArgs fileArgs)
        {
            path = fileArgs.Files.OfType<StorageFile>().FirstOrDefault()?.Path;
        }
        else
        {
            // コマンドライン引数（exe直起動/右クリック「プログラムから開く」）
            var cmd = Environment.GetCommandLineArgs();
            if (cmd.Length > 1 && File.Exists(cmd[1])) path = cmd[1];
        }

        _ = ((MainWindow)_window).OpenInitialAsync(path);
    }
}
