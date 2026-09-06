namespace WAVE.Application.Abstractions;

/// <summary>Identifies the machine a test ran on, recorded in the history.</summary>
/// <remarks>
/// WAVE records the device rather than a person. On a shared field tablet a per-operator
/// login converges on one account and the resulting audit trail names someone who may not
/// have run the test — worse than no name, because the report implies accountability that
/// is not there. The device is a fact the app can actually assert.
/// </remarks>
public interface IDeviceIdentity
{
    string Name { get; }
}
