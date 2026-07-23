using System.Diagnostics;

namespace WAVE.Infrastructure.Process;

/// <summary>Result of running a command-line process.</summary>
internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

/// <summary>
/// Runs command-line utilities (e.g. <c>netsh</c>) capturing output, without opening
/// a window. Single responsibility: run the process and collect the result.
/// </summary>
internal static class CommandLineExecutor
{
    public static async Task<CommandResult> RunAsync(
        string fileName, string arguments, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        return new CommandResult(process.ExitCode, stdout, stderr);
    }
}
