using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Scans the Wi-Fi networks currently visible.</summary>
public interface IWifiNetworkScanner
{
    Task<IReadOnlyList<AvailableNetwork>> ScanAsync(CancellationToken cancellationToken = default);
}
