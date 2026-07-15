using WAVE.Application.Abstractions;
using WAVE.Application.Security;
using WAVE.Application.Testing;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

public class WifiTestOrchestratorTests
{
    private static WifiNetworkProfile OpenProfile() => new("RedeAberta", "Rede Aberta", SecurityType.Open);

    private static WifiNetworkProfile ProtectedProfile() => new("Protegida", "Protegida", SecurityType.Wpa2Personal);

    private static TestRunnerOptions FastOptions() => new()
    {
        StabilizationDelay = TimeSpan.Zero,
        DhcpTimeout = TimeSpan.FromSeconds(1),
        DhcpPollInterval = TimeSpan.FromMilliseconds(1),
        StreamingTargetMbps = 8
    };

    private static WifiTestOrchestrator Build(
        FakeAuthorizationService authorization,
        FakeWifiConnector connector,
        FakeDhcpValidator dhcp,
        FakeVisiblePingTerminal visiblePing,
        FakePingMonitor pingMonitor,
        FakeTestRunRepository history,
        IClock clock,
        TestRunnerOptions options,
        FakeWifiProfileCatalog? catalog = null,
        FakeSpeedMeter? speedMeter = null,
        FakeStreamingProbe? streamingProbe = null) =>
        new(
            authorization,
            new CurrentUserContext(),
            new FakeCredentialStore(),
            connector,
            catalog ?? new FakeWifiProfileCatalog(),
            dhcp,
            visiblePing,
            pingMonitor,
            speedMeter ?? new FakeSpeedMeter(),
            streamingProbe ?? new FakeStreamingProbe(),
            history,
            clock,
            new NullLogger(),
            options);

    [Fact]
    public async Task RunTest_WhenUnauthorized_FailsAndStaysIdle()
    {
        var orchestrator = Build(
            new FakeAuthorizationService(allow: false),
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            FastOptions());

        var result = await orchestrator.RunTestAsync(OpenProfile());

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.Idle, orchestrator.CurrentState);
    }

    [Fact]
    public async Task RunTest_HappyPath_ReachesTestRunningAndMeasures()
    {
        var visiblePing = new FakeVisiblePingTerminal();
        var pingMonitor = new FakePingMonitor();
        var speedMeter = new FakeSpeedMeter();
        var streamingProbe = new FakeStreamingProbe();
        var options = FastOptions();

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            visiblePing,
            pingMonitor,
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            options,
            speedMeter: speedMeter,
            streamingProbe: streamingProbe);

        var result = await orchestrator.RunTestAsync(OpenProfile());

        Assert.True(result.IsSuccess);
        Assert.Equal(TestOperationState.TestRunning, orchestrator.CurrentState);
        Assert.True(pingMonitor.Started);
        Assert.Equal(options.PingTargetHost, visiblePing.LaunchedHost);
        Assert.True(speedMeter.Called);
        Assert.True(streamingProbe.Called);
    }

    [Fact]
    public async Task RunThenStop_RecordsSpeedAndStreaming()
    {
        var history = new FakeTestRunRepository();
        var speedMeter = new FakeSpeedMeter(new SpeedResult(150, 40, DateTimeOffset.UnixEpoch));
        var streamingProbe = new FakeStreamingProbe(new double[] { 20, 22, 25, 21 }); // todas >= 8 => Smooth

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            speedMeter: speedMeter,
            streamingProbe: streamingProbe);

        await orchestrator.RunTestAsync(OpenProfile());
        await orchestrator.StopAsync();

        var run = Assert.Single(history.Added);
        Assert.NotNull(run.Speed);
        Assert.Equal(150, run.Speed!.Value.DownloadMbps);
        Assert.Equal(40, run.Speed!.Value.UploadMbps);
        Assert.NotNull(run.Streaming);
        Assert.Equal(StreamingStability.Smooth, run.Streaming!.Value.Stability);
        Assert.Equal(0, run.Streaming!.Value.RebufferEvents);
    }

    [Fact]
    public async Task RunTest_WhenDhcpTimesOut_FailsAndRecordsHistory()
    {
        var history = new FakeTestRunRepository();

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            new FakeWifiConnector(),
            new FakeDhcpValidator(false),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.FromSeconds(30)),
            FastOptions());

        var result = await orchestrator.RunTestAsync(OpenProfile());

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.Failed, orchestrator.CurrentState);
        Assert.Single(history.Added);
        Assert.Equal(TestFailureReason.DhcpTimeout, history.Added[0].FailureReason);
    }

    [Fact]
    public async Task RunTest_WhenConnectionFails_ReportsAuthenticationFailure()
    {
        var history = new FakeTestRunRepository();
        var connector = new FakeWifiConnector { ConnectResult = Result.Failure("sem sinal") };

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            connector,
            new FakeDhcpValidator(true),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.Zero),
            FastOptions());

        var result = await orchestrator.RunTestAsync(OpenProfile());

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.Failed, orchestrator.CurrentState);
        Assert.Single(history.Added);
        Assert.Equal(TestFailureReason.AuthenticationFailed, history.Added[0].FailureReason);
    }

    [Fact]
    public async Task RunTest_WhenNewProfileFailsToConfirm_RollsBackProfileAndUsesProvidedSecret()
    {
        // A protected network unknown to Windows: the operator's password is used only
        // for this run. When the connection is not confirmed (DHCP never leases, e.g. a
        // wrong password), WAVE must delete the profile it just created so the bad
        // credential is not remembered and the network asks for the password again.
        var connector = new FakeWifiConnector();
        var providedSecret = new WifiSecret("wrong-password");

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            connector,
            new FakeDhcpValidator(false),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.FromSeconds(30)),
            FastOptions(),
            new FakeWifiProfileCatalog(exists: false));

        var result = await orchestrator.RunTestAsync(ProtectedProfile(), providedSecret);

        Assert.True(result.IsFailure);
        Assert.Equal(providedSecret, connector.EnsuredSecret);
        Assert.Contains("Protegida", connector.RemovedProfiles);
    }

    [Fact]
    public async Task RunTest_WhenKnownProfileFails_DoesNotRollBackProfile()
    {
        // Windows already knows the profile (pre-existing / admin-registered): WAVE did
        // not create it this run, so a transient failure must NOT delete it.
        var connector = new FakeWifiConnector();

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            connector,
            new FakeDhcpValidator(false),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.FromSeconds(30)),
            FastOptions(),
            new FakeWifiProfileCatalog(exists: true));

        var result = await orchestrator.RunTestAsync(ProtectedProfile());

        Assert.True(result.IsFailure);
        Assert.Empty(connector.RemovedProfiles);
    }

    [Fact]
    public async Task RunTest_WhenNewProfileSucceeds_KeepsProfileAndDoesNotSelfPersist()
    {
        // On a confirmed success the created profile is kept (no rollback). Persisting
        // the credential is the caller's job; the orchestrator never writes it itself.
        var connector = new FakeWifiConnector();
        var credentialStore = new FakeCredentialStore();
        var providedSecret = new WifiSecret("right-password");

        var orchestrator = new WifiTestOrchestrator(
            new FakeAuthorizationService(allow: true),
            new CurrentUserContext(),
            credentialStore,
            connector,
            new FakeWifiProfileCatalog(exists: false),
            new FakeDhcpValidator(true),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            new FakeSpeedMeter(),
            new FakeStreamingProbe(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            new NullLogger(),
            FastOptions());

        var result = await orchestrator.RunTestAsync(ProtectedProfile(), providedSecret);

        Assert.True(result.IsSuccess);
        Assert.Equal(providedSecret, connector.EnsuredSecret);
        Assert.Empty(connector.RemovedProfiles);
        Assert.Empty(credentialStore.Saved);
    }

    [Fact]
    public async Task RunTest_WhenWindowsKnowsProfile_SkipsCredentialAndSucceeds()
    {
        var profile = new WifiNetworkProfile("Corporativa", "Corporativa", SecurityType.Wpa2Personal);

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            new FakeWifiProfileCatalog(exists: true));

        var result = await orchestrator.RunTestAsync(profile);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestOperationState.TestRunning, orchestrator.CurrentState);
    }
}
