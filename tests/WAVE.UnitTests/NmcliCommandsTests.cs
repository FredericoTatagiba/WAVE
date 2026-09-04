using WAVE.Application.Networking;
using WAVE.Domain.Networking;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Guards the argument vectors. The security mapping is the correctness half; the
/// one-element-per-argument shape is the injection half — an SSID must never be able to
/// split into extra nmcli arguments.
/// </summary>
public class NmcliCommandsTests
{
    private static WifiNetworkProfile Profile(SecurityType security, string ssid = "Corp") =>
        new(ssid, null, security);

    private static string ValueAfter(string[] arguments, string key)
    {
        var index = Array.IndexOf(arguments, key);
        Assert.True(index >= 0 && index + 1 < arguments.Length, $"'{key}' missing from: {string.Join(' ', arguments)}");
        return arguments[index + 1];
    }

    [Fact]
    public void Add_OpenNetwork_CarriesNoSecuritySettings()
    {
        var arguments = NmcliCommands.Add(Profile(SecurityType.Open), null);

        Assert.DoesNotContain("wifi-sec.key-mgmt", arguments);
        Assert.Equal("Corp", ValueAfter(arguments, "ssid"));
        Assert.Equal("no", ValueAfter(arguments, "connection.autoconnect"));
    }

    [Fact]
    public void Add_Wpa2Personal_UsesPreSharedKey()
    {
        var arguments = NmcliCommands.Add(Profile(SecurityType.Wpa2Personal), new WifiSecret("s3cret"));

        Assert.Equal("wpa-psk", ValueAfter(arguments, "wifi-sec.key-mgmt"));
        Assert.Equal("s3cret", ValueAfter(arguments, "wifi-sec.psk"));
        Assert.DoesNotContain("wifi-sec.pmf", arguments);
    }

    [Fact]
    public void Add_Wpa3Personal_UsesSaeAndRequiresManagementFrameProtection()
    {
        var arguments = NmcliCommands.Add(Profile(SecurityType.Wpa3Personal), new WifiSecret("s3cret"));

        Assert.Equal("sae", ValueAfter(arguments, "wifi-sec.key-mgmt"));
        // WPA3 mandates PMF; without it NetworkManager negotiates down and fails.
        Assert.Equal("2", ValueAfter(arguments, "wifi-sec.pmf"));
    }

    [Fact]
    public void Add_Wpa2Enterprise_ConfiguresPeapMschapV2()
    {
        var secret = new WifiSecret("s3cret", Username: "tecnico");

        var arguments = NmcliCommands.Add(Profile(SecurityType.Wpa2Enterprise), secret);

        Assert.Equal("wpa-eap", ValueAfter(arguments, "wifi-sec.key-mgmt"));
        Assert.Equal("peap", ValueAfter(arguments, "802-1x.eap"));
        Assert.Equal("mschapv2", ValueAfter(arguments, "802-1x.phase2-auth"));
        Assert.Equal("tecnico", ValueAfter(arguments, "802-1x.identity"));
        Assert.Equal("s3cret", ValueAfter(arguments, "802-1x.password"));
        Assert.DoesNotContain("wifi-sec.pmf", arguments);
    }

    [Fact]
    public void Add_Wpa3Enterprise_AddsManagementFrameProtection()
    {
        var secret = new WifiSecret("s3cret", Username: "tecnico");

        var arguments = NmcliCommands.Add(Profile(SecurityType.Wpa3Enterprise), secret);

        Assert.Equal("wpa-eap", ValueAfter(arguments, "wifi-sec.key-mgmt"));
        Assert.Equal("2", ValueAfter(arguments, "wifi-sec.pmf"));
    }

    [Fact]
    public void Add_EnterpriseWithDomain_ComposesBackslashIdentity()
    {
        var secret = new WifiSecret("s3cret", Username: "tecnico", Domain: "CORP");

        var arguments = NmcliCommands.Add(Profile(SecurityType.Wpa2Enterprise), secret);

        Assert.Equal(@"CORP\tecnico", ValueAfter(arguments, "802-1x.identity"));
    }

    [Fact]
    public void Add_ProtectedWithoutSecret_Throws() =>
        Assert.Throws<InvalidOperationException>(
            () => NmcliCommands.Add(Profile(SecurityType.Wpa2Personal), null));

    [Fact]
    public void Add_EnterpriseWithoutUsername_Throws() =>
        Assert.Throws<InvalidOperationException>(
            () => NmcliCommands.Add(Profile(SecurityType.Wpa2Enterprise), new WifiSecret("s3cret")));

    [Fact]
    public void Add_SsidWithSpacesAndQuotes_StaysASingleArgument()
    {
        // Passed through ArgumentList, never a shell: this must survive verbatim.
        const string Hostile = @"Galaxy A16 ""5G"" $(x)";

        var arguments = NmcliCommands.Add(Profile(SecurityType.Open, Hostile), null);

        Assert.Equal(Hostile, ValueAfter(arguments, "ssid"));
        Assert.Equal(Hostile, ValueAfter(arguments, "con-name"));

        // Exactly the two slots above, each holding the value whole: no element was split
        // on the spaces, and no quoting was introduced.
        Assert.Equal(2, arguments.Count(argument => argument == Hostile));
        Assert.DoesNotContain(arguments, argument => argument is "A16" or "\"5G\"");
    }

    [Fact]
    public void Connect_And_Delete_PassSsidAsOneArgument()
    {
        const string Ssid = "Rede Com Espaço";

        Assert.Equal(["connection", "up", Ssid], NmcliCommands.Connect(Ssid));
        Assert.Equal(["connection", "delete", Ssid], NmcliCommands.Delete(Ssid));
    }
}
