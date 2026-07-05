using System.Windows;
using WAVE.App.ViewModels;

namespace WAVE.App.Views;

/// <summary>Janela de login / primeiro acesso. Lê os PasswordBox (não vinculáveis).</summary>
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

    private async void OnSubmit(object sender, RoutedEventArgs e)
    {
        var authenticated = await _viewModel.SubmitAsync(PasswordInput.Password, ConfirmInput.Password);
        if (authenticated)
        {
            DialogResult = true;
            Close();
        }
    }
}
