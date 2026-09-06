using WAVE.Application.Abstractions;
using WAVE.Domain.Common;

namespace WAVE.Application.History;

/// <summary>
/// Coordinates history export: loads the run history, applies a <see cref="HistoryFilter"/>
/// and delegates writing to the <see cref="IHistoryExporter"/> that owns the requested
/// format.
/// </summary>
public sealed class HistoryExportService
{
    private const int LoadAllHistory = int.MaxValue;

    private readonly ITestRunRepository _history;
    private readonly Dictionary<ExportFormat, IHistoryExporter> _exporters;

    public HistoryExportService(ITestRunRepository history, IEnumerable<IHistoryExporter> exporters)
    {
        _history = history;
        _exporters = exporters.ToDictionary(exporter => exporter.Format);
    }

    /// <summary>The registered exporters, ordered by format — used to build the UI choices.</summary>
    public IReadOnlyList<IHistoryExporter> AvailableExporters =>
        _exporters.Values.OrderBy(exporter => exporter.Format).ToList();

    /// <summary>
    /// Writes the filtered history to <paramref name="output"/> in the requested format.
    /// Fails (without throwing) when the format has no exporter.
    /// </summary>
    public async Task<Result> ExportAsync(
        HistoryFilter filter, ExportFormat format, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(output);

        if (!_exporters.TryGetValue(format, out var exporter))
        {
            return Result.Failure($"Formato de exportação não suportado: {format}.");
        }

        var history = await _history.GetRecentAsync(LoadAllHistory, cancellationToken).ConfigureAwait(false);
        var rows = filter.Apply(history);
        await exporter.ExportAsync(rows, output, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
