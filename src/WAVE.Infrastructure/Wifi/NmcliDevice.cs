using WAVE.Application.Abstractions;
using WAVE.Application.Networking;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>Locates the interfaces NetworkManager is managing.</summary>
internal static class NmcliDevice
{
    public static Task<string?> FindWifiInterfaceAsync(
        IAppLogger logger, CancellationToken cancellationToken) =>
        FindAsync(logger, NmcliOutputParser.ParseFirstWifiDevice, "Wi-Fi", cancellationToken);

    public static Task<string?> FindEthernetInterfaceAsync(
        IAppLogger logger, CancellationToken cancellationToken) =>
        FindAsync(logger, NmcliOutputParser.ParseFirstEthernetDevice, "wired", cancellationToken);

    private static async Task<string?> FindAsync(
        IAppLogger logger,
        Func<string, string?> parse,
        string description,
        CancellationToken cancellationToken)
    {
        var result = await CommandLineExecutor
            .RunAsync("nmcli", NmcliCommands.ListDevices(), cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            logger.Warn($"nmcli device status failed: {result.StandardOutput} {result.StandardError}");
            return null;
        }

        var device = parse(result.StandardOutput);
        if (device is null)
        {
            logger.Warn($"No {description} interface reported by NetworkManager.");
        }

        return device;
    }
}
