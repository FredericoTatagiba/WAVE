using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using WAVE.App.ViewModels;
using WAVE.Domain.Security;

namespace WAVE.App.Views;

/// <summary>User management window. Reads the fields and delegates to the ViewModel.</summary>
public partial class UserManagementWindow : Window
{
    private readonly UserManagementViewModel _viewModel;

    public UserManagementWindow(UserManagementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        var role = NewRole.SelectedItem is UserRole selected ? selected : UserRole.Operator;
        var added = await _viewModel.AddAsync(
            NewUsername.Text ?? string.Empty,
            NewDisplayName.Text ?? string.Empty,
            role,
            PasswordInput.Text ?? string.Empty);

        if (added)
        {
            NewUsername.Clear();
            NewDisplayName.Clear();
            PasswordInput.Clear();
        }
    }

    private async void OnResetClick(object? sender, RoutedEventArgs e)
    {
        // Avalonia's element base type is StyledElement, not FrameworkElement.
        if (sender is StyledElement { DataContext: UserRowViewModel row })
        {
            var done = await _viewModel.ResetPasswordAsync(row.Account.Id, PasswordInput.Text ?? string.Empty);
            if (done)
            {
                PasswordInput.Clear();
            }
        }
    }
}
