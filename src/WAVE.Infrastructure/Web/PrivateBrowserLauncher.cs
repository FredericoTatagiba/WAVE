using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Web;

/// <summary>
/// Opens URLs in a private window, trying the browsers installed on this platform in
/// order and falling back to the default browser. Accepts only http/https.
/// </summary>
public sealed class PrivateBrowserLauncher : IPrivateBrowserLauncher
{
    private readonly IAppLogger _logger;

    public PrivateBrowserLauncher(IAppLogger logger) => _logger = logger;

    /// <summary>
    /// Private-mode invocations to try, most preferred first. Each entry is the
    /// executable plus its private-window flag.
    /// </summary>
    private static IEnumerable<(string Executable, string Flags)> Candidates =>
        OperatingSystem.IsWindows()
            ? [("msedge.exe", "--inprivate"), ("chrome.exe", "--incognito")]
            :
            [
                ("google-chrome", "--incognito"),
                ("chromium", "--incognito"),
                ("chromium-browser", "--incognito"),
                ("firefox", "--private-window")
            ];

    public void Launch(string url)
    {
        if (!IsValidHttpUrl(url))
        {
            _logger.Warn($"Invalid URL ignored: {url}");
            return;
        }

        foreach (var (executable, flags) in Candidates)
        {
            if (TryStart(executable, [.. flags.Split(' '), "--new-window", url]))
            {
                return;
            }
        }

        TryStartDefaultBrowser(url);
    }

    private bool TryStart(string executable, IReadOnlyList<string> arguments)
    {
        try
        {
            // UseShellExecute=false: on Linux the shell-execute path only handles URLs,
            // not an executable name with arguments, so an unfound browser must surface
            // as an exception here for the fallback chain to advance.
            var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false };
            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch (Exception exception)
        {
            _logger.Warn($"Could not start '{executable}': {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// Last resort: hand the URL to the desktop. Not private browsing, but better than
    /// showing nothing — and the only option when no known browser is installed.
    /// </summary>
    private void TryStartDefaultBrowser(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                return;
            }

            var startInfo = new ProcessStartInfo("xdg-open") { UseShellExecute = false };
            startInfo.ArgumentList.Add(url);
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to open the default browser.", exception);
        }
    }

    private static bool IsValidHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
