using System.Text.Json;
using WAVE.Application.Abstractions;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Persistence;

/// <summary>Profile repository in a JSON file, with serialized access.</summary>
public sealed class JsonNetworkProfileRepository : INetworkProfileRepository
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly IAppLogger _logger;
    private readonly string _file;

    public JsonNetworkProfileRepository(IAppLogger logger)
    {
        _logger = logger;
        AppPaths.EnsureCreated();
        _file = AppPaths.ProfilesFile;
    }

    public async Task<IReadOnlyList<WifiNetworkProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<WifiNetworkProfile?> FindAsync(string ssid, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(p => string.Equals(p.Ssid, ssid, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveAsync(WifiNetworkProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profiles = await LoadAsync(cancellationToken).ConfigureAwait(false);
            profiles.RemoveAll(p => string.Equals(p.Ssid, profile.Ssid, StringComparison.OrdinalIgnoreCase));
            profiles.Add(profile);
            await PersistAsync(profiles, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task DeleteAsync(string ssid, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profiles = await LoadAsync(cancellationToken).ConfigureAwait(false);
            profiles.RemoveAll(p => string.Equals(p.Ssid, ssid, StringComparison.OrdinalIgnoreCase));
            await PersistAsync(profiles, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<List<WifiNetworkProfile>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_file))
        {
            return new List<WifiNetworkProfile>();
        }

        try
        {
            await using var stream = File.OpenRead(_file);
            var profiles = await JsonSerializer
                .DeserializeAsync<List<WifiNetworkProfile>>(stream, WaveJson.Options, cancellationToken)
                .ConfigureAwait(false);
            return profiles ?? new List<WifiNetworkProfile>();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to read profiles; returning empty list.", exception);
            return new List<WifiNetworkProfile>();
        }
    }

    private async Task PersistAsync(List<WifiNetworkProfile> profiles, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_file);
        await JsonSerializer.SerializeAsync(stream, profiles, WaveJson.Options, cancellationToken).ConfigureAwait(false);
    }
}
