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

        if (OperatingSystem.IsWindows())
        {
            TryStart(
                new ProcessStartInfo("cmd.exe", $"/k ping {safeHost} -t")
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                });
            return;
        }

        // No single terminal emulator is guaranteed on Linux, so try the common ones.
        // `ping` there runs indefinitely by default — the Windows `-t` flag would be
        // rejected as an unknown option.
        foreach (var (emulator, flag) in LinuxTerminals)
        {
            var startInfo = new ProcessStartInfo(emulator) { UseShellExecute = false };
            startInfo.ArgumentList.Add(flag);
            startInfo.ArgumentList.Add("ping");
            startInfo.ArgumentList.Add(safeHost);

            if (TryStart(startInfo))
            {
                return;
            }
        }

        _logger.Warn("No terminal emulator found; the in-app latency chart still updates.");
    }

    private static IEnumerable<(string Emulator, string Flag)> LinuxTerminals =>
    [
        ("x-terminal-emulator", "-e"),
        ("gnome-terminal", "--"),
        ("konsole", "-e"),
        ("xfce4-terminal", "-e"),
        ("xterm", "-e")
    ];

    private bool TryStart(ProcessStartInfo startInfo)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is not null)
            {
                _terminator.Track(process.Id);
            }

            return true;
        }
        catch (Exception exception)
        {
            _logger.Warn($"Could not open '{startInfo.FileName}': {exception.Message}");
            return false;
        }
    }

    public void Close() => _terminator.TerminateTracked();

    private static string SanitizeHost(string host) =>
        new((host ?? string.Empty).Where(c => char.IsLetterOrDigit(c) || c is '.' or '-').ToArray());
}
