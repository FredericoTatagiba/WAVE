using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Process;

/// <summary>
/// Encerra processos por nome (sem extensão), conforme os "Pontos de Atenção"
/// da especificação (evitar acúmulo de RAM entre execuções).
/// </summary>
public sealed class SystemProcessTerminator : IProcessTerminator
{
    private const int WaitForExitMilliseconds = 2000;

    private readonly IAppLogger _logger;

    public SystemProcessTerminator(IAppLogger logger) => _logger = logger;

    public int TerminateByNames(IReadOnlyCollection<string> processNames)
    {
        ArgumentNullException.ThrowIfNull(processNames);

        var terminated = 0;

        foreach (var name in processNames)
        {
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(name))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(WaitForExitMilliseconds);
                    terminated++;
                }
                catch (Exception exception)
                {
                    _logger.Warn($"Falha ao encerrar '{name}': {exception.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        return terminated;
    }
}
