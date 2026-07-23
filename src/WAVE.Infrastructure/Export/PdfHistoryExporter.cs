using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using WAVE.Application.Abstractions;
using WAVE.Application.History;
using WAVE.Domain.Testing;

namespace WAVE.Infrastructure.Export;

/// <summary>
/// Renders the history as a formatted PDF report (MigraDoc/PDFsharp, MIT-licensed),
/// driven by <see cref="HistoryReport.Columns"/> and formatted through
/// <see cref="ReportCellText"/>. Landscape A4 with a title, a generation stamp, a repeating
/// table header and zebra striping. The GDI build resolves Windows fonts automatically.
/// </summary>
public sealed class PdfHistoryExporter : IHistoryExporter
{
    private static readonly Color HeaderFill = Color.FromRgb(0x0E, 0x16, 0x21);
    private static readonly Color ZebraFill = Color.FromRgb(0xF2, 0xF4, 0xF6);

    // Landscape A4 (29.7 cm) minus the 1.5 cm margins on each side.
    private const double UsableWidthCm = 29.7 - 3.0;

    public ExportFormat Format => ExportFormat.Pdf;

    public string FileExtension => "pdf";

    public string DisplayName => "PDF (relatório)";

    public Task ExportAsync(
        IReadOnlyList<TestRun> runs, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(output);
        cancellationToken.ThrowIfCancellationRequested();

        var document = BuildDocument(runs, cancellationToken);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(output);

        return Task.CompletedTask;
    }

    private static Document BuildDocument(IReadOnlyList<TestRun> runs, CancellationToken cancellationToken)
    {
        var columns = HistoryReport.Columns;

        var document = new Document();
        var setup = document.DefaultPageSetup;
        setup.Orientation = Orientation.Landscape;
        setup.PageFormat = PageFormat.A4;
        setup.LeftMargin = Unit.FromCentimeter(1.5);
        setup.RightMargin = Unit.FromCentimeter(1.5);
        setup.TopMargin = Unit.FromCentimeter(1.5);
        setup.BottomMargin = Unit.FromCentimeter(1.5);

        // The "Normal" style always exists in a fresh MigraDoc document.
        var normal = document.Styles["Normal"]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 8;

        var section = document.AddSection();

        var title = section.AddParagraph("WAVE — Histórico de execuções");
        title.Format.Font.Size = 16;
        title.Format.Font.Bold = true;
        title.Format.SpaceAfter = Unit.FromPoint(4);

        var subtitle = section.AddParagraph(
            $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} · {runs.Count} execução(ões)");
        subtitle.Format.Font.Size = 9;
        subtitle.Format.Font.Color = Colors.Gray;
        subtitle.Format.SpaceAfter = Unit.FromPoint(10);

        BuildTable(section, columns, runs, cancellationToken);

        return document;
    }

    private static void BuildTable(
        Section section,
        IReadOnlyList<HistoryColumn> columns,
        IReadOnlyList<TestRun> runs,
        CancellationToken cancellationToken)
    {
        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Borders.Color = Colors.LightGray;

        var columnWidth = Unit.FromCentimeter(UsableWidthCm / columns.Count);
        foreach (var _ in columns)
        {
            table.AddColumn(columnWidth);
        }

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Format.Font.Bold = true;
        header.Format.Font.Color = Colors.White;
        header.Shading.Color = HeaderFill;
        header.VerticalAlignment = VerticalAlignment.Center;
        for (var c = 0; c < columns.Count; c++)
        {
            header.Cells[c].AddParagraph(columns[c].Header);
        }

        for (var r = 0; r < runs.Count; r++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = table.AddRow();
            if (r % 2 == 1)
            {
                row.Shading.Color = ZebraFill;
            }

            for (var c = 0; c < columns.Count; c++)
            {
                row.Cells[c].AddParagraph(ReportCellText.Format(columns[c].Value(runs[r])));
            }
        }
    }
}
