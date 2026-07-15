using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>
/// Classifies streaming stability from the sustained-throughput samples and the
/// target bitrate (e.g. ~8 Mbps for 1080p). Pure, testable logic: a sample below
/// the target counts as a "rebuffer" (risk of quality stalling).
/// </summary>
public static class StreamingStabilityEvaluator
{
    /// <summary>Above this ratio of samples below the target, it is considered unstable.</summary>
    public const double UnstableRatioThreshold = 0.25;

    public static StreamingObservation Evaluate(
        IReadOnlyList<double> mbpsSamples, double targetMbps, DateTimeOffset observedAt)
    {
        if (mbpsSamples is null || mbpsSamples.Count == 0)
        {
            return new StreamingObservation(StreamingStability.Unknown, 0, observedAt);
        }

        var rebuffers = mbpsSamples.Count(sample => sample < targetMbps);
        var ratio = (double)rebuffers / mbpsSamples.Count;

        var stability = rebuffers == 0
            ? StreamingStability.Smooth
            : ratio <= UnstableRatioThreshold
                ? StreamingStability.MinorBuffering
                : StreamingStability.Unstable;

        return new StreamingObservation(stability, rebuffers, observedAt);
    }
}
