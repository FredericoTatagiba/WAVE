using ClosedXML.Excel;
using WAVE.Application.Abstractions;
using WAVE.Application.History;
using WAVE.Domain.Testing;

namespace WAVE.Infrastructure.Export;

/// <summary>
/// Writes the history as a real .xlsx workbook (ClosedXML), driven by
/// <see cref="HistoryReport.Columns"/>. Cells are typed (numbers and dates keep their
/// native type, not text) so Excel can sort/filter them; the header is styled, frozen
/// and gets an auto-filter.
/// </summary>
public sealed class XlsxHistoryExporter : IHistoryExporter
{
    private const string DateFormat = "dd/mm/yyyy hh:mm:ss";
    private const string NumberFormat = "0.##";

    public ExportFormat Format => ExportFormat.Xlsx;

    public string FileExtension => "xlsx";

    public string DisplayName => "Excel (.xlsx)";

    public Task ExportAsync(
        IReadOnlyList<TestRun> runs, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(output);
        cancellationToken.ThrowIfCancellationRequested();

        var columns = HistoryReport.Columns;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Histórico");

        for (var c = 0; c < columns.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c].Header;
        }

        for (var r = 0; r < runs.Count; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var c = 0; c < columns.Count; c++)
            {
                SetCell(sheet.Cell(r + 2, c + 1), columns[c].Value(runs[r]));
            }
        }

        StyleHeader(sheet, columns.Count);

        var lastRow = runs.Count + 1;
        sheet.Range(1, 1, lastRow, columns.Count).SetAutoFilter();
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        workbook.SaveAs(output);
        return Task.CompletedTask;
    }

    private static void SetCell(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case DateTimeOffset dateTime:
                cell.Value = dateTime.LocalDateTime;
                cell.Style.NumberFormat.Format = DateFormat;
                break;
            case double number:
                cell.Value = number;
                cell.Style.NumberFormat.Format = NumberFormat;
                break;
            case int number:
                cell.Value = number;
                break;
            case string text:
                cell.Value = text;
                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }

    private static void StyleHeader(IXLWorksheet sheet, int columnCount)
    {
        var header = sheet.Range(1, 1, 1, columnCount);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0E1621");
    }
}
