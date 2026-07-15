using System.Diagnostics;
using WAVE.Application.Abstractions;
using WAVE.Application.Testing;

namespace WAVE.Infrastructure.Web;

/// <summary>
/// Streaming probe: downloads a sustained stream for a period and samples the throughput
/// (Mbps) in each interval. It does not interpret the data — the stability classification
/// lives in <see cref="StreamingStabilityEvaluator"/> (pure logic).
/// </summary>
public sealed class HttpStreamingProbe : IStreamingProbe
{
    private readonly TestRunnerOptions _options;

    public HttpStreamingProbe(TestRunnerOptions options) => _options = options;

    public async Task<IReadOnlyList<double>> SampleAsync(CancellationToken cancellationToken = default)
    {
        var samples = new List<double>();

        // The duration is controlled by the loop; the client timeout is disabled so it
        // does not interrupt the stream prematurely.
        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        using var response = await client
            .GetAsync(_options.StreamingProbeUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        var buffer = new byte[81920];
        var interval = _options.StreamingSampleInterval;
        var deadline = DateTime.UtcNow + _options.StreamingDuration;
        var intervalStopwatch = Stopwatch.StartNew();
        long intervalBytes = 0;
        int read;

        while (DateTime.UtcNow < deadline &&
               (read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            intervalBytes += read;

            if (intervalStopwatch.Elapsed >= interval)
            {
                samples.Add(ThroughputCalculator.ToMbps(intervalBytes, intervalStopwatch.Elapsed));
                intervalBytes = 0;
                intervalStopwatch.Restart();
            }
        }

        if (intervalBytes > 0 && intervalStopwatch.Elapsed > TimeSpan.Zero)
        {
            samples.Add(ThroughputCalculator.ToMbps(intervalBytes, intervalStopwatch.Elapsed));
        }

        return samples;
    }
}
