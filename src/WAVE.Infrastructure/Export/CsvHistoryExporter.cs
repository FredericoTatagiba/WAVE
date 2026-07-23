using System.Text;
using WAVE.Application.Abstractions;
using WAVE.Application.History;
using WAVE.Domain.Testing;

namespace WAVE.Infrastructure.Export;

/// <summary>
/// Writes the history as CSV, driven by <see cref="HistoryReport.Columns"/>. Uses ';' as
/// the delimiter and pt-BR number/date formatting (via <see cref="ReportCellText"/>) so the
/// file opens cleanly in a Brazilian Excel, RFC 4180 quoting for fields with special
/// characters, and a UTF-8 BOM so accented headers are detected correctly. No external dependency.
/// </summary>
public sealed class CsvHistoryExporter : IHistoryExporter
{
    private const char Delimiter = ';';

    public ExportFormat Format => ExportFormat.Csv;

    public string FileExtension => "csv";

    public string DisplayName => "CSV (Excel / planilha)";

    public async Task ExportAsync(
        IReadOnlyList<TestRun> runs, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(output);

        await using var writer = new StreamWriter(
            output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);

        var header = string.Join(Delimiter, HistoryReport.Columns.Select(column => Escape(column.Header)));
        await writer.WriteLineAsync(header).ConfigureAwait(false);

        foreach (var run in runs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cells = HistoryReport.Columns.Select(column => Escape(ReportCellText.Format(column.Value(run))));
            await writer.WriteLineAsync(string.Join(Delimiter, cells)).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string Escape(string field)
    {
        var needsQuoting = field.Contains(Delimiter) || field.Contains('"')
            || field.Contains('\n') || field.Contains('\r');

        return needsQuoting ? $"\"{field.Replace("\"", "\"\"")}\"" : field;
    }
}
