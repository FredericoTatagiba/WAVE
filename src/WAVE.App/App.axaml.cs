using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Bootstrap;
using WAVE.App.Views;

namespace WAVE.App;

/// <summary>
/// Composition Root. Builds the DI container and shows the main window.
/// </summary>
/// <remarks>
/// The base type is spelled out: inside the <c>WAVE</c> namespace an unqualified
/// <c>Application</c> binds to the <c>WAVE.Application</c> namespace, not to Avalonia's type.
/// <para>
/// There is no sign-in step. Running tests and reading history need no identity, so the
/// app opens straight into the network list; the administrator password is asked only when
/// someone reaches for an administrator action.
/// </para>
/// </remarks>
public partial class App : Avalonia.Application
{
    private ServiceProvider? _provider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            ServiceRegistration.AddWave(services);
            _provider = services.BuildServiceProvider();

            desktop.Exit += (_, _) => _provider.Dispose();
            desktop.MainWindow = _provider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
