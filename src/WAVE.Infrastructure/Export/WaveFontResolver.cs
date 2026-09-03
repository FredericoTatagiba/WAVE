using System.Reflection;
using PdfSharp.Fonts;

namespace WAVE.Infrastructure.Export;

/// <summary>
/// Supplies the report font to PDFsharp from faces embedded in this assembly.
/// The core (non-GDI) PDFsharp build does no system font lookup at all, so without a
/// resolver every render throws. Embedding the faces — rather than probing
/// <c>/usr/share/fonts</c> or the Windows font directory — is what makes a report
/// byte-identical regardless of which machine produced it.
/// </summary>
/// <remarks>
/// Every requested family maps to DejaVu Sans. The exporter asks for "Arial"; on a
/// stock Linux box that family does not exist, and silently falling back would change
/// the column widths of a document whose layout is measured in centimetres.
/// </remarks>
public sealed class WaveFontResolver : IFontResolver
{
    private const string Regular = "WAVE#DejaVuSans";
    private const string Bold = "WAVE#DejaVuSans-Bold";

    private const string ResourcePrefix = "WAVE.Infrastructure.Export.Fonts.";

    /// <summary>Registers this resolver globally. Idempotent; safe to call at every startup.</summary>
    public static void Register()
    {
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new WaveFontResolver();
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        // Italic is not requested anywhere in the report and DejaVu's oblique face is not
        // embedded; PDFsharp synthesises a slant from the regular face if it ever is.
        new FontResolverInfo(bold ? Bold : Regular);

    public byte[]? GetFont(string faceName)
    {
        var fileName = faceName switch
        {
            Bold => "DejaVuSans-Bold.ttf",
            _ => "DejaVuSans.ttf"
        };

        var assembly = typeof(WaveFontResolver).GetTypeInfo().Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException(
                $"Embedded font '{fileName}' not found. Check the EmbeddedResource glob in WAVE.Infrastructure.csproj.");

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
