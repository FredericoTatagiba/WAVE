using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Views;

namespace WAVE.App.Services;

/// <summary>
/// Coordena a abertura de janelas que dependem de DI (login e gestão de usuários),
/// evitando que as Views construam dependências manualmente.
/// </summary>
public sealed class AppNavigator
{
    private readonly IServiceProvider _provider;

    public AppNavigator(IServiceProvider provider) => _provider = provider;

    /// <summary>Exibe o login (modal). Retorna true se autenticado.</summary>
    public bool Authenticate(Window? owner = null)
    {
        var window = _provider.GetRequiredService<LoginWindow>();
        if (owner is not null)
        {
            window.Owner = owner;
        }

        return window.ShowDialog() == true;
    }

    /// <summary>Exibe a gestão de usuários (modal, apenas Administrador).</summary>
    public void ShowUserManagement(Window owner)
    {
        var window = _provider.GetRequiredService<UserManagementWindow>();
        window.Owner = owner;
        window.ShowDialog();
    }
}
