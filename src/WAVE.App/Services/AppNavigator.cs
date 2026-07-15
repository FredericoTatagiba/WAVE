using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Views;

namespace WAVE.App.Services;

/// <summary>
/// Coordinates opening windows that depend on DI (login and user management),
/// preventing the Views from constructing dependencies manually.
/// </summary>
public sealed class AppNavigator
{
    private readonly IServiceProvider _provider;

    public AppNavigator(IServiceProvider provider) => _provider = provider;

    /// <summary>Shows the login (modal). Returns true if authenticated.</summary>
    public bool Authenticate(Window? owner = null)
    {
        var window = _provider.GetRequiredService<LoginWindow>();
        if (owner is not null)
        {
            window.Owner = owner;
        }

        return window.ShowDialog() == true;
    }

    /// <summary>
    /// Ends the current session: hides the given window (so the previous page is not
    /// visible), shows the login and, if authenticated, re-shows the window for the new
    /// user. If the login is cancelled, exits the app. Returns true when a new user
    /// authenticated — the window should then reload its state.
    /// </summary>
    public bool Logout(Window current)
    {
        ArgumentNullException.ThrowIfNull(current);

        var app = System.Windows.Application.Current;
        var previousMode = app.ShutdownMode;

        // Between hiding the window and the new login there is no visible window: without
        // this the app would exit (OnLastWindowClose) when the login closes.
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        current.Hide();

        if (!Authenticate())
        {
            app.Shutdown();
            return false;
        }

        app.ShutdownMode = previousMode;
        current.Show();
        return true;
    }

    /// <summary>Shows user management (modal, Administrator only).</summary>
    public void ShowUserManagement(Window owner)
    {
        var window = _provider.GetRequiredService<UserManagementWindow>();
        window.Owner = owner;
        window.ShowDialog();
    }
}
