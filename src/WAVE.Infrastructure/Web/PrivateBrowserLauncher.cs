using System.Diagnostics;
using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Web;

/// <summary>
/// Abre URLs em janela anônima. Tenta Edge (--inprivate), depois Chrome
/// (--incognito) e, por fim, o navegador padrão. Aceita apenas http/https.
/// </summary>
public sealed class PrivateBrowserLauncher : IPrivateBrowserLauncher
{
    private readonly IAppLogger _logger;

    public PrivateBrowserLauncher(IAppLogger logger) => _logger = logger;

    public void Launch(string url)
    {
        if (!IsValidHttpUrl(url))
        {
            _logger.Warn($"URL inválida ignorada: {url}");
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
            _logger.Warn($"Não foi possível iniciar '{executable}': {exception.Message}");
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
            _logger.Error("Falha ao abrir o navegador padrão.", exception);
        }
    }

    private static bool IsValidHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
