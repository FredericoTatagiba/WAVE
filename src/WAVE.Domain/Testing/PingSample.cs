namespace WAVE.Domain.Testing;

/// <summary>Uma amostra de ping. <see cref="LatencyMs"/> só é válida quando <see cref="Success"/>.</summary>
public readonly record struct PingSample(DateTimeOffset Timestamp, double LatencyMs, bool Success)
{
    public static PingSample Reply(DateTimeOffset at, double latencyMs) => new(at, latencyMs, true);

    public static PingSample Timeout(DateTimeOffset at) => new(at, 0d, false);
}
