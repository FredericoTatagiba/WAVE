using WAVE.Domain.Common;
using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Integra com o subsistema Wi-Fi do Windows (netsh / Native Wi-Fi):
/// garante o perfil e realiza a associação.
/// </summary>
public interface IWifiConnector
{
    /// <summary>Cria o perfil da rede caso ainda não exista no Windows.</summary>
    Task<Result> EnsureProfileAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default);

    /// <summary>Solicita a conexão com o SSID informado.</summary>
    Task<Result> ConnectAsync(string ssid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the network profile from Windows. Used to roll back a just-created
    /// profile when the connection is not confirmed (e.g. wrong password), preventing
    /// an invalid credential from being remembered. Best-effort.
    /// </summary>
    Task RemoveProfileAsync(string ssid, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
