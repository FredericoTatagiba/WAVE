namespace WAVE.Application.Abstractions;

/// <summary>Valida se o adaptador Wi-Fi obteve um IP roteável via DHCP.</summary>
public interface IDhcpAddressValidator
{
    Task<bool> HasValidLeaseAsync(CancellationToken cancellationToken = default);
}
