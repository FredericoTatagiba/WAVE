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

    /// <summary>
    /// Encerra a sessão atual: oculta a janela informada (para a página anterior não
    /// ficar visível), exibe o login e, se autenticado, reexibe a janela para o novo
    /// usuário. Se o login for cancelado, encerra o app. Retorna true quando um novo
    /// usuário autenticou — a janela deve então recarregar seu estado.
    /// </summary>
    public bool Logout(Window current)
    {
        ArgumentNullException.ThrowIfNull(current);

        var app = System.Windows.Application.Current;
        var previousMode = app.ShutdownMode;

        // Entre ocultar a janela e o novo login não há janela visível: sem isto o app
        // encerraria (OnLastWindowClose) ao fechar o login.
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

    /// <summary>Exibe a gestão de usuários (modal, apenas Administrador).</summary>
    public void ShowUserManagement(Window owner)
    {
        var window = _provider.GetRequiredService<UserManagementWindow>();
        window.Owner = owner;
        window.ShowDialog();
    }
}
