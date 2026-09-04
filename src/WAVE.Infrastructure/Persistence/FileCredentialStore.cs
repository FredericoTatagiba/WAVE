using System.Text.Json;
using WAVE.Application.Abstractions;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Persistence;

/// <summary>
/// Shared credential-store mechanics: a single JSON file mapping the normalized SSID to
/// a Base64 ciphertext, guarded by an in-process mutex. Subclasses supply only the
/// encryption, which is the one part that differs per platform (DPAPI on Windows,
/// AES-GCM with a local key file elsewhere).
/// </summary>
public abstract class FileCredentialStore : ICredentialStore, IDisposable
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _file;

    protected FileCredentialStore(IAppLogger logger)
    {
        Logger = logger;
        AppPaths.EnsureCreated();
        _file = AppPaths.CredentialsFile;
    }

    protected IAppLogger Logger { get; }

    /// <summary>Encrypts a serialized secret. Must be readable back by <see cref="Unprotect"/>.</summary>
    protected abstract byte[] Protect(byte[] plaintext);

    /// <summary>Decrypts a stored blob. May throw; callers treat a failure as "no credential".</summary>
    protected abstract byte[] Unprotect(byte[] ciphertext);

    public async Task SaveAsync(string ssid, WifiSecret secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(secret);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var store = await LoadAsync(cancellationToken).ConfigureAwait(false);
            var plaintext = JsonSerializer.SerializeToUtf8Bytes(secret, WaveJson.Options);
            store[Key(ssid)] = Convert.ToBase64String(Protect(plaintext));
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

            var plaintext = Unprotect(Convert.FromBase64String(encoded));
            return JsonSerializer.Deserialize<WifiSecret>(plaintext, WaveJson.Options);
        }
        catch (Exception exception)
        {
            // Also the path taken by a store carried over from another OS or another user
            // account: the blob simply will not decrypt, and the app re-prompts once.
            Logger.Error("Failed to retrieve credential.", exception);
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
            Logger.Error("Failed to read credentials; returning empty.", exception);
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Writes to a temporary file and swaps it in. Truncating the live file in place means
    /// a crash mid-write leaves invalid JSON, which the read path turns into "no
    /// credentials at all" — every saved network silently lost.
    /// </summary>
    private async Task PersistAsync(Dictionary<string, string> store, CancellationToken cancellationToken)
    {
        var temporaryFile = _file + ".tmp";

        await using (var stream = File.Create(temporaryFile))
        {
            SecureFile.RestrictToOwner(temporaryFile);
            await JsonSerializer
                .SerializeAsync(stream, store, WaveJson.Options, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(temporaryFile, _file, overwrite: true);
    }

    public void Dispose()
    {
        _mutex.Dispose();
        GC.SuppressFinalize(this);
    }
}
