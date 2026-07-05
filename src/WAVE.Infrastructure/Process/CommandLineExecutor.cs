using System.Diagnostics;

namespace WAVE.Infrastructure.Process;

/// <summary>Resultado da execução de um processo de linha de comando.</summary>
internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

/// <summary>
/// Executa utilitários de linha de comando (ex.: <c>netsh</c>) capturando saída,
/// sem abrir janela. Responsabilidade única: rodar processo e coletar resultado.
/// </summary>
internal sealed class CommandLineExecutor
{
    public async Task<CommandResult> RunAsync(
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
