using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Gera o XML de perfil WLAN do Windows a partir de um perfil de rede.</summary>
public interface IWifiProfileXmlFactory
{
    string Build(WifiNetworkProfile profile, WifiSecret? secret);
}
