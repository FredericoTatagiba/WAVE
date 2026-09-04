using WAVE.Application.Networking;
using WAVE.Domain.Networking;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Derives the security type from the platform's security text, distinguishing Personal
/// from Enterprise (802.1X) — the basis for discovery to offer username/domain. One
/// parser serves both backends; the netsh cases below are the regression guard on that.
/// </summary>
public class WifiSecurityParserTests
{
    [Theory]
    [InlineData("Authentication : Open", SecurityType.Open)]
    [InlineData("Authentication : WPA2-Personal", SecurityType.Wpa2Personal)]
    [InlineData("Authentication : WPA3-Personal", SecurityType.Wpa3Personal)]
    [InlineData("Authentication : WPA2-Enterprise", SecurityType.Wpa2Enterprise)]
    [InlineData("Authentication : WPA3-Enterprise", SecurityType.Wpa3Enterprise)]
    [InlineData("Autenticação : WPA2-Enterprise", SecurityType.Wpa2Enterprise)]
    [InlineData("Authentication : WPA-Personal", SecurityType.Wpa2Personal)]
    public void FromSecurityText_MapsAuthenticationToken(string text, SecurityType expected) =>
        Assert.Equal(expected, WifiSecurityParser.FromSecurityText(text));

    [Theory]
    // nmcli SECURITY field values, which use "802.1X" where netsh says "Enterprise".
    [InlineData("WPA2", SecurityType.Wpa2Personal)]
    [InlineData("WPA1 WPA2", SecurityType.Wpa2Personal)]
    [InlineData("WPA3", SecurityType.Wpa3Personal)]
    [InlineData("WPA2 802.1X", SecurityType.Wpa2Enterprise)]
    [InlineData("WPA3 802.1X", SecurityType.Wpa3Enterprise)]
    [InlineData("--", SecurityType.Open)]
    public void FromSecurityText_MapsNmcliSecurityField(string text, SecurityType expected) =>
        Assert.Equal(expected, WifiSecurityParser.FromSecurityText(text));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FromSecurityText_EmptyIsOpen(string? text) =>
        Assert.Equal(SecurityType.Open, WifiSecurityParser.FromSecurityText(text!));

    [Fact]
    public void FromSecurityText_IgnoresLabelLocaleUsesValueTokens()
    {
        // Realistic block (labels may be localized; the values are stable).
        var block =
            "    Tipo de rede            : Infraestrutura\n" +
            "    Autenticação            : WPA3-Enterprise\n" +
            "    Criptografia            : CCMP\n";

        Assert.Equal(SecurityType.Wpa3Enterprise, WifiSecurityParser.FromSecurityText(block));
    }
}
