namespace WAVE.Domain.Testing;

/// <summary>Which throughput phase a live <see cref="SpeedSample"/> belongs to.</summary>
public enum SpeedPhase
{
    Download = 0,
    Upload = 1
}

/// <summary>
/// A single live throughput reading emitted while a measurement is in progress
/// (fast.com-style ramp). Carries the running rate in Mbps for the current phase.
/// The authoritative end result is still recorded as a <see cref="SpeedResult"/>.
/// </summary>
public readonly record struct SpeedSample(SpeedPhase Phase, double Mbps);
