namespace WAVE.Application.Testing;

/// <summary>
/// Configurable test parameters (no "magic numbers" scattered around).
/// Default values follow the specification; they may come from external configuration.
/// </summary>
public sealed class TestRunnerOptions
{
    /// <summary>Target host for the continuous ping.</summary>
    public string PingTargetHost { get; init; } = "google.com";

    /// <summary>Download endpoint used to measure throughput (returns N bytes).</summary>
    public string SpeedDownloadUrl { get; init; } = "https://speed.cloudflare.com/__down?bytes=52428800";

    /// <summary>Upload endpoint used to measure throughput (accepts POST).</summary>
    public string SpeedUploadUrl { get; init; } = "https://speed.cloudflare.com/__up";

    /// <summary>Number of bytes sent in the upload measurement.</summary>
    public long SpeedUploadBytes { get; init; } = 10_485_760;

    /// <summary>If false, measures download only.</summary>
    public bool MeasureUpload { get; init; } = true;

    /// <summary>Maximum time for each throughput measurement.</summary>
    public TimeSpan SpeedTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Endpoint downloaded in a sustained way to probe the streaming.</summary>
    public string StreamingProbeUrl { get; init; } = "https://speed.cloudflare.com/__down?bytes=104857600";

    /// <summary>Total duration of the streaming probe.</summary>
    public TimeSpan StreamingDuration { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Throughput sampling interval during the streaming probe.</summary>
    public TimeSpan StreamingSampleInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Target bitrate (Mbps) to sustain the quality (e.g. ~8 Mbps for 1080p).</summary>
    public double StreamingTargetMbps { get; init; } = 8;

    /// <summary>Hardware stabilization time after the association.</summary>
    public TimeSpan StabilizationDelay { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>Maximum time to obtain an IP via DHCP.</summary>
    public TimeSpan DhcpTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Interval between DHCP lease checks.</summary>
    public TimeSpan DhcpPollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Maximum number of items kept in the history.</summary>
    public int MaxHistoryEntries { get; init; } = 200;
}
