using System.Text.Json;
using WAVE.Application.Abstractions;
using WAVE.Application.Testing;
using WAVE.Domain.Testing;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Persistence;

/// <summary>Run history in a JSON file (most recent first).</summary>
public sealed class JsonTestRunRepository : ITestRunRepository
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly IAppLogger _logger;
    private readonly string _file;
    private readonly int _maxItems;

    public JsonTestRunRepository(IAppLogger logger, TestRunnerOptions options)
    {
        _logger = logger;
        _maxItems = options.MaxHistoryEntries;
        AppPaths.EnsureCreated();
        _file = AppPaths.HistoryFile;
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
        if (!File.Exists(_file))
        {
            return new List<TestRun>();
        }

        try
        {
            await using var stream = File.OpenRead(_file);
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
        await using var stream = File.Create(_file);
        await JsonSerializer.SerializeAsync(stream, runs, WaveJson.Options, cancellationToken).ConfigureAwait(false);
    }
}
