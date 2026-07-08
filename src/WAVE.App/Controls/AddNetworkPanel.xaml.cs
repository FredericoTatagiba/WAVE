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
    public AddNetworkPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Começa em "Open" (índice 0) para os campos condicionais terem um estado
        // consistente desde o início, em vez de o combo aparecer vazio.
        if (SecurityBox.SelectedItem is null && SecurityBox.Items.Count > 0)
        {
            SecurityBox.SelectedItem = SecurityType.Open;
        }
    }

    private SecurityType SelectedSecurity =>
        SecurityBox.SelectedItem is SecurityType selected ? selected : SecurityType.Open;

    /// <summary>
    /// Mostra apenas os campos que o tipo de segurança escolhido exige: senha para
    /// redes protegidas; usuário/domínio somente para redes Enterprise (802.1X).
    /// </summary>
    private void OnSecurityChanged(object sender, SelectionChangedEventArgs e)
    {
        var security = SelectedSecurity;
        var requiresCredential = security.RequiresCredential();
        var isEnterprise = security.IsEnterprise();

        PasswordField.Visibility = requiresCredential ? Visibility.Visible : Visibility.Collapsed;
        UsernameField.Visibility = isEnterprise ? Visibility.Visible : Visibility.Collapsed;
        DomainField.Visibility = isEnterprise ? Visibility.Visible : Visibility.Collapsed;

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

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        await viewModel.AddNetworkAsync(
            DisplayNameBox.Text, SsidBox.Text, SelectedSecurity, PasswordInput.Password, UsernameBox.Text, DomainBox.Text);
        PasswordInput.Clear();
        UsernameBox.Clear();
        DomainBox.Clear();
    }
}
