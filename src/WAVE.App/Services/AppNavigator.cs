using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Views;

namespace WAVE.App.Services;

/// <summary>
/// Opens windows that depend on DI, so the Views never construct their own dependencies.
/// </summary>
public sealed class AppNavigator
{
    private readonly IServiceProvider _provider;

    public AppNavigator(IServiceProvider provider) => _provider = provider;

    /// <summary>Shows the settings window (modal). The caller unlocks the session first.</summary>
    public async Task ShowSettingsAsync(Window owner)
    {
        var window = _provider.GetRequiredService<SettingsWindow>();
        await window.ShowDialog(owner);
    }
}
