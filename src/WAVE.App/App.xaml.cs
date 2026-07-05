using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Bootstrap;
using WAVE.App.Services;
using WAVE.App.Views;

namespace WAVE.App;

/// <summary>
/// Ponto de composição (Composition Root). Monta o contêiner de DI e, após o
/// login, exibe a janela principal.
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _provider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Evita que o app encerre quando a janela de login (única aberta) fecha
        // antes de a janela principal aparecer.
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
