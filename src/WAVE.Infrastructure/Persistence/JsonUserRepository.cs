using System.Text.Json;
using WAVE.Application.Abstractions;
using WAVE.Domain.Security;
using WAVE.Infrastructure.Configuration;

namespace WAVE.Infrastructure.Persistence;

/// <summary>Repositório de usuários em arquivo JSON, com acesso serializado.</summary>
public sealed class JsonUserRepository : IUserRepository
{
    private sealed record UserRecord(Guid Id, string Username, string DisplayName, UserRole Role, string PasswordHash);

    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly IAppLogger _logger;
    private readonly string _file;

    public JsonUserRepository(IAppLogger logger)
    {
        _logger = logger;
        AppPaths.EnsureCreated();
        _file = AppPaths.UsersFile;
    }

    public async Task<bool> HasAnyAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return (await LoadAsync(cancellationToken).ConfigureAwait(false)).Count > 0;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return (await LoadAsync(cancellationToken).ConfigureAwait(false))
                .Select(ToAccount)
                .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var record = (await LoadAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            return record is null ? null : ToAccount(record);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return (await LoadAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(u => u.Id == userId)?.PasswordHash;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task UpsertAsync(UserAccount user, string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var users = await LoadAsync(cancellationToken).ConfigureAwait(false);
            users.RemoveAll(u => u.Id == user.Id);
            users.Add(new UserRecord(user.Id, user.Username, user.DisplayName, user.Role, passwordHash));
            await PersistAsync(users, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var users = await LoadAsync(cancellationToken).ConfigureAwait(false);
            users.RemoveAll(u => u.Id == userId);
            await PersistAsync(users, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private static UserAccount ToAccount(UserRecord record) =>
        new(record.Id, record.Username, record.DisplayName, record.Role);

    private async Task<List<UserRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_file))
        {
            return new List<UserRecord>();
        }

        try
        {
            await using var stream = File.OpenRead(_file);
            var users = await JsonSerializer
                .DeserializeAsync<List<UserRecord>>(stream, WaveJson.Options, cancellationToken)
                .ConfigureAwait(false);
            return users ?? new List<UserRecord>();
        }
        catch (Exception exception)
        {
            _logger.Error("Falha ao ler usuários; retornando lista vazia.", exception);
            return new List<UserRecord>();
        }
    }

    private async Task PersistAsync(List<UserRecord> users, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_file);
        await JsonSerializer.SerializeAsync(stream, users, WaveJson.Options, cancellationToken).ConfigureAwait(false);
    }
}
