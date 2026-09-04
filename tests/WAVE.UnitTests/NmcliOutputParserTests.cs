using WAVE.Application.Networking;
using WAVE.Domain.Networking;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Covers the terse-format traps: escaped separators, the empty SECURITY field an open
/// network produces, and the per-BSS duplication nmcli emits but netsh does not.
/// </summary>
public class NmcliOutputParserTests
{
    [Fact]
    public void ParseScan_ReadsSsidSecurityAndSignal()
    {
        var output = string.Join('\n',
            "Corp-Guest:WPA2:72",
            "Corp-Secure:WPA2 802.1X:65",
            "Open-Hotspot::40",
            "Modern-Net:WPA3:88");

        var networks = ParseScanByName(output);

        Assert.Equal(SecurityType.Wpa2Personal, networks["Corp-Guest"].Security);
        Assert.Equal(72, networks["Corp-Guest"].SignalPercent);
        Assert.Equal(SecurityType.Wpa2Enterprise, networks["Corp-Secure"].Security);
        Assert.Equal(SecurityType.Open, networks["Open-Hotspot"].Security);
        Assert.Equal(SecurityType.Wpa3Personal, networks["Modern-Net"].Security);
    }

    [Fact]
    public void ParseScan_UnescapesColonAndBackslashInSsid()
    {
        // nmcli escapes the field separator inside a value; splitting naively on ':'
        // would yield "Rede" and drop the rest.
        var networks = ParseScanByName(@"Rede\:2G:WPA2:50" + "\n" + @"Back\\slash:WPA2:30");

        Assert.True(networks.ContainsKey("Rede:2G"));
        Assert.True(networks.ContainsKey(@"Back\slash"));
    }

    [Fact]
    public void ParseScan_DeduplicatesBySsidKeepingStrongestSignal()
    {
        // The same AP on 2.4 GHz and 5 GHz: one network, not two buttons.
        var networks = ParseScan("Dual-Band:WPA2:41\nDual-Band:WPA2:83\nDual-Band:WPA2:60");

        var only = Assert.Single(networks);
        Assert.Equal("Dual-Band", only.Ssid);
        Assert.Equal(83, only.SignalPercent);
    }

    [Fact]
    public void ParseScan_SkipsHiddenAndMalformedRows()
    {
        var networks = ParseScan(":WPA2:70\nincomplete-row\n\nVisible:WPA2:55");

        var only = Assert.Single(networks);
        Assert.Equal("Visible", only.Ssid);
    }

    [Theory]
    [InlineData("abc", 0)]
    [InlineData("", 0)]
    [InlineData("140", 100)]
    [InlineData("-5", 0)]
    public void ParseScan_ClampsSignalToPercentRange(string signal, int expected) =>
        Assert.Equal(expected, ParseScan($"Net:WPA2:{signal}")[0].SignalPercent);

    [Fact]
    public void ParseConnectionNames_KeepsOnlyWifiConnections()
    {
        // Captured verbatim from `nmcli -t -f NAME,TYPE connection show`, including the
        // localized ethernet name with a space in it.
        var output = string.Join('\n',
            "Conexão cabeada 1:802-3-ethernet",
            "CPAPS_TI:802-11-wireless",
            "FRED:802-11-wireless",
            "Galaxy A16 5G 9AC6:802-11-wireless");

        var names = NmcliOutputParser.ParseConnectionNames(output);

        Assert.Equal(["CPAPS_TI", "FRED", "Galaxy A16 5G 9AC6"], names);
    }

    [Fact]
    public void ParseFirstWifiDevice_FindsWlanAmongOtherInterfaces()
    {
        // Captured verbatim from `nmcli -t -f DEVICE,TYPE device status`.
        var output = "eno1:ethernet\nwlp2s0:wifi\nlo:loopback";

        Assert.Equal("wlp2s0", NmcliOutputParser.ParseFirstWifiDevice(output));
    }

    [Fact]
    public void ParseFirstWifiDevice_ReturnsNullWithoutWirelessHardware() =>
        Assert.Null(NmcliOutputParser.ParseFirstWifiDevice("eno1:ethernet\nlo:loopback"));

    [Fact]
    public void Parsers_TolerateEmptyOutput()
    {
        // What this machine returns with the radio disabled.
        Assert.Empty(NmcliOutputParser.ParseScan(string.Empty));
        Assert.Empty(NmcliOutputParser.ParseConnectionNames(string.Empty));
        Assert.Null(NmcliOutputParser.ParseFirstWifiDevice(string.Empty));
    }

    private static IReadOnlyList<AvailableNetwork> ParseScan(string output) =>
        NmcliOutputParser.ParseScan(output);

    private static Dictionary<string, AvailableNetwork> ParseScanByName(string output) =>
        NmcliOutputParser.ParseScan(output).ToDictionary(network => network.Ssid, StringComparer.Ordinal);
}
