using System.Windows;
using WAVE.App.Services;
using WAVE.App.ViewModels;

namespace WAVE.App.Views;

/// <summary>Janela principal. Conecta a ViewModel e coordena login/logout e gestão de usuários.</summary>
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
        // Fecha (oculta) a página anterior, faz login e recarrega o estado do novo
        // usuário. Se cancelado, o navegador encerra o app.
        if (_navigator.Logout(this))
        {
            await _viewModel.InitializeAsync();
        }
    }
}
