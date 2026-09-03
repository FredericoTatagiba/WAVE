using WAVE.Domain.Networking;

namespace WAVE.Application.Networking;

/// <summary>
/// Builds the <c>nmcli</c> argument vectors. The Linux counterpart to
/// <see cref="WlanProfileXmlBuilder"/>: NetworkManager has no profile-XML concept, so a
/// <see cref="WifiNetworkProfile"/> maps straight onto connection properties.
/// </summary>
/// <remarks>
/// Every element returned is one argument, passed through
/// <c>ProcessStartInfo.ArgumentList</c>. Nothing here is quoted or escaped, and nothing
/// should be: an SSID containing a space, a quote or a <c>$</c> is delivered intact
/// precisely because it never passes through a shell.
/// </remarks>
public static class NmcliCommands
{
    /// <summary>Scans for visible networks, forcing a fresh sweep rather than the cache.</summary>
    public static string[] Scan() =>
        ["-t", "-f", "SSID,SECURITY,SIGNAL", "device", "wifi", "list", "--rescan", "yes"];

    /// <summary>Lists saved connections with their type.</summary>
    public static string[] ListConnections() => ["-t", "-f", "NAME,TYPE", "connection", "show"];

    /// <summary>Lists devices with their type, to locate the Wi-Fi interface.</summary>
    public static string[] ListDevices() => ["-t", "-f", "DEVICE,TYPE", "device", "status"];

    /// <summary>Brings up a saved connection by name.</summary>
    public static string[] Connect(string ssid) => ["connection", "up", ssid];

    /// <summary>Deletes a saved connection.</summary>
    public static string[] Delete(string ssid) => ["connection", "delete", ssid];

    /// <summary>Disconnects a device, leaving the saved connection in place.</summary>
    public static string[] Disconnect(string device) => ["device", "disconnect", device];

    /// <summary>
    /// Creates a saved Wi-Fi connection carrying its own secret, mirroring the Windows
    /// model where the profile holds the passphrase and connecting merely associates.
    /// </summary>
    /// <remarks>
    /// <c>autoconnect no</c> keeps NetworkManager from grabbing the network on its own:
    /// this app decides when to associate, and an automatic reconnect mid-test would
    /// corrupt a measurement.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A secret is required for the security type but was not supplied.
    /// </exception>
    /// <exception cref="NotSupportedException">The security type has no nmcli mapping.</exception>
    public static string[] Add(WifiNetworkProfile profile, WifiSecret? secret)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var arguments = new List<string>
        {
            "connection", "add",
            "type", "wifi",
            "con-name", profile.Ssid,
            "ifname", "*",
            "ssid", profile.Ssid,
            "connection.autoconnect", "no"
        };

        arguments.AddRange(SecurityArguments(profile, secret));
        return [.. arguments];
    }

    private static List<string> SecurityArguments(WifiNetworkProfile profile, WifiSecret? secret)
    {
        switch (profile.Security)
        {
            case SecurityType.Open:
                return [];

            case SecurityType.Wpa2Personal:
                return ["wifi-sec.key-mgmt", "wpa-psk", "wifi-sec.psk", RequirePassphrase(profile, secret)];

            case SecurityType.Wpa3Personal:
                return
                [
                    "wifi-sec.key-mgmt", "sae",
                    "wifi-sec.psk", RequirePassphrase(profile, secret),
                    // Protected Management Frames are mandatory for WPA3; without this
                    // NetworkManager negotiates down and the association fails.
                    "wifi-sec.pmf", "2"
                ];

            case SecurityType.Wpa2Enterprise:
                return EnterpriseArguments(profile, secret, requireManagementFrameProtection: false);

            case SecurityType.Wpa3Enterprise:
                return EnterpriseArguments(profile, secret, requireManagementFrameProtection: true);

            default:
                throw new NotSupportedException(
                    $"Security type '{profile.Security}' is not supported on NetworkManager.");
        }
    }

    private static List<string> EnterpriseArguments(
        WifiNetworkProfile profile, WifiSecret? secret, bool requireManagementFrameProtection)
    {
        var passphrase = RequirePassphrase(profile, secret);

        if (string.IsNullOrWhiteSpace(secret!.Username))
        {
            throw new InvalidOperationException(
                $"Network '{profile.Ssid}' is Enterprise and requires a username.");
        }

        var arguments = new List<string>
        {
            "wifi-sec.key-mgmt", "wpa-eap",
            "802-1x.eap", "peap",
            "802-1x.phase2-auth", "mschapv2",
            "802-1x.identity", Identity(secret),
            "802-1x.password", passphrase
        };

        if (requireManagementFrameProtection)
        {
            arguments.Add("wifi-sec.pmf");
            arguments.Add("2");
        }

        return arguments;
    }

    /// <summary>
    /// Composes the 802.1X identity, matching the <c>DOMAIN\user</c> convention the
    /// Windows profile builder already uses.
    /// </summary>
    private static string Identity(WifiSecret secret) =>
        string.IsNullOrWhiteSpace(secret.Domain)
            ? secret.Username!
            : $"{secret.Domain}\\{secret.Username}";

    private static string RequirePassphrase(WifiNetworkProfile profile, WifiSecret? secret) =>
        string.IsNullOrEmpty(secret?.Passphrase)
            ? throw new InvalidOperationException(
                $"Network '{profile.Ssid}' is protected and requires a passphrase.")
            : secret.Passphrase;
}
