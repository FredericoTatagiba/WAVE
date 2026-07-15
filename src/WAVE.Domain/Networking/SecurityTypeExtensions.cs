namespace WAVE.Domain.Networking;

/// <summary>
/// Rules derived from the security type, in a single place (shared source of truth
/// between domain and UI). Avoids duplicating the "needs a password" / "is enterprise"
/// logic across several layers.
/// </summary>
public static class SecurityTypeExtensions
{
    /// <summary>Open networks do not require a credential; all others do.</summary>
    public static bool RequiresCredential(this SecurityType security) =>
        security != SecurityType.Open;

    /// <summary>802.1X networks (user/domain), unlike Personal (password only).</summary>
    public static bool IsEnterprise(this SecurityType security) =>
        security is SecurityType.Wpa2Enterprise or SecurityType.Wpa3Enterprise;
}
