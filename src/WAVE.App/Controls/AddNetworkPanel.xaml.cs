using System.Windows;
using System.Windows.Controls;
using WAVE.App.ViewModels;
using WAVE.Domain.Networking;

namespace WAVE.App.Controls;

/// <summary>
/// Componente de cadastro de rede (visível apenas ao Administrador). Tem seu
/// próprio namescope, evitando conflito de nomes ao ser hospedado em outros
/// componentes. Usa a <see cref="MainViewModel"/> do DataContext herdado.
/// </summary>
public partial class AddNetworkPanel : UserControl
{
    public AddNetworkPanel() => InitializeComponent();

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var security = SecurityBox.SelectedItem is SecurityType selected ? selected : SecurityType.Open;
        await viewModel.AddNetworkAsync(DisplayNameBox.Text, SsidBox.Text, security, PasswordInput.Password);
        PasswordInput.Clear();
    }
}
