using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Security;

namespace WAVE.Application.Profiles;

/// <summary>
/// Casos de uso de gerenciamento de perfis. Operação sensível: gravar/excluir
/// exige a permissão <see cref="Permission.ManageProfiles"/>, validada aqui na
/// Application (não apenas na UI). Apenas a listagem é liberada ao operador,
/// pois é necessária para montar os botões de teste.
/// </summary>
public sealed class NetworkProfileService
{
    private readonly INetworkProfileRepository _repository;
    private readonly ICredentialStore _credentials;
    private readonly IAuthorizationService _authorization;

    public NetworkProfileService(
        INetworkProfileRepository repository,
        ICredentialStore credentials,
        IAuthorizationService authorization)
    {
        _repository = repository;
        _credentials = credentials;
        _authorization = authorization;
    }

    public Task<IReadOnlyList<WifiNetworkProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<Result> SaveAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var authorization = _authorization.Authorize(Permission.ManageProfiles);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        if (profile.RequiresCredential && secret is null)
        {
            return Result.Failure("Rede protegida exige uma credencial.");
        }

        await _repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        if (secret is not null)
        {
            await _credentials.SaveAsync(profile.Ssid, secret, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <summary>
    /// Memoriza uma rede recém-selecionada (perfil + credencial) para os próximos
    /// testes. Diferente de <see cref="SaveAsync"/> (a curadoria do catalogo, que
    /// e do Administrador), esta e uma acao do operador durante um teste: exige
    /// apenas <see cref="Permission.RunTest"/>. Assim, ao tocar numa rede ainda
    /// desconhecida pelo sistema e informar a senha, a rede fica disponível para
    /// re-testes sem digitar a senha de novo.
    /// </summary>
    public async Task<Result> RememberForTestingAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var authorization = _authorization.Authorize(Permission.RunTest);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        if (profile.RequiresCredential && secret is null)
        {
            return Result.Failure("Rede protegida exige uma credencial.");
        }

        await _repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        if (secret is not null)
        {
            await _credentials.SaveAsync(profile.Ssid, secret, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return Result.Failure("SSID inválido.");
        }

        var authorization = _authorization.Authorize(Permission.ManageProfiles);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        await _repository.DeleteAsync(ssid, cancellationToken).ConfigureAwait(false);
        await _credentials.DeleteAsync(ssid, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
