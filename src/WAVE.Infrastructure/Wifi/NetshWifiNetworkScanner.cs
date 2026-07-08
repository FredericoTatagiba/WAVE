using System.Text;
using System.Text.RegularExpressions;
using WAVE.Application.Abstractions;
using WAVE.Application.Networking;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Varre redes visíveis via <c>netsh wlan show networks</c>. A análise foca em
/// tokens estáveis (o rótulo "SSID N :" e os padrões "WPA2"/"WPA3"/"%"), evitando
/// depender de textos localizados do Windows.
/// </summary>
public sealed class NetshWifiNetworkScanner : IWifiNetworkScanner
{
    private static readonly Regex SsidLine =
        new(@"^\s*SSID\s+\d+\s*:\s*(?<name>.*)$", RegexOptions.Compiled);

    private static readonly Regex SignalValue =
        new(@"(?<pct>\d{1,3})\s*%", RegexOptions.Compiled);

    private readonly IAppLogger _logger;
    private readonly CommandLineExecutor _executor = new();

    public NetshWifiNetworkScanner(IAppLogger logger) => _logger = logger;

    public async Task<IReadOnlyList<AvailableNetwork>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var result = await _executor
            .RunAsync("netsh", "wlan show networks mode=ssid", cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"netsh show networks falhou: {result.StandardOutput} {result.StandardError}");
            return Array.Empty<AvailableNetwork>();
        }

        return Parse(result.StandardOutput);
    }

    private static IReadOnlyList<AvailableNetwork> Parse(string output)
    {
        var networks = new List<AvailableNetwork>();
        var block = new StringBuilder();
        string? currentSsid = null;

        void Flush()
        {
            if (!string.IsNullOrWhiteSpace(currentSsid))
            {
                var text = block.ToString();
                networks.Add(new AvailableNetwork(currentSsid!.Trim(), DetermineSecurity(text), ExtractSignal(text)));
            }

            block.Clear();
        }

        foreach (var line in output.Replace("\r\n", "\n").Split('\n'))
        {
            var match = SsidLine.Match(line);
            if (match.Success)
            {
                Flush();
                currentSsid = match.Groups["name"].Value;
            }
            else
            {
                block.AppendLine(line);
            }
        }

        Flush();
        return networks;
    }

    private static SecurityType DetermineSecurity(string blockText) =>
        WifiSecurityParser.FromNetshBlock(blockText);

    private static int ExtractSignal(string blockText)
    {
        var match = SignalValue.Match(blockText);
        return match.Success && int.TryParse(match.Groups["pct"].Value, out var percent)
            ? Math.Clamp(percent, 0, 100)
            : 0;
    }
}
