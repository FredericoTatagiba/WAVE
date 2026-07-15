using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WAVE.App.Services;
using WAVE.Application.Abstractions;
using WAVE.Application.Discovery;
using WAVE.Application.History;
using WAVE.Application.Profiles;
using WAVE.Application.Testing;
using WAVE.Domain.Networking;
using WAVE.Domain.Security;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>ViewModel principal: coordena lista de redes, telemetria e histórico.</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IWifiTestOrchestrator _orchestrator;
    private readonly NetworkProfileService _profiles;
    private readonly NetworkDiscoveryService _discovery;
    private readonly TestHistoryService _history;
    private readonly ICurrentUserContext _currentUser;
    private readonly IUserAlerts _alerts;
    private readonly IAppLogger _logger;
    private readonly TestRunnerOptions _options;
    private readonly ICredentialStore _credentialStore;
    private readonly ICredentialPrompt _credentialPrompt;

    private string _statusMessage = string.Empty;
    private TestOperationState _state = TestOperationState.Idle;
    private bool _isBusy;

    public MainViewModel(
        IWifiTestOrchestrator orchestrator,
        NetworkProfileService profiles,
        NetworkDiscoveryService discovery,
        TestHistoryService history,
        ICurrentUserContext currentUser,
        IUserAlerts alerts,
        IAppLogger logger,
        TestRunnerOptions options,
        ICredentialStore credentialStore,
        ICredentialPrompt credentialPrompt)
    {
        _orchestrator = orchestrator;
        _profiles = profiles;
        _discovery = discovery;
        _history = history;
        _currentUser = currentUser;
        _alerts = alerts;
        _logger = logger;
        _options = options;
        _credentialStore = credentialStore;
        _credentialPrompt = credentialPrompt;

        Telemetry = new TelemetryViewModel();

        StopCommand = new AsyncRelayCommand(
            StopAsync,
            () => State is TestOperationState.Connecting or TestOperationState.TestRunning);
        ScanCommand = new AsyncRelayCommand(LoadNetworksAsync);

        _orchestrator.StateChanged += OnStateChanged;
        _orchestrator.PingSampled += OnPingSampled;
        _currentUser.Changed += OnUserChanged;
    }

    public ObservableCollection<NetworkButtonViewModel> Networks { get; } = new();

    public ObservableCollection<TestRunViewModel> History { get; } = new();

    public TelemetryViewModel Telemetry { get; }

    public string RoleName => _currentUser.Role == UserRole.Administrator ? "Administrador" : "Operador";

    public bool IsAdministrator => _currentUser.Role == UserRole.Administrator;

    public string CurrentUserText => $"{_currentUser.UserName} · {RoleName}";

    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatus));
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
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public IAsyncRelayCommand StopCommand { get; }

    public IAsyncRelayCommand ScanCommand { get; }

    public async Task InitializeAsync()
    {
        await LoadNetworksAsync().ConfigureAwait(false);
        await LoadHistoryAsync().ConfigureAwait(false);
    }

    /// <summary>Opções de segurança para o formulário de cadastro (admin).</summary>
    public Array SecurityOptions { get; } = Enum.GetValues(typeof(SecurityType));

    /// <summary>Cadastra/atualiza uma rede (operação de Administrador).</summary>
    public async Task AddNetworkAsync(
        string displayName, string ssid, SecurityType security, string password,
        string? username = null, string? domain = null)
    {
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
            _logger.Error("Falha ao cadastrar a rede.", exception);
            _alerts.Error("Falha ao cadastrar a rede.");
        }
    }

    private async Task RunNetworkAsync(NetworkButtonViewModel button)
    {
        StatusMessage = string.Empty;
        try
        {
            var profile = button.Profile;

            // Protected network still unknown to the system: ask for the password once.
            // The credential is kept in memory only and is remembered *after* a confirmed
            // successful connection (see below), never before. Networks already ready
            // (open, saved in Windows or registered) go straight through.
            WifiSecret? transientSecret = null;
            if (profile.RequiresCredential && !button.ReadyToConnect)
            {
                var prompt = await PromptForCredentialAsync(profile).ConfigureAwait(false);
                if (prompt.Cancelled)
                {
                    return;
                }

                transientSecret = prompt.Secret;
            }

            var result = await _orchestrator.RunTestAsync(profile, transientSecret).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await RememberOnSuccessAsync(profile, transientSecret).ConfigureAwait(false);
            }
            else
            {
                // Nothing was persisted, so the next tap will ask for the password again.
                _alerts.Error(result.Error);
                _orchestrator.AcknowledgeFailure();
            }

            await LoadHistoryAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Erro ao executar o teste de conectividade.", exception);
            _alerts.Error("Erro inesperado ao executar o teste.");
        }
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

        WifiSecret? secret = null;
        RunOnUi(() => secret = _credentialPrompt.Request(profile));
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
            RunOnUi(() =>
            {
                Networks.Clear();
                foreach (var network in networks)
                {
                    Networks.Add(new NetworkButtonViewModel(
                        network.Profile, BuildInfo(network), network.ReadyToConnect, RunNetworkAsync));
                }

                StatusMessage = Networks.Count == 0
                    ? "Nenhuma rede encontrada. Clique em 'Buscar redes' ou aproxime-se de um ponto de acesso."
                    : string.Empty;
            });
        }
        catch (Exception exception)
        {
            _logger.Error("Falha ao descobrir redes.", exception);
            _alerts.Error("Falha ao buscar as redes.");
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
        var result = await _history.GetRecentAsync(_options.MaxHistoryEntries).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return;
        }

        RunOnUi(() =>
        {
            History.Clear();
            foreach (var run in result.Value)
            {
                History.Add(new TestRunViewModel(run));
            }
        });
    }

    private void OnStateChanged(object? sender, TestStateChangedEventArgs e) => RunOnUi(() => ApplyState(e));

    private void ApplyState(TestStateChangedEventArgs e)
    {
        State = e.State;
        IsBusy = e.State is TestOperationState.Connecting or TestOperationState.TestRunning;

        if (e.State == TestOperationState.Connecting)
        {
            Telemetry.Reset();
        }

        if (!string.IsNullOrEmpty(e.Message))
        {
            StatusMessage = e.Message;
        }

        foreach (var network in Networks)
        {
            var isActive = string.Equals(network.Ssid, e.Ssid, StringComparison.OrdinalIgnoreCase);
            network.State = isActive ? e.State : TestOperationState.Idle;
            network.IsEnabled = !IsBusy;
        }
    }

    private void OnPingSampled(object? sender, PingSample sample) => RunOnUi(() => Telemetry.AddSample(sample));

    private void OnUserChanged(object? sender, EventArgs e) => RunOnUi(() =>
    {
        OnPropertyChanged(nameof(RoleName));
        OnPropertyChanged(nameof(IsAdministrator));
        OnPropertyChanged(nameof(CurrentUserText));
    });

    private static void RunOnUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }
}
