using WAVE.Domain.Security;

namespace WAVE.Application.Abstractions;

/// <summary>Current user context (role and identity) for RBAC.</summary>
public interface ICurrentUserContext
{
    UserRole Role { get; }

    string UserName { get; }

    event EventHandler? Changed;

    /// <summary>Sets the current role/identity. Internal use by role elevation.</summary>
    void Set(UserRole role, string userName);
}
