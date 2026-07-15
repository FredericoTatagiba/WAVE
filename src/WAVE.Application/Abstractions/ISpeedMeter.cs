using WAVE.Domain.Testing;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Measures the connection throughput (download and, optionally, upload) directly
/// in the app, without depending on the browser. The implementation performs the HTTP
/// transfer and converts to Mbps; it throws on network failure (the orchestrator handles it and continues).
/// </summary>
public interface ISpeedMeter
{
    Task<SpeedResult> MeasureAsync(CancellationToken cancellationToken = default);
}
