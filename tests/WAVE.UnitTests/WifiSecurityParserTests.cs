using WAVE.Application.Networking;
using WAVE.Domain.Networking;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Derives the security type from the netsh text, distinguishing Personal from
/// Enterprise (802.1X) — the basis for discovery to offer username/domain.
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
    public void FromNetshBlock_MapsAuthenticationToken(string text, SecurityType expected) =>
        Assert.Equal(expected, WifiSecurityParser.FromNetshBlock(text));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void FromNetshBlock_EmptyIsOpen(string? text) =>
        Assert.Equal(SecurityType.Open, WifiSecurityParser.FromNetshBlock(text!));

    [Fact]
    public void FromNetshBlock_IgnoresLabelLocaleUsesValueTokens()
    {
        // Realistic block (labels may be localized; the values are stable).
        var block =
            "    Tipo de rede            : Infraestrutura\n" +
            "    Autenticação            : WPA3-Enterprise\n" +
            "    Criptografia            : CCMP\n";

        Assert.Equal(SecurityType.Wpa3Enterprise, WifiSecurityParser.FromNetshBlock(block));
    }
}
