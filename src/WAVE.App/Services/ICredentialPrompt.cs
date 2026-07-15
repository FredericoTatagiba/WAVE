using WAVE.Domain.Networking;

namespace WAVE.App.Services;

/// <summary>Asks the user for the credential of a protected network (modal dialog).</summary>
public interface ICredentialPrompt
{
    /// <summary>Returns the entered credential, or null if the user cancels.</summary>
    WifiSecret? Request(WifiNetworkProfile profile);
}
