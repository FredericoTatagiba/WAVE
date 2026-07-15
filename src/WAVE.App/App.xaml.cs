using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Bootstrap;
using WAVE.App.Services;
using WAVE.App.Views;

namespace WAVE.App;

/// <summary>
/// Composition Root. Builds the DI container and, after login, shows the main window.
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _provider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Prevents the app from exiting when the login window (the only one open) closes
        // before the main window appears.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var services = new ServiceCollection();
        ServiceRegistration.AddWave(services);
        _provider = services.BuildServiceProvider();

        var navigator = _provider.GetRequiredService<AppNavigator>();
        if (!navigator.Authenticate())
        {
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnLastWindowClose;
        _provider.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _provider?.Dispose();
        base.OnExit(e);
    }
}
