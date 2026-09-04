using Avalonia.Controls;
using Avalonia.Interactivity;
using WAVE.Domain.Networking;

namespace WAVE.App.Views;

/// <summary>Modal dialog to capture the credential of a protected network.</summary>
public partial class CredentialPromptWindow : Window
{
    private readonly WifiNetworkProfile _profile;

    public CredentialPromptWindow(WifiNetworkProfile profile)
    {
        InitializeComponent();
        _profile = profile;
        TitleText.Text = profile.DisplayName;
        EnterprisePanel.IsVisible = profile.IsEnterprise;
        Loaded += (_, _) => PassphraseInput.Focus();
    }

    /// <summary>The captured credential, or null while the user has not confirmed.</summary>
    public WifiSecret? Secret { get; private set; }

    private void OnSubmit(object? sender, RoutedEventArgs e)
    {
        var passphrase = PassphraseInput.Text;
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            ErrorText.Text = "Informe a senha da rede.";
            ErrorText.IsVisible = true;
            return;
        }

        var username = _profile.IsEnterprise && !string.IsNullOrWhiteSpace(UsernameInput.Text)
            ? UsernameInput.Text.Trim()
            : null;
        var domain = _profile.IsEnterprise && !string.IsNullOrWhiteSpace(DomainInput.Text)
            ? DomainInput.Text.Trim()
            : null;

        Secret = new WifiSecret(passphrase, username, domain);
        Close();
    }
}
