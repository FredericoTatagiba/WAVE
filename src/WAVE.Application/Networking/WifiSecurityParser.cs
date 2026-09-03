using WAVE.Domain.Networking;

namespace WAVE.Application.Networking;

/// <summary>
/// Derives the <see cref="SecurityType"/> from the security text reported by the
/// platform: the authentication line of <c>netsh wlan show networks</c> on Windows, the
/// SECURITY field of <c>nmcli device wifi list</c> on Linux. It relies on stable tokens
/// in the value (WPA2/WPA3/Enterprise/802.1X) — not on the localized label — which allows
/// distinguishing Personal from Enterprise. Pure, testable logic.
/// </summary>
public static class WifiSecurityParser
{
    /// <summary>Reads the security tokens out of a platform-reported security string.</summary>
    /// <remarks>
    /// WEP collapses to <see cref="SecurityType.Open"/>: neither backend can build a WEP
    /// profile, so there is no type to map it to. Pre-existing behaviour on Windows.
    /// </remarks>
    public static SecurityType FromSecurityText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            // nmcli reports an open network as an empty SECURITY field.
            return SecurityType.Open;
        }

        var isEnterprise =
            text.Contains("Enterprise", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("802.1X", StringComparison.OrdinalIgnoreCase);
        var isWpa3 = text.Contains("WPA3", StringComparison.OrdinalIgnoreCase);
        var isWpa = text.Contains("WPA", StringComparison.OrdinalIgnoreCase);

        if (isEnterprise)
        {
            return isWpa3 ? SecurityType.Wpa3Enterprise : SecurityType.Wpa2Enterprise;
        }

        if (isWpa3)
        {
            return SecurityType.Wpa3Personal;
        }

        if (isWpa)
        {
            return SecurityType.Wpa2Personal;
        }

        return SecurityType.Open;
    }
}
