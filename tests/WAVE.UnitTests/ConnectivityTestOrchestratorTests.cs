using WAVE.Application.Abstractions;
using WAVE.Application.Testing;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

public class ConnectivityTestOrchestratorTests
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

    private static ConnectivityTestOrchestrator Build(
        FakeWifiConnector connector,
        FakeDhcpValidator dhcp,
        FakePingMonitor pingMonitor,
        FakeTestRunRepository history,
        IClock clock,
        TestRunnerOptions options,
        FakeWifiProfileCatalog? catalog = null,
        FakeSpeedMeter? speedMeter = null,
        FakeStreamingProbe? streamingProbe = null,
        FakeEthernetLinkProbe? ethernet = null) =>
        new(
            new FakeDeviceIdentity(),
            new FakeCredentialStore(),
            connector,
            catalog ?? new FakeWifiProfileCatalog(),
            dhcp,
            ethernet ?? new FakeEthernetLinkProbe(),
            pingMonitor,
            speedMeter ?? new FakeSpeedMeter(),
            streamingProbe ?? new FakeStreamingProbe(),
            history,
            clock,
            new NullLogger(),
            options);

    [Fact]
    public async Task RunWifiTest_HappyPath_ReachesTestRunningAndMeasures()
    {
        var pingMonitor = new FakePingMonitor();
        var speedMeter = new FakeSpeedMeter();
        var streamingProbe = new FakeStreamingProbe();
        var options = FastOptions();

        var orchestrator = Build(
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            pingMonitor,
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            options,
            speedMeter: speedMeter,
            streamingProbe: streamingProbe);

        var result = await orchestrator.RunWifiTestAsync(OpenProfile());

        Assert.True(result.IsSuccess);
        Assert.Equal(TestOperationState.TestRunning, orchestrator.CurrentState);
        // The continuous monitor is the only ping now: it feeds the in-app latency chart,
        // and nothing opens a terminal window.
        Assert.True(pingMonitor.Started);
        Assert.Equal(options.PingTargetHost, pingMonitor.Host);
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
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            speedMeter: speedMeter,
            streamingProbe: streamingProbe);

        await orchestrator.RunWifiTestAsync(OpenProfile());
        await orchestrator.StopAsync();

        var run = Assert.Single(history.Added);
        Assert.Equal(TestMedium.WiFi, run.Medium);
        // Stopping a test that reached TEST_RUNNING records a completed run, not a failure.
        Assert.True(run.Succeeded);
        Assert.NotNull(run.Speed);
        Assert.Equal(150, run.Speed!.Value.DownloadMbps);
        Assert.Equal(40, run.Speed!.Value.UploadMbps);
        Assert.NotNull(run.Streaming);
        Assert.Equal(StreamingStability.Smooth, run.Streaming!.Value.Stability);
        Assert.Equal(0, run.Streaming!.Value.RebufferEvents);
    }

    [Fact]
    public async Task RunWifiTest_WhenDhcpTimesOut_FailsAndRecordsHistory()
    {
        var history = new FakeTestRunRepository();

        var orchestrator = Build(
            new FakeWifiConnector(),
            new FakeDhcpValidator(false),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.FromSeconds(30)),
            FastOptions());

        var result = await orchestrator.RunWifiTestAsync(OpenProfile());

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.Failed, orchestrator.CurrentState);
        Assert.Single(history.Added);
        Assert.Equal(TestFailureReason.DhcpTimeout, history.Added[0].FailureReason);
    }

    [Fact]
    public async Task RunWifiTest_WhenConnectionFails_ReportsAuthenticationFailure()
    {
        var history = new FakeTestRunRepository();
        var connector = new FakeWifiConnector { ConnectResult = Result.Failure("sem sinal") };

        var orchestrator = Build(
            connector,
            new FakeDhcpValidator(true),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.Zero),
            FastOptions());

        var result = await orchestrator.RunWifiTestAsync(OpenProfile());

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.Failed, orchestrator.CurrentState);
        Assert.Single(history.Added);
        Assert.Equal(TestFailureReason.AuthenticationFailed, history.Added[0].FailureReason);
    }

    [Fact]
    public async Task RunWifiTest_WhenNewProfileFailsToConfirm_RollsBackProfileAndUsesProvidedSecret()
    {
        // A protected network unknown to Windows: the operator's password is used only
        // for this run. When the connection is not confirmed (DHCP never leases, e.g. a
        // wrong password), WAVE must delete the profile it just created so the bad
        // credential is not remembered and the network asks for the password again.
        var connector = new FakeWifiConnector();
        var providedSecret = new WifiSecret("wrong-password");

        var orchestrator = Build(
            connector,
            new FakeDhcpValidator(false),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.FromSeconds(30)),
            FastOptions(),
            new FakeWifiProfileCatalog(exists: false));

        var result = await orchestrator.RunWifiTestAsync(ProtectedProfile(), providedSecret);

        Assert.True(result.IsFailure);
        Assert.Equal(providedSecret, connector.EnsuredSecret);
        Assert.Contains("Protegida", connector.RemovedProfiles);
    }

    [Fact]
    public async Task RunWifiTest_WhenKnownProfileFails_DoesNotRollBackProfile()
    {
        // Windows already knows the profile (pre-existing / admin-registered): WAVE did
        // not create it this run, so a transient failure must NOT delete it.
        var connector = new FakeWifiConnector();

        var orchestrator = Build(
            connector,
            new FakeDhcpValidator(false),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.FromSeconds(30)),
            FastOptions(),
            new FakeWifiProfileCatalog(exists: true));

        var result = await orchestrator.RunWifiTestAsync(ProtectedProfile());

        Assert.True(result.IsFailure);
        Assert.Empty(connector.RemovedProfiles);
    }

    [Fact]
    public async Task RunWifiTest_WhenNewProfileSucceeds_KeepsProfileAndDoesNotSelfPersist()
    {
        // On a confirmed success the created profile is kept (no rollback). Persisting
        // the credential is the caller's job; the orchestrator never writes it itself.
        var connector = new FakeWifiConnector();
        var credentialStore = new FakeCredentialStore();
        var providedSecret = new WifiSecret("right-password");

        var orchestrator = new ConnectivityTestOrchestrator(
            new FakeDeviceIdentity(),
            credentialStore,
            connector,
            new FakeWifiProfileCatalog(exists: false),
            new FakeDhcpValidator(true),
            new FakeEthernetLinkProbe(),
            new FakePingMonitor(),
            new FakeSpeedMeter(),
            new FakeStreamingProbe(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            new NullLogger(),
            FastOptions());

        var result = await orchestrator.RunWifiTestAsync(ProtectedProfile(), providedSecret);

        Assert.True(result.IsSuccess);
        Assert.Equal(providedSecret, connector.EnsuredSecret);
        Assert.Empty(connector.RemovedProfiles);
        Assert.Empty(credentialStore.Saved);
    }

    [Fact]
    public async Task RunWifiTest_WhenWindowsKnowsProfile_SkipsCredentialAndSucceeds()
    {
        var profile = new WifiNetworkProfile("Corporativa", "Corporativa", SecurityType.Wpa2Personal);

        var orchestrator = Build(
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            new FakeWifiProfileCatalog(exists: true));

        var result = await orchestrator.RunWifiTestAsync(profile);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestOperationState.TestRunning, orchestrator.CurrentState);
    }

    [Fact]
    public async Task RunWiredTest_HappyPath_MeasuresWithoutTouchingWifi()
    {
        // The cable path must never create a profile or associate: doing so would leave
        // Windows connected to a network the operator did not ask for.
        var connector = new FakeWifiConnector();
        var history = new FakeTestRunRepository();

        var orchestrator = Build(
            connector,
            new FakeDhcpValidator(false),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            ethernet: new FakeEthernetLinkProbe(FakeEthernetLinkProbe.Ready()));

        var result = await orchestrator.RunWiredTestAsync();
        await orchestrator.StopAsync();

        Assert.True(result.IsSuccess);
        Assert.False(connector.Connected);
        var run = Assert.Single(history.Added);
        Assert.Equal(TestMedium.Ethernet, run.Medium);
        Assert.Equal("eth0", run.Ssid);
        Assert.Equal("TABLET-01", run.DeviceName);
        Assert.True(run.Succeeded);
    }

    [Fact]
    public async Task RunWiredTest_WhenCableUnplugged_FailsWithNoLink()
    {
        var history = new FakeTestRunRepository();

        var orchestrator = Build(
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            ethernet: new FakeEthernetLinkProbe(FakeEthernetLinkProbe.Unplugged()));

        var result = await orchestrator.RunWiredTestAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.Failed, orchestrator.CurrentState);
        Assert.Equal(TestFailureReason.NoLink, Assert.Single(history.Added).FailureReason);
    }

    [Fact]
    public async Task RunWiredTest_WhenNoAdapter_FailsWithNoLink()
    {
        var orchestrator = Build(
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            ethernet: new FakeEthernetLinkProbe());

        var result = await orchestrator.RunWiredTestAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.Failed, orchestrator.CurrentState);
    }

    [Fact]
    public async Task RunWiredTest_WhenLinkHasNoAddress_TimesOutOnDhcp()
    {
        // A live link with no lease is the classic "switch port with no DHCP" case: the
        // wired path must not confirm it just because the cable is in.
        var history = new FakeTestRunRepository();

        var orchestrator = Build(
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakePingMonitor(),
            history,
            new AdvancingClock(TimeSpan.FromSeconds(30)),
            FastOptions(),
            ethernet: new FakeEthernetLinkProbe(FakeEthernetLinkProbe.WithoutLease()));

        var result = await orchestrator.RunWiredTestAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(TestFailureReason.DhcpTimeout, Assert.Single(history.Added).FailureReason);
    }

    [Fact]
    public async Task RunWiredTest_WhileWifiTestRuns_IsRejected()
    {
        var orchestrator = Build(
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakePingMonitor(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            ethernet: new FakeEthernetLinkProbe(FakeEthernetLinkProbe.Ready()));

        await orchestrator.RunWifiTestAsync(OpenProfile());
        var result = await orchestrator.RunWiredTestAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.TestRunning, orchestrator.CurrentState);
    }
}
