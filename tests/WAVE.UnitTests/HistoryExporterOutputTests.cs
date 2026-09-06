using System.Text;
using WAVE.Application.Abstractions;
using WAVE.Domain.Testing;
using WAVE.Infrastructure.Export;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Renders each exporter for real. The PDF case is the one that matters: the core
/// (non-GDI) PDFsharp build does no system font lookup, so a missing or misnamed embedded
/// face throws here rather than in front of an operator hitting "Export".
/// </summary>
public class HistoryExporterOutputTests
{
    private static IReadOnlyList<TestRun> SampleRuns() =>
    [
        new TestRun
        {
            Id = Guid.NewGuid(),
            Ssid = "Corp-Guest",
            DeviceName = "TABLET-01",
            StartedAt = new DateTimeOffset(2026, 3, 1, 9, 30, 0, TimeSpan.Zero),
            FinalState = TestOperationState.TestRunning
        },
        new TestRun
        {
            Id = Guid.NewGuid(),
            // Accented and non-Latin text: DejaVu covers these, a wrong fallback would not.
            Ssid = "Rede-Ação",
            DeviceName = "TABLET-02",
            StartedAt = new DateTimeOffset(2026, 3, 2, 14, 0, 0, TimeSpan.Zero),
            FinalState = TestOperationState.Failed
        }
    ];

    private static async Task<byte[]> ExportAsync(IHistoryExporter exporter)
    {
        using var buffer = new MemoryStream();
        await exporter.ExportAsync(SampleRuns(), buffer);
        return buffer.ToArray();
    }

    [Fact]
    public async Task Pdf_RendersWithEmbeddedFonts()
    {
        var bytes = await ExportAsync(new PdfHistoryExporter());

        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5), StringComparison.Ordinal);
        Assert.True(bytes.Length > 1024, $"PDF is only {bytes.Length} bytes; expected a real document.");
    }

    [Fact]
    public async Task Xlsx_ProducesZipContainer()
    {
        var bytes = await ExportAsync(new XlsxHistoryExporter());

        // XLSX is a zip; "PK" is the local file header signature.
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public async Task Csv_KeepsAccentedTextIntact()
    {
        var text = Encoding.UTF8.GetString(await ExportAsync(new CsvHistoryExporter()));

        Assert.Contains("Rede-Ação", text, StringComparison.Ordinal);
    }
}
