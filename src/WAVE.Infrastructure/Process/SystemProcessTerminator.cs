using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Process;

/// <summary>
/// Terminates only the processes WAVE started, tracked by PID. It no longer terminates
/// by name — that used to close the user's browsers/terminals. Scope restricted
/// to what the app opened (today, the ping window).
/// </summary>
public sealed class SystemProcessTerminator : IProcessTerminator
{
    private const int WaitForExitMilliseconds = 2000;

    private readonly IAppLogger _logger;
    private readonly HashSet<int> _tracked = new();
    private readonly object _gate = new();

    public SystemProcessTerminator(IAppLogger logger) => _logger = logger;

    public void Track(int processId)
    {
        if (processId <= 0)
        {
            return;
        }

        lock (_gate)
        {
            _tracked.Add(processId);
        }
    }

    public int TerminateTracked()
    {
        int[] ids;
        lock (_gate)
        {
            ids = _tracked.ToArray();
            _tracked.Clear();
        }

        var terminated = 0;
        foreach (var id in ids)
        {
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById(id);
                process.Kill(entireProcessTree: true);
                process.WaitForExit(WaitForExitMilliseconds);
                terminated++;
            }
            catch (ArgumentException)
            {
                // Process no longer exists: nothing to terminate.
            }
            catch (Exception exception)
            {
                _logger.Warn($"Failed to terminate process {id}: {exception.Message}");
            }
        }

        return terminated;
    }
}
