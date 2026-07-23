using System.Windows;
using WAVE.App.Services;
using WAVE.App.ViewModels;

namespace WAVE.App.Views;

/// <summary>Main window. Wires up the ViewModel and coordinates login/logout and user management.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly AppNavigator _navigator;

    public MainWindow(MainViewModel viewModel, AppNavigator navigator)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigator = navigator;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private void OnUsersClick(object sender, RoutedEventArgs e) => _navigator.ShowUserManagement(this);

    private async void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        // Closes (hides) the previous page, signs in and reloads the new user's
        // state. If cancelled, the navigator shuts the app down.
        if (_navigator.Logout(this))
        {
            await _viewModel.InitializeAsync();
        }
    }
}
