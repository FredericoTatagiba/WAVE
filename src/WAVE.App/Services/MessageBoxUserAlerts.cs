using System.Media;
using System.Windows;

namespace WAVE.App.Services;

/// <summary>
/// Implementa alertas com <see cref="MessageBox"/> e som do sistema
/// (feedback sonoro/visual de erro exigido pela especificação).
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
