namespace WAVE.Domain.Testing;

/// <summary>Aggregated statistics for a sequence of pings.</summary>
public sealed record PingStatistics(
    int Sent,
    int Received,
    int Lost,
    double MinMs,
    double AvgMs,
    double MaxMs,
    double PacketLossPercent)
{
    public static PingStatistics Empty { get; } = new(0, 0, 0, 0d, 0d, 0d, 0d);
}
