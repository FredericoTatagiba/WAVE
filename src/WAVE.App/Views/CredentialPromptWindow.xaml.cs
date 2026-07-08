using System.Windows;
using WAVE.Domain.Networking;

namespace WAVE.App.Views;

/// <summary>Diálogo modal para captura da credencial de uma rede protegida.</summary>
public partial class CredentialPromptWindow : Window
{
    private readonly WifiNetworkProfile _profile;

    public CredentialPromptWindow(WifiNetworkProfile profile)
    {
        InitializeComponent();
        _profile = profile;
        TitleText.Text = profile.DisplayName;
        EnterprisePanel.Visibility = profile.IsEnterprise ? Visibility.Visible : Visibility.Collapsed;
        Loaded += (_, _) => PassphraseInput.Focus();
    }

    public WifiSecret? Secret { get; private set; }

    private void OnSubmit(object sender, RoutedEventArgs e)
    {
        var passphrase = PassphraseInput.Password;
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            ErrorText.Text = "Informe a senha da rede.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        var username = _profile.IsEnterprise && !string.IsNullOrWhiteSpace(UsernameInput.Text)
            ? UsernameInput.Text.Trim()
            : null;
        var domain = _profile.IsEnterprise && !string.IsNullOrWhiteSpace(DomainInput.Text)
            ? DomainInput.Text.Trim()
            : null;

        Secret = new WifiSecret(passphrase, username, domain);
        DialogResult = true;
        Close();
    }
}
