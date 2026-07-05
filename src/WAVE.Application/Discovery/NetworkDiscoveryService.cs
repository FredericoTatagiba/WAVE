using WAVE.Application.Abstractions;
using WAVE.Domain.Networking;

namespace WAVE.Application.Discovery;

/// <summary>
/// Monta a lista de redes para teste combinando: (1) redes próximas visíveis,
/// (2) perfis já salvos no Windows e (3) perfis cadastrados no WAVE. Assim o
/// operador não precisa digitar SSIDs, e redes conhecidas pelo Windows já ficam
/// prontas para testar sem senha.
/// </summary>
public sealed class NetworkDiscoveryService
{
    private readonly IWifiNetworkScanner _scanner;
    private readonly IWifiProfileCatalog _catalog;
    private readonly INetworkProfileRepository _repository;
    private readonly IAppLogger _logger;

    public NetworkDiscoveryService(
        IWifiNetworkScanner scanner,
        IWifiProfileCatalog catalog,
        INetworkProfileRepository repository,
        IAppLogger logger)
    {
        _scanner = scanner;
        _catalog = catalog;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DiscoveredNetwork>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var scanned = await SafeScanAsync(cancellationToken).ConfigureAwait(false);
        var savedNames = await SafeSavedProfilesAsync(cancellationToken).ConfigureAwait(false);
        var stored = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var saved = new HashSet<string>(savedNames, StringComparer.OrdinalIgnoreCase);
        var storedSsids = new HashSet<string>(stored.Select(p => p.Ssid), StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, DiscoveredNetwork>(StringComparer.OrdinalIgnoreCase);

        foreach (var network in scanned)
        {
            var profile = TryCreateProfile(network.Ssid, network.Security);
            if (profile is null || result.ContainsKey(profile.Ssid))
            {
                continue;
            }

            var ready = IsReady(profile.Ssid, network.Security, saved, storedSsids);
            result[profile.Ssid] = new DiscoveredNetwork(profile, ready, network.SignalPercent);
        }

        foreach (var name in savedNames)
        {
            var profile = TryCreateProfile(name, SecurityType.Wpa2Personal);
            if (profile is not null && !result.ContainsKey(profile.Ssid))
            {
                result[profile.Ssid] = new DiscoveredNetwork(profile, ReadyToConnect: true, SignalPercent: 0);
            }
        }

        foreach (var profile in stored)
        {
            if (!result.ContainsKey(profile.Ssid))
            {
                result[profile.Ssid] = new DiscoveredNetwork(profile, ReadyToConnect: true, SignalPercent: 0);
            }
        }

        return result.Values
            .OrderByDescending(n => n.SignalPercent)
            .ThenBy(n => n.Profile.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static bool IsReady(string ssid, SecurityType security, HashSet<string> saved, HashSet<string> stored) =>
        security == SecurityType.Open || saved.Contains(ssid) || stored.Contains(ssid);

    private WifiNetworkProfile? TryCreateProfile(string ssid, SecurityType security)
    {
        try
        {
            return new WifiNetworkProfile(ssid, ssid, security);
        }
        catch (ArgumentException exception)
        {
            _logger.Warn($"SSID ignorado na descoberta: {exception.Message}");
            return null;
        }
    }

    private async Task<IReadOnlyList<AvailableNetwork>> SafeScanAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _scanner.ScanAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Falha ao escanear redes próximas.", exception);
            return Array.Empty<AvailableNetwork>();
        }
    }

    private async Task<IReadOnlyList<string>> SafeSavedProfilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _catalog.GetSavedProfileNamesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Falha ao listar perfis salvos no Windows.", exception);
            return Array.Empty<string>();
        }
    }
}
