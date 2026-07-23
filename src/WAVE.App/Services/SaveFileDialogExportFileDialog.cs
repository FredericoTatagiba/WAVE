using Microsoft.Win32;
using WAVE.Application.Abstractions;

namespace WAVE.App.Services;

/// <summary>
/// Implements <see cref="IExportFileDialog"/> with a WPF <see cref="SaveFileDialog"/>. The
/// available exporters become the dialog's file-type entries (in order), so the selected
/// filter index maps back to the chosen exporter's format.
/// </summary>
public sealed class SaveFileDialogExportFileDialog : IExportFileDialog
{
    public ExportTarget? PickSaveTarget(IReadOnlyList<IHistoryExporter> formats, string suggestedFileName)
    {
        ArgumentNullException.ThrowIfNull(formats);
        if (formats.Count == 0)
        {
            return null;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Exportar histórico",
            FileName = suggestedFileName,
            Filter = string.Join("|", formats.Select(f => $"{f.DisplayName}|*.{f.FileExtension}")),
            DefaultExt = formats[0].FileExtension,
            AddExtension = true,
            OverwritePrompt = true
        };

        var owner = System.Windows.Application.Current?.MainWindow;
        var confirmed = owner is not null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        if (confirmed != true)
        {
            return null;
        }

        // FilterIndex is 1-based and follows the same order as the formats list.
        var chosen = formats[Math.Clamp(dialog.FilterIndex - 1, 0, formats.Count - 1)];
        return new ExportTarget(dialog.FileName, chosen.Format);
    }
}
