using WAVE.Domain.Common;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Guards the administrator actions: registering networks and changing settings. There is
/// no sign-in — running tests and reading history need nothing — so the password is asked
/// at the moment of the action and stays unlocked for the rest of the session.
/// </summary>
/// <remarks>
/// This is an application-level control, not a cryptographic boundary. Anyone able to edit
/// the settings file can clear the stored hash, and that is deliberately also the recovery
/// path when the password is lost.
/// </remarks>
public interface IAdminSession
{
    /// <summary>A password has already been set on this device.</summary>
    bool IsConfigured { get; }

    /// <summary>Administrator actions are unlocked for this session.</summary>
    bool IsUnlocked { get; }

    event EventHandler? Changed;

    /// <summary>Sets the password for the first time and unlocks the session.</summary>
    Task<Result> ConfigureAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>Unlocks administrator actions for this session.</summary>
    Task<Result> UnlockAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>Replaces the password, which requires knowing the current one.</summary>
    Task<Result> ChangePasswordAsync(
        string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    void Lock();

    /// <summary>
    /// Guard for administrator use cases, enforced in the Application layer so the rule
    /// does not depend on the UI having hidden a button.
    /// </summary>
    Result RequireUnlocked();
}
