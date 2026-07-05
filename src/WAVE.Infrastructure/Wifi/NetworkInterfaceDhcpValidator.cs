using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Confirma que algum adaptador Wi-Fi ativo possui IPv4 roteável (não-APIPA) e
/// gateway — indicativo de concessão DHCP válida.
/// </summary>
public sealed class NetworkInterfaceDhcpValidator : IDhcpAddressValidator
{
    private const int ApipaFirstOctet = 169;
    private const int ApipaSecondOctet = 254;

    public Task<bool> HasValidLeaseAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(HasValidLease());

    private static bool HasValidLease()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
            {
                continue;
            }

            if (nic.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            var properties = nic.GetIPProperties();

            var hasGateway = properties.GatewayAddresses.Any(gateway =>
                gateway.Address.AddressFamily == AddressFamily.InterNetwork && !IsZero(gateway.Address));

            var hasRoutableIpv4 = properties.UnicastAddresses.Any(unicast =>
                unicast.Address.AddressFamily == AddressFamily.InterNetwork
                && !IsApipa(unicast.Address)
                && !IPAddress.IsLoopback(unicast.Address));

            if (hasGateway && hasRoutableIpv4)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsApipa(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == ApipaFirstOctet && bytes[1] == ApipaSecondOctet;
    }

    private static bool IsZero(IPAddress address) => address.GetAddressBytes().All(octet => octet == 0);
}
