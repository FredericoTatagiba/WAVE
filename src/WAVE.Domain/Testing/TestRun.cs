namespace WAVE.Domain.Testing;

/// <summary>
/// Immutable record of a test run, for history and auditing (a Core Rules
/// requirement). Serializable for JSON persistence.
/// </summary>
public sealed record TestRun
{
    public required Guid Id { get; init; }

    public required string Ssid { get; init; }

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
