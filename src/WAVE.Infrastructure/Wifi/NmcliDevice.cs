using WAVE.Application.Abstractions;
using WAVE.Application.Networking;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>Locates the Wi-Fi interface NetworkManager is managing.</summary>
internal static class NmcliDevice
{
    public static async Task<string?> FindWifiInterfaceAsync(
        IAppLogger logger, CancellationToken cancellationToken)
    {
        var result = await CommandLineExecutor
            .RunAsync("nmcli", NmcliCommands.ListDevices(), cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            logger.Warn($"nmcli device status failed: {result.StandardOutput} {result.StandardError}");
            return null;
        }

        var device = NmcliOutputParser.ParseFirstWifiDevice(result.StandardOutput);
        if (device is null)
        {
            logger.Warn("No Wi-Fi interface reported by NetworkManager.");
        }

        return device;
    }
}
