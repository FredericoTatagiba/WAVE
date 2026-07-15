using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Diagnostics;

/// <summary>
/// Opens a visible terminal window with <c>ping host -t</c> so the technician can
/// follow latency/loss in real time. Registers the PID with the
/// <see cref="IProcessTerminator"/>, which terminates only what WAVE opened —
/// without touching the user's browsers/terminals.
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
            _logger.Warn("Invalid ping host; terminal not opened.");
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
            _logger.Error("Could not open the ping terminal.", exception);
        }
    }

    public void Close() => _terminator.TerminateTracked();

    private static string SanitizeHost(string host) =>
        new((host ?? string.Empty).Where(c => char.IsLetterOrDigit(c) || c is '.' or '-').ToArray());
}
