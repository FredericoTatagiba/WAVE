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
using WAVE.Infrastructure.Configuration;
using WAVE.Infrastructure.Diagnostics;
using WAVE.Infrastructure.Ethernet;
using WAVE.Infrastructure.Export;
using WAVE.Infrastructure.Logging;
using WAVE.Infrastructure.Persistence;
using WAVE.Infrastructure.Security;
using WAVE.Infrastructure.Time;
using WAVE.Infrastructure.Web;
using WAVE.Infrastructure.Wifi;

namespace WAVE.App.Bootstrap;

/// <summary>
/// Registers all dependencies per layer. The Composition Root knows the concrete
/// implementations; the rest of the code depends only on abstractions.
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
        services.AddSingleton<IAdminSession, AdminSession>();
        services.AddSingleton<IWifiProfileXmlFactory, WlanProfileXmlBuilder>();
        services.AddSingleton<IConnectivityTestOrchestrator, ConnectivityTestOrchestrator>();
        services.AddSingleton<NetworkProfileService>();
        services.AddSingleton<HistoryExportService>();
        services.AddSingleton<NetworkDiscoveryService>();
    }

    private static void AddInfrastructure(IServiceCollection services)
    {
        // The settings and the paths they resolve come first: the logger writes to a
        // configured directory, so it cannot itself be a dependency of the settings store.
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<WaveDataPaths>();
        services.AddSingleton<IDeviceIdentity, MachineDeviceIdentity>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IAppLogger, FileAppLogger>();
        AddPlatformServices(services);
        services.AddSingleton<IDhcpAddressValidator, NetworkInterfaceDhcpValidator>();
        services.AddSingleton<IEthernetLinkProbe, NetworkInterfaceEthernetProbe>();
        services.AddSingleton<IContinuousPingMonitor, ContinuousPingMonitor>();
        services.AddSingleton<ISpeedMeter, HttpSpeedMeter>();
        services.AddSingleton<IStreamingProbe, HttpStreamingProbe>();
        services.AddSingleton<IHistoryExporter, CsvHistoryExporter>();
        services.AddSingleton<IHistoryExporter, XlsxHistoryExporter>();
        services.AddSingleton<IHistoryExporter, PdfHistoryExporter>();
        services.AddSingleton<INetworkProfileRepository, JsonNetworkProfileRepository>();
        services.AddSingleton<ITestRunRepository, JsonTestRunRepository>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
    }

    /// <summary>
    /// Registers the implementations that talk to the operating system's Wi-Fi and
    /// secret storage. The only place in the app that branches on the platform:
    /// everything above depends on the abstraction, not on netsh or nmcli.
    /// </summary>
    private static void AddPlatformServices(IServiceCollection services)
    {
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IWifiConnector, NetshWifiConnector>();
            services.AddSingleton<IWifiNetworkScanner, NetshWifiNetworkScanner>();
            services.AddSingleton<IWifiProfileCatalog, NetshWifiProfileCatalog>();
            services.AddSingleton<ICredentialStore, DpapiCredentialStore>();
            return;
        }

        services.AddSingleton<IWifiConnector, NmcliWifiConnector>();
        services.AddSingleton<IWifiNetworkScanner, NmcliWifiNetworkScanner>();
        services.AddSingleton<IWifiProfileCatalog, NmcliWifiProfileCatalog>();
        services.AddSingleton<ICredentialStore, LocalKeyCredentialStore>();
    }

    private static void AddPresentation(IServiceCollection services)
    {
        services.AddSingleton<IUserAlerts, MessageBoxUserAlerts>();
        services.AddSingleton<ICredentialPrompt, CredentialPromptService>();
        services.AddSingleton<IExportFileDialog, StorageProviderExportFileDialog>();
        services.AddSingleton<IAdminGate, AdminGateService>();
        services.AddSingleton<AppNavigator>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<AdminPasswordWindow>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
    }
}
