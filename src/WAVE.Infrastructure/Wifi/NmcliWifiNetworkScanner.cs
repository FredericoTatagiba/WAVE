using WAVE.Application.Abstractions;
using WAVE.Application.Networking;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Lists the visible networks via <c>nmcli device wifi list</c>. Parsing lives in
/// <see cref="NmcliOutputParser"/>; this class only runs the command.
/// </summary>
public sealed class NmcliWifiNetworkScanner : IWifiNetworkScanner
{
    private readonly IAppLogger _logger;

    public NmcliWifiNetworkScanner(IAppLogger logger) => _logger = logger;

    public async Task<IReadOnlyList<AvailableNetwork>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var result = await CommandLineExecutor
            .RunAsync("nmcli", NmcliCommands.Scan(), cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            // Same contract as the netsh scanner: a scan failure yields no networks
            // rather than an exception, so the UI shows an empty list and stays usable.
            _logger.Warn($"nmcli wifi list failed: {result.StandardOutput} {result.StandardError}");
            return [];
        }

        return NmcliOutputParser.ParseScan(result.StandardOutput);
    }
}
