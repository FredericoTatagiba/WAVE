using WAVE.Application.Abstractions;
using WAVE.Domain.Security;

namespace WAVE.Application.Security;

/// <summary>
/// In-memory user context. The role and identity are set at login
/// (see AuthenticationService).
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    private const string DefaultOperatorName = "Operador";

    public UserRole Role { get; private set; } = UserRole.Operator;

    public string UserName { get; private set; } = DefaultOperatorName;

    public event EventHandler? Changed;

    public void Set(UserRole role, string userName)
    {
        Role = role;
        UserName = string.IsNullOrWhiteSpace(userName) ? role.ToString() : userName.Trim();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
