namespace WAVE.Application.Testing;

/// <summary>
/// Converte bytes transferidos e tempo decorrido em vazão (Mbps). Lógica pura,
/// isolada do IO para ser testável sem rede. 1 Mbps = 1.000.000 bits/s.
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
