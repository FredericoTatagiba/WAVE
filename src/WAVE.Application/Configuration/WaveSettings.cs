namespace WAVE.Application.Configuration;

/// <summary>
/// Per-device settings, persisted outside the binary: where the audit artefacts are
/// written, and the administrator password that guards changing any of it.
/// </summary>
/// <remarks>
/// Only the history and the logs are relocatable. Network profiles and credentials stay
/// on the machine on purpose: the Windows credential blobs are encrypted for one user
/// account and simply would not decrypt if a shared folder carried them to another
/// device, so a "move everything" option would silently break the credential store.
/// </remarks>
public sealed record WaveSettings
{
    /// <summary>Directory holding <c>history.json</c>; null or empty means the local default.</summary>
    public string? HistoryDirectory { get; init; }

    /// <summary>Directory holding the daily log files; null or empty means the local default.</summary>
    public string? LogsDirectory { get; init; }

    /// <summary>
    /// Default ping target for this device; null means the built-in one. The operator can
    /// override it per test, which is why every run records the target it actually used —
    /// otherwise two rows of the history would stop being comparable without saying so.
    /// </summary>
    public string? PingTargetHost { get; init; }

    /// <summary>
    /// PBKDF2 hash of the administrator password, or null while no password has been set.
    /// Absent by design on a fresh install: the operator path costs nothing until someone
    /// reaches for an administrator action.
    /// </summary>
    public string? AdminPasswordHash { get; init; }
}
