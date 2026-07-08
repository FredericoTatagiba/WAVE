using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Process;

/// <summary>
/// Encerra apenas os processos que o WAVE iniciou, rastreados por PID. Não encerra
/// mais por nome — isso fechava navegadores/terminais do usuário. Escopo restrito
/// ao que o app abriu (hoje, a janela de ping).
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
                // Processo já não existe: nada a encerrar.
            }
            catch (Exception exception)
            {
                _logger.Warn($"Falha ao encerrar o processo {id}: {exception.Message}");
            }
        }

        return terminated;
    }
}
