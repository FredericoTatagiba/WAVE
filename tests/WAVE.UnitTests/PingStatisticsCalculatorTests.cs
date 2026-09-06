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
    public void Jitter_SeparatesSteadyFromSwingingLinks()
    {
        // The point of the metric: identical average, opposite behaviour. A game or a call
        // rides the steady one fine and stutters on the other.
        var steady = new[]
        {
            PingSample.Reply(At, 20), PingSample.Reply(At, 20),
            PingSample.Reply(At, 20), PingSample.Reply(At, 20)
        };
        var swinging = new[]
        {
            PingSample.Reply(At, 5), PingSample.Reply(At, 35),
            PingSample.Reply(At, 5), PingSample.Reply(At, 35)
        };

        var steadyStats = PingStatisticsCalculator.Calculate(steady);
        var swingingStats = PingStatisticsCalculator.Calculate(swinging);

        Assert.Equal(swingingStats.AvgMs, steadyStats.AvgMs);
        Assert.Equal(0, steadyStats.JitterMs);
        Assert.Equal(30, swingingStats.JitterMs);
    }

    [Fact]
    public void Jitter_WithASingleReply_IsUnknownRatherThanZero()
    {
        // One reply gives no pair to compare, and reporting zero would claim a steadiness
        // that was never observed.
        var statistics = PingStatisticsCalculator.Calculate(new[] { PingSample.Reply(At, 42) });

        Assert.Null(statistics.JitterMs);
        Assert.Equal(42, statistics.P95Ms);
    }

    [Fact]
    public void Calculate_WithNoReplies_LeavesSteadinessUnknown()
    {
        var statistics = PingStatisticsCalculator.Calculate(
            new[] { PingSample.Timeout(At), PingSample.Timeout(At) });

        Assert.Null(statistics.JitterMs);
        Assert.Null(statistics.P95Ms);
    }

    [Fact]
    public void P95_IsNotMovedByASingleOutlierInTwenty()
    {
        // Nineteen good samples and one terrible one. The average is dragged to 39 ms by
        // that single spike — a figure no packet actually experienced — while p95 keeps
        // reporting what the connection does almost all of the time. The spike is still
        // visible, as the maximum.
        var samples = Enumerable.Repeat(PingSample.Reply(At, 20), 19)
            .Append(PingSample.Reply(At, 400))
            .ToArray();

        var statistics = PingStatisticsCalculator.Calculate(samples);

        Assert.Equal(20, statistics.P95Ms);
        Assert.Equal(400, statistics.MaxMs);
        Assert.Equal(39, statistics.AvgMs);
    }

    [Fact]
    public void P95_RisesOnceTheTailIsNoLongerASingleSpike()
    {
        // Two bad samples in twenty is 10% of the traffic, and now the tail is the story.
        var samples = Enumerable.Repeat(PingSample.Reply(At, 20), 18)
            .Concat(Enumerable.Repeat(PingSample.Reply(At, 400), 2))
            .ToArray();

        var statistics = PingStatisticsCalculator.Calculate(samples);

        Assert.Equal(400, statistics.P95Ms);
    }

    [Fact]
    public void P95_ReturnsAMeasuredValue_NotAnInterpolatedOne()
    {
        var samples = new[]
        {
            PingSample.Reply(At, 10), PingSample.Reply(At, 20), PingSample.Reply(At, 30)
        };

        var statistics = PingStatisticsCalculator.Calculate(samples);

        Assert.Equal(30, statistics.P95Ms);
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
