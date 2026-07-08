using System.Diagnostics;
using WAVE.Application.Abstractions;
using WAVE.Application.Testing;
using WAVE.Domain.Testing;

namespace WAVE.Infrastructure.Web;

/// <summary>
/// Mede a vazão (download e, opcionalmente, upload) via HTTP, sem navegador. Baixa
/// N bytes de um endpoint configurável cronometrando a transferência e converte para
/// Mbps com <see cref="ThroughputCalculator"/>. Lança em erro de rede (o orquestrador
/// tolera e registra a execução sem o valor).
/// </summary>
public sealed class HttpSpeedMeter : ISpeedMeter
{
    private readonly TestRunnerOptions _options;

    public HttpSpeedMeter(TestRunnerOptions options) => _options = options;

    public async Task<SpeedResult> MeasureAsync(CancellationToken cancellationToken = default)
    {
        var download = await MeasureDownloadAsync(cancellationToken).ConfigureAwait(false);
        var upload = _options.MeasureUpload
            ? await MeasureUploadAsync(cancellationToken).ConfigureAwait(false)
            : 0d;

        return new SpeedResult(download, upload, DateTimeOffset.Now);
    }

    private async Task<double> MeasureDownloadAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = _options.SpeedTimeout };

        var stopwatch = Stopwatch.StartNew();
        using var response = await client
            .GetAsync(_options.SpeedDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        long total = 0;
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
        }

        stopwatch.Stop();
        return ThroughputCalculator.ToMbps(total, stopwatch.Elapsed);
    }

    private async Task<double> MeasureUploadAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = _options.SpeedTimeout };

        var payload = new byte[_options.SpeedUploadBytes];
        using var content = new ByteArrayContent(payload);

        var stopwatch = Stopwatch.StartNew();
        using var response = await client
            .PostAsync(_options.SpeedUploadUrl, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        stopwatch.Stop();

        return ThroughputCalculator.ToMbps(_options.SpeedUploadBytes, stopwatch.Elapsed);
    }
}
