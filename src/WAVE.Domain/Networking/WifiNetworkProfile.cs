namespace WAVE.Domain.Networking;

/// <summary>
/// Wi-Fi network previously configured for testing. Logical identity = SSID.
/// Does not hold the password: the credential lives in secure storage (DPAPI),
/// referenced by SSID. This reduces the exposure surface for secrets.
/// </summary>
public sealed class WifiNetworkProfile
{
    /// <summary>Maximum length of an SSID in octets (IEEE 802.11 standard).</summary>
    public const int MaxSsidLength = 32;

    public string Ssid { get; }

    public string DisplayName { get; }

    public SecurityType Security { get; }

    public bool IsEnterprise => Security.IsEnterprise();

    /// <summary>Open networks do not require a credential.</summary>
    public bool RequiresCredential => Security.RequiresCredential();

    public WifiNetworkProfile(string ssid, string? displayName, SecurityType security)
    {
        if (string.IsNullOrWhiteSpace(ssid))
        {
            throw new ArgumentException("SSID é obrigatório.", nameof(ssid));
        }

        var trimmed = ssid.Trim();
        if (trimmed.Length > MaxSsidLength)
        {
            throw new ArgumentException(
                $"SSID excede {MaxSsidLength} caracteres.", nameof(ssid));
        }

        Ssid = trimmed;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? trimmed : displayName.Trim();
        Security = security;
    }
}
