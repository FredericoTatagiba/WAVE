using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WAVE.App.Services;
using WAVE.Application.Abstractions;
using WAVE.Application.Discovery;
using WAVE.Application.History;
using WAVE.Application.Profiles;
using WAVE.Application.Testing;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>Main ViewModel: coordinates the network list, telemetry and history.</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IConnectivityTestOrchestrator _orchestrator;
    private readonly NetworkProfileService _profiles;
    private readonly NetworkDiscoveryService _discovery;
    private readonly IEthernetLinkProbe _ethernet;
    private readonly ITestRunRepository _history;
    private readonly IAdminGate _adminGate;
    private readonly IUserAlerts _alerts;
    private readonly IAppLogger _logger;
    private readonly TestRunnerOptions _options;
    private readonly ICredentialStore _credentialStore;
    private readonly ICredentialPrompt _credentialPrompt;
    private readonly HistoryExportService _exportService;
    private readonly IExportFileDialog _exportDialog;

    private readonly List<TestRun> _allRuns = new();

    private string _statusMessage = string.Empty;
    private TestOperationState _state = TestOperationState.Idle;
    private bool _isBusy;
    private DateTime? _filterFrom;
    private DateTime? _filterTo;
    private string _filterSsid = string.Empty;
    private WiredButtonViewModel _wired;

    public MainViewModel(
        IConnectivityTestOrchestrator orchestrator,
        NetworkProfileService profiles,
        NetworkDiscoveryService discovery,
        IEthernetLinkProbe ethernet,
        ITestRunRepository history,
        IAdminGate adminGate,
        IUserAlerts alerts,
        IAppLogger logger,
        TestRunnerOptions options,
        ICredentialStore credentialStore,
        ICredentialPrompt credentialPrompt,
        HistoryExportService exportService,
        IExportFileDialog exportDialog)
    {
        _orchestrator = orchestrator;
        _profiles = profiles;
        _discovery = discovery;
        _ethernet = ethernet;
        _history = history;
        _adminGate = adminGate;
        _alerts = alerts;
        _logger = logger;
        _options = options;
        _credentialStore = credentialStore;
        _credentialPrompt = credentialPrompt;
        _exportService = exportService;
        _exportDialog = exportDialog;

        Telemetry = new TelemetryViewModel();
        _wired = new WiredButtonViewModel(null, RunWiredAsync);

        StopCommand = new AsyncRelayCommand(
            StopAsync,
            () => State is TestOperationState.Connecting or TestOperationState.TestRunning);
        ScanCommand = new AsyncRelayCommand(LoadNetworksAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        ClearFilterCommand = new RelayCommand(ClearFilter);

        _orchestrator.StateChanged += OnStateChanged;
        _orchestrator.PingSampled += OnPingSampled;
        _orchestrator.SpeedSampled += OnSpeedSampled;
    }

    public ObservableCollection<NetworkButtonViewModel> Networks { get; } = new();

    /// <summary>
    /// The wired target. Replaced on every scan rather than mutated: its labels describe
    /// a link snapshot, so a new snapshot is a new button.
    /// </summary>
    public WiredButtonViewModel Wired
    {
        get => _wired;
        private set => SetProperty(ref _wired, value);
    }

    public ObservableCollection<TestRunViewModel> History { get; } = new();

    public TelemetryViewModel Telemetry { get; }

    /// <summary>Start of the history date filter (inclusive), or null for no lower bound.</summary>
    public DateTime? FilterFrom
    {
        get => _filterFrom;
        set
        {
            if (SetProperty(ref _filterFrom, value))
            {
                RefreshHistoryView();
            }
        }
    }

    /// <summary>End of the history date filter (inclusive), or null for no upper bound.</summary>
    public DateTime? FilterTo
    {
        get => _filterTo;
        set
        {
            if (SetProperty(ref _filterTo, value))
            {
                RefreshHistoryView();
            }
        }
    }

    /// <summary>SSID substring filter (case-insensitive); empty means no SSID filter.</summary>
    public string FilterSsid
    {
        get => _filterSsid;
        set
        {
            if (SetProperty(ref _filterSsid, value))
            {
                RefreshHistoryView();
            }
        }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatus));
                OnPropertyChanged(nameof(ShowStatusBar));
            }
        }
    }

    public TestOperationState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                StopCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(IsConnecting));
            }
        }
    }

    /// <summary>
    /// Association and DHCP in progress: the phase with no telemetry of its own, so it is
    /// the one that needs a progress indicator. Once the test is running, the latency
    /// chart and the speed gauge are the feedback.
    /// </summary>
    public bool IsConnecting => _state == TestOperationState.Connecting;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(ShowStatusBar));
            }
        }
    }

    /// <summary>
    /// Keeps the bottom bar on while a test runs, even before any message arrives — the
    /// progress indicator lives there and must not appear only once there is text.
    /// </summary>
    public bool ShowStatusBar => IsBusy || HasStatus;

    public IAsyncRelayCommand StopCommand { get; }

    public IAsyncRelayCommand ScanCommand { get; }

    public IAsyncRelayCommand ExportCommand { get; }

    public IRelayCommand ClearFilterCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadNetworksAsync().ConfigureAwait(false);
        await LoadHistoryAsync().ConfigureAwait(false);
    }

    /// <summary>Security options for the registration form (admin).</summary>
    public Array SecurityOptions { get; } = Enum.GetValues(typeof(SecurityType));

    /// <summary>
    /// Registers/updates a network. Administrator action: the password is asked here, at
    /// the moment it is needed, rather than at a sign-in screen nobody else has to pass.
    /// </summary>
    public async Task AddNetworkAsync(
        string displayName, string ssid, SecurityType security, string password,
        string? username = null, string? domain = null)
    {
        if (!await _adminGate.EnsureUnlockedAsync().ConfigureAwait(true))
        {
            return;
        }

        WifiNetworkProfile profile;
        try
        {
            profile = new WifiNetworkProfile(ssid, displayName, security);
        }
        catch (ArgumentException exception)
        {
            _alerts.Error(exception.Message);
            return;
        }

        if (profile.RequiresCredential && string.IsNullOrEmpty(password))
        {
            _alerts.Error("Informe a senha da rede protegida.");
            return;
        }

        try
        {
            var secret = profile.RequiresCredential
                ? new WifiSecret(
                    password,
                    profile.IsEnterprise && !string.IsNullOrWhiteSpace(username) ? username.Trim() : null,
                    profile.IsEnterprise && !string.IsNullOrWhiteSpace(domain) ? domain.Trim() : null)
                : null;
            var result = await _profiles.SaveAsync(profile, secret).ConfigureAwait(false);
            if (result.IsFailure)
            {
                _alerts.Error(result.Error);
                return;
            }

            StatusMessage = $"Rede '{profile.DisplayName}' cadastrada.";
            await LoadNetworksAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to register the network.", exception);
            _alerts.Error("Falha ao cadastrar a rede.");
        }
    }

    private async Task RunNetworkAsync(WifiNetworkProfile profile, bool readyToConnect)
    {
        StatusMessage = string.Empty;
        try
        {
            // Protected network still unknown to the system: ask for the password once.
            // The credential is kept in memory only and is remembered *after* a confirmed
            // successful connection (see below), never before. Networks already ready
            // (open, saved in Windows or registered) go straight through.
            WifiSecret? transientSecret = null;
            if (profile.RequiresCredential && !readyToConnect)
            {
                var prompt = await PromptForCredentialAsync(profile).ConfigureAwait(false);
                if (prompt.Cancelled)
                {
                    return;
                }

                transientSecret = prompt.Secret;
            }

            var result = await _orchestrator.RunWifiTestAsync(profile, transientSecret).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await RememberOnSuccessAsync(profile, transientSecret).ConfigureAwait(false);
            }
            else
            {
                // Nothing was persisted, so the next tap will ask for the password again.
                ReportFailure(result.Error);
            }

            await LoadHistoryAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Error while running the connectivity test.", exception);
            _alerts.Error("Erro inesperado ao executar o teste.");
        }
    }

    /// <summary>
    /// Runs the cable test. No credential and no profile are involved, so the whole
    /// Wi-Fi preamble is absent: the orchestrator confirms link and lease on its own.
    /// </summary>
    private async Task RunWiredAsync()
    {
        StatusMessage = string.Empty;
        try
        {
            var result = await _orchestrator.RunWiredTestAsync().ConfigureAwait(false);
            if (result.IsFailure)
            {
                ReportFailure(result.Error);
            }

            await LoadHistoryAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Error while running the wired connectivity test.", exception);
            _alerts.Error("Erro inesperado ao executar o teste.");
        }
    }

    /// <summary>Shows the failure and clears the Failed state so the next tap can start.</summary>
    private void ReportFailure(string error)
    {
        _alerts.Error(error);
        _orchestrator.AcknowledgeFailure();
    }

    /// <summary>Outcome of asking the operator for a network credential.</summary>
    private readonly record struct CredentialPromptResult(WifiSecret? Secret, bool Cancelled);

    /// <summary>
    /// Asks the operator for the password of an unknown network, without saving anything.
    /// If a credential is already stored, returns it (no prompt). The returned secret is
    /// meant for a single test run and is only remembered on success.
    /// </summary>
    private async Task<CredentialPromptResult> PromptForCredentialAsync(WifiNetworkProfile profile)
    {
        var existing = await _credentialStore.GetAsync(profile.Ssid).ConfigureAwait(false);
        if (existing is not null)
        {
            // Already remembered from a previous successful run: reuse it.
            return new CredentialPromptResult(null, Cancelled: false);
        }

        // The dialog is awaitable now, so InvokeAsync replaces the fire-and-assign that
        // WPF's blocking ShowDialog allowed.
        var secret = await Dispatcher.UIThread
            .InvokeAsync(() => _credentialPrompt.RequestAsync(profile))
            .ConfigureAwait(false);

        return secret is null
            ? new CredentialPromptResult(null, Cancelled: true)
            : new CredentialPromptResult(secret, Cancelled: false);
    }

    /// <summary>
    /// Remembers a freshly entered credential (profile + secret) for future tests, but
    /// only after the connection has actually succeeded. A wrong password never reaches
    /// this point, so it is never persisted.
    /// </summary>
    private async Task RememberOnSuccessAsync(WifiNetworkProfile profile, WifiSecret? secret)
    {
        if (secret is null)
        {
            return;
        }

        var remembered = await _profiles.RememberForTestingAsync(profile, secret).ConfigureAwait(false);
        if (remembered.IsFailure)
        {
            _alerts.Error(remembered.Error);
            return;
        }

        StatusMessage = $"Rede '{profile.DisplayName}' salva para testes futuros.";
    }

    private async Task StopAsync()
    {
        await _orchestrator.StopAsync().ConfigureAwait(false);
        await LoadHistoryAsync().ConfigureAwait(false);
    }

    private async Task LoadNetworksAsync()
    {
        try
        {
            var networks = await _discovery.DiscoverAsync().ConfigureAwait(false);
            var link = await DetectWiredLinkAsync().ConfigureAwait(false);

            RunOnUi(() =>
            {
                Wired = new WiredButtonViewModel(link, RunWiredAsync);

                Networks.Clear();
                foreach (var network in networks)
                {
                    var profile = network.Profile;
                    var ready = network.ReadyToConnect;
                    Networks.Add(new NetworkButtonViewModel(
                        profile, BuildInfo(network), ready, () => RunNetworkAsync(profile, ready)));
                }

                StatusMessage = Networks.Count == 0
                    ? "Nenhuma rede Wi-Fi encontrada. Clique em 'Buscar redes' ou aproxime-se de um ponto de acesso."
                    : string.Empty;
            });
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to discover networks.", exception);
            _alerts.Error("Falha ao buscar as redes.");
        }
    }

    /// <summary>
    /// Reads the wired adapter alongside the Wi-Fi scan. A failure here must not take the
    /// network list down with it: the cable button simply reports no adapter.
    /// </summary>
    private async Task<EthernetLink?> DetectWiredLinkAsync()
    {
        try
        {
            return await _ethernet.DetectAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to detect the wired adapter.", exception);
            return null;
        }
    }

    private static string BuildInfo(DiscoveredNetwork network)
    {
        var security = network.Profile.Security == SecurityType.Open
            ? "Aberta"
            : network.Profile.Security.ToString();
        var readiness = network.ReadyToConnect ? "pronta" : "cadastrar senha";

        return network.SignalPercent > 0
            ? $"{security} · {readiness} · {network.SignalPercent}%"
            : $"{security} · {readiness}";
    }

    private async Task LoadHistoryAsync()
    {
        var runs = await _history.GetRecentAsync(_options.MaxHistoryEntries).ConfigureAwait(false);

        RunOnUi(() =>
        {
            _allRuns.Clear();
            _allRuns.AddRange(runs);
            RefreshHistoryView();
        });
    }

    /// <summary>Rebuilds the visible history from the loaded runs, applying the current filter.</summary>
    private void RefreshHistoryView()
    {
        History.Clear();
        foreach (var run in CurrentFilter().Apply(_allRuns))
        {
            History.Add(new TestRunViewModel(run));
        }
    }

    /// <summary>Builds the <see cref="HistoryFilter"/> from the current UI inputs.</summary>
    private HistoryFilter CurrentFilter()
    {
        var offset = DateTimeOffset.Now.Offset;

        // Dates are day-granular in the picker: include the whole "from" day and "to" day.
        DateTimeOffset? from = FilterFrom is { } f ? new DateTimeOffset(f.Date, offset) : null;
        DateTimeOffset? to = FilterTo is { } t ? new DateTimeOffset(t.Date.AddDays(1).AddTicks(-1), offset) : null;
        var ssid = string.IsNullOrWhiteSpace(FilterSsid) ? null : FilterSsid.Trim();

        return new HistoryFilter(from, to, ssid);
    }

    private void ClearFilter()
    {
        FilterFrom = null;
        FilterTo = null;
        FilterSsid = string.Empty;
    }

    private async Task ExportAsync()
    {
        var target = await _exportDialog
            .PickSaveTargetAsync(_exportService.AvailableExporters, SuggestedFileName())
            .ConfigureAwait(false);

        if (target is null)
        {
            return;
        }

        try
        {
            Result result;
            await using (var stream = File.Create(target.Path))
            {
                result = await _exportService.ExportAsync(CurrentFilter(), target.Format, stream).ConfigureAwait(false);
            }

            if (result.IsFailure)
            {
                _alerts.Error(result.Error);
                return;
            }

            StatusMessage = $"Histórico exportado: {target.Path}";
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to export the history.", exception);
            _alerts.Error("Falha ao exportar o histórico.");
        }
    }

    private static string SuggestedFileName() => $"wave-historico-{DateTime.Now:yyyyMMdd-HHmm}";

    private void OnStateChanged(object? sender, TestStateChangedEventArgs e) => RunOnUi(() => ApplyState(e));

    private void ApplyState(TestStateChangedEventArgs e)
    {
        State = e.State;
        IsBusy = e.State is TestOperationState.Connecting or TestOperationState.TestRunning;

        if (e.State == TestOperationState.Connecting)
        {
            Telemetry.Reset();
        }

        // The orchestrator only sends a message when it has something specific to report
        // (a failure reason). Progress phrasing is presentation, so it is composed here.
        // Going Idle clears the bar: keeping the previous text would leave it claiming a
        // test is running after the operator stopped it.
        StatusMessage = !string.IsNullOrEmpty(e.Message)
            ? e.Message
            : ProgressMessage(e.State, e.Target, e.Medium);

        foreach (var target in Targets())
        {
            var isActive = !string.IsNullOrEmpty(e.Target)
                && string.Equals(target.TargetKey, e.Target, StringComparison.OrdinalIgnoreCase);
            target.State = isActive ? e.State : TestOperationState.Idle;
            target.IsEnabled = !IsBusy;
        }
    }

    /// <summary>Every button that can start a test: the Wi-Fi networks plus the cable.</summary>
    private IEnumerable<TestTargetButtonViewModel> Targets() =>
        Networks.Cast<TestTargetButtonViewModel>().Append(Wired);

    /// <summary>
    /// Text describing the phase the test just entered. Empty for the states that are not
    /// progress (Idle, and Failed — which always arrives with its own reason), so the bar
    /// goes away instead of keeping a stale claim.
    /// </summary>
    private static string ProgressMessage(TestOperationState state, string? target, TestMedium medium)
    {
        // The cable has nothing to associate with, so "connecting" is really "checking
        // the link" — saying "conectando" there would misdescribe what WAVE is waiting on.
        if (medium == TestMedium.Ethernet)
        {
            return state switch
            {
                TestOperationState.Connecting => "Verificando o link do cabo e aguardando endereço IP…",
                TestOperationState.TestRunning => "Testando a rede cabeada: medindo latência, velocidade e streaming…",
                _ => string.Empty
            };
        }

        var network = string.IsNullOrEmpty(target) ? "a rede" : $"'{target}'";

        return state switch
        {
            TestOperationState.Connecting => $"Conectando a {network} e aguardando endereço IP…",
            TestOperationState.TestRunning => $"Testando {network}: medindo latência, velocidade e streaming…",
            _ => string.Empty
        };
    }

    private void OnPingSampled(object? sender, PingSample sample) => RunOnUi(() => Telemetry.AddSample(sample));

    private void OnSpeedSampled(object? sender, SpeedSample sample) => RunOnUi(() => Telemetry.AddSpeedSample(sample));

    /// <summary>
    /// Marshals a mutation onto the UI thread. Invoke already runs inline when called
    /// from that thread, so the explicit CheckAccess branch WPF needed is gone.
    /// </summary>
    private static void RunOnUi(Action action) => Dispatcher.UIThread.Invoke(action);
}
