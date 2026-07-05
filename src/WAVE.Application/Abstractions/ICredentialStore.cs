using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Armazena/recupera segredos de rede de forma cifrada (DPAPI).</summary>
public interface ICredentialStore
{
    Task SaveAsync(string ssid, WifiSecret secret, CancellationToken cancellationToken = default);

    Task<WifiSecret?> GetAsync(string ssid, CancellationToken cancellationToken = default);

    Task DeleteAsync(string ssid, CancellationToken cancellationToken = default);
}
