using WAVE.Application.Abstractions;

namespace WAVE.Infrastructure.Configuration;

/// <summary>
/// Resolves the relocatable paths — history and logs — from the current settings.
/// </summary>
/// <remarks>
/// Resolved on every read rather than cached, so pointing the history at a new folder
/// takes effect without restarting the app.
/// </remarks>
public sealed class WaveDataPaths
{
    private const string HistoryFileName = "history.json";

    private readonly ISettingsStore _settings;

    public WaveDataPaths(ISettingsStore settings) => _settings = settings;

    public string HistoryDirectory => Resolve(_settings.Current.HistoryDirectory, AppPaths.RootDirectory);

    public string HistoryFile => Path.Combine(HistoryDirectory, HistoryFileName);

    public string LogsDirectory => Resolve(_settings.Current.LogsDirectory, AppPaths.DefaultLogsDirectory);

    /// <summary>
    /// Uses the configured directory when it can actually be created, and silently falls
    /// back to the local default when it cannot.
    /// </summary>
    /// <remarks>
    /// A field tablet is routinely configured to write to a network share that is not
    /// reachable at the moment — the site has no link, the VPN is down. Losing the run, or
    /// crashing the app, would be a far worse outcome than writing the record locally, so
    /// an unreachable target degrades instead of failing.
    /// </remarks>
    private static string Resolve(string? configured, string fallback)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return EnsureExists(fallback);
        }

        try
        {
            Directory.CreateDirectory(configured);
            return configured;
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or ArgumentException
                                              or NotSupportedException)
        {
            return EnsureExists(fallback);
        }
    }

    private static string EnsureExists(string directory)
    {
        Directory.CreateDirectory(directory);
        return directory;
    }
}
