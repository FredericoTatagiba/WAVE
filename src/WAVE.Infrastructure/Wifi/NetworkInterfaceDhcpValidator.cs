using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Confirms that the active Wi-Fi adapter has a routable IPv4 (non-APIPA) and
/// a gateway — an indication of a valid DHCP lease.
/// </summary>
public sealed class NetworkInterfaceDhcpValidator : IDhcpAddressValidator
{
    private const int ApipaFirstOctet = 169;
    private const int ApipaSecondOctet = 254;

    private readonly IAppLogger _logger;

    public NetworkInterfaceDhcpValidator(IAppLogger logger) => _logger = logger;

    public async Task<bool> HasValidLeaseAsync(CancellationToken cancellationToken = default)
    {
        var adapters = await ResolveWifiAdaptersAsync(cancellationToken).ConfigureAwait(false);

        return adapters.Any(adapter =>
        {
            var properties = adapter.GetIPProperties();
            return HasLease(
                properties.GatewayAddresses.Select(gateway => gateway.Address),
                properties.UnicastAddresses.Select(unicast => unicast.Address));
        });
    }

    /// <summary>
    /// Decides whether a set of addresses represents a working DHCP lease: a non-zero
    /// IPv4 gateway plus an IPv4 address that is neither APIPA (the 169.254/16 block a
    /// host self-assigns when DHCP fails) nor loopback.
    /// </summary>
    public static bool HasLease(IEnumerable<IPAddress> gateways, IEnumerable<IPAddress> unicastAddresses)
    {
        var hasGateway = gateways.Any(gateway =>
            gateway.AddressFamily == AddressFamily.InterNetwork && !IsZero(gateway));

        var hasRoutableIpv4 = unicastAddresses.Any(address =>
            address.AddressFamily == AddressFamily.InterNetwork
            && !IsApipa(address)
            && !IPAddress.IsLoopback(address));

        return hasGateway && hasRoutableIpv4;
    }

    /// <summary>
    /// Finds the Wi-Fi adapters to inspect.
    /// </summary>
    /// <remarks>
    /// The obvious filter — <see cref="NetworkInterfaceType.Wireless80211"/> — only works
    /// on Windows. On Linux .NET derives the type from the kernel's ARPHRD value, which is
    /// <c>ARPHRD_ETHER</c> for wireless adapters too, so every wlan interface reports as
    /// <see cref="NetworkInterfaceType.Ethernet"/> and the filter silently matches nothing.
    /// <para>
    /// Dropping the filter is not the fix: a wired adapter also carries a gateway and a
    /// routable address, so the validator would confirm a "successful" Wi-Fi test that was
    /// really running over the cable. The interface is therefore resolved by name, from the
    /// same NetworkManager view the rest of the Linux stack uses.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyList<NetworkInterface>> ResolveWifiAdaptersAsync(CancellationToken cancellationToken)
    {
        var active = NetworkInterface.GetAllNetworkInterfaces()
            .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up)
            .ToList();

        if (OperatingSystem.IsWindows())
        {
            return active
                .Where(adapter => adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .ToList();
        }

        var device = await NmcliDevice
            .FindWifiInterfaceAsync(_logger, cancellationToken)
            .ConfigureAwait(false);

        return device is null
            ? []
            : active.Where(adapter => adapter.Name.Equals(device, StringComparison.Ordinal)).ToList();
    }

    private static bool IsApipa(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == ApipaFirstOctet && bytes[1] == ApipaSecondOctet;
    }

    private static bool IsZero(IPAddress address) => address.GetAddressBytes().All(octet => octet == 0);
}
