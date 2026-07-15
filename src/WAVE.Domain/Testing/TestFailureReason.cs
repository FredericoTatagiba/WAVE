namespace WAVE.Domain.Testing;

/// <summary>Structured failure reason, for auditing and operator messages.</summary>
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
