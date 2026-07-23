using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WAVE.Application.Testing;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>In-app telemetry: latency series for the chart and aggregated indicators.</summary>
public sealed class TelemetryViewModel : ObservableObject
{
    private const int MaxPoints = 120;

    private readonly List<PingSample> _samples = new();

    private double _lastLatencyMs;
    private double _averageLatencyMs;
    private double _packetLossPercent;
    private int _sent;
    private int _received;
    private double _downloadMbps;
    private double _uploadMbps;
    private string _speedPhaseText = string.Empty;

    /// <summary>Latest latencies (ms), consumed by the chart component.</summary>
    public ObservableCollection<double> Latencies { get; } = new();

    /// <summary>Live download rate (Mbps) — the hero number of the fast.com-style gauge.</summary>
    public double DownloadMbps
    {
        get => _downloadMbps;
        private set => SetProperty(ref _downloadMbps, value);
    }

    /// <summary>Live upload rate (Mbps), shown as the secondary value on the gauge.</summary>
    public double UploadMbps
    {
        get => _uploadMbps;
        private set => SetProperty(ref _uploadMbps, value);
    }

    /// <summary>Current speed phase label ("Baixando…"/"Enviando…"), empty when idle.</summary>
    public string SpeedPhaseText
    {
        get => _speedPhaseText;
        private set => SetProperty(ref _speedPhaseText, value);
    }

    public double LastLatencyMs
    {
        get => _lastLatencyMs;
        private set => SetProperty(ref _lastLatencyMs, value);
    }

    public double AverageLatencyMs
    {
        get => _averageLatencyMs;
        private set => SetProperty(ref _averageLatencyMs, value);
    }

    public double PacketLossPercent
    {
        get => _packetLossPercent;
        private set => SetProperty(ref _packetLossPercent, value);
    }

    public int Sent
    {
        get => _sent;
        private set => SetProperty(ref _sent, value);
    }

    public int Received
    {
        get => _received;
        private set => SetProperty(ref _received, value);
    }

    public void AddSample(PingSample sample)
    {
        _samples.Add(sample);

        Latencies.Add(sample.Success ? sample.LatencyMs : 0d);
        while (Latencies.Count > MaxPoints)
        {
            Latencies.RemoveAt(0);
        }

        var statistics = PingStatisticsCalculator.Calculate(_samples);
        Sent = statistics.Sent;
        Received = statistics.Received;
        AverageLatencyMs = Math.Round(statistics.AvgMs, 1);
        PacketLossPercent = Math.Round(statistics.PacketLossPercent, 1);
        LastLatencyMs = sample.Success ? Math.Round(sample.LatencyMs, 0) : 0d;
    }

    /// <summary>
    /// Applies a live throughput reading to the gauge. Download drives the hero number
    /// (climbs during the download phase); upload updates the secondary value.
    /// </summary>
    public void AddSpeedSample(SpeedSample sample)
    {
        var mbps = Math.Round(sample.Mbps, 1);
        if (sample.Phase == SpeedPhase.Download)
        {
            DownloadMbps = mbps;
            SpeedPhaseText = "Baixando…";
        }
        else
        {
            UploadMbps = mbps;
            SpeedPhaseText = "Enviando…";
        }
    }

    public void Reset()
    {
        _samples.Clear();
        Latencies.Clear();
        Sent = 0;
        Received = 0;
        LastLatencyMs = 0d;
        AverageLatencyMs = 0d;
        PacketLossPercent = 0d;
        DownloadMbps = 0d;
        UploadMbps = 0d;
        SpeedPhaseText = string.Empty;
    }
}
