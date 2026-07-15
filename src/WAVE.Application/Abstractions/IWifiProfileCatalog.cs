namespace WAVE.Application.Abstractions;

/// <summary>
/// Queries the network profiles Windows already has saved. When a profile
/// exists, it is possible to connect without entering the password again.
/// </summary>
public interface IWifiProfileCatalog
{
    Task<IReadOnlyList<string>> GetSavedProfileNamesAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string ssid, CancellationToken cancellationToken = default);
}
