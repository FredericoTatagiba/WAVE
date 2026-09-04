using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace WAVE.App.Services;

/// <summary>
/// Implements alerts with a modal message box carrying a severity icon (the visual error
/// feedback required by the specification).
/// </summary>
/// <remarks>
/// The interface stays synchronous although Avalonia's dialogs are not: no caller uses the
/// result, and making it awaitable would ripple through sixteen call sites to no effect.
/// The dialog is posted to the UI thread and left to run.
/// <para>
/// The audible cue the WPF version played (<c>SystemSounds.Hand</c>) is dropped: it has no
/// cross-platform equivalent. The icon carries the severity.
/// </para>
/// </remarks>
public sealed class MessageBoxUserAlerts : IUserAlerts
{
    public void Error(string message) => Show("WAVE — Falha", message, Icon.Error);

    public void Info(string message) => Show("WAVE", message, Icon.Info);

    private static void Show(string title, string message, Icon icon) =>
        Dispatcher.UIThread.Post(() =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok, icon);

            // Before the main window exists (a failure during login) there is nothing to
            // be modal to, so the alert is shown as its own window.
            _ = AppWindows.Owner is { } owner
                ? box.ShowWindowDialogAsync(owner)
                : box.ShowWindowAsync();
        });
}
