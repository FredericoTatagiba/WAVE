using WAVE.Domain.Security;

namespace WAVE.Application.Abstractions;

/// <summary>Persistência das contas de usuário e seus hashes de senha.</summary>
public interface IUserRepository
{
    Task<bool> HasAnyAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken cancellationToken = default);

    Task UpsertAsync(UserAccount user, string passwordHash, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}
