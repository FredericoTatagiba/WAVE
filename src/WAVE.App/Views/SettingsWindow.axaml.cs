using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WAVE.App.ViewModels;

namespace WAVE.App.Views;

/// <summary>Device settings window (administrator only).</summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnBrowseHistory(object? sender, RoutedEventArgs e) =>
        _viewModel.HistoryDirectory = await PickFolderAsync("Pasta do histórico") ?? _viewModel.HistoryDirectory;

    private async void OnBrowseLogs(object? sender, RoutedEventArgs e) =>
        _viewModel.LogsDirectory = await PickFolderAsync("Pasta dos logs") ?? _viewModel.LogsDirectory;

    private async void OnChangePassword(object? sender, RoutedEventArgs e)
    {
        await _viewModel.ChangePasswordAsync(
            CurrentPasswordBox.Text ?? string.Empty,
            NewPasswordBox.Text ?? string.Empty,
            ConfirmPasswordBox.Text ?? string.Empty);

        CurrentPasswordBox.Clear();
        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();
    }

    /// <summary>
    /// Returns the picked folder's local path, or null when the operator cancels or the
    /// chosen location has no filesystem path (a virtual folder the storage provider can
    /// enumerate but nothing can write a file into).
    /// </summary>
    private async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title, AllowMultiple = false });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
