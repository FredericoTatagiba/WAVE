using System.Text.Json;
using WAVE.Application.Abstractions;
using WAVE.Application.Testing;
using WAVE.Domain.Testing;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Persistence;

/// <summary>Run history in a JSON file (most recent first).</summary>
public sealed class JsonTestRunRepository : ITestRunRepository, IDisposable
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly IAppLogger _logger;
    private readonly WaveDataPaths _paths;
    private readonly int _maxItems;

    public JsonTestRunRepository(IAppLogger logger, WaveDataPaths paths, TestRunnerOptions options)
    {
        _logger = logger;
        _paths = paths;
        _maxItems = options.MaxHistoryEntries;
    }

    public async Task AddAsync(TestRun run, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var runs = await LoadAsync(cancellationToken).ConfigureAwait(false);
            runs.Insert(0, run);

            if (runs.Count > _maxItems)
            {
                runs = runs.Take(_maxItems).ToList();
            }

            await PersistAsync(runs, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<TestRun>> GetRecentAsync(int maxItems, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var runs = await LoadAsync(cancellationToken).ConfigureAwait(false);
            return runs.Take(Math.Max(0, maxItems)).ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<List<TestRun>> LoadAsync(CancellationToken cancellationToken)
    {
        var file = _paths.HistoryFile;
        if (!File.Exists(file))
        {
            return new List<TestRun>();
        }

        try
        {
            await using var stream = File.OpenRead(file);
            var runs = await JsonSerializer
                .DeserializeAsync<List<TestRun>>(stream, WaveJson.Options, cancellationToken)
                .ConfigureAwait(false);
            return runs ?? new List<TestRun>();
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to read history; returning empty list.", exception);
            return new List<TestRun>();
        }
    }

    private async Task PersistAsync(List<TestRun> runs, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_paths.HistoryFile);
        await JsonSerializer.SerializeAsync(stream, runs, WaveJson.Options, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _mutex.Dispose();
}
