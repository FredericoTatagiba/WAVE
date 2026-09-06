using System.Globalization;
using WAVE.Application.History;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>Read-only projection of a <see cref="TestRun"/> for the history.</summary>
public sealed class TestRunViewModel
{
    public TestRunViewModel(TestRun run)
    {
        Ssid = run.Ssid;
        MediumText = HistoryReport.MediumText(run.Medium);
        StartedAt = run.StartedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);
        Succeeded = run.Succeeded;
        ResultText = run.Succeeded ? "Sucesso" : $"Falha: {run.FailureReason}";
        PacketLossText = $"{run.Ping.PacketLossPercent.ToString("0.#", CultureInfo.CurrentCulture)}% perda";
        AverageLatencyText = run.Ping.Received > 0
            ? $"{run.Ping.AvgMs.ToString("0", CultureInfo.CurrentCulture)} ms{PingTargetSuffix(run.PingTarget)}"
            : "—";
        SpeedText = FormatSpeed(run.Speed);
        StreamingText = FormatStreaming(run.Streaming);
        JitterText = FormatJitter(run.Ping);
        BufferbloatText = FormatBufferbloat(run);
    }

    /// <summary>
    /// Names what answered, since the operator can change it between runs and two rows
    /// with different targets are not comparable. Empty for runs recorded before the
    /// target started being stored.
    /// </summary>
    private static string PingTargetSuffix(string target) =>
        string.IsNullOrWhiteSpace(target) ? string.Empty : $" · {target}";

    /// <summary>
    /// "jitter 3 ms · p95 24 ms", or "—" for a run that never measured them — a row from
    /// before these metrics existed must not read as a perfectly steady link.
    /// </summary>
    private static string FormatJitter(PingStatistics ping)
    {
        if (ping.JitterMs is not { } jitterMs || ping.P95Ms is not { } p95Ms)
        {
            return "—";
        }

        var jitter = jitterMs.ToString("0.#", CultureInfo.CurrentCulture);
        var p95 = p95Ms.ToString("0", CultureInfo.CurrentCulture);
        return $"jitter {jitter} ms · p95 {p95} ms";
    }

    /// <summary>
    /// "18 → 240 ms sob carga (+222)": what the link does to latency once it is saturated,
    /// which is what decides whether a call or a game survives a download on the same link.
    /// </summary>
    private static string FormatBufferbloat(TestRun run)
    {
        if (run.PingIdle is not { Received: > 0 } idle ||
            run.PingUnderLoad is not { Received: > 0 } load)
        {
            return "—";
        }

        var from = idle.AvgMs.ToString("0", CultureInfo.CurrentCulture);
        var to = load.AvgMs.ToString("0", CultureInfo.CurrentCulture);
        var delta = (load.AvgMs - idle.AvgMs).ToString("+0;-0;0", CultureInfo.CurrentCulture);

        return $"{from} → {to} ms sob carga ({delta})";
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

    /// <summary>Whether the run went over Wi-Fi or over the cable.</summary>
    public string MediumText { get; }

    public string StartedAt { get; }

    public bool Succeeded { get; }

    public string ResultText { get; }

    public string PacketLossText { get; }

    public string AverageLatencyText { get; }

    /// <summary>Measured throughput (download/upload), or "—" when not captured.</summary>
    public string SpeedText { get; }

    /// <summary>Streaming stability, or "—" when not captured.</summary>
    public string StreamingText { get; }

    /// <summary>Jitter and 95th percentile — how steady the link was, not just how fast.</summary>
    public string JitterText { get; }

    /// <summary>Idle vs saturated latency, or "—" for runs recorded before the split.</summary>
    public string BufferbloatText { get; }
}
