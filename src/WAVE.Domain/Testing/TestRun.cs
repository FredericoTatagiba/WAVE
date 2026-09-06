namespace WAVE.Domain.Testing;

/// <summary>
/// Immutable record of a test run, for history and auditing (a Core Rules
/// requirement). Serializable for JSON persistence.
/// </summary>
public sealed record TestRun
{
    public required Guid Id { get; init; }

    /// <summary>SSID of the tested network, or the adapter name for a wired run.</summary>
    public required string Ssid { get; init; }

    /// <summary>
    /// Defaults to Wi-Fi so runs recorded before wired testing existed still deserialize.
    /// </summary>
    public TestMedium Medium { get; init; } = TestMedium.WiFi;

    public required string OperatorName { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public TestOperationState FinalState { get; init; } = TestOperationState.Idle;

    public TestFailureReason FailureReason { get; init; } = TestFailureReason.None;

    public PingStatistics Ping { get; init; } = PingStatistics.Empty;

    public SpeedResult? Speed { get; init; }

    public StreamingObservation? Streaming { get; init; }

    public bool Succeeded => FinalState == TestOperationState.TestRunning
                             && FailureReason == TestFailureReason.None;
}
