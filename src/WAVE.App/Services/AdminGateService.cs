using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Views;
using WAVE.Application.Abstractions;

namespace WAVE.App.Services;

/// <summary>Shows <see cref="AdminPasswordWindow"/> when the session is still locked.</summary>
public sealed class AdminGateService : IAdminGate
{
    private readonly IServiceProvider _provider;
    private readonly IAdminSession _session;

    public AdminGateService(IServiceProvider provider, IAdminSession session)
    {
        _provider = provider;
        _session = session;
    }

    public async Task<bool> EnsureUnlockedAsync()
    {
        if (_session.IsUnlocked)
        {
            return true;
        }

        var owner = AppWindows.Owner;
        if (owner is null)
        {
            return false;
        }

        var window = _provider.GetRequiredService<AdminPasswordWindow>();
        await window.ShowDialog(owner);
        return window.Unlocked;
    }
}
