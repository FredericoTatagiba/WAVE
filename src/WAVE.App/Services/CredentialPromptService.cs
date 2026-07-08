using System.Windows;
using WAVE.App.Views;
using WAVE.Domain.Networking;

namespace WAVE.App.Services;

/// <summary>Implementa <see cref="ICredentialPrompt"/> exibindo <see cref="CredentialPromptWindow"/>.</summary>
public sealed class CredentialPromptService : ICredentialPrompt
{
    public WifiSecret? Request(WifiNetworkProfile profile)
    {
        var window = new CredentialPromptWindow(profile)
        {
            Owner = System.Windows.Application.Current?.MainWindow,
        };

        return window.ShowDialog() == true ? window.Secret : null;
    }
}
