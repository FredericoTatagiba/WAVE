using System.ComponentModel;
using System.Diagnostics;

namespace WAVE.Infrastructure.Process;

/// <summary>Result of running a command-line process.</summary>
internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    /// <summary>Exit code used when the executable itself could not be started.</summary>
    public const int NotLaunchedExitCode = -1;

    public bool Succeeded => ExitCode == 0;

    /// <summary>True when the tool is missing from the system (netsh/nmcli not installed).</summary>
    public bool ToolMissing => ExitCode == NotLaunchedExitCode;
}

/// <summary>
/// Runs command-line utilities (<c>netsh</c> on Windows, <c>nmcli</c> on Linux) capturing
/// output, without opening a window. Single responsibility: run the process and collect
/// the result.
/// </summary>
internal static class CommandLineExecutor
{
    /// <summary>
    /// Runs a command with pre-split arguments. Prefer this overload: the single-string
    /// form is parsed with Windows quoting rules on every platform, which mangles values
    /// containing spaces or quotes (SSIDs, passphrases). Each element here reaches the
    /// child process as exactly one argument, with no escaping needed at the call site.
    /// </summary>
    public static Task<CommandResult> RunAsync(
        string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = CreateStartInfo(fileName);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return RunAsync(startInfo, cancellationToken);
    }

    public static Task<CommandResult> RunAsync(
        string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        var startInfo = CreateStartInfo(fileName);
        startInfo.Arguments = arguments;

        return RunAsync(startInfo, cancellationToken);
    }

    private static ProcessStartInfo CreateStartInfo(string fileName) =>
        new(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

    private static async Task<CommandResult> RunAsync(
        ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new System.Diagnostics.Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException)
        {
            // The tool is absent (no NetworkManager on this box, netsh on a stripped
            // image). Callers already handle a failed CommandResult; letting this escape
            // would crash the UI instead, since none of them wrap the call in a try.
            return new CommandResult(CommandResult.NotLaunchedExitCode, string.Empty, exception.Message);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        return new CommandResult(process.ExitCode, stdout, stderr);
    }
}
