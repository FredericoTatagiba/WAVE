namespace WAVE.Application.Abstractions;

/// <summary>Validates whether the Wi-Fi adapter obtained a routable IP via DHCP.</summary>
public interface IDhcpAddressValidator
{
    Task<bool> HasValidLeaseAsync(CancellationToken cancellationToken = default);
}
