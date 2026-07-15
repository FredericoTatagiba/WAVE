using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WAVE.Application.Abstractions;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Persistence;

/// <summary>
/// Stores network secrets encrypted with DPAPI (current-user scope).
/// The secrets are never written in clear text.
/// </summary>
public sealed class DpapiCredentialStore : ICredentialStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WAVE.Credential.v1");

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly IAppLogger _logger;
    private readonly string _file;

    public DpapiCredentialStore(IAppLogger logger)
    {
        _logger = logger;
        AppPaths.EnsureCreated();
        _file = AppPaths.CredentialsFile;
    }

    public async Task SaveAsync(string ssid, WifiSecret secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(secret);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await LoadAsync(cancellationToken).ConfigureAwait(false);
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(secret, WaveJson.Options);
            var encrypted = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
            store[Key(ssid)] = Convert.ToBase64String(encrypted);
            await PersistAsync(store, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<WifiSecret?> GetAsync(string ssid, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!store.TryGetValue(Key(ssid), out var encoded))
            {
                return null;
            }

            var encrypted = Convert.FromBase64String(encoded);
            var plaintext = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<WifiSecret>(plaintext, WaveJson.Options);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to retrieve credential.", exception);
            return null;
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
            var store = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (store.Remove(Key(ssid)))
            {
                await PersistAsync(store, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    private static string Key(string ssid) => ssid.Trim().ToLowerInvariant();

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_file))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            await using var stream = File.OpenRead(_file);
            var store = await JsonSerializer
                .DeserializeAsync<Dictionary<string, string>>(stream, WaveJson.Options, cancellationToken)
                .ConfigureAwait(false);
            return store ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to read credentials; returning empty.", exception);
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private async Task PersistAsync(Dictionary<string, string> store, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_file);
        await JsonSerializer.SerializeAsync(stream, store, WaveJson.Options, cancellationToken).ConfigureAwait(false);
    }
}
