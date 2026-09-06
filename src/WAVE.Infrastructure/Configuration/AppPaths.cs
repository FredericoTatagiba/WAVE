namespace WAVE.Infrastructure.Configuration;

/// <summary>
/// Resolves the application's fixed local paths: <c>%LOCALAPPDATA%\WAVE</c> on Windows,
/// <c>~/.local/share/WAVE</c> on Linux. Centralized to avoid scattered paths.
/// </summary>
/// <remarks>
/// Everything here is deliberately not configurable. The settings file has to live at a
/// known location or there is nothing to read the configuration from, and the credential
/// store is encrypted per user account, so relocating it to a share would only produce
/// blobs that no longer decrypt. The relocatable paths live in <see cref="WaveDataPaths"/>.
/// </remarks>
public static class AppPaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAVE");

    public static string DefaultLogsDirectory => Path.Combine(RootDirectory, "logs");

    public static string SettingsFile => Path.Combine(RootDirectory, "settings.json");

    public static string ProfilesFile => Path.Combine(RootDirectory, "profiles.json");

    public static string CredentialsFile => Path.Combine(RootDirectory, "credentials.dat");

    /// <summary>AES key backing the credential store on platforms without DPAPI.</summary>
    public static string CredentialsKeyFile => Path.Combine(RootDirectory, "credentials.key");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(DefaultLogsDirectory);
        RestrictToOwner(RootDirectory);
    }

    /// <summary>
    /// Restricts the data directory to the owner on Unix. It holds credentials and the
    /// encryption key; the default umask on most distros leaves new directories
    /// world-readable. On Windows the equivalent protection already comes from the
    /// per-user LocalAppData ACL.
    /// </summary>
    private static void RestrictToOwner(string directory)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(
                directory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Non-fatal: a mode we cannot tighten (an exotic mount, a shared home) must
            // not stop the app from starting. The files themselves are still created 0600.
        }
    }
}
