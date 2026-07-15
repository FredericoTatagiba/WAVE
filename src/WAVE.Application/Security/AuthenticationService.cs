using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Security;

namespace WAVE.Application.Security;

/// <summary>
/// Authentication and first access. On the first run (no users), it creates the
/// initial administrator. At login, it validates the password and sets the current user.
/// </summary>
public sealed class AuthenticationService
{
    private const int MinPasswordLength = 8;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUserContext _currentUser;

    public AuthenticationService(IUserRepository users, IPasswordHasher hasher, ICurrentUserContext currentUser)
    {
        _users = users;
        _hasher = hasher;
        _currentUser = currentUser;
    }

    public async Task<bool> IsFirstRunAsync(CancellationToken cancellationToken = default) =>
        !await _users.HasAnyAsync(cancellationToken).ConfigureAwait(false);

    public async Task<Result> CreateInitialAdministratorAsync(
        string username, string? displayName, string password, CancellationToken cancellationToken = default)
    {
        if (!await IsFirstRunAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure("Já existe um usuário configurado.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return Result.Failure("Informe o login.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
        {
            return Result.Failure($"A senha deve ter ao menos {MinPasswordLength} caracteres.");
        }

        var administrator = new UserAccount(Guid.NewGuid(), username, displayName, UserRole.Administrator);
        await _users.UpsertAsync(administrator, _hasher.Hash(password), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<UserAccount>> AuthenticateAsync(
        string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _users
            .FindByUsernameAsync(username?.Trim() ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (user is not null)
        {
            var hash = await _users.GetPasswordHashAsync(user.Id, cancellationToken).ConfigureAwait(false);
            if (hash is not null && _hasher.Verify(password ?? string.Empty, hash))
            {
                _currentUser.Set(user.Role, user.DisplayName);
                return Result<UserAccount>.Success(user);
            }
        }

        return Result<UserAccount>.Failure("Login ou senha inválidos.");
    }

    public void SignOut() => _currentUser.Set(UserRole.Operator, "—");
}
