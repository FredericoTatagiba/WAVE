using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>
/// Classifica a estabilidade do streaming a partir das amostras de vazão sustentada
/// e do bitrate-alvo (ex.: ~8 Mbps para 1080p). Lógica pura e testável: uma amostra
/// abaixo do alvo conta como "rebuffer" (risco de travar a qualidade).
/// </summary>
public static class StreamingStabilityEvaluator
{
    /// <summary>Acima deste percentual de amostras abaixo do alvo, considera-se instável.</summary>
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
