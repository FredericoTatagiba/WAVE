using WAVE.Application.Abstractions;
using WAVE.Application.Networking;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Lists the Wi-Fi connections already saved in NetworkManager, the Linux equivalent of
/// the profiles Windows keeps. A network listed here connects without a password prompt.
/// </summary>
public sealed class NmcliWifiProfileCatalog : IWifiProfileCatalog
{
    private readonly IAppLogger _logger;

    public NmcliWifiProfileCatalog(IAppLogger logger) => _logger = logger;

    public async Task<IReadOnlyList<string>> GetSavedProfileNamesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await CommandLineExecutor
            .RunAsync("nmcli", NmcliCommands.ListConnections(), cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"nmcli connection show failed: {result.StandardOutput} {result.StandardError}");
            return [];
        }

        return NmcliOutputParser.ParseConnectionNames(result.StandardOutput);
    }

    public async Task<bool> ExistsAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return false;
        }

        var names = await GetSavedProfileNamesAsync(cancellationToken).ConfigureAwait(false);
        return names.Contains(ssid.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
