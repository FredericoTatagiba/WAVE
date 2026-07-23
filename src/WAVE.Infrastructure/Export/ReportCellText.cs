using System.Globalization;

namespace WAVE.Infrastructure.Export;

/// <summary>
/// Formats a typed report cell value (from <c>HistoryReport.Columns</c>) into a pt-BR
/// display string. Shared by the text-based exporters (CSV, PDF) so dates and numbers
/// look the same across formats.
/// </summary>
internal static class ReportCellText
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    public static string Format(object? value) => value switch
    {
        null => string.Empty,
        DateTimeOffset dateTime => dateTime.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss", Culture),
        double number => number.ToString("0.##", Culture),
        int number => number.ToString(Culture),
        string text => text,
        _ => Convert.ToString(value, Culture) ?? string.Empty
    };
}
