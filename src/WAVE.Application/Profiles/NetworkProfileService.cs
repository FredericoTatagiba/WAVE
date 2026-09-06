using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;

namespace WAVE.Application.Profiles;

/// <summary>
/// Profile management use cases. Curating the catalog — saving and deleting — is an
/// administrator action, validated here in the Application layer and not only in the UI.
/// Listing stays open, since it is what builds the test buttons.
/// </summary>
public sealed class NetworkProfileService
{
    private readonly INetworkProfileRepository _repository;
    private readonly ICredentialStore _credentials;
    private readonly IAdminSession _admin;

    public NetworkProfileService(
        INetworkProfileRepository repository,
        ICredentialStore credentials,
        IAdminSession admin)
    {
        _repository = repository;
        _credentials = credentials;
        _admin = admin;
    }

    public Task<IReadOnlyList<WifiNetworkProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<Result> SaveAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var authorization = _admin.RequireUnlocked();
        if (authorization.IsFailure)
        {
            return authorization;
        }

        if (profile.RequiresCredential && secret is null)
        {
            return Result.Failure("Rede protegida exige uma credencial.");
        }

        return await StoreAsync(profile, secret, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Remembers a just-selected network (profile + credential) for the next tests.
    /// Unlike <see cref="SaveAsync"/> — curating the catalog, an administrator action —
    /// this happens during a test and needs no password: tapping a network still unknown
    /// to the system and entering its passphrase makes it available for re-tests without
    /// typing the passphrase again.
    /// </summary>
    public async Task<Result> RememberForTestingAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.RequiresCredential && secret is null)
        {
            return Result.Failure("Rede protegida exige uma credencial.");
        }

        return await StoreAsync(profile, secret, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> DeleteAsync(string ssid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            return Result.Failure("SSID inválido.");
        }

        var authorization = _admin.RequireUnlocked();
        if (authorization.IsFailure)
        {
            return authorization;
        }

        await _repository.DeleteAsync(ssid, cancellationToken).ConfigureAwait(false);
        await _credentials.DeleteAsync(ssid, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<Result> StoreAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken)
    {
        await _repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        if (secret is not null)
        {
            await _credentials.SaveAsync(profile.Ssid, secret, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
