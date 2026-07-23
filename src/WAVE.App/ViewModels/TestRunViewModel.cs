using System.Globalization;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>Read-only projection of a <see cref="TestRun"/> for the history.</summary>
public sealed class TestRunViewModel
{
    public TestRunViewModel(TestRun run)
    {
        Ssid = run.Ssid;
        StartedAt = run.StartedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);
        Succeeded = run.Succeeded;
        ResultText = run.Succeeded ? "Sucesso" : $"Falha: {run.FailureReason}";
        PacketLossText = $"{run.Ping.PacketLossPercent.ToString("0.#", CultureInfo.CurrentCulture)}% perda";
        AverageLatencyText = run.Ping.Received > 0
            ? $"{run.Ping.AvgMs.ToString("0", CultureInfo.CurrentCulture)} ms"
            : "—";
        SpeedText = FormatSpeed(run.Speed);
        StreamingText = FormatStreaming(run.Streaming);
    }

    private static string FormatSpeed(SpeedResult? speed)
    {
        if (speed is not { } value)
        {
            return "—";
        }

        var down = value.DownloadMbps.ToString("0.#", CultureInfo.CurrentCulture);
        var up = value.UploadMbps.ToString("0.#", CultureInfo.CurrentCulture);
        return value.UploadMbps > 0 ? $"↓ {down} / ↑ {up} Mbps" : $"↓ {down} Mbps";
    }

    private static string FormatStreaming(StreamingObservation? streaming)
    {
        if (streaming is not { } value)
        {
            return "—";
        }

        var label = value.Stability switch
        {
            StreamingStability.Smooth => "Estável",
            StreamingStability.MinorBuffering => "Travadas leves",
            StreamingStability.Unstable => "Instável",
            _ => "—"
        };

        return value.RebufferEvents > 0 ? $"{label} ({value.RebufferEvents})" : label;
    }

    public string Ssid { get; }

    public string StartedAt { get; }

    public bool Succeeded { get; }

    public string ResultText { get; }

    public string PacketLossText { get; }

    public string AverageLatencyText { get; }

    /// <summary>Measured throughput (download/upload), or "—" when not captured.</summary>
    public string SpeedText { get; }

    /// <summary>Streaming stability, or "—" when not captured.</summary>
    public string StreamingText { get; }
}
