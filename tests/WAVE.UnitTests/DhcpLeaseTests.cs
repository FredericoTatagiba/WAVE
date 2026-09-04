using System.Net;
using WAVE.Infrastructure.Wifi;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// The predicate behind "did we get an IP?". APIPA is the case that matters: a host that
/// fails DHCP self-assigns a 169.254 address, so accepting one would report a working
/// connection where there is none.
/// </summary>
public class DhcpLeaseTests
{
    private static IPAddress[] Addresses(params string[] values) =>
        [.. values.Select(IPAddress.Parse)];

    [Fact]
    public void HasLease_GatewayAndRoutableAddress_IsValid() =>
        Assert.True(NetworkInterfaceDhcpValidator.HasLease(
            Addresses("172.16.10.254"), Addresses("172.16.10.124")));

    [Fact]
    public void HasLease_ApipaAddress_IsNotALease() =>
        // 169.254/16 is what the host assigns itself when no DHCP server answered.
        Assert.False(NetworkInterfaceDhcpValidator.HasLease(
            Addresses("172.16.10.254"), Addresses("169.254.13.7")));

    [Fact]
    public void HasLease_ZeroGateway_IsNotALease() =>
        // An adapter that is up but unrouted reports 0.0.0.0 rather than no gateway.
        Assert.False(NetworkInterfaceDhcpValidator.HasLease(
            Addresses("0.0.0.0"), Addresses("172.16.10.124")));

    [Fact]
    public void HasLease_NoGateway_IsNotALease() =>
        // Docker bridges look like this: a routable-looking address and nowhere to route.
        Assert.False(NetworkInterfaceDhcpValidator.HasLease([], Addresses("172.18.0.1")));

    [Fact]
    public void HasLease_LoopbackOnly_IsNotALease() =>
        Assert.False(NetworkInterfaceDhcpValidator.HasLease(
            Addresses("172.16.10.254"), Addresses("127.0.0.1")));

    [Fact]
    public void HasLease_IgnoresIpv6WhenDecidingIpv4() =>
        // An IPv6-only gateway does not make an IPv4 lease.
        Assert.False(NetworkInterfaceDhcpValidator.HasLease(
            Addresses("fe80::1"), Addresses("172.16.10.124")));

    [Fact]
    public void HasLease_PicksValidPairAmongSeveralAddresses() =>
        Assert.True(NetworkInterfaceDhcpValidator.HasLease(
            Addresses("0.0.0.0", "172.16.10.254"),
            Addresses("169.254.13.7", "172.16.10.124")));
}
