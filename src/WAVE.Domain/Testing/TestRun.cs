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

    /// <summary>
    /// Device the test ran on. WAVE records the machine rather than a person: on a shared
    /// tablet a per-operator login collapses into one account, and a name nobody can trust
    /// is worse in an audit than no name at all.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>required</c>. Runs recorded before this field existed carry an
    /// operator name instead, and a missing required property makes the deserializer throw
    /// for the whole file — losing every historical run, not just the field.
    /// </remarks>
    public string DeviceName { get; init; } = string.Empty;

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
