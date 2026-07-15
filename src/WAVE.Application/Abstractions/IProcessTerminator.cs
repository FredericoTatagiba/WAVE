namespace WAVE.Application.Abstractions;

/// <summary>
/// Terminates only the processes WAVE started itself, tracked by PID (e.g. the
/// ping window). Avoids the collateral damage of terminating by name, which would
/// close the user's browsers/terminals. Replaces the old termination by name, now
/// unnecessary (throughput/streaming measurement runs in the app).
/// </summary>
public interface IProcessTerminator
{
    /// <summary>Starts tracking a process started by WAVE (by PID).</summary>
    void Track(int processId);

    /// <summary>Terminates the tracked processes still alive and clears the registry. Returns how many were terminated.</summary>
    int TerminateTracked();
}
