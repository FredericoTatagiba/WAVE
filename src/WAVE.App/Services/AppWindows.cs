using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace WAVE.App.Services;

/// <summary>
/// Access to the desktop lifetime. Avalonia keeps the window list and the shutdown mode on
/// the lifetime rather than on Application, so the WPF habit of reaching for
/// <c>Avalonia.Application.Current.MainWindow</c> has a single replacement here.
/// </summary>
internal static class AppWindows
{
    public static IClassicDesktopStyleApplicationLifetime? Lifetime =>
        Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    /// <summary>
    /// The window a dialog should be owned by, or null during startup when the main
    /// window does not exist yet.
    /// </summary>
    public static Window? Owner => Lifetime?.MainWindow;
}
