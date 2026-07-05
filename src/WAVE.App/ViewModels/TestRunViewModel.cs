using System.Globalization;
using WAVE.Domain.Testing;

namespace WAVE.App.ViewModels;

/// <summary>Projeção somente-leitura de um <see cref="TestRun"/> para o histórico.</summary>
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
    }

    public string Ssid { get; }

    public string StartedAt { get; }

    public bool Succeeded { get; }

    public string ResultText { get; }

    public string PacketLossText { get; }

    public string AverageLatencyText { get; }
}
