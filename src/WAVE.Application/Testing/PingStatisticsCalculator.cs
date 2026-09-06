using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>Pure (and testable) computation of statistics from ping samples.</summary>
public static class PingStatisticsCalculator
{
    public static PingStatistics Calculate(IReadOnlyCollection<PingSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (samples.Count == 0)
        {
            return PingStatistics.Empty;
        }

        var latencies = samples.Where(s => s.Success).Select(s => s.LatencyMs).ToList();

        var sent = samples.Count;
        var received = latencies.Count;
        var lost = sent - received;
        var lossPercent = (double)lost / sent * 100d;

        return received == 0
            ? new PingStatistics(sent, 0, lost, 0d, 0d, 0d, lossPercent)
            : new PingStatistics(
                sent,
                received,
                lost,
                latencies.Min(),
                latencies.Average(),
                latencies.Max(),
                lossPercent,
                Jitter(latencies),
                Percentile(latencies, 95));
    }

    /// <summary>
    /// Mean absolute difference between consecutive replies (the IPDV of RFC 3393).
    /// </summary>
    /// <remarks>
    /// Computed over the replies in arrival order, so a lost packet joins the two samples
    /// around it into a single pair rather than breaking the series. With losses that
    /// slightly understates the disturbance, which is the safe direction: it never invents
    /// jitter that was not observed.
    /// </remarks>
    private static double? Jitter(IReadOnlyList<double> latencies)
    {
        if (latencies.Count < 2)
        {
            // A single reply offers no pair to compare; zero would claim a steadiness that
            // was never observed.
            return null;
        }

        var total = 0d;
        for (var index = 1; index < latencies.Count; index++)
        {
            total += Math.Abs(latencies[index] - latencies[index - 1]);
        }

        return total / (latencies.Count - 1);
    }

    /// <summary>
    /// Nearest-rank percentile: returns a latency that was actually measured, rather than
    /// interpolating a value that never occurred.
    /// </summary>
    private static double Percentile(IReadOnlyList<double> latencies, int percentile)
    {
        var ordered = latencies.OrderBy(latency => latency).ToList();
        var rank = (int)Math.Ceiling(percentile / 100d * ordered.Count);

        return ordered[Math.Clamp(rank - 1, 0, ordered.Count - 1)];
    }
}
