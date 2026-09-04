namespace WAVE.Infrastructure.Configuration;

/// <summary>
/// Resolves the application's local data paths: <c>%LOCALAPPDATA%\WAVE</c> on Windows,
/// <c>~/.local/share/WAVE</c> on Linux. Centralized to avoid scattered paths.
/// </summary>
public static class AppPaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAVE");

    public static string LogsDirectory => Path.Combine(RootDirectory, "logs");

    public static string ProfilesFile => Path.Combine(RootDirectory, "profiles.json");

    public static string HistoryFile => Path.Combine(RootDirectory, "history.json");

    public static string CredentialsFile => Path.Combine(RootDirectory, "credentials.dat");

    /// <summary>AES key backing the credential store on platforms without DPAPI.</summary>
    public static string CredentialsKeyFile => Path.Combine(RootDirectory, "credentials.key");

    public static string UsersFile => Path.Combine(RootDirectory, "users.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LogsDirectory);
        RestrictToOwner(RootDirectory);
    }

    /// <summary>
    /// Restricts the data directory to the owner on Unix. It holds credentials, the
    /// encryption key and the user database; the default umask on most distros leaves
    /// new directories world-readable. On Windows the equivalent protection already
    /// comes from the per-user LocalAppData ACL.
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
