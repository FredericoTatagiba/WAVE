using Microsoft.Extensions.DependencyInjection;
using WAVE.App.Services;
using WAVE.App.ViewModels;
using WAVE.App.Views;
using WAVE.Application.Abstractions;
using WAVE.Application.Discovery;
using WAVE.Application.History;
using WAVE.Application.Networking;
using WAVE.Application.Profiles;
using WAVE.Application.Security;
using WAVE.Application.Testing;
using WAVE.Infrastructure.Diagnostics;
using WAVE.Infrastructure.Logging;
using WAVE.Infrastructure.Persistence;
using WAVE.Infrastructure.Process;
using WAVE.Infrastructure.Security;
using WAVE.Infrastructure.Time;
using WAVE.Infrastructure.Web;
using WAVE.Infrastructure.Wifi;

namespace WAVE.App.Bootstrap;

/// <summary>
/// Registra todas as dependências por camada. O Composition Root conhece as
/// implementações concretas; o restante do código depende apenas de abstrações.
/// </summary>
public static class ServiceRegistration
{
    public static void AddWave(IServiceCollection services)
    {
        AddApplication(services);
        AddInfrastructure(services);
        AddPresentation(services);
    }

    private static void AddApplication(IServiceCollection services)
    {
        services.AddSingleton(new TestRunnerOptions());
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IAuthorizationService, AuthorizationService>();
        services.AddSingleton<IRoleElevationService, RoleElevationService>();
        services.AddSingleton<IWifiProfileXmlFactory, WlanProfileXmlBuilder>();
        services.AddSingleton<IWifiTestOrchestrator, WifiTestOrchestrator>();
        services.AddSingleton<NetworkProfileService>();
        services.AddSingleton<TestHistoryService>();
        services.AddSingleton<NetworkDiscoveryService>();
    }

    private static void AddInfrastructure(IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IAppLogger, FileAppLogger>();
        services.AddSingleton<IProcessTerminator, SystemProcessTerminator>();
        services.AddSingleton<IWifiConnector, NetshWifiConnector>();
        services.AddSingleton<IWifiNetworkScanner, NetshWifiNetworkScanner>();
        services.AddSingleton<IWifiProfileCatalog, NetshWifiProfileCatalog>();
        services.AddSingleton<IDhcpAddressValidator, NetworkInterfaceDhcpValidator>();
        services.AddSingleton<IContinuousPingMonitor, ContinuousPingMonitor>();
        services.AddSingleton<IVisiblePingTerminal, VisiblePingTerminal>();
        services.AddSingleton<IPrivateBrowserLauncher, PrivateBrowserLauncher>();
        services.AddSingleton<INetworkProfileRepository, JsonNetworkProfileRepository>();
        services.AddSingleton<ITestRunRepository, JsonTestRunRepository>();
        services.AddSingleton<ICredentialStore, DpapiCredentialStore>();

        // Mesma instância atende verificação e gerenciamento da senha administrativa.
        services.AddSingleton<Pbkdf2AdminPasswordManager>();
        services.AddSingleton<IAdminPasswordVerifier>(sp => sp.GetRequiredService<Pbkdf2AdminPasswordManager>());
        services.AddSingleton<IAdminPasswordManager>(sp => sp.GetRequiredService<Pbkdf2AdminPasswordManager>());
    }

    private static void AddPresentation(IServiceCollection services)
    {
        services.AddSingleton<IUserAlerts, MessageBoxUserAlerts>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}
