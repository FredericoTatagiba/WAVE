namespace WAVE.Application.Testing;

/// <summary>
/// Converts transferred bytes and elapsed time into throughput (Mbps). Pure logic,
/// isolated from IO to be testable without a network. 1 Mbps = 1,000,000 bits/s.
/// </summary>
public static class ThroughputCalculator
{
    public static double ToMbps(long bytes, TimeSpan elapsed)
    {
        if (bytes <= 0 || elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        return bytes * 8d / elapsed.TotalSeconds / 1_000_000d;
    }
}
