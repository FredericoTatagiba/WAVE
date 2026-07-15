using WAVE.Domain.Networking;

namespace WAVE.Application.Networking;

/// <summary>
/// Derives the <see cref="SecurityType"/> from the authentication text of
/// <c>netsh wlan show networks</c>. It relies on stable tokens in the value
/// (WPA2/WPA3/Enterprise/Open) — not on the localized Windows label — which
/// allows distinguishing Personal from Enterprise (802.1X). Pure, testable logic.
/// </summary>
public static class WifiSecurityParser
{
    public static SecurityType FromNetshBlock(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return SecurityType.Open;
        }

        var isEnterprise = text.Contains("Enterprise", StringComparison.OrdinalIgnoreCase);
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
