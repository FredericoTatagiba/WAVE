using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Security;
using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>
/// Implementa a pseudológica da especificação como máquina de estados:
/// IDLE -> CONNECTING -> (TEST_RUNNING | FAILED). Coordena autorização, encerramento
/// de processos, criação de perfil, conexão, validação de DHCP, disparo das rotinas
/// de teste e registro de histórico. Cada etapa está isolada em um método próprio.
/// </summary>
public sealed class WifiTestOrchestrator : IWifiTestOrchestrator
{
    private readonly IAuthorizationService _authorization;
    private readonly ICurrentUserContext _currentUser;
    private readonly ICredentialStore _credentials;
    private readonly IWifiConnector _connector;
    private readonly IWifiProfileCatalog _catalog;
    private readonly IDhcpAddressValidator _dhcp;
    private readonly IVisiblePingTerminal _visiblePing;
    private readonly IContinuousPingMonitor _pingMonitor;
    private readonly ISpeedMeter _speedMeter;
    private readonly IStreamingProbe _streamingProbe;
    private readonly ITestRunRepository _history;
    private readonly IClock _clock;
    private readonly IAppLogger _logger;
    private readonly TestRunnerOptions _options;

    private readonly object _gate = new();
    private readonly List<PingSample> _samples = new();

    private int _running;
    private Guid _runId;
    private DateTimeOffset _startedAt;
    private string _ssid = string.Empty;
    private string _operatorName = string.Empty;
    private SpeedResult? _speed;
    private StreamingObservation? _streaming;

    public WifiTestOrchestrator(
        IAuthorizationService authorization,
        ICurrentUserContext currentUser,
        ICredentialStore credentials,
        IWifiConnector connector,
        IWifiProfileCatalog catalog,
        IDhcpAddressValidator dhcp,
        IVisiblePingTerminal visiblePing,
        IContinuousPingMonitor pingMonitor,
        ISpeedMeter speedMeter,
        IStreamingProbe streamingProbe,
        ITestRunRepository history,
        IClock clock,
        IAppLogger logger,
        TestRunnerOptions options)
    {
        _authorization = authorization;
        _currentUser = currentUser;
        _credentials = credentials;
        _connector = connector;
        _catalog = catalog;
        _dhcp = dhcp;
        _visiblePing = visiblePing;
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

    public string? ActiveSsid { get; private set; }

    public event EventHandler<TestStateChangedEventArgs>? StateChanged;

    public event EventHandler<PingSample>? PingSampled;

    public async Task<Result> RunTestAsync(WifiNetworkProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var authorization = _authorization.Authorize(Permission.RunTest);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return Result.Failure("Já existe um teste em execução.");
        }

        try
        {
            BeginSession(profile);
            SetState(TestOperationState.Connecting, profile.Ssid);

            // Reaproveita o perfil que o Windows já tem salvo: nesses casos não é
            // preciso recriar o perfil nem informar a senha novamente.
            var alreadyKnown = await _catalog.ExistsAsync(profile.Ssid, cancellationToken).ConfigureAwait(false);

            if (!alreadyKnown)
            {
                var secret = await ResolveCredentialAsync(profile, cancellationToken).ConfigureAwait(false);
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
            }

            var connected = await _connector.ConnectAsync(profile.Ssid, cancellationToken).ConfigureAwait(false);
            if (connected.IsFailure)
            {
                return await FailAsync(TestFailureReason.AuthenticationFailed, connected.Error).ConfigureAwait(false);
            }

            await Task.Delay(_options.StabilizationDelay, cancellationToken).ConfigureAwait(false);

            if (!await WaitForDhcpAsync(cancellationToken).ConfigureAwait(false))
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
            _logger.Error("Falha inesperada durante o teste de conectividade.", exception);
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
        _visiblePing.Close();
        await PersistRunAsync(TestOperationState.Idle, TestFailureReason.None).ConfigureAwait(false);
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

    private async Task StartValidationRoutinesAsync(string ssid, CancellationToken cancellationToken)
    {
        SetState(TestOperationState.TestRunning, ssid);

        _visiblePing.Launch(_options.PingTargetHost);
        _pingMonitor.Start(_options.PingTargetHost);

        // Mede vazão e estabilidade de streaming no próprio app (sem navegador) e
        // registra os números para auditoria. Falhas são toleradas: o teste segue e
        // o campo correspondente fica sem valor.
        await MeasureSpeedAsync(cancellationToken).ConfigureAwait(false);
        await MeasureStreamingAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task MeasureSpeedAsync(CancellationToken cancellationToken)
    {
        try
        {
            _speed = await _speedMeter.MeasureAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Warn($"Falha ao medir a vazão: {exception.Message}");
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
            _logger.Warn($"Falha ao sondar o streaming: {exception.Message}");
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

    private async Task<bool> WaitForDhcpAsync(CancellationToken cancellationToken)
    {
        var deadline = _clock.Now + _options.DhcpTimeout;

        while (_clock.Now < deadline)
        {
            if (await _dhcp.HasValidLeaseAsync(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            await Task.Delay(_options.DhcpPollInterval, cancellationToken).ConfigureAwait(false);
        }

        return await _dhcp.HasValidLeaseAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> FailAsync(TestFailureReason reason, string message)
    {
        await _pingMonitor.StopAsync().ConfigureAwait(false);
        _visiblePing.Close();
        await PersistRunAsync(TestOperationState.Failed, reason).ConfigureAwait(false);
        Interlocked.Exchange(ref _running, 0);
        SetState(TestOperationState.Failed, _ssid, reason, message);
        return Result.Failure(message);
    }

    private async Task ResetToIdleAsync()
    {
        await _pingMonitor.StopAsync().ConfigureAwait(false);
        _visiblePing.Close();
        Interlocked.Exchange(ref _running, 0);
        EndSession();
        SetState(TestOperationState.Idle, null);
    }

    private void OnPingSampled(object? sender, PingSample sample)
    {
        lock (_gate)
        {
            _samples.Add(sample);
        }

        PingSampled?.Invoke(this, sample);
    }

    private void BeginSession(WifiNetworkProfile profile)
    {
        _runId = Guid.NewGuid();
        _startedAt = _clock.Now;
        _ssid = profile.Ssid;
        _operatorName = _currentUser.UserName;
        _speed = null;
        _streaming = null;

        lock (_gate)
        {
            _samples.Clear();
        }
    }

    private void EndSession() => _runId = Guid.Empty;

    private async Task PersistRunAsync(TestOperationState finalState, TestFailureReason reason)
    {
        if (_runId == Guid.Empty)
        {
            return;
        }

        PingStatistics statistics;
        lock (_gate)
        {
            statistics = PingStatisticsCalculator.Calculate(_samples);
        }

        var run = new TestRun
        {
            Id = _runId,
            Ssid = _ssid,
            OperatorName = _operatorName,
            StartedAt = _startedAt,
            FinishedAt = _clock.Now,
            FinalState = finalState,
            FailureReason = reason,
            Ping = statistics,
            Speed = _speed,
            Streaming = _streaming
        };

        try
        {
            await _history.AddAsync(run).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Falha ao registrar o histórico da execução.", exception);
        }
    }

    private void SetState(
        TestOperationState state,
        string? ssid,
        TestFailureReason reason = TestFailureReason.None,
        string message = "")
    {
        CurrentState = state;
        ActiveSsid = ssid;
        StateChanged?.Invoke(this, new TestStateChangedEventArgs(state, ssid, reason, message));
    }
}
