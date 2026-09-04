using WAVE.Application.Abstractions;
using WAVE.Application.Networking;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Integration with Linux Wi-Fi via NetworkManager's <c>nmcli</c>: creates the saved
/// connection (carrying its secret) and requests the association. The real connectivity
/// confirmation is done later by DHCP validation.
/// </summary>
/// <remarks>
/// NetworkManager has no profile-XML equivalent, so <see cref="IWifiProfileXmlFactory"/>
/// plays no part here; <see cref="NmcliCommands"/> maps the profile onto connection
/// properties instead. 802.1X PEAP-MSCHAPv2 needs no separate credential push either —
/// unlike Windows, the identity and password go in with the connection.
/// </remarks>
public sealed class NmcliWifiConnector : IWifiConnector
{
    private const string Tool = "nmcli";

    private readonly IAppLogger _logger;

    public NmcliWifiConnector(IAppLogger logger) => _logger = logger;

    public async Task<Result> EnsureProfileAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        string[] arguments;
        try
        {
            arguments = NmcliCommands.Add(profile, secret);
        }
        catch (NotSupportedException exception)
        {
            return Result.Failure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            _logger.Warn($"Incomplete credentials for '{profile.Ssid}': {exception.Message}");
            return Result.Failure("Credenciais incompletas para esta rede.");
        }
        catch (Exception exception)
        {
            _logger.Error("Error building the network connection.", exception);
            return Result.Failure("Erro ao gerar o perfil de rede.");
        }

        // netsh's "add profile" overwrites; nmcli's "connection add" would stack a second
        // connection with the same name, so an existing one is removed first. A failure
        // here is expected and ignored: usually there was nothing to delete.
        await RemoveProfileAsync(profile.Ssid, cancellationToken).ConfigureAwait(false);

        var result = await CommandLineExecutor
            .RunAsync(Tool, arguments, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"nmcli connection add failed: {result.StandardOutput} {result.StandardError}");
            return Result.Failure(
                NetworkToolDiagnosis.Explain(result, Tool)
                ?? "Não foi possível criar o perfil de rede no NetworkManager.");
        }

        return Result.Success();
    }

    public async Task<Result> ConnectAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return Result.Failure("SSID inválido.");
        }

        var result = await CommandLineExecutor
            .RunAsync(Tool, NmcliCommands.Connect(ssid.Trim()), cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"nmcli connection up failed: {result.StandardOutput} {result.StandardError}");
            return Result.Failure(
                NetworkToolDiagnosis.Explain(result, Tool)
                ?? "Falha ao solicitar a conexão com a rede.");
        }

        return Result.Success();
    }

    public async Task RemoveProfileAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return;
        }

        var result = await CommandLineExecutor
            .RunAsync(Tool, NmcliCommands.Delete(ssid.Trim()), cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"nmcli connection delete failed: {result.StandardOutput} {result.StandardError}");
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        var device = await NmcliDevice
            .FindWifiInterfaceAsync(_logger, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            return;
        }

        await CommandLineExecutor
            .RunAsync(Tool, NmcliCommands.Disconnect(device), cancellationToken)
            .ConfigureAwait(false);
    }
}
