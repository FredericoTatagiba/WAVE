using WAVE.Application.Testing;
using WAVE.Domain.Testing;
using Xunit;

namespace WAVE.UnitTests;

public class ThroughputCalculatorTests
{
    [Fact]
    public void ToMbps_ConvertsBytesAndTimeToMegabitsPerSecond()
    {
        // 12.5 MB em 1 s = 100 Mbps (12.5e6 * 8 / 1e6).
        var mbps = ThroughputCalculator.ToMbps(12_500_000, TimeSpan.FromSeconds(1));

        Assert.Equal(100, mbps, precision: 3);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1000, 0)]
    [InlineData(-5, 1)]
    public void ToMbps_InvalidInputs_ReturnZero(long bytes, double seconds)
    {
        Assert.Equal(0, ThroughputCalculator.ToMbps(bytes, TimeSpan.FromSeconds(seconds)));
    }
}

public class StreamingStabilityEvaluatorTests
{
    private static readonly DateTimeOffset When = DateTimeOffset.UnixEpoch;

    [Fact]
    public void Evaluate_NoSamples_IsUnknown()
    {
        var result = StreamingStabilityEvaluator.Evaluate(Array.Empty<double>(), 8, When);

        Assert.Equal(StreamingStability.Unknown, result.Stability);
        Assert.Equal(0, result.RebufferEvents);
    }

    [Fact]
    public void Evaluate_AllAboveTarget_IsSmooth()
    {
        var result = StreamingStabilityEvaluator.Evaluate(new double[] { 10, 12, 9, 20 }, 8, When);

        Assert.Equal(StreamingStability.Smooth, result.Stability);
        Assert.Equal(0, result.RebufferEvents);
    }

    [Fact]
    public void Evaluate_FewBelowTarget_IsMinorBuffering()
    {
        // 1 de 8 abaixo do alvo = 12,5% (<= 25%).
        var result = StreamingStabilityEvaluator.Evaluate(
            new double[] { 10, 10, 10, 10, 10, 10, 10, 2 }, 8, When);

        Assert.Equal(StreamingStability.MinorBuffering, result.Stability);
        Assert.Equal(1, result.RebufferEvents);
    }

    [Fact]
    public void Evaluate_ManyBelowTarget_IsUnstable()
    {
        // 2 de 4 abaixo do alvo = 50% (> 25%).
        var result = StreamingStabilityEvaluator.Evaluate(new double[] { 2, 10, 3, 10 }, 8, When);

        Assert.Equal(StreamingStability.Unstable, result.Stability);
        Assert.Equal(2, result.RebufferEvents);
    }
}
