using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Web;

/// <summary>
/// Opens URLs in a private window. Tries Edge (--inprivate), then Chrome
/// (--incognito) and, finally, the default browser. Accepts only http/https.
/// </summary>
public sealed class PrivateBrowserLauncher : IPrivateBrowserLauncher
{
    private readonly IAppLogger _logger;

    public PrivateBrowserLauncher(IAppLogger logger) => _logger = logger;

    public void Launch(string url)
    {
        if (!IsValidHttpUrl(url))
        {
            _logger.Warn($"Invalid URL ignored: {url}");
            return;
        }

        if (TryStart("msedge.exe", $"--inprivate --new-window {url}"))
        {
            return;
        }

        if (TryStart("chrome.exe", $"--incognito --new-window {url}"))
        {
            return;
        }

        TryStartDefaultBrowser(url);
    }

    private bool TryStart(string executable, string arguments)
    {
        try
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = true });
            return true;
        }
        catch (Exception exception)
        {
            _logger.Warn($"Could not start '{executable}': {exception.Message}");
            return false;
        }
    }

    private void TryStartDefaultBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
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
