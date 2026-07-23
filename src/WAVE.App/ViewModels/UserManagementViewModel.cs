using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WAVE.App.Services;
using WAVE.Application.Users;
using WAVE.Domain.Security;

namespace WAVE.App.ViewModels;

/// <summary>User management (Administrator): list, create, change role, reset password and delete.</summary>
public sealed class UserManagementViewModel : ObservableObject
{
    private readonly UserManagementService _service;
    private readonly IUserAlerts _alerts;

    public UserManagementViewModel(UserManagementService service, IUserAlerts alerts)
    {
        _service = service;
        _alerts = alerts;
    }

    public ObservableCollection<UserRowViewModel> Users { get; } = new();

    public Array Roles { get; } = Enum.GetValues(typeof(UserRole));

    public async Task InitializeAsync() => await ReloadAsync();

    public async Task<bool> AddAsync(string username, string displayName, UserRole role, string password)
    {
        var result = await _service.CreateAsync(username, displayName, role, password);
        if (result.IsFailure)
        {
            _alerts.Error(result.Error);
            return false;
        }

        await ReloadAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword)
    {
        var result = await _service.ResetPasswordAsync(userId, newPassword);
        if (result.IsFailure)
        {
            _alerts.Error(result.Error);
            return false;
        }

        _alerts.Info("Senha redefinida.");
        return true;
    }

    private async Task ToggleRoleAsync(UserRowViewModel row)
    {
        var newRole = row.Account.IsAdministrator ? UserRole.Operator : UserRole.Administrator;
        var result = await _service.ChangeRoleAsync(row.Account.Id, newRole);
        if (result.IsFailure)
        {
            _alerts.Error(result.Error);
            return;
        }

        await ReloadAsync();
    }

    private async Task DeleteAsync(UserRowViewModel row)
    {
        var result = await _service.DeleteAsync(row.Account.Id);
        if (result.IsFailure)
        {
            _alerts.Error(result.Error);
            return;
        }

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var result = await _service.GetAllAsync();
        Users.Clear();

        if (result.IsFailure)
        {
            _alerts.Error(result.Error);
            return;
        }

        foreach (var user in result.Value)
        {
            Users.Add(new UserRowViewModel(user, ToggleRoleAsync, DeleteAsync));
        }
    }
}
