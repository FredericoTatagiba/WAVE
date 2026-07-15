using WAVE.Domain.Testing;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Runs a continuous background ping and emits samples for in-app telemetry
/// (latency chart), without depending on the visible terminal window.
/// </summary>
public interface IContinuousPingMonitor
{
    event EventHandler<PingSample>? Sampled;

    bool IsRunning { get; }

    void Start(string host);

    Task StopAsync();
}
