using WAVE.Application.History;
using WAVE.Domain.Testing;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Writes a set of test runs to a specific file format (Strategy). Each implementation
/// owns one <see cref="Format"/>; new formats are added without touching the caller
/// (Open/Closed). The service resolves the right strategy from the requested format.
/// </summary>
public interface IHistoryExporter
{
    /// <summary>The format this exporter produces.</summary>
    ExportFormat Format { get; }

    /// <summary>File extension without the dot (e.g. "csv"), for the save dialog and file name.</summary>
    string FileExtension { get; }

    /// <summary>Human-readable label for the format (e.g. "CSV (comma-separated)").</summary>
    string DisplayName { get; }

    /// <summary>Writes the runs to <paramref name="output"/> in this format.</summary>
    Task ExportAsync(
        IReadOnlyList<TestRun> runs, Stream output, CancellationToken cancellationToken = default);
}
