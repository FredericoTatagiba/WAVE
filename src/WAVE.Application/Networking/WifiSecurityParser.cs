using WAVE.Domain.Networking;

namespace WAVE.Application.Networking;

/// <summary>
/// Deriva o <see cref="SecurityType"/> a partir do texto de autenticação do
/// <c>netsh wlan show networks</c>. Baseia-se em tokens estáveis do valor
/// (WPA2/WPA3/Enterprise/Open) — não no rótulo localizado do Windows — o que
/// permite distinguir Personal de Enterprise (802.1X). Lógica pura e testável.
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
