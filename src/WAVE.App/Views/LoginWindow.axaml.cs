using Avalonia.Controls;
using Avalonia.Interactivity;
using WAVE.App.ViewModels;

namespace WAVE.App.Views;

/// <summary>Login / first-run window.</summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            await _viewModel.InitializeAsync();
            UsernameInput.Focus();
        };
    }

    /// <summary>
    /// Whether the user authenticated. Read by <see cref="Services.AppNavigator"/> after
    /// the window closes: the login is shown without an owner at startup, so there is no
    /// ShowDialog result to carry it.
    /// </summary>
    public bool Authenticated { get; private set; }

    private async void OnSubmit(object? sender, RoutedEventArgs e)
    {
        Authenticated = await _viewModel.SubmitAsync(PasswordInput.Text ?? string.Empty, ConfirmInput.Text ?? string.Empty);
        if (Authenticated)
        {
            Close();
        }
    }
}
