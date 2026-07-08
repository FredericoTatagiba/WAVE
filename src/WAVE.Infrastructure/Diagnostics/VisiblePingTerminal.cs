using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Diagnostics;

/// <summary>
/// Abre uma janela de terminal visível com <c>ping host -t</c> para o técnico
/// acompanhar latência/perda em tempo real. Registra o PID no
/// <see cref="IProcessTerminator"/>, que encerra apenas o que o WAVE abriu —
/// sem tocar em navegadores/terminais do usuário.
/// </summary>
public sealed class VisiblePingTerminal : IVisiblePingTerminal
{
    private readonly IAppLogger _logger;
    private readonly IProcessTerminator _terminator;

    public VisiblePingTerminal(IAppLogger logger, IProcessTerminator terminator)
    {
        _logger = logger;
        _terminator = terminator;
    }

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

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is not null)
            {
                _terminator.Track(process.Id);
            }
        }
        catch (Exception exception)
        {
            _logger.Error("Não foi possível abrir o terminal de ping.", exception);
        }
    }

    public void Close() => _terminator.TerminateTracked();

    private static string SanitizeHost(string host) =>
        new((host ?? string.Empty).Where(c => char.IsLetterOrDigit(c) || c is '.' or '-').ToArray());
}
