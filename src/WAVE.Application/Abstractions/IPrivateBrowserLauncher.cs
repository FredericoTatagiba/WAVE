namespace WAVE.Application.Abstractions;

/// <summary>
/// Opens a URL in an incognito/private window of a clean browser instance,
/// avoiding interference from cache or previous sessions.
/// </summary>
public interface IPrivateBrowserLauncher
{
    void Launch(string url);
}
