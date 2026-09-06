using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>
/// Implements the specification's pseudo-logic as a state machine:
/// IDLE -> CONNECTING -> (TEST_RUNNING | FAILED). Coordinates authorization, process
/// termination, profile creation, connection, DHCP validation, firing the validation
/// routines and recording history. Each step is isolated in its own method.
/// </summary>
/// <remarks>
/// Wi-Fi and Ethernet differ only in how the link is established: Wi-Fi has to create a
/// profile and associate, the cable only has to be plugged in. Everything after the lease
/// — ping, throughput, streaming, history — is shared, so both paths converge on
/// <see cref="StartValidationRoutinesAsync"/>.
/// </remarks>
public sealed class ConnectivityTestOrchestrator : IConnectivityTestOrchestrator
{
    /// <summary>Label recorded when no wired adapter was found at all.</summary>
    private const string UnknownWiredTarget = "Cabo de rede";

    private readonly IDeviceIdentity _device;
    private readonly ICredentialStore _credentials;
    private readonly IWifiConnector _connector;
    private readonly IWifiProfileCatalog _catalog;
    private readonly IDhcpAddressValidator _dhcp;
    private readonly IEthernetLinkProbe _ethernet;
    private readonly IContinuousPingMonitor _pingMonitor;
    private readonly ISpeedMeter _speedMeter;
    private readonly IStreamingProbe _streamingProbe;
    private readonly ITestRunRepository _history;
    private readonly IClock _clock;
    private readonly IAppLogger _logger;
    private readonly TestRunnerOptions _options;

    private readonly object _gate = new();

    // Kept apart rather than tagged after the fact: the phase is known when the sample
    // arrives, and reconstructing it later from timestamps would be guesswork.
    private readonly List<PingSample> _idleSamples = new();
    private readonly List<PingSample> _loadSamples = new();

    private bool _underLoad;

    private int _running;
    private bool _profileCreatedThisRun;
    private Guid _runId;
    private DateTimeOffset _startedAt;
    private string _target = string.Empty;
    private string _pingTarget = string.Empty;
    private TestMedium _medium = TestMedium.WiFi;
    private SpeedResult? _speed;
    private StreamingObservation? _streaming;

    public ConnectivityTestOrchestrator(
        IDeviceIdentity device,
        ICredentialStore credentials,
        IWifiConnector connector,
        IWifiProfileCatalog catalog,
        IDhcpAddressValidator dhcp,
        IEthernetLinkProbe ethernet,
        IContinuousPingMonitor pingMonitor,
        ISpeedMeter speedMeter,
        IStreamingProbe streamingProbe,
        ITestRunRepository history,
        IClock clock,
        IAppLogger logger,
        TestRunnerOptions options)
    {
        _device = device;
        _credentials = credentials;
        _connector = connector;
        _catalog = catalog;
        _dhcp = dhcp;
        _ethernet = ethernet;
        _pingMonitor = pingMonitor;
        _speedMeter = speedMeter;
        _streamingProbe = streamingProbe;
        _history = history;
        _clock = clock;
        _logger = logger;
        _options = options;

        _pingMonitor.Sampled += OnPingSampled;
    }

    public TestOperationState CurrentState { get; private set; } = TestOperationState.Idle;

    public string? ActiveTarget { get; private set; }

    public event EventHandler<TestStateChangedEventArgs>? StateChanged;

    public event EventHandler<PingSample>? PingSampled;

    public event EventHandler<SpeedSample>? SpeedSampled;

    public async Task<Result> RunWifiTestAsync(
        WifiNetworkProfile profile,
        WifiSecret? providedSecret = null,
        string? pingTarget = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var start = TryStart();
        if (start.IsFailure)
        {
            return start;
        }

        try
        {
            BeginSession(profile.Ssid, TestMedium.WiFi, pingTarget);
            SetState(TestOperationState.Connecting, profile.Ssid);

            // Reuses the profile Windows already has saved: in those cases there is no
            // need to recreate the profile or enter the password again.
            var alreadyKnown = await _catalog.ExistsAsync(profile.Ssid, cancellationToken).ConfigureAwait(false);

            if (!alreadyKnown)
            {
                // Prefer the credential the operator just typed (used only for this run);
                // fall back to a previously remembered one when there is no fresh input.
                var secret = providedSecret ?? await ResolveCredentialAsync(profile, cancellationToken).ConfigureAwait(false);
                if (profile.RequiresCredential && secret is null)
                {
                    return await FailAsync(TestFailureReason.MissingCredential,
                        "Rede protegida ainda não conhecida pelo Windows. Cadastre a senha uma vez (admin).")
                        .ConfigureAwait(false);
                }

                var ensured = await _connector.EnsureProfileAsync(profile, secret, cancellationToken).ConfigureAwait(false);
                if (ensured.IsFailure)
                {
                    return await FailAsync(TestFailureReason.ProfileCreationFailed, ensured.Error).ConfigureAwait(false);
                }

                // We created the Windows profile this run; if the connection is not
                // confirmed, FailAsync rolls it back so a bad credential isn't kept.
                _profileCreatedThisRun = true;
            }

            var connected = await _connector.ConnectAsync(profile.Ssid, cancellationToken).ConfigureAwait(false);
            if (connected.IsFailure)
            {
                return await FailAsync(TestFailureReason.AuthenticationFailed, connected.Error).ConfigureAwait(false);
            }

            await Task.Delay(_options.StabilizationDelay, cancellationToken).ConfigureAwait(false);

            if (!await WaitForLeaseAsync(_dhcp.HasValidLeaseAsync, cancellationToken).ConfigureAwait(false))
            {
                return await FailAsync(TestFailureReason.DhcpTimeout,
                    "Timeout ao obter endereço IP via DHCP.").ConfigureAwait(false);
            }

            await StartValidationRoutinesAsync(profile.Ssid, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            await ResetToIdleAsync().ConfigureAwait(false);
            return Result.Failure("Operação cancelada.");
        }
        catch (Exception exception)
        {
            _logger.Error("Unexpected failure during the connectivity test.", exception);
            return await FailAsync(TestFailureReason.Unexpected,
                "Erro inesperado ao executar o teste.").ConfigureAwait(false);
        }
    }

    public async Task<Result> RunWiredTestAsync(
        string? pingTarget = null, CancellationToken cancellationToken = default)
    {
        var start = TryStart();
        if (start.IsFailure)
        {
            return start;
        }

        try
        {
            var link = await _ethernet.DetectAsync(cancellationToken).ConfigureAwait(false);

            BeginSession(link?.InterfaceName ?? UnknownWiredTarget, TestMedium.Ethernet, pingTarget);
            SetState(TestOperationState.Connecting, _target);

            if (link is null)
            {
                return await FailAsync(TestFailureReason.NoLink,
                    "Nenhum adaptador de rede cabeada foi encontrado nesta máquina.").ConfigureAwait(false);
            }

            if (!link.IsUp)
            {
                return await FailAsync(TestFailureReason.NoLink,
                    $"Sem link em '{link.Description}'. Verifique se o cabo está conectado.").ConfigureAwait(false);
            }

            if (!await WaitForLeaseAsync(WiredHasLeaseAsync, cancellationToken).ConfigureAwait(false))
            {
                return await FailAsync(TestFailureReason.DhcpTimeout,
                    "Timeout ao obter endereço IP via DHCP na rede cabeada.").ConfigureAwait(false);
            }

            await StartValidationRoutinesAsync(_target, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            await ResetToIdleAsync().ConfigureAwait(false);
            return Result.Failure("Operação cancelada.");
        }
        catch (Exception exception)
        {
            _logger.Error("Unexpected failure during the wired connectivity test.", exception);
            return await FailAsync(TestFailureReason.Unexpected,
                "Erro inesperado ao executar o teste.").ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _running, 0) == 0 && CurrentState == TestOperationState.Idle)
        {
            return;
        }

        await _pingMonitor.StopAsync().ConfigureAwait(false);

        // Records the phase the run actually reached, not the idle state it is going to.
        // Stopping a test that got as far as TEST_RUNNING means it completed and its
        // measurements are valid; filing that as anything else would make every finished
        // test read as a failure in the history.
        await PersistRunAsync(CurrentState, TestFailureReason.None).ConfigureAwait(false);
        EndSession();
        SetState(TestOperationState.Idle, null);
    }

    public void AcknowledgeFailure()
    {
        if (CurrentState == TestOperationState.Failed)
        {
            SetState(TestOperationState.Idle, null);
        }
    }

    /// <summary>
    /// Claims the single-run slot. Both entry points share it so a wired test cannot start
    /// on top of a Wi-Fi one.
    /// </summary>
    private Result TryStart() =>
        Interlocked.CompareExchange(ref _running, 1, 0) != 0
            ? Result.Failure("Já existe um teste em execução.")
            : Result.Success();

    private async Task StartValidationRoutinesAsync(string target, CancellationToken cancellationToken)
    {
        SetState(TestOperationState.TestRunning, target);

        _pingMonitor.Start(_pingTarget);

        // Baseline first, with nothing else on the link. Without this window every sample
        // would be taken while the throughput measurement saturates the connection, and the
        // loaded latency would have nothing to be compared against.
        await Task.Delay(_options.IdleBaselineDuration, cancellationToken).ConfigureAwait(false);

        // Measures throughput and streaming stability in the app itself (no browser) and
        // records the numbers for auditing. Failures are tolerated: the test continues and
        // the corresponding field is left without a value. The ping keeps running here on
        // purpose — latency under saturation is the point.
        SetUnderLoad(true);
        try
        {
            await MeasureSpeedAsync(cancellationToken).ConfigureAwait(false);
            await MeasureStreamingAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SetUnderLoad(false);
        }
    }

    private void SetUnderLoad(bool underLoad)
    {
        lock (_gate)
        {
            _underLoad = underLoad;
        }
    }

    private async Task MeasureSpeedAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Relays each live reading to subscribers (UI gauge) as an Observer event.
            var progress = new Progress<SpeedSample>(sample => SpeedSampled?.Invoke(this, sample));
            _speed = await _speedMeter.MeasureAsync(progress, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Warn($"Failed to measure throughput: {exception.Message}");
        }
    }

    private async Task MeasureStreamingAsync(CancellationToken cancellationToken)
    {
        try
        {
            var samples = await _streamingProbe.SampleAsync(cancellationToken).ConfigureAwait(false);
            _streaming = StreamingStabilityEvaluator.Evaluate(
                samples, _options.StreamingTargetMbps, _clock.Now);
        }
        catch (Exception exception)
        {
            _logger.Warn($"Failed to probe the streaming: {exception.Message}");
        }
    }

    private async Task<WifiSecret?> ResolveCredentialAsync(WifiNetworkProfile profile, CancellationToken cancellationToken)
    {
        if (!profile.RequiresCredential)
        {
            return null;
        }

        return await _credentials.GetAsync(profile.Ssid, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> WiredHasLeaseAsync(CancellationToken cancellationToken)
    {
        var link = await _ethernet.DetectAsync(cancellationToken).ConfigureAwait(false);
        return link is { IsUp: true, HasDhcpLease: true };
    }

    /// <summary>
    /// Polls until the medium reports a usable address or the DHCP budget runs out. The
    /// final check after the deadline keeps a lease that landed during the last sleep.
    /// </summary>
    private async Task<bool> WaitForLeaseAsync(
        Func<CancellationToken, Task<bool>> hasLease, CancellationToken cancellationToken)
    {
        var deadline = _clock.Now + _options.DhcpTimeout;

        while (_clock.Now < deadline)
        {
            if (await hasLease(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            await Task.Delay(_options.DhcpPollInterval, cancellationToken).ConfigureAwait(false);
        }

        return await hasLease(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> FailAsync(TestFailureReason reason, string message)
    {
        await _pingMonitor.StopAsync().ConfigureAwait(false);
        await RollbackProfileIfCreatedAsync().ConfigureAwait(false);
        await PersistRunAsync(TestOperationState.Failed, reason).ConfigureAwait(false);
        Interlocked.Exchange(ref _running, 0);
        SetState(TestOperationState.Failed, _target, reason, message);
        return Result.Failure(message);
    }

    /// <summary>
    /// Rolls back the Windows profile created during this run when the connection is
    /// not confirmed (e.g. wrong password), so an invalid credential is not remembered
    /// and the network keeps asking for the password on the next attempt. Best-effort.
    /// </summary>
    private async Task RollbackProfileIfCreatedAsync()
    {
        if (!_profileCreatedThisRun)
        {
            return;
        }

        _profileCreatedThisRun = false;

        try
        {
            await _connector.RemoveProfileAsync(_target).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Warn($"Could not roll back the network profile '{_target}': {exception.Message}");
        }
    }

    private async Task ResetToIdleAsync()
    {
        await _pingMonitor.StopAsync().ConfigureAwait(false);
        Interlocked.Exchange(ref _running, 0);
        EndSession();
        SetState(TestOperationState.Idle, null);
    }

    private void OnPingSampled(object? sender, PingSample sample)
    {
        lock (_gate)
        {
            (_underLoad ? _loadSamples : _idleSamples).Add(sample);
        }

        PingSampled?.Invoke(this, sample);
    }

    private void BeginSession(string target, TestMedium medium, string? pingTarget)
    {
        _runId = Guid.NewGuid();
        _startedAt = _clock.Now;
        _target = target;
        _medium = medium;
        _pingTarget = string.IsNullOrWhiteSpace(pingTarget) ? _options.PingTargetHost : pingTarget.Trim();
        _profileCreatedThisRun = false;
        _speed = null;
        _streaming = null;

        lock (_gate)
        {
            _underLoad = false;
            _idleSamples.Clear();
            _loadSamples.Clear();
        }
    }

    private void EndSession() => _runId = Guid.Empty;

    private async Task PersistRunAsync(TestOperationState finalState, TestFailureReason reason)
    {
        if (_runId == Guid.Empty)
        {
            return;
        }

        PingStatistics overall;
        PingStatistics idle;
        PingStatistics underLoad;
        lock (_gate)
        {
            idle = PingStatisticsCalculator.Calculate(_idleSamples);
            underLoad = PingStatisticsCalculator.Calculate(_loadSamples);
            overall = PingStatisticsCalculator.Calculate([.. _idleSamples, .. _loadSamples]);
        }

        var run = new TestRun
        {
            Id = _runId,
            Ssid = _target,
            Medium = _medium,
            DeviceName = _device.Name,
            StartedAt = _startedAt,
            FinishedAt = _clock.Now,
            FinalState = finalState,
            FailureReason = reason,
            PingTarget = _pingTarget,
            Ping = overall,
            PingIdle = idle.Sent > 0 ? idle : null,
            PingUnderLoad = underLoad.Sent > 0 ? underLoad : null,
            Speed = _speed,
            Streaming = _streaming
        };

        try
        {
            await _history.AddAsync(run).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to record the run history.", exception);
        }
    }

    private void SetState(
        TestOperationState state,
        string? target,
        TestFailureReason reason = TestFailureReason.None,
        string message = "")
    {
        CurrentState = state;
        ActiveTarget = target;
        StateChanged?.Invoke(this, new TestStateChangedEventArgs(state, target, _medium, reason, message));
    }
}
