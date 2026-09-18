using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace SagarIsland;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            File.WriteAllText("crash.log", args.ExceptionObject.ToString());
        };

        DispatcherUnhandledException += (s, args) =>
        {
            File.WriteAllText("crash.log", args.Exception.ToString());
            args.Handled = true;
        };
        if (e.Args.Length > 0 && e.Args[0] == "--generate-icons")
        {
            string projectAssets = Path.GetFullPath("Assets");
            IconGenerator.GenerateAll(projectAssets);
            Shutdown();
            return;
        }
    }
}

