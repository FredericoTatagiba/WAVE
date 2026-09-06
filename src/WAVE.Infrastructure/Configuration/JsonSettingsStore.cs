using System.Text.Json;
using WAVE.Application.Abstractions;
using WAVE.Application.Configuration;
using WAVE.Infrastructure.Persistence;

namespace WAVE.Infrastructure.Configuration;

/// <summary>Settings in a JSON file at the fixed local data directory.</summary>
/// <remarks>
/// Loads eagerly and synchronously in the constructor, and takes no logger. Everything
/// else — the logger included — resolves its paths through these settings, so depending on
/// <see cref="IAppLogger"/> here would close a cycle. A settings file that cannot be read
/// therefore degrades to defaults silently; the alternative is an app that will not start.
/// </remarks>
public sealed class JsonSettingsStore : ISettingsStore, IDisposable
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly string _file;

    private WaveSettings _current;

    public JsonSettingsStore()
    {
        AppPaths.EnsureCreated();
        _file = AppPaths.SettingsFile;
        _current = Load();
    }

    public WaveSettings Current => _current;

    public event EventHandler? Changed;

    public async Task SaveAsync(WaveSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using (var stream = File.Create(_file))
            {
                await JsonSerializer
                    .SerializeAsync(stream, settings, WaveJson.Options, cancellationToken)
                    .ConfigureAwait(false);
            }

            _current = settings;
        }
        finally
        {
            _mutex.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private WaveSettings Load()
    {
        if (!File.Exists(_file))
        {
            return new WaveSettings();
        }

        try
        {
            using var stream = File.OpenRead(_file);
            return JsonSerializer.Deserialize<WaveSettings>(stream, WaveJson.Options) ?? new WaveSettings();
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or JsonException)
        {
            return new WaveSettings();
        }
    }

    public void Dispose() => _mutex.Dispose();
}
