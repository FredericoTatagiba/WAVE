using System.Diagnostics;
using WAVE.Application.Abstractions;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Logging;

/// <summary>
/// Simple logger with a daily file in <c>%LOCALAPPDATA%\WAVE\logs</c>.
/// Logging failures never interrupt the application flow.
/// </summary>
public sealed class FileAppLogger : IAppLogger
{
    private readonly object _gate = new();
    private readonly string _file;

    public FileAppLogger()
    {
        AppPaths.EnsureCreated();
        _file = Path.Combine(AppPaths.LogsDirectory, $"wave-{DateTime.Now:yyyyMMdd}.log");
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message} :: {exception}");

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:O} [{level}] {message}";
        Debug.WriteLine(line);

        try
        {
            lock (_gate)
            {
                File.AppendAllText(_file, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never break the main flow.
        }
    }
}
