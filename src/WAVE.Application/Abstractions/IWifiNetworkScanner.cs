using WAVE.Domain.Networking;

namespace WAVE.Application.Abstractions;

/// <summary>Varre as redes Wi-Fi visíveis no momento.</summary>
public interface IWifiNetworkScanner
{
    Task<IReadOnlyList<AvailableNetwork>> ScanAsync(CancellationToken cancellationToken = default);
}
