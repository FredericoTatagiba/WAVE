using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Reads the current state of the machine's wired adapter.</summary>
public interface IEthernetLinkProbe
{
    /// <summary>
    /// The wired adapter to test over, or null when the machine has none. Called
    /// repeatedly while waiting for a lease, so it must reflect the live adapter state.
    /// </summary>
    Task<EthernetLink?> DetectAsync(CancellationToken cancellationToken = default);
}
