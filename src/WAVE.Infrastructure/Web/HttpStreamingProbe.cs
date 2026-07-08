using System.Diagnostics;
using WAVE.Application.Abstractions;
using WAVE.Application.Testing;

namespace WAVE.Infrastructure.Web;

/// <summary>
/// Sonda de streaming: baixa um fluxo sustentado por um período e amostra a vazão
/// (Mbps) em cada intervalo. Não interpreta os dados — a classificação de
/// estabilidade fica em <see cref="StreamingStabilityEvaluator"/> (lógica pura).
/// </summary>
public sealed class HttpStreamingProbe : IStreamingProbe
{
    private readonly TestRunnerOptions _options;

    public HttpStreamingProbe(TestRunnerOptions options) => _options = options;

    public async Task<IReadOnlyList<double>> SampleAsync(CancellationToken cancellationToken = default)
    {
        var samples = new List<double>();

        // A duração é controlada pelo laço; o timeout do cliente fica desligado para
        // não interromper o fluxo antes da hora.
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
