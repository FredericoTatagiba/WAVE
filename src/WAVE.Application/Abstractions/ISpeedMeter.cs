using WAVE.Domain.Testing;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Measures the connection throughput (download and, optionally, upload) directly
/// in the app, without depending on the browser. The implementation performs the HTTP
/// transfer and converts to Mbps; it throws on network failure (the orchestrator handles it and continues).
/// While the transfer runs it may report intermediate readings through
/// <paramref name="progress"/> so the UI can animate a fast.com-style gauge.
/// </summary>
public interface ISpeedMeter
{
    Task<SpeedResult> MeasureAsync(
        IProgress<SpeedSample>? progress = null, CancellationToken cancellationToken = default);
}
