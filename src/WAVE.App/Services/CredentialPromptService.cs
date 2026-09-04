using WAVE.App.Views;
using WAVE.Domain.Networking;

namespace WAVE.App.Services;

/// <summary>Implements <see cref="ICredentialPrompt"/> by showing a <see cref="CredentialPromptWindow"/>.</summary>
public sealed class CredentialPromptService : ICredentialPrompt
{
    public async Task<WifiSecret?> RequestAsync(WifiNetworkProfile profile)
    {
        var window = new CredentialPromptWindow(profile);
        var owner = AppWindows.Owner;

        // ShowDialog requires an owner in Avalonia; without a main window there is nothing
        // to be modal to, so the dialog is shown standalone instead.
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            await window.ShowStandaloneAsync();
        }

        // Null unless the user confirmed: the window clears it on cancel.
        return window.Secret;
    }
}
