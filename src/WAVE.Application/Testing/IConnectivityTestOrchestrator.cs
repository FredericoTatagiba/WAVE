using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>
/// Orchestrates the lifecycle of a connectivity test (state machine), over Wi-Fi or over
/// the cable. The UI depends on this abstraction, not on the implementation.
/// </summary>
public interface IConnectivityTestOrchestrator
{
    TestOperationState CurrentState { get; }

    /// <summary>SSID or wired adapter currently under test; null once idle.</summary>
    string? ActiveTarget { get; }

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
    /// <param name="pingTarget">
    /// Host to ping during this run; blank falls back to the configured default. It is
    /// per-run because switching the target is a diagnostic move — comparing the gateway
    /// against a public address separates "my link" from "upstream".
    /// </param>
    Task<Result> RunWifiTestAsync(
        WifiNetworkProfile profile,
        WifiSecret? providedSecret = null,
        string? pingTarget = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the validation flow over the wired adapter. There is nothing to associate
    /// with and no credential involved: the test confirms the link and the DHCP lease,
    /// then fires the same ping, throughput and streaming routines as Wi-Fi.
    /// </summary>
    Task<Result> RunWiredTestAsync(string? pingTarget = null, CancellationToken cancellationToken = default);

    /// <summary>Stops the running test and returns to the idle state.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Acknowledges a failure (after the alert) and returns to the idle state.</summary>
    void AcknowledgeFailure();
}
