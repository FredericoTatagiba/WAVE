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
                lossPercent);
    }
}
