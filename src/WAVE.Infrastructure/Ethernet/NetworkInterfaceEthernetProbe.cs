using System.Net.NetworkInformation;
using WAVE.Application.Abstractions;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Wifi;

namespace WAVE.Infrastructure.Ethernet;

/// <summary>
/// Locates the physical wired adapter and reports its link and lease state.
/// </summary>
/// <remarks>
/// The hard part is not reading the state but choosing the adapter. A workstation is
/// full of interfaces that report as Ethernet and would answer "yes, I have a gateway":
/// Hyper-V switches, VPN taps, VirtualBox and VMware host adapters. Testing over one of
/// those would certify a cable that is not even plugged in, so they are filtered out and
/// an adapter with an actual link wins over one without.
/// </remarks>
public sealed class NetworkInterfaceEthernetProbe : IEthernetLinkProbe
{
    private const long BitsPerMegabit = 1_000_000;

    private static readonly string[] VirtualAdapterMarkers =
    [
        "virtual", "vethernet", "vmware", "virtualbox", "hyper-v", "tap-", "tunnel",
        "pseudo", "loopback", "bluetooth", "wan miniport", "docker", "npcap", "wintun"
    ];

    private static readonly NetworkInterfaceType[] WiredTypes =
    [
        NetworkInterfaceType.Ethernet,
        NetworkInterfaceType.GigabitEthernet,
        NetworkInterfaceType.FastEthernetT,
        NetworkInterfaceType.FastEthernetFx
    ];

    private readonly IAppLogger _logger;

    public NetworkInterfaceEthernetProbe(IAppLogger logger) => _logger = logger;

    public async Task<EthernetLink?> DetectAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await ResolveWiredAdaptersAsync(cancellationToken).ConfigureAwait(false);

        var adapter = candidates
            .OrderByDescending(candidate => candidate.OperationalStatus == OperationalStatus.Up)
            .FirstOrDefault();

        if (adapter is null)
        {
            return null;
        }

        var properties = adapter.GetIPProperties();

        return new EthernetLink(
            adapter.Name,
            string.IsNullOrWhiteSpace(adapter.Description) ? adapter.Name : adapter.Description,
            adapter.OperationalStatus == OperationalStatus.Up,
            adapter.Speed > 0 ? adapter.Speed / BitsPerMegabit : 0,
            NetworkInterfaceDhcpValidator.HasLease(
                properties.GatewayAddresses.Select(gateway => gateway.Address),
                properties.UnicastAddresses.Select(unicast => unicast.Address)));
    }

    /// <summary>
    /// Finds the wired adapters to inspect.
    /// </summary>
    /// <remarks>
    /// On Linux .NET derives the interface type from the kernel's ARPHRD value, which is
    /// <c>ARPHRD_ETHER</c> for wireless adapters too — so the type filter would happily
    /// return a wlan interface and the "cable" test would silently run over Wi-Fi. The
    /// interface is therefore resolved by name from NetworkManager, the same view the
    /// rest of the Linux stack uses.
    /// </remarks>
    private async Task<IReadOnlyList<NetworkInterface>> ResolveWiredAdaptersAsync(CancellationToken cancellationToken)
    {
        var all = NetworkInterface.GetAllNetworkInterfaces();

        if (OperatingSystem.IsWindows())
        {
            return all
                .Where(adapter => WiredTypes.Contains(adapter.NetworkInterfaceType))
                .Where(adapter => !IsVirtual(adapter))
                .ToList();
        }

        var device = await NmcliDevice
            .FindEthernetInterfaceAsync(_logger, cancellationToken)
            .ConfigureAwait(false);

        return device is null
            ? []
            : all.Where(adapter => adapter.Name.Equals(device, StringComparison.Ordinal)).ToList();
    }

    private static bool IsVirtual(NetworkInterface adapter) =>
        VirtualAdapterMarkers.Any(marker =>
            adapter.Description.Contains(marker, StringComparison.OrdinalIgnoreCase)
            || adapter.Name.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
