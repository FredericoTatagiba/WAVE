using System.Xml.Linq;
using WAVE.Application.Abstractions;
using WAVE.Domain.Networking;

namespace WAVE.Application.Networking;

/// <summary>
/// Gera o XML de perfil WLAN do Windows (Factory). Suporta Open, WPA2-Personal e
/// WPA3-Personal. Enterprise (802.1X) fica como evolução futura.
/// O uso de <see cref="XElement"/> garante o escape correto de SSID/senha.
/// </summary>
public sealed class WlanProfileXmlBuilder : IWifiProfileXmlFactory
{
    private static readonly XNamespace Ns = "http://www.microsoft.com/networking/WLAN/profile/v1";

    public string Build(WifiNetworkProfile profile, WifiSecret? secret)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.IsEnterprise)
        {
            throw new NotSupportedException(
                "Perfis Enterprise (802.1X) ainda não são suportados pelo gerador de perfil.");
        }

        var security = new XElement(Ns + "security", BuildAuthEncryption(profile.Security));

        if (profile.RequiresCredential)
        {
            if (secret is null || string.IsNullOrEmpty(secret.Passphrase))
            {
                throw new InvalidOperationException("Credencial ausente para rede protegida.");
            }

            security.Add(new XElement(Ns + "sharedKey",
                new XElement(Ns + "keyType", "passPhrase"),
                new XElement(Ns + "protected", "false"),
                new XElement(Ns + "keyMaterial", secret.Passphrase)));
        }

        var wlanProfile = new XElement(Ns + "WLANProfile",
            new XElement(Ns + "name", profile.Ssid),
            new XElement(Ns + "SSIDConfig",
                new XElement(Ns + "SSID",
                    new XElement(Ns + "name", profile.Ssid))),
            new XElement(Ns + "connectionType", "ESS"),
            new XElement(Ns + "connectionMode", "manual"),
            new XElement(Ns + "MSM", security));

        var document = new XDocument(new XDeclaration("1.0", "UTF-8", null), wlanProfile);
        return document.Declaration + Environment.NewLine + wlanProfile;
    }

    private static XElement BuildAuthEncryption(SecurityType security)
    {
        var authentication = security switch
        {
            SecurityType.Open => "open",
            SecurityType.Wpa2Personal => "WPA2PSK",
            SecurityType.Wpa3Personal => "WPA3SAE",
            _ => throw new NotSupportedException($"Segurança não suportada: {security}.")
        };

        var encryption = security == SecurityType.Open ? "none" : "AES";

        return new XElement(Ns + "authEncryption",
            new XElement(Ns + "authentication", authentication),
            new XElement(Ns + "encryption", encryption),
            new XElement(Ns + "useOneX", "false"));
    }
}
