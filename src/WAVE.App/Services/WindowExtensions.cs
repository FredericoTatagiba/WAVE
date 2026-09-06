using Avalonia.Controls;

namespace WAVE.App.Services;

internal static class WindowExtensions
{
    /// <summary>
    /// Shows a window with no owner and completes when it closes.
    /// </summary>
    /// <remarks>
    /// Avalonia's <c>ShowDialog</c> requires an owner window. The main window now exists
    /// for the whole life of the app, so this is a fallback rather than a normal path: it
    /// keeps a prompt from being swallowed if it is ever raised before the shell is up.
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
