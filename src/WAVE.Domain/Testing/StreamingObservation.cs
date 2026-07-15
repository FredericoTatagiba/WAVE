namespace WAVE.Domain.Testing;

/// <summary>Stability classification for the test streaming.</summary>
public enum StreamingStability
{
    Unknown,
    Smooth,
    MinorBuffering,
    Unstable
}

/// <summary>Stability observation for the test video (YouTube).</summary>
public readonly record struct StreamingObservation(
    StreamingStability Stability,
    int RebufferEvents,
    DateTimeOffset ObservedAt);
