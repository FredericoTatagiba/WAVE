namespace WAVE.Domain.Testing;

/// <summary>Operation states, per section 5 of the technical specification.</summary>
public enum TestOperationState
{
    /// <summary>Interface loaded, awaiting interaction.</summary>
    Idle,

    /// <summary>Connection requested; authentication/DHCP in progress.</summary>
    Connecting,

    /// <summary>Connection established; validation routines running.</summary>
    TestRunning,

    /// <summary>Association failure or DHCP timeout.</summary>
    Failed
}
