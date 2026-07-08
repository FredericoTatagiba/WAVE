namespace WAVE.Domain.Networking;

/// <summary>
/// Rede Wi-Fi previamente configurada para teste. Identidade lógica = SSID.
/// Não guarda a senha: a credencial fica no armazenamento seguro (DPAPI),
/// referenciada pelo SSID. Isso reduz a superfície de exposição de segredos.
/// </summary>
public sealed class WifiNetworkProfile
{
    /// <summary>Tamanho máximo de um SSID em octetos (padrão IEEE 802.11).</summary>
    public const int MaxSsidLength = 32;

    public string Ssid { get; }

    public string DisplayName { get; }

    public SecurityType Security { get; }

    public bool IsEnterprise => Security.IsEnterprise();

    /// <summary>Redes abertas não exigem credencial.</summary>
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
