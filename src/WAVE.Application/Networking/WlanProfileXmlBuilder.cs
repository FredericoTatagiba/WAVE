using System.Xml.Linq;
using WAVE.Application.Abstractions;
using WAVE.Domain.Networking;

namespace WAVE.Application.Networking;

/// <summary>
/// Builds the Windows WLAN profile XML (Factory). Supports Open, WPA2/WPA3-Personal
/// and WPA2/WPA3-Enterprise (802.1X via PEAP-MSCHAPv2). Using <see cref="XElement"/>
/// guarantees correct escaping of SSID/password. The user credentials (Enterprise)
/// do not go in the profile: they are built by <see cref="BuildEapUserData"/> and applied
/// separately to the profile by the connector.
/// </summary>
public sealed class WlanProfileXmlBuilder : IWifiProfileXmlFactory
{
    private static readonly XNamespace Ns = "http://www.microsoft.com/networking/WLAN/profile/v1";

    public string Build(WifiNetworkProfile profile, WifiSecret? secret)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var security = new XElement(Ns + "security", BuildAuthEncryption(profile.Security));

        if (profile.IsEnterprise)
        {
            security.Add(XElement.Parse(PeapMschapV2OneX));
        }
        else if (profile.RequiresCredential)
        {
            if (secret is null || string.IsNullOrEmpty(secret.Passphrase))
            {
                throw new InvalidOperationException("Missing credential for a protected network.");
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

    public string? BuildEapUserData(WifiNetworkProfile profile, WifiSecret? secret)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!profile.IsEnterprise)
        {
            return null;
        }

        if (secret is null || string.IsNullOrEmpty(secret.Passphrase))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(secret.Username))
        {
            throw new InvalidOperationException("An Enterprise network requires a username.");
        }

        var user = EscapeXmlText(secret.Username.Trim());
        var password = EscapeXmlText(secret.Passphrase);
        var domainLine = string.IsNullOrWhiteSpace(secret.Domain)
            ? string.Empty
            : $"\n        <MsChapV2:LogonDomain>{EscapeXmlText(secret.Domain.Trim())}</MsChapV2:LogonDomain>";

        return
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<EapHostUserCredentials xmlns=\"http://www.microsoft.com/provisioning/EapHostUserCredentials\" " +
            "xmlns:eapCommon=\"http://www.microsoft.com/provisioning/EapCommon\" " +
            "xmlns:baseEap=\"http://www.microsoft.com/provisioning/BaseEapMethodUserCredentials\">\n" +
            "  <EapMethod>\n" +
            "    <eapCommon:Type>25</eapCommon:Type>\n" +
            "    <eapCommon:AuthorId>0</eapCommon:AuthorId>\n" +
            "  </EapMethod>\n" +
            "  <Credentials xmlns:eapUser=\"http://www.microsoft.com/provisioning/EapUserPropertiesV1\" " +
            "xmlns:MsPeap=\"http://www.microsoft.com/provisioning/MsPeapUserPropertiesV1\" " +
            "xmlns:MsChapV2=\"http://www.microsoft.com/provisioning/MsChapV2UserPropertiesV1\">\n" +
            "    <MsPeap:Eap>\n" +
            "      <eapUser:Type>26</eapUser:Type>\n" +
            "      <MsChapV2:Eap>\n" +
            $"        <MsChapV2:Username>{user}</MsChapV2:Username>\n" +
            $"        <MsChapV2:Password>{password}</MsChapV2:Password>{domainLine}\n" +
            "      </MsChapV2:Eap>\n" +
            "    </MsPeap:Eap>\n" +
            "  </Credentials>\n" +
            "</EapHostUserCredentials>";
    }

    /// <summary>
    /// Escapes the characters that are significant in XML element content (&amp;, &lt;, &gt;).
    /// Quotes are inert inside element content, so they are left as-is — matching the EAP
    /// credentials, which are always written as element text, never as attribute values.
    /// </summary>
    private static string EscapeXmlText(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");

    private static XElement BuildAuthEncryption(SecurityType security)
    {
        var (authentication, useOneX) = security switch
        {
            SecurityType.Open => ("open", false),
            SecurityType.Wpa2Personal => ("WPA2PSK", false),
            SecurityType.Wpa3Personal => ("WPA3SAE", false),
            SecurityType.Wpa2Enterprise => ("WPA2", true),
            SecurityType.Wpa3Enterprise => ("WPA3ENT", true),
            _ => throw new NotSupportedException($"Unsupported security type: {security}.")
        };

        var encryption = security == SecurityType.Open ? "none" : "AES";

        return new XElement(Ns + "authEncryption",
            new XElement(Ns + "authentication", authentication),
            new XElement(Ns + "encryption", encryption),
            new XElement(Ns + "useOneX", useOneX ? "true" : "false"));
    }

    /// <summary>
    /// OneX/EAP block for PEAP-MSCHAPv2 (EAP type 25 with inner type 26). It is static:
    /// it contains no secrets — the user credentials go in the EAP user data.
    /// </summary>
    private const string PeapMschapV2OneX =
        "<OneX xmlns=\"http://www.microsoft.com/networking/OneX/v1\">" +
        "<authMode>user</authMode>" +
        "<EAPConfig>" +
        "<EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\">" +
        "<EapMethod>" +
        "<Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type>" +
        "<VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId>" +
        "<VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType>" +
        "<AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId>" +
        "</EapMethod>" +
        "<Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\">" +
        "<Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\">" +
        "<Type>25</Type>" +
        "<EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\">" +
        "<ServerValidation>" +
        "<DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation>" +
        "<ServerNames></ServerNames>" +
        "</ServerValidation>" +
        "<FastReconnect>true</FastReconnect>" +
        "<InnerEapOptional>false</InnerEapOptional>" +
        "<Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\">" +
        "<Type>26</Type>" +
        "<EapType xmlns=\"http://www.microsoft.com/provisioning/MsChapV2ConnectionPropertiesV1\">" +
        "<UseWinLogonCredentials>false</UseWinLogonCredentials>" +
        "</EapType>" +
        "</Eap>" +
        "<EnableQuarantineChecks>false</EnableQuarantineChecks>" +
        "<RequireCryptoBinding>false</RequireCryptoBinding>" +
        "<PeapExtensions>" +
        "<PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">false</PerformServerValidation>" +
        "<AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">false</AcceptServerName>" +
        "</PeapExtensions>" +
        "</EapType>" +
        "</Eap>" +
        "</Config>" +
        "</EapHostConfig>" +
        "</EAPConfig>" +
        "</OneX>";
}
