using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Gera o XML de perfil WLAN do Windows a partir de um perfil de rede.</summary>
public interface IWifiProfileXmlFactory
{
    string Build(WifiNetworkProfile profile, WifiSecret? secret);

    /// <summary>
    /// Gera o XML de credenciais de usuário (EAP) para redes Enterprise (802.1X),
    /// aplicado ao perfil após a criação. Retorna null quando a rede não é
    /// Enterprise ou não há credencial a aplicar.
    /// </summary>
    string? BuildEapUserData(WifiNetworkProfile profile, WifiSecret? secret);
}
