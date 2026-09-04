using Avalonia.Controls;
using Avalonia.Interactivity;
using WAVE.App.ViewModels;
using WAVE.Domain.Networking;

namespace WAVE.App.Controls;

/// <summary>
/// Network registration component (visible only to the Administrator). It has its
/// own namescope, avoiding name conflicts when hosted inside other components.
/// Uses the <see cref="MainViewModel"/> from the inherited DataContext.
/// </summary>
public partial class AddNetworkPanel : UserControl
{
    public AddNetworkPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Starts at "Open" (index 0) so the conditional fields have a consistent state
        // from the start, instead of the combo appearing empty.
        if (SecurityBox.SelectedItem is null && SecurityBox.ItemCount > 0)
        {
            SecurityBox.SelectedItem = SecurityType.Open;
        }
    }

    private SecurityType SelectedSecurity =>
        SecurityBox.SelectedItem is SecurityType selected ? selected : SecurityType.Open;

    /// <summary>
    /// Shows only the fields the chosen security type requires: password for protected
    /// networks; username/domain only for Enterprise (802.1X) networks.
    /// </summary>
    private void OnSecurityChanged(object? sender, SelectionChangedEventArgs e)
    {
        var security = SelectedSecurity;
        var requiresCredential = security.RequiresCredential();
        var isEnterprise = security.IsEnterprise();

        PasswordField.IsVisible = requiresCredential;
        UsernameField.IsVisible = isEnterprise;
        DomainField.IsVisible = isEnterprise;

        if (!requiresCredential)
        {
            PasswordInput.Clear();
        }

        if (!isEnterprise)
        {
            UsernameBox.Clear();
            DomainBox.Clear();
        }
    }

    private async void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        await viewModel.AddNetworkAsync(
            DisplayNameBox.Text ?? string.Empty,
            SsidBox.Text ?? string.Empty,
            SelectedSecurity,
            PasswordInput.Text ?? string.Empty,
            UsernameBox.Text,
            DomainBox.Text);

        PasswordInput.Clear();
        UsernameBox.Clear();
        DomainBox.Clear();
    }
}
