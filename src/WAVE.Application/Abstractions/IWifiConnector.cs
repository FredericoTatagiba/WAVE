using WAVE.Domain.Common;
using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Integrates with the Windows Wi-Fi subsystem (netsh / Native Wi-Fi):
/// ensures the profile and performs the association.
/// </summary>
public interface IWifiConnector
{
    /// <summary>Creates the network profile if it does not yet exist in Windows.</summary>
    Task<Result> EnsureProfileAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default);

    /// <summary>Requests connection to the given SSID.</summary>
    Task<Result> ConnectAsync(string ssid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the network profile from Windows. Used to roll back a just-created
    /// profile when the connection is not confirmed (e.g. wrong password), preventing
    /// an invalid credential from being remembered. Best-effort.
    /// </summary>
    Task RemoveProfileAsync(string ssid, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
