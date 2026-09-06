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

    /// <summary>
    /// Host that was pinged. Recorded because the operator can change it per test, and a
    /// latency figure means nothing without knowing what answered it.
    /// </summary>
    public string PingTarget { get; init; } = string.Empty;

    /// <summary>Whole run: the baseline window and the loaded window together.</summary>
    public PingStatistics Ping { get; init; } = PingStatistics.Empty;

    /// <summary>Latency with the link at rest. Null for runs recorded before this split.</summary>
    public PingStatistics? PingIdle { get; init; }

    /// <summary>Latency while the throughput measurement saturated the link.</summary>
    public PingStatistics? PingUnderLoad { get; init; }

    /// <summary>
    /// How much the latency grew once the link was saturated — bufferbloat. The single
    /// most useful number for "will a call or a game survive someone downloading here":
    /// a link can show a fine average and still add hundreds of milliseconds under load.
    /// Null when a run has no baseline to compare against.
    /// </summary>
    public double? BufferbloatMs =>
        PingIdle is { Received: > 0 } idle && PingUnderLoad is { Received: > 0 } load
            ? load.AvgMs - idle.AvgMs
            : null;

    public SpeedResult? Speed { get; init; }

    public StreamingObservation? Streaming { get; init; }

    public bool Succeeded => FinalState == TestOperationState.TestRunning
                             && FailureReason == TestFailureReason.None;
}
