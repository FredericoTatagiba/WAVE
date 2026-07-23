using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Integration with Windows Wi-Fi via <c>netsh wlan</c>: creates the profile (if
/// needed) and requests the association. The real connectivity confirmation is
/// done later by DHCP validation.
/// </summary>
public sealed class NetshWifiConnector : IWifiConnector
{
    private readonly IWifiProfileXmlFactory _profileFactory;
    private readonly IAppLogger _logger;

    public NetshWifiConnector(IWifiProfileXmlFactory profileFactory, IAppLogger logger)
    {
        _profileFactory = profileFactory;
        _logger = logger;
    }

    public async Task<Result> EnsureProfileAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        string temporaryFile;
        try
        {
            var xml = _profileFactory.Build(profile, secret);
            temporaryFile = Path.Combine(Path.GetTempPath(), $"wave-{Guid.NewGuid():N}.xml");
            await File.WriteAllTextAsync(temporaryFile, xml, cancellationToken).ConfigureAwait(false);
        }
        catch (NotSupportedException exception)
        {
            return Result.Failure(exception.Message);
        }
        catch (Exception exception)
        {
            _logger.Error("Error building the network profile.", exception);
            return Result.Failure("Erro ao gerar o perfil de rede.");
        }

        try
        {
            var result = await CommandLineExecutor.RunAsync(
                "netsh", $"wlan add profile filename=\"{temporaryFile}\" user=all", cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                _logger.Warn($"netsh add profile failed: {result.StandardOutput} {result.StandardError}");
                return Result.Failure("Não foi possível criar o perfil de rede no Windows.");
            }

            ApplyEnterpriseCredentials(profile, secret);
            return Result.Success();
        }
        finally
        {
            TryDelete(temporaryFile);
        }
    }

    /// <summary>
    /// For Enterprise (802.1X) networks, applies the user credentials (EAP) to the
    /// just-created profile. Best-effort: a failure here does not prevent the connection —
    /// Windows will prompt for the credentials at connect time.
    /// </summary>
    private void ApplyEnterpriseCredentials(WifiNetworkProfile profile, WifiSecret? secret)
    {
        if (!profile.IsEnterprise)
        {
            return;
        }

        string? eapUserData;
        try
        {
            eapUserData = _profileFactory.BuildEapUserData(profile, secret);
        }
        catch (InvalidOperationException exception)
        {
            _logger.Warn($"Incomplete Enterprise credentials for '{profile.Ssid}': {exception.Message}");
            return;
        }

        if (string.IsNullOrEmpty(eapUserData))
        {
            return;
        }

        if (!WlanEapUserData.TryApply(profile.Ssid, eapUserData, _logger))
        {
            _logger.Warn(
                $"Could not apply the Enterprise credentials for '{profile.Ssid}'. " +
                "Windows may prompt for them at connect time.");
        }
    }

    public async Task<Result> ConnectAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return Result.Failure("SSID inválido.");
        }

        var safeSsid = ssid.Replace("\"", string.Empty);

        var result = await CommandLineExecutor.RunAsync(
            "netsh", $"wlan connect name=\"{safeSsid}\" ssid=\"{safeSsid}\"", cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"netsh connect failed: {result.StandardOutput} {result.StandardError}");
            return Result.Failure("Falha ao solicitar a conexão com a rede.");
        }

        return Result.Success();
    }

    public async Task RemoveProfileAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return;
        }

        var safeSsid = ssid.Replace("\"", string.Empty);

        var result = await CommandLineExecutor.RunAsync(
            "netsh", $"wlan delete profile name=\"{safeSsid}\"", cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"netsh delete profile failed: {result.StandardOutput} {result.StandardError}");
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        await CommandLineExecutor.RunAsync("netsh", "wlan disconnect", cancellationToken).ConfigureAwait(false);

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            _logger.Warn($"Could not remove the temporary profile file: {exception.Message}");
        }
    }
}
