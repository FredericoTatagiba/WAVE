using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Security;

namespace WAVE.Application.Users;

/// <summary>
/// User management use cases (Administrator only). Every operation is validated
/// by <see cref="Permission.ManageUsers"/> at the application layer.
/// Protects the last administrator against removal/demotion.
/// </summary>
public sealed class UserManagementService
{
    private const int MinPasswordLength = 8;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IAuthorizationService _authorization;

    public UserManagementService(
        IUserRepository users, IPasswordHasher hasher, IAuthorizationService authorization)
    {
        _users = users;
        _hasher = hasher;
        _authorization = authorization;
    }

    public async Task<Result<IReadOnlyList<UserAccount>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var authorization = _authorization.Authorize(Permission.ManageUsers);
        if (authorization.IsFailure)
        {
            return Result<IReadOnlyList<UserAccount>>.Failure(authorization.Error);
        }

        return Result<IReadOnlyList<UserAccount>>.Success(await _users.GetAllAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> CreateAsync(
        string username, string? displayName, UserRole role, string password, CancellationToken cancellationToken = default)
    {
        var authorization = _authorization.Authorize(Permission.ManageUsers);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure("Informe o login.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
        {
            return Result.Failure($"A senha deve ter ao menos {MinPasswordLength} caracteres.");
        }

        var existing = await _users.FindByUsernameAsync(username.Trim(), cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result.Failure("Já existe um usuário com esse login.");
        }

        var user = new UserAccount(Guid.NewGuid(), username, displayName, role);
        await _users.UpsertAsync(user, _hasher.Hash(password), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ChangeRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var authorization = _authorization.Authorize(Permission.ManageUsers);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var all = await _users.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var user = all.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            return Result.Failure("Usuário não encontrado.");
        }

        if (user.IsAdministrator && role != UserRole.Administrator && CountAdministrators(all) <= 1)
        {
            return Result.Failure("Não é possível rebaixar o último administrador.");
        }

        var hash = await _users.GetPasswordHashAsync(userId, cancellationToken).ConfigureAwait(false);
        if (hash is null)
        {
            return Result.Failure("Credencial do usuário não encontrada.");
        }

        await _users.UpsertAsync(user.WithRole(role), hash, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var authorization = _authorization.Authorize(Permission.ManageUsers);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MinPasswordLength)
        {
            return Result.Failure($"A senha deve ter ao menos {MinPasswordLength} caracteres.");
        }

        var user = (await _users.GetAllAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            return Result.Failure("Usuário não encontrado.");
        }

        await _users.UpsertAsync(user, _hasher.Hash(newPassword), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var authorization = _authorization.Authorize(Permission.ManageUsers);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var all = await _users.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var user = all.FirstOrDefault(u => u.Id == userId);
        if (user is null)
        {
            return Result.Success();
        }

        if (user.IsAdministrator && CountAdministrators(all) <= 1)
        {
            return Result.Failure("Não é possível excluir o último administrador.");
        }

        await _users.DeleteAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static int CountAdministrators(IReadOnlyList<UserAccount> users) => users.Count(u => u.IsAdministrator);
}
