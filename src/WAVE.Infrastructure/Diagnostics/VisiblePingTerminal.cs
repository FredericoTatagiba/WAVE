using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Diagnostics;

/// <summary>
/// Abre uma janela de terminal visível com <c>ping host -t</c> para o técnico
/// acompanhar latência/perda em tempo real. Mantém o processo rastreado para
/// permitir encerramento limpo.
/// </summary>
public sealed class VisiblePingTerminal : IVisiblePingTerminal
{
    private readonly IAppLogger _logger;
    private readonly object _gate = new();

    private System.Diagnostics.Process? _process;

    public VisiblePingTerminal(IAppLogger logger) => _logger = logger;

    public void Launch(string host)
    {
        Close();

        var safeHost = SanitizeHost(host);
        if (safeHost.Length == 0)
        {
            _logger.Warn("Host de ping inválido; terminal não aberto.");
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo("cmd.exe", $"/k ping {safeHost} -t")
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            lock (_gate)
            {
                _process = System.Diagnostics.Process.Start(startInfo);
            }
        }
        catch (Exception exception)
        {
            _logger.Error("Não foi possível abrir o terminal de ping.", exception);
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            if (_process is null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception exception)
            {
                _logger.Warn($"Falha ao encerrar o terminal de ping: {exception.Message}");
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }
    }

    private static string SanitizeHost(string host) =>
        new((host ?? string.Empty).Where(c => char.IsLetterOrDigit(c) || c is '.' or '-').ToArray());
}
