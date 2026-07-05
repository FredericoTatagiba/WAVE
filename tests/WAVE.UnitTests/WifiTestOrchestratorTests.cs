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

    private static TestRunnerOptions FastOptions() => new()
    {
        StabilizationDelay = TimeSpan.Zero,
        DhcpTimeout = TimeSpan.FromSeconds(1),
        DhcpPollInterval = TimeSpan.FromMilliseconds(1),
        BetweenLaunchesDelay = TimeSpan.Zero
    };

    private static WifiTestOrchestrator Build(
        FakeAuthorizationService authorization,
        FakeWifiConnector connector,
        FakeDhcpValidator dhcp,
        FakeVisiblePingTerminal visiblePing,
        FakePingMonitor pingMonitor,
        FakeBrowserLauncher browser,
        FakeTestRunRepository history,
        IClock clock,
        TestRunnerOptions options,
        FakeWifiProfileCatalog? catalog = null) =>
        new(
            authorization,
            new CurrentUserContext(),
            new FakeCredentialStore(),
            connector,
            catalog ?? new FakeWifiProfileCatalog(),
            dhcp,
            new FakeProcessTerminator(),
            visiblePing,
            pingMonitor,
            browser,
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
            new FakeBrowserLauncher(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            FastOptions());

        var result = await orchestrator.RunTestAsync(OpenProfile());

        Assert.True(result.IsFailure);
        Assert.Equal(TestOperationState.Idle, orchestrator.CurrentState);
    }

    [Fact]
    public async Task RunTest_HappyPath_ReachesTestRunningAndLaunchesTools()
    {
        var visiblePing = new FakeVisiblePingTerminal();
        var pingMonitor = new FakePingMonitor();
        var browser = new FakeBrowserLauncher();
        var options = FastOptions();

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            visiblePing,
            pingMonitor,
            browser,
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            options);

        var result = await orchestrator.RunTestAsync(OpenProfile());

        Assert.True(result.IsSuccess);
        Assert.Equal(TestOperationState.TestRunning, orchestrator.CurrentState);
        Assert.True(pingMonitor.Started);
        Assert.Equal(options.PingTargetHost, visiblePing.LaunchedHost);
        Assert.Contains(options.SpeedTestUrl, browser.LaunchedUrls);
        Assert.Contains(options.StreamingUrl, browser.LaunchedUrls);
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
            new FakeBrowserLauncher(),
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
            new FakeBrowserLauncher(),
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
    public async Task RunTest_WhenWindowsKnowsProfile_SkipsCredentialAndSucceeds()
    {
        // Rede protegida, sem credencial cadastrada no WAVE, mas já salva no Windows.
        var profile = new WifiNetworkProfile("Corporativa", "Corporativa", SecurityType.Wpa2Personal);

        var orchestrator = Build(
            new FakeAuthorizationService(allow: true),
            new FakeWifiConnector(),
            new FakeDhcpValidator(true),
            new FakeVisiblePingTerminal(),
            new FakePingMonitor(),
            new FakeBrowserLauncher(),
            new FakeTestRunRepository(),
            new AdvancingClock(TimeSpan.Zero),
            FastOptions(),
            new FakeWifiProfileCatalog(exists: true));

        var result = await orchestrator.RunTestAsync(profile);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestOperationState.TestRunning, orchestrator.CurrentState);
    }
}
