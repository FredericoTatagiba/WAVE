namespace WAVE.Infrastructure.Persistence;

/// <summary>File permission helpers for the files holding secrets.</summary>
internal static class SecureFile
{
    /// <summary>
    /// Restricts a file to owner read/write on Unix. No-op on Windows, where the
    /// per-user LocalAppData ACL already covers it.
    /// </summary>
    public static void RestrictToOwner(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort, same reasoning as AppPaths.RestrictToOwner: a mode we cannot
            // set must not break saving a credential.
        }
    }
}
