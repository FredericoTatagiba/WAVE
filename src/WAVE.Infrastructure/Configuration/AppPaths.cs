namespace WAVE.Infrastructure.Configuration;

/// <summary>
/// Resolve os caminhos de dados locais da aplicação em
/// <c>%LOCALAPPDATA%\WAVE</c>. Centralizado para evitar caminhos espalhados.
/// </summary>
public static class AppPaths
{
    public static string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAVE");

    public static string LogsDirectory => Path.Combine(RootDirectory, "logs");

    public static string ProfilesFile => Path.Combine(RootDirectory, "profiles.json");

    public static string HistoryFile => Path.Combine(RootDirectory, "history.json");

    public static string CredentialsFile => Path.Combine(RootDirectory, "credentials.dat");

    public static string UsersFile => Path.Combine(RootDirectory, "users.json");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
