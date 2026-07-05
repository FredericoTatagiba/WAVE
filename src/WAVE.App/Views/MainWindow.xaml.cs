using System.Windows;
using WAVE.App.ViewModels;

namespace WAVE.App.Views;

/// <summary>
/// Janela principal. Apenas conecta a ViewModel e trata a senha administrativa
/// (o <see cref="System.Windows.Controls.PasswordBox"/> não é vinculável por segurança).
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private void OnElevateClick(object sender, RoutedEventArgs e)
    {
        _viewModel.Elevate(AdminPasswordBox.Password);
        AdminPasswordBox.Clear();
    }
}
