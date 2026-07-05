using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Bootstrap;
using WAVE.App.Views;

namespace WAVE.App;

/// <summary>
/// Ponto de composição (Composition Root). Monta o contêiner de DI e exibe a
/// janela principal. Nenhuma outra parte do app constrói dependências manualmente.
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? _provider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ServiceRegistration.AddWave(services);
        _provider = services.BuildServiceProvider();

        _provider.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _provider?.Dispose();
        base.OnExit(e);
    }
}
