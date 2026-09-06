using System.Diagnostics;
using WAVE.Application.Abstractions;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Logging;

/// <summary>
/// Simple logger with a daily file in the configured logs directory.
/// Logging failures never interrupt the application flow.
/// </summary>
/// <remarks>
/// The target file is resolved on every write, not cached: it makes the daily rollover
/// fall out for free and lets a directory change in the settings take effect without
/// restarting the app.
/// </remarks>
public sealed class FileAppLogger : IAppLogger
{
    private readonly object _gate = new();
    private readonly WaveDataPaths _paths;

    public FileAppLogger(WaveDataPaths paths) => _paths = paths;

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
                var file = Path.Combine(_paths.LogsDirectory, $"wave-{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(file, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never break the main flow.
        }
    }
}
