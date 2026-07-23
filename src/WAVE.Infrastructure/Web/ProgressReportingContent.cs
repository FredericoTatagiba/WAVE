using System.Diagnostics;
using System.Net;

namespace WAVE.Infrastructure.Web;

/// <summary>
/// HTTP body that streams a fixed number of bytes to the request stream in chunks,
/// invoking a callback with the cumulative bytes sent (throttled). Lets the upload
/// measurement report live progress the same way the download does. Single responsibility:
/// produce the payload and surface transfer progress.
/// </summary>
internal sealed class ProgressReportingContent : HttpContent
{
    private const int ChunkSize = 81920;

    private readonly long _totalBytes;
    private readonly TimeSpan _reportInterval;
    private readonly Action<long> _onProgress;

    public ProgressReportingContent(long totalBytes, TimeSpan reportInterval, Action<long> onProgress)
    {
        _totalBytes = totalBytes;
        _reportInterval = reportInterval;
        _onProgress = onProgress;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var buffer = new byte[ChunkSize];
        long sent = 0;
        var stopwatch = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;

        while (sent < _totalBytes)
        {
            var chunk = (int)Math.Min(ChunkSize, _totalBytes - sent);
            await stream.WriteAsync(buffer.AsMemory(0, chunk)).ConfigureAwait(false);
            sent += chunk;

            var elapsed = stopwatch.Elapsed;
            if (elapsed - lastReport >= _reportInterval)
            {
                lastReport = elapsed;
                _onProgress(sent);
            }
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _totalBytes;
        return true;
    }
}
