using Avalonia.Controls;

namespace WAVE.App.Services;

internal static class WindowExtensions
{
    /// <summary>
    /// Shows a window with no owner and completes when it closes.
    /// </summary>
    /// <remarks>
    /// Avalonia's <c>ShowDialog</c> requires an owner window, which does not exist during
    /// startup — the login runs before the main window is created. This fills that gap:
    /// the window is shown normally and its <c>Closed</c> event completes the task, so the
    /// caller can still await it the way it awaited WPF's blocking ShowDialog.
    /// </remarks>
    public static Task ShowStandaloneAsync(this Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var completion = new TaskCompletionSource();
        window.Closed += (_, _) => completion.TrySetResult();
        window.Show();
        return completion.Task;
    }
}
