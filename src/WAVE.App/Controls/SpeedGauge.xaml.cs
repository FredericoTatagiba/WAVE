using System.Windows;
using System.Windows.Controls;

namespace WAVE.App.Controls;

/// <summary>
/// fast.com-style speed gauge: a large central download number that climbs live during
/// the measurement, the upload rate as a secondary value, and the current phase label.
/// A dumb, reusable component driven entirely by its dependency properties.
/// </summary>
public partial class SpeedGauge : UserControl
{
    public static readonly DependencyProperty DownloadMbpsProperty = DependencyProperty.Register(
        nameof(DownloadMbps), typeof(double), typeof(SpeedGauge), new PropertyMetadata(0d));

    public static readonly DependencyProperty UploadMbpsProperty = DependencyProperty.Register(
        nameof(UploadMbps), typeof(double), typeof(SpeedGauge), new PropertyMetadata(0d));

    public static readonly DependencyProperty PhaseTextProperty = DependencyProperty.Register(
        nameof(PhaseText), typeof(string), typeof(SpeedGauge), new PropertyMetadata(string.Empty));

    public SpeedGauge() => InitializeComponent();

    /// <summary>Live download rate (Mbps) — the hero number.</summary>
    public double DownloadMbps
    {
        get => (double)GetValue(DownloadMbpsProperty);
        set => SetValue(DownloadMbpsProperty, value);
    }

    /// <summary>Live upload rate (Mbps) — the secondary value.</summary>
    public double UploadMbps
    {
        get => (double)GetValue(UploadMbpsProperty);
        set => SetValue(UploadMbpsProperty, value);
    }

    /// <summary>Current phase label ("Baixando…"/"Enviando…"); hidden when empty.</summary>
    public string PhaseText
    {
        get => (string)GetValue(PhaseTextProperty);
        set => SetValue(PhaseTextProperty, value);
    }
}
