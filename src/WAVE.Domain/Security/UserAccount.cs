namespace WAVE.Domain.Security;

/// <summary>
/// System user account. Logical identity = <see cref="Username"/>.
/// Does not hold the password: the hash lives in persistence (see IUserRepository).
/// </summary>
public sealed class UserAccount
{
    public UserAccount(Guid id, string username, string? displayName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Login é obrigatório.", nameof(username));
        }

        Id = id;
        Username = username.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Username : displayName.Trim();
        Role = role;
    }

    public Guid Id { get; }

    public string Username { get; }

    public string DisplayName { get; }

    public UserRole Role { get; }

    public bool IsAdministrator => Role == UserRole.Administrator;

    public UserAccount WithRole(UserRole role) => new(Id, Username, DisplayName, role);
}
