using WAVE.Application.Abstractions;
using WAVE.Application.History;

namespace WAVE.App.Services;

/// <summary>The location and format the user picked in the save dialog.</summary>
public sealed record ExportTarget(string Path, ExportFormat Format);

/// <summary>
/// Abstracts the "Save as…" picker for the history export, keeping the ViewModel free of
/// WPF dialog types (Dependency Inversion, mirroring <see cref="ICredentialPrompt"/>). The
/// dialog offers one entry per available exporter and reports which one the user chose.
/// </summary>
public interface IExportFileDialog
{
    /// <summary>Shows the save dialog; returns the chosen target, or null if cancelled.</summary>
    ExportTarget? PickSaveTarget(IReadOnlyList<IHistoryExporter> formats, string suggestedFileName);
}
