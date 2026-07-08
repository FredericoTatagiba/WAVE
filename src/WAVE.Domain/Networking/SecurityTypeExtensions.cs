namespace WAVE.Domain.Networking;

/// <summary>
/// Regras derivadas do tipo de seguranca, em um unico lugar (fonte de verdade
/// compartilhada entre dominio e UI). Evita duplicar a logica de "precisa de
/// senha" / "e enterprise" em varias camadas.
/// </summary>
public static class SecurityTypeExtensions
{
    /// <summary>Redes abertas nao exigem credencial; as demais sim.</summary>
    public static bool RequiresCredential(this SecurityType security) =>
        security != SecurityType.Open;

    /// <summary>Redes 802.1X (usuario/dominio), diferente das Personal (so senha).</summary>
    public static bool IsEnterprise(this SecurityType security) =>
        security is SecurityType.Wpa2Enterprise or SecurityType.Wpa3Enterprise;
}
