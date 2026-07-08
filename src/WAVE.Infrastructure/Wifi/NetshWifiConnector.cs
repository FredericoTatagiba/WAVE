using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Infrastructure.Process;

namespace WAVE.Infrastructure.Wifi;

/// <summary>
/// Integração com o Wi-Fi do Windows via <c>netsh wlan</c>: cria o perfil (se
/// necessário) e solicita a associação. A confirmação real de conectividade é
/// feita depois pela validação de DHCP.
/// </summary>
public sealed class NetshWifiConnector : IWifiConnector
{
    private readonly IWifiProfileXmlFactory _profileFactory;
    private readonly IAppLogger _logger;
    private readonly CommandLineExecutor _executor = new();

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
            _logger.Error("Erro ao gerar o perfil de rede.", exception);
            return Result.Failure("Erro ao gerar o perfil de rede.");
        }

        try
        {
            var result = await _executor.RunAsync(
                "netsh", $"wlan add profile filename=\"{temporaryFile}\" user=all", cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                _logger.Warn($"netsh add profile falhou: {result.StandardOutput} {result.StandardError}");
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
    /// Para redes Enterprise (802.1X), aplica as credenciais de usuário (EAP) ao
    /// perfil recém-criado. Best-effort: falha aqui não impede a conexão — o Windows
    /// solicitará as credenciais no momento de conectar.
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
            _logger.Warn($"Credenciais Enterprise incompletas para '{profile.Ssid}': {exception.Message}");
            return;
        }

        if (string.IsNullOrEmpty(eapUserData))
        {
            return;
        }

        if (!WlanEapUserData.TryApply(profile.Ssid, eapUserData, _logger))
        {
            _logger.Warn(
                $"Não foi possível aplicar as credenciais Enterprise de '{profile.Ssid}'. " +
                "O Windows poderá solicitá-las ao conectar.");
        }
    }

    public async Task<Result> ConnectAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return Result.Failure("SSID inválido.");
        }

        var safeSsid = ssid.Replace("\"", string.Empty);

        var result = await _executor.RunAsync(
            "netsh", $"wlan connect name=\"{safeSsid}\" ssid=\"{safeSsid}\"", cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            _logger.Warn($"netsh connect falhou: {result.StandardOutput} {result.StandardError}");
            return Result.Failure("Falha ao solicitar a conexão com a rede.");
        }

        return Result.Success();
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        await _executor.RunAsync("netsh", "wlan disconnect", cancellationToken).ConfigureAwait(false);

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
            _logger.Warn($"Não foi possível remover o arquivo temporário de perfil: {exception.Message}");
        }
    }
}
