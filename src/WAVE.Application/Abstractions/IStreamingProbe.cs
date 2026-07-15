namespace WAVE.Application.Abstractions;

/// <summary>
/// Streaming probe: downloads a sustained stream for a period and returns the
/// throughput (Mbps) measured in each interval. The stability classification is done by
/// <see cref="Testing.StreamingStabilityEvaluator"/> (pure logic), keeping this
/// abstraction restricted to IO.
/// </summary>
public interface IStreamingProbe
{
    Task<IReadOnlyList<double>> SampleAsync(CancellationToken cancellationToken = default);
}
