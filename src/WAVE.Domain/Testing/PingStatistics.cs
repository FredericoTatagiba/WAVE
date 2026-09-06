namespace WAVE.Domain.Testing;

/// <summary>Aggregated statistics for a sequence of pings.</summary>
/// <param name="JitterMs">
/// Mean absolute difference between consecutive replies. A link parked at 20 ms and one
/// swinging between 5 and 35 ms share an average; only the second one stutters, and this
/// is the number that tells them apart.
/// </param>
/// <param name="P95Ms">
/// 95th percentile. Interactive traffic is judged by its worst moments, not its typical
/// ones, and an average hides exactly those.
/// </param>
/// <remarks>
/// The last two are nullable, and default to null, so a run recorded before they existed
/// reads as "not measured" instead of as a flawless zero — which is what a plain default
/// would have claimed, in the one place where a false clean bill of health is worst.
/// </remarks>
public sealed record PingStatistics(
    int Sent,
    int Received,
    int Lost,
    double MinMs,
    double AvgMs,
    double MaxMs,
    double PacketLossPercent,
    double? JitterMs = null,
    double? P95Ms = null)
{
    public static PingStatistics Empty { get; } = new(0, 0, 0, 0d, 0d, 0d, 0d);
}
