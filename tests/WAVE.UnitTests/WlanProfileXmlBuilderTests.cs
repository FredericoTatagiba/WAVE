using WAVE.Application.Networking;
using WAVE.Domain.Networking;
using Xunit;

namespace WAVE.UnitTests;

public class WlanProfileXmlBuilderTests
{
    private readonly WlanProfileXmlBuilder _builder = new();

    [Fact]
    public void Build_Wpa2Personal_IncludesAuthenticationAndPassphrase()
    {
        var profile = new WifiNetworkProfile("MinhaRede", null, SecurityType.Wpa2Personal);

        var xml = _builder.Build(profile, new WifiSecret("segredo123"));

        Assert.Contains("WPA2PSK", xml);
        Assert.Contains("segredo123", xml);
        Assert.Contains("<name>MinhaRede</name>", xml);
    }

    [Fact]
    public void Build_Wpa3Personal_UsesSae()
    {
        var profile = new WifiNetworkProfile("Rede3", null, SecurityType.Wpa3Personal);

        var xml = _builder.Build(profile, new WifiSecret("segredo123"));

        Assert.Contains("WPA3SAE", xml);
    }

    [Fact]
    public void Build_Open_HasNoSharedKey()
    {
        var profile = new WifiNetworkProfile("Aberta", null, SecurityType.Open);

        var xml = _builder.Build(profile, null);

        Assert.Contains("<authentication>open</authentication>", xml);
        Assert.DoesNotContain("sharedKey", xml);
    }

    [Fact]
    public void Build_ProtectedWithoutSecret_Throws()
    {
        var profile = new WifiNetworkProfile("Rede", null, SecurityType.Wpa2Personal);

        Assert.Throws<InvalidOperationException>(() => _builder.Build(profile, null));
    }

    [Fact]
    public void Build_Enterprise_ThrowsNotSupported()
    {
        var profile = new WifiNetworkProfile("Corp", null, SecurityType.Wpa2Enterprise);

        Assert.Throws<NotSupportedException>(() => _builder.Build(profile, new WifiSecret("x", "user")));
    }
}
