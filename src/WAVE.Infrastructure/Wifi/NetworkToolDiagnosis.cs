using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Turns a failed network-tool invocation into a message the operator can act on.
/// </summary>
/// <remarks>
/// Both backends fail this way for reasons the operator can fix, and both used to report
/// only a generic "could not create the profile": netsh needs elevation to write a
/// machine-wide profile, and nmcli needs polkit authorization, which is absent over SSH
/// or on a kiosk with no authentication agent.
/// </remarks>
internal static class NetworkToolDiagnosis
{
    private static readonly string[] PermissionMarkers =
    [
        // nmcli / polkit
        "not authorized",
        "insufficient privileges",
        "permission denied",
        // netsh, English and Portuguese Windows
        "access is denied",
        "acesso negado",
        "requires elevation",
        "privilégios"
    ];

    /// <summary>
    /// Returns an operator-facing reason for a failed command, or null when the failure
    /// is not one this can explain.
    /// </summary>
    public static string? Explain(CommandResult result, string toolName)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.ToolMissing)
        {
            return $"Ferramenta de rede '{toolName}' não encontrada neste sistema.";
        }

        var text = result.StandardError + " " + result.StandardOutput;

        return PermissionMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase))
            ? "Permissão insuficiente para alterar a configuração de rede."
            : null;
    }
}
