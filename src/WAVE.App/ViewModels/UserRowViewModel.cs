using CommunityToolkit.Mvvm.Input;
using WAVE.Domain.Security;

namespace WAVE.App.ViewModels;

/// <summary>Row in the user list, with role and deletion actions.</summary>
public sealed class UserRowViewModel
{
    public UserRowViewModel(
        UserAccount account,
        Func<UserRowViewModel, Task> onToggleRole,
        Func<UserRowViewModel, Task> onDelete)
    {
        Account = account;
        ToggleRoleCommand = new AsyncRelayCommand(() => onToggleRole(this));
        DeleteCommand = new AsyncRelayCommand(() => onDelete(this));
    }

    public UserAccount Account { get; }

    public string Username => Account.Username;

    public string DisplayName => Account.DisplayName;

    public string RoleText => Account.IsAdministrator ? "Administrador" : "Operador";

    public string ToggleRoleText => Account.IsAdministrator ? "Tornar Operador" : "Tornar Admin";

    public IAsyncRelayCommand ToggleRoleCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }
}
