using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Security;

namespace WAVE.Application.Profiles;

/// <summary>
/// Profile management use cases. Sensitive operation: saving/deleting requires the
/// <see cref="Permission.ManageProfiles"/> permission, validated here in the
/// Application (not only in the UI). Only listing is allowed to the operator,
/// since it is needed to build the test buttons.
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
    /// Remembers a just-selected network (profile + credential) for the next tests.
    /// Unlike <see cref="SaveAsync"/> (the catalog curation, which belongs to the
    /// Administrator), this is an operator action during a test: it requires only
    /// <see cref="Permission.RunTest"/>. This way, when tapping a network still unknown
    /// to the system and entering the password, the network becomes available for
    /// re-tests without typing the password again.
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
