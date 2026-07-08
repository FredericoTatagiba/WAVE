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
    public void Build_Wpa2Enterprise_GeneratesOneXPeapProfile()
    {
        var profile = new WifiNetworkProfile("Corp", null, SecurityType.Wpa2Enterprise);

        var xml = _builder.Build(profile, new WifiSecret("senha", "joao", "EMPRESA"));

        Assert.Contains("<authentication>WPA2</authentication>", xml);
        Assert.Contains("<useOneX>true</useOneX>", xml);
        Assert.Contains("<OneX", xml);
        Assert.Contains("<Type>25</Type>", xml); // PEAP
        Assert.Contains("<Type>26</Type>", xml); // MSCHAPv2
        // O perfil NUNCA embute a senha do usuário (vai no EAP user data).
        Assert.DoesNotContain("senha", xml);
        Assert.DoesNotContain("sharedKey", xml);
    }

    [Fact]
    public void Build_Wpa3Enterprise_UsesWpa3Ent()
    {
        var profile = new WifiNetworkProfile("Corp3", null, SecurityType.Wpa3Enterprise);

        var xml = _builder.Build(profile, new WifiSecret("senha", "joao"));

        Assert.Contains("<authentication>WPA3ENT</authentication>", xml);
        Assert.Contains("<useOneX>true</useOneX>", xml);
    }

    [Fact]
    public void BuildEapUserData_Enterprise_IncludesCredentials()
    {
        var profile = new WifiNetworkProfile("Corp", null, SecurityType.Wpa2Enterprise);

        var xml = _builder.BuildEapUserData(profile, new WifiSecret("segredo123", "joao.silva", "EMPRESA"));

        Assert.NotNull(xml);
        Assert.Contains("<MsChapV2:Username>joao.silva</MsChapV2:Username>", xml);
        Assert.Contains("<MsChapV2:Password>segredo123</MsChapV2:Password>", xml);
        Assert.Contains("<MsChapV2:LogonDomain>EMPRESA</MsChapV2:LogonDomain>", xml);
    }

    [Fact]
    public void BuildEapUserData_WithoutDomain_OmitsLogonDomain()
    {
        var profile = new WifiNetworkProfile("Corp", null, SecurityType.Wpa2Enterprise);

        var xml = _builder.BuildEapUserData(profile, new WifiSecret("segredo", "joao"));

        Assert.NotNull(xml);
        Assert.Contains("<MsChapV2:Username>joao</MsChapV2:Username>", xml);
        Assert.DoesNotContain("LogonDomain", xml);
    }

    [Fact]
    public void BuildEapUserData_EscapesSpecialCharacters()
    {
        var profile = new WifiNetworkProfile("Corp", null, SecurityType.Wpa2Enterprise);

        var xml = _builder.BuildEapUserData(profile, new WifiSecret("a<b>&\"c", "user&1"));

        Assert.NotNull(xml);
        Assert.Contains("user&amp;1", xml);
        Assert.Contains("a&lt;b&gt;&amp;\"c", xml);
        Assert.DoesNotContain("<b>", xml);
    }

    [Fact]
    public void BuildEapUserData_NonEnterprise_ReturnsNull()
    {
        var profile = new WifiNetworkProfile("Casa", null, SecurityType.Wpa2Personal);

        Assert.Null(_builder.BuildEapUserData(profile, new WifiSecret("senha")));
    }

    [Fact]
    public void BuildEapUserData_EnterpriseWithoutUsername_Throws()
    {
        var profile = new WifiNetworkProfile("Corp", null, SecurityType.Wpa2Enterprise);

        Assert.Throws<InvalidOperationException>(
            () => _builder.BuildEapUserData(profile, new WifiSecret("senha")));
    }
}
