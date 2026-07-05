using WAVE.Application.Testing;
using WAVE.Domain.Testing;
using Xunit;

namespace WAVE.UnitTests;

public class PingStatisticsCalculatorTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.UnixEpoch;

    [Fact]
    public void Calculate_WithNoSamples_ReturnsEmpty()
    {
        var statistics = PingStatisticsCalculator.Calculate(Array.Empty<PingSample>());

        Assert.Same(PingStatistics.Empty, statistics);
    }

    [Fact]
    public void Calculate_WithAllSuccess_ComputesMinAverageMax()
    {
        var samples = new[]
        {
            PingSample.Reply(At, 10),
            PingSample.Reply(At, 20),
            PingSample.Reply(At, 30)
        };

        var statistics = PingStatisticsCalculator.Calculate(samples);

        Assert.Equal(3, statistics.Sent);
        Assert.Equal(3, statistics.Received);
        Assert.Equal(0, statistics.Lost);
        Assert.Equal(10, statistics.MinMs);
        Assert.Equal(20, statistics.AvgMs);
        Assert.Equal(30, statistics.MaxMs);
        Assert.Equal(0, statistics.PacketLossPercent);
    }

    [Fact]
    public void Calculate_WithLosses_ComputesLossPercentAndIgnoresTimeoutsInLatency()
    {
        var samples = new[]
        {
            PingSample.Reply(At, 10),
            PingSample.Timeout(At),
            PingSample.Reply(At, 30),
            PingSample.Timeout(At)
        };

        var statistics = PingStatisticsCalculator.Calculate(samples);

        Assert.Equal(4, statistics.Sent);
        Assert.Equal(2, statistics.Received);
        Assert.Equal(2, statistics.Lost);
        Assert.Equal(50, statistics.PacketLossPercent);
        Assert.Equal(10, statistics.MinMs);
        Assert.Equal(20, statistics.AvgMs);
        Assert.Equal(30, statistics.MaxMs);
    }

    [Fact]
    public void Calculate_WithAllTimeouts_Reports100PercentLoss()
    {
        var samples = new[]
        {
            PingSample.Timeout(At),
            PingSample.Timeout(At)
        };

        var statistics = PingStatisticsCalculator.Calculate(samples);

        Assert.Equal(2, statistics.Sent);
        Assert.Equal(0, statistics.Received);
        Assert.Equal(100, statistics.PacketLossPercent);
        Assert.Equal(0, statistics.AvgMs);
    }
}
