using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Persistência dos perfis de rede configurados para teste.</summary>
public interface INetworkProfileRepository
{
    Task<IReadOnlyList<WifiNetworkProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WifiNetworkProfile?> FindAsync(string ssid, CancellationToken cancellationToken = default);

    Task SaveAsync(WifiNetworkProfile profile, CancellationToken cancellationToken = default);

    Task DeleteAsync(string ssid, CancellationToken cancellationToken = default);
}
