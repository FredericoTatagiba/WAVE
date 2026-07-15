namespace WAVE.Domain.Testing;

/// <summary>A single ping sample. <see cref="LatencyMs"/> is only valid when <see cref="Success"/>.</summary>
public readonly record struct PingSample(DateTimeOffset Timestamp, double LatencyMs, bool Success)
{
    public static PingSample Reply(DateTimeOffset at, double latencyMs) => new(at, latencyMs, true);

    public static PingSample Timeout(DateTimeOffset at) => new(at, 0d, false);
}
