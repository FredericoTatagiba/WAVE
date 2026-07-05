namespace WAVE.Domain.Networking;

/// <summary>Tipo de segurança da rede Wi-Fi (base para gerar o perfil WLAN).</summary>
public enum SecurityType
{
    Open,
    Wpa2Personal,
    Wpa3Personal,
    Wpa2Enterprise,
    Wpa3Enterprise
}
