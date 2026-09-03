using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Views;

namespace WAVE.App.Services;

/// <summary>
/// Coordinates opening windows that depend on DI (login and user management),
/// preventing the Views from constructing dependencies manually.
/// </summary>
/// <remarks>
/// Every method is asynchronous because Avalonia's <c>ShowDialog</c> is awaitable rather
/// than blocking. The shutdown-mode juggling is unchanged in intent, but lives on the
/// desktop lifetime instead of on Application.
/// </remarks>
public sealed class AppNavigator
{
    private readonly IServiceProvider _provider;

    public AppNavigator(IServiceProvider provider) => _provider = provider;

    /// <summary>Shows the login (modal). Returns true if authenticated.</summary>
    public async Task<bool> AuthenticateAsync(Window? owner = null)
    {
        var window = _provider.GetRequiredService<LoginWindow>();

        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            // Startup: there is no window to own this one yet.
            await window.ShowStandaloneAsync();
        }

        return window.Authenticated;
    }

    /// <summary>
    /// Ends the current session: hides the given window (so the previous page is not
    /// visible), shows the login and, if authenticated, re-shows the window for the new
    /// user. If the login is cancelled, exits the app. Returns true when a new user
    /// authenticated — the window should then reload its state.
    /// </summary>
    public async Task<bool> LogoutAsync(Window current)
    {
        ArgumentNullException.ThrowIfNull(current);

        var lifetime = AppWindows.Lifetime;
        if (lifetime is null)
        {
            return false;
        }

        var previousMode = lifetime.ShutdownMode;

        // Between hiding the window and the new login there is no visible window: without
        // this the app would exit (OnLastWindowClose) when the login closes.
        lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        current.Hide();

        if (!await AuthenticateAsync())
        {
            lifetime.Shutdown();
            return false;
        }

        lifetime.ShutdownMode = previousMode;
        current.Show();
        return true;
    }

    /// <summary>Shows user management (modal, Administrator only).</summary>
    public async Task ShowUserManagementAsync(Window owner)
    {
        var window = _provider.GetRequiredService<UserManagementWindow>();
        await window.ShowDialog(owner);
    }
}
