namespace WAVE.Domain.Security;

/// <summary>Application access roles (RBAC).</summary>
public enum UserRole
{
    /// <summary>Runs tests and views history.</summary>
    Operator = 0,

    /// <summary>Everything the operator can do, plus manages profiles/credentials and settings.</summary>
    Administrator = 1
}
