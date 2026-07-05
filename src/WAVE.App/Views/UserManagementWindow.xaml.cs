using System.Windows;
using WAVE.App.ViewModels;
using WAVE.Domain.Security;

namespace WAVE.App.Views;

/// <summary>Janela de gestão de usuários. Lê os campos e delega à ViewModel.</summary>
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

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        var role = NewRole.SelectedItem is UserRole selected ? selected : UserRole.Operator;
        var added = await _viewModel.AddAsync(NewUsername.Text, NewDisplayName.Text, role, PasswordInput.Password);
        if (added)
        {
            NewUsername.Clear();
            NewDisplayName.Clear();
            PasswordInput.Clear();
        }
    }

    private async void OnResetClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: UserRowViewModel row })
        {
            var done = await _viewModel.ResetPasswordAsync(row.Account.Id, PasswordInput.Password);
            if (done)
            {
                PasswordInput.Clear();
            }
        }
    }
}
