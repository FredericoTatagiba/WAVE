using System.Diagnostics;
using WAVE.Application.Abstractions;
using WAVE.Application.Testing;
using WAVE.Domain.Testing;

namespace WAVE.Infrastructure.Web;

/// <summary>
/// Measures throughput (download and, optionally, upload) over HTTP, without a browser.
/// Transfers N bytes from a configurable endpoint, timing the transfer and converting to
/// Mbps with <see cref="ThroughputCalculator"/>. While the transfer runs it reports the
/// running rate through <see cref="IProgress{T}"/> so the UI can animate a fast.com-style
/// gauge. Throws on network error (the orchestrator tolerates it and records the run
/// without the value).
/// </summary>
public sealed class HttpSpeedMeter : ISpeedMeter
{
    private static readonly TimeSpan ReportInterval = TimeSpan.FromMilliseconds(100);
    private const int BufferSize = 81920;

    private readonly TestRunnerOptions _options;

    public HttpSpeedMeter(TestRunnerOptions options) => _options = options;

    public async Task<SpeedResult> MeasureAsync(
        IProgress<SpeedSample>? progress = null, CancellationToken cancellationToken = default)
    {
        var download = await MeasureDownloadAsync(progress, cancellationToken).ConfigureAwait(false);
        var upload = _options.MeasureUpload
            ? await MeasureUploadAsync(progress, cancellationToken).ConfigureAwait(false)
            : 0d;

        return new SpeedResult(download, upload, DateTimeOffset.Now);
    }

    private async Task<double> MeasureDownloadAsync(
        IProgress<SpeedSample>? progress, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = _options.SpeedTimeout };

        var stopwatch = Stopwatch.StartNew();
        using var response = await client
            .GetAsync(_options.SpeedDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        long total = 0;
        var buffer = new byte[BufferSize];
        var lastReport = TimeSpan.Zero;
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;

            var elapsed = stopwatch.Elapsed;
            if (progress is not null && elapsed - lastReport >= ReportInterval)
            {
                lastReport = elapsed;
                progress.Report(new SpeedSample(SpeedPhase.Download, ThroughputCalculator.ToMbps(total, elapsed)));
            }
        }

        stopwatch.Stop();
        var mbps = ThroughputCalculator.ToMbps(total, stopwatch.Elapsed);
        progress?.Report(new SpeedSample(SpeedPhase.Download, mbps));
        return mbps;
    }

    private async Task<double> MeasureUploadAsync(
        IProgress<SpeedSample>? progress, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = _options.SpeedTimeout };

        var stopwatch = Stopwatch.StartNew();
        using var content = new ProgressReportingContent(
            _options.SpeedUploadBytes,
            ReportInterval,
            sent => progress?.Report(
                new SpeedSample(SpeedPhase.Upload, ThroughputCalculator.ToMbps(sent, stopwatch.Elapsed))));

        using var response = await client
            .PostAsync(_options.SpeedUploadUrl, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        stopwatch.Stop();

        var mbps = ThroughputCalculator.ToMbps(_options.SpeedUploadBytes, stopwatch.Elapsed);
        progress?.Report(new SpeedSample(SpeedPhase.Upload, mbps));
        return mbps;
    }
}
