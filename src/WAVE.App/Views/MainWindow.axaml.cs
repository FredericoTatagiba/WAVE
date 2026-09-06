using Avalonia.Controls;
using Avalonia.Interactivity;
using WAVE.App.Services;
using WAVE.App.ViewModels;

namespace WAVE.App.Views;

/// <summary>Main window. Wires up the ViewModel and opens the settings.</summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly AppNavigator _navigator;
    private readonly IAdminGate _adminGate;

    public MainWindow(MainViewModel viewModel, AppNavigator navigator, IAdminGate adminGate)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _navigator = navigator;
        _adminGate = adminGate;
        DataContext = viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (await _adminGate.EnsureUnlockedAsync())
        {
            await _navigator.ShowSettingsAsync(this);
        }
    }
}
