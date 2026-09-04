using Avalonia;
using Avalonia.Controls;

namespace WAVE.App.Controls;

/// <summary>
/// fast.com-style speed gauge: a large central download number that climbs live during
/// the measurement, the upload rate as a secondary value, and the current phase label.
/// A dumb, reusable component driven entirely by its styled properties.
/// </summary>
public partial class SpeedGauge : UserControl
{
    public static readonly StyledProperty<double> DownloadMbpsProperty =
        AvaloniaProperty.Register<SpeedGauge, double>(nameof(DownloadMbps));

    public static readonly StyledProperty<double> UploadMbpsProperty =
        AvaloniaProperty.Register<SpeedGauge, double>(nameof(UploadMbps));

    public static readonly StyledProperty<string> PhaseTextProperty =
        AvaloniaProperty.Register<SpeedGauge, string>(nameof(PhaseText), string.Empty);

    public SpeedGauge() => InitializeComponent();

    /// <summary>Live download rate (Mbps) — the hero number.</summary>
    public double DownloadMbps
    {
        get => GetValue(DownloadMbpsProperty);
        set => SetValue(DownloadMbpsProperty, value);
    }

    /// <summary>Live upload rate (Mbps) — the secondary value.</summary>
    public double UploadMbps
    {
        get => GetValue(UploadMbpsProperty);
        set => SetValue(UploadMbpsProperty, value);
    }

    /// <summary>Current phase label ("Baixando…"/"Enviando…"); hidden when empty.</summary>
    public string PhaseText
    {
        get => GetValue(PhaseTextProperty);
        set => SetValue(PhaseTextProperty, value);
    }
}
