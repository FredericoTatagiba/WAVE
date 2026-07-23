using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>
/// Orchestrates the lifecycle of a connectivity test (state machine).
/// The UI depends on this abstraction, not on the implementation.
/// </summary>
public interface IWifiTestOrchestrator
{
    TestOperationState CurrentState { get; }

    string? ActiveSsid { get; }

    event EventHandler<TestStateChangedEventArgs>? StateChanged;

    event EventHandler<PingSample>? PingSampled;

    /// <summary>Live throughput readings emitted during the speed measurement (fast.com-style).</summary>
    event EventHandler<SpeedSample>? SpeedSampled;

    /// <summary>
    /// Runs the connect + validation flow for the given network. A credential just
    /// entered by the operator can be passed in <paramref name="providedSecret"/> for
    /// use during this test only; it must be remembered by the caller only after a
    /// confirmed success.
    /// </summary>
    Task<Result> RunTestAsync(
        WifiNetworkProfile profile,
        WifiSecret? providedSecret = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stops the running test and returns to the idle state.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Acknowledges a failure (after the alert) and returns to the idle state.</summary>
    void AcknowledgeFailure();
}
