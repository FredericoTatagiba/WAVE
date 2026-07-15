using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Stores/retrieves network secrets in encrypted form (DPAPI).</summary>
public interface ICredentialStore
{
    Task SaveAsync(string ssid, WifiSecret secret, CancellationToken cancellationToken = default);

    Task<WifiSecret?> GetAsync(string ssid, CancellationToken cancellationToken = default);

    Task DeleteAsync(string ssid, CancellationToken cancellationToken = default);
}
