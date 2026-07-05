using WAVE.Application.Abstractions;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Lista os perfis salvos no Windows via <c>netsh wlan show profiles</c>. Extrai o
/// nome à direita do separador " : ", independente do idioma do sistema.
/// </summary>
public sealed class NetshWifiProfileCatalog : IWifiProfileCatalog
{
    private const string KeyValueSeparator = " : ";

    private readonly IAppLogger _logger;
    private readonly CommandLineExecutor _executor = new();

    public NetshWifiProfileCatalog(IAppLogger logger) => _logger = logger;

    public async Task<IReadOnlyList<string>> GetSavedProfileNamesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _executor
            .RunAsync("netsh", "wlan show profiles", cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"netsh show profiles falhou: {result.StandardOutput} {result.StandardError}");
            return Array.Empty<string>();
        }

        return Parse(result.StandardOutput);
    }

    public async Task<bool> ExistsAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return false;
        }

        var names = await GetSavedProfileNamesAsync(cancellationToken).ConfigureAwait(false);
        return names.Any(name => string.Equals(name, ssid.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> Parse(string output)
    {
        var names = new List<string>();

        foreach (var line in output.Replace("\r\n", "\n").Split('\n'))
        {
            var index = line.IndexOf(KeyValueSeparator, StringComparison.Ordinal);
            if (index < 0)
            {
                continue;
            }

            var name = line[(index + KeyValueSeparator.Length)..].Trim();
            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        return names;
    }
}
