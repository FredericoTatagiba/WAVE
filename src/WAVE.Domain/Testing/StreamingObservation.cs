namespace WAVE.Domain.Testing;

/// <summary>Classificação de estabilidade do streaming de teste.</summary>
public enum StreamingStability
{
    Unknown,
    Smooth,
    MinorBuffering,
    Unstable
}

/// <summary>Observação de estabilidade do vídeo de teste (YouTube).</summary>
public readonly record struct StreamingObservation(
    StreamingStability Stability,
    int RebufferEvents,
    DateTimeOffset ObservedAt);
