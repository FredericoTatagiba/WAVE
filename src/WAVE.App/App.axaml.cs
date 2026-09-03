using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Bootstrap;
using WAVE.App.Services;
using WAVE.App.Views;

namespace WAVE.App;

/// <summary>
/// Composition Root. Builds the DI container and, after login, shows the main window.
/// </summary>
/// <remarks>
/// The base type is spelled out: inside the <c>WAVE</c> namespace an unqualified
/// <c>Application</c> binds to the <c>WAVE.Application</c> namespace, not to Avalonia's type.
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

            // Prevents the app from exiting when the login window (the only one open)
            // closes before the main window appears.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) => _provider.Dispose();

            // Avalonia's ShowDialog is awaitable and must not block the UI thread, so the
            // login runs as a continuation rather than inline as it did under WPF.
            _ = StartAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task StartAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var provider = _provider!;
        var navigator = provider.GetRequiredService<AppNavigator>();

        if (!await navigator.AuthenticateAsync())
        {
            desktop.Shutdown();
            return;
        }

        desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

        var main = provider.GetRequiredService<MainWindow>();
        desktop.MainWindow = main;
        main.Show();
    }
}
