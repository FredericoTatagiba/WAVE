using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Builds the Windows WLAN profile XML from a network profile.</summary>
public interface IWifiProfileXmlFactory
{
    string Build(WifiNetworkProfile profile, WifiSecret? secret);

    /// <summary>
    /// Builds the EAP user-credentials XML for Enterprise (802.1X) networks,
    /// applied to the profile after creation. Returns null when the network is not
    /// Enterprise or there is no credential to apply.
    /// </summary>
    string? BuildEapUserData(WifiNetworkProfile profile, WifiSecret? secret);
}
