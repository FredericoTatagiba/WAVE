namespace WAVE.Domain.Testing;

/// <summary>Motivo estruturado de falha, para auditoria e mensagens ao operador.</summary>
public enum TestFailureReason
{
    None,
    Unauthorized,
    AlreadyRunning,
    MissingCredential,
    ProfileCreationFailed,
    AuthenticationFailed,
    DhcpTimeout,
    Unexpected
}
