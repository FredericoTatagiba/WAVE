namespace WAVE.Domain.Networking;

/// <summary>Wi-Fi network security type (basis for building the WLAN profile).</summary>
public enum SecurityType
{
    Open,
    Wpa2Personal,
    Wpa3Personal,
    Wpa2Enterprise,
    Wpa3Enterprise
}
