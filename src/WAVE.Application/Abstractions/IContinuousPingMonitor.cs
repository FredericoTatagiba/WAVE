using WAVE.Domain.Testing;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Executa ping contínuo em segundo plano e emite amostras para telemetria
/// in-app (gráfico de latência), sem depender da janela visível do terminal.
/// </summary>
public interface IContinuousPingMonitor
{
    event EventHandler<PingSample>? Sampled;

    bool IsRunning { get; }

    void Start(string host);

    Task StopAsync();
}
