using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WAVE.Application.Testing;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>Telemetria in-app: série de latência para o gráfico e indicadores agregados.</summary>
public sealed class TelemetryViewModel : ObservableObject
{
    private const int MaxPoints = 120;

    private readonly List<PingSample> _samples = new();

    private double _lastLatencyMs;
    private double _averageLatencyMs;
    private double _packetLossPercent;
    private int _sent;
    private int _received;

    /// <summary>Últimas latências (ms), consumidas pelo componente de gráfico.</summary>
    public ObservableCollection<double> Latencies { get; } = new();

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

    public void Reset()
    {
        _samples.Clear();
        Latencies.Clear();
        Sent = 0;
        Received = 0;
        LastLatencyMs = 0d;
        AverageLatencyMs = 0d;
        PacketLossPercent = 0d;
    }
}
