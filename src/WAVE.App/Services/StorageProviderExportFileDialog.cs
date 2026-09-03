using Avalonia.Controls;
using Avalonia.Platform.Storage;
using WAVE.Application.Abstractions;

namespace WAVE.App.Services;

/// <summary>
/// Implements <see cref="IExportFileDialog"/> with Avalonia's storage provider, which maps
/// to the platform's native save dialog. The available exporters become the dialog's file
/// types (in order), and the type the user picked identifies the chosen format.
/// </summary>
public sealed class StorageProviderExportFileDialog : IExportFileDialog
{
    public async Task<ExportTarget?> PickSaveTargetAsync(
        IReadOnlyList<IHistoryExporter> formats, string suggestedFileName)
    {
        ArgumentNullException.ThrowIfNull(formats);

        if (formats.Count == 0 || AppWindows.Owner is not { } owner)
        {
            return null;
        }

        var storage = TopLevel.GetTopLevel(owner)?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Exportar histórico",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = formats[0].FileExtension,
            ShowOverwritePrompt = true,
            FileTypeChoices = [.. formats.Select(ToFileType)]
        });

        if (file is null)
        {
            return null;
        }

        var path = file.Path.LocalPath;

        // Unlike WPF's FilterIndex, the picker does not report which type was selected —
        // GTK and the macOS panel do not expose it. The extension the user ended up with
        // is the reliable signal, and it also honours a manually typed one.
        var chosen = formats.FirstOrDefault(
            format => path.EndsWith($".{format.FileExtension}", StringComparison.OrdinalIgnoreCase))
            ?? formats[0];

        return new ExportTarget(EnsureExtension(path, chosen.FileExtension), chosen.Format);
    }

    private static FilePickerFileType ToFileType(IHistoryExporter exporter) =>
        new(exporter.DisplayName) { Patterns = [$"*.{exporter.FileExtension}"] };

    /// <summary>
    /// Appends the format's extension when the picker returned a bare name — some Linux
    /// dialogs do not add one, and the exporter would then write a file the desktop
    /// cannot associate with anything.
    /// </summary>
    private static string EnsureExtension(string path, string extension) =>
        path.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase) ? path : $"{path}.{extension}";
}
