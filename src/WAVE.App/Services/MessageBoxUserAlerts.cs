using System.Media;
using System.Windows;

namespace WAVE.App.Services;

/// <summary>
/// Implements alerts with <see cref="MessageBox"/> and a system sound
/// (audible/visual error feedback required by the specification).
/// </summary>
public sealed class MessageBoxUserAlerts : IUserAlerts
{
    public void Error(string message)
    {
        SystemSounds.Hand.Play();
        MessageBox.Show(message, "WAVE — Falha", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void Info(string message) =>
        MessageBox.Show(message, "WAVE", MessageBoxButton.OK, MessageBoxImage.Information);
}
