using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>Notifies test-operation state changes to the UI.</summary>
public sealed class TestStateChangedEventArgs : EventArgs
{
    public TestStateChangedEventArgs(
        TestOperationState state,
        string? target,
        TestMedium medium = TestMedium.WiFi,
        TestFailureReason failureReason = TestFailureReason.None,
        string message = "")
    {
        State = state;
        Target = target;
        Medium = medium;
        FailureReason = failureReason;
        Message = message;
    }

    public TestOperationState State { get; }

    /// <summary>SSID being tested, or the wired adapter name; null once idle.</summary>
    public string? Target { get; }

    public TestMedium Medium { get; }

    public TestFailureReason FailureReason { get; }

    public string Message { get; }
}
