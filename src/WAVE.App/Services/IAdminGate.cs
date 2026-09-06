namespace WAVE.App.Services;

/// <summary>
/// Ensures administrator actions are unlocked, asking for the password at the moment the
/// action is attempted (and creating it on first use).
/// </summary>
public interface IAdminGate
{
    /// <summary>
    /// True when the caller may proceed. Returns false when the operator cancels the
    /// prompt or gets the password wrong.
    /// </summary>
    Task<bool> EnsureUnlockedAsync();
}
