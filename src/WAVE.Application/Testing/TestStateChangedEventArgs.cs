using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>Notifies test-operation state changes to the UI.</summary>
public sealed class TestStateChangedEventArgs : EventArgs
{
    public TestStateChangedEventArgs(
        TestOperationState state,
        string? ssid,
        TestFailureReason failureReason = TestFailureReason.None,
        string message = "")
    {
        State = state;
        Ssid = ssid;
        FailureReason = failureReason;
        Message = message;
    }

    public TestOperationState State { get; }

    public string? Ssid { get; }

    public TestFailureReason FailureReason { get; }

    public string Message { get; }
}
