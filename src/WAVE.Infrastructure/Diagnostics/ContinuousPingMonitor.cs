using System.Net.NetworkInformation;
using WAVE.Application.Abstractions;
using WAVE.Domain.Testing;

namespace WAVE.Infrastructure.Diagnostics;

/// <summary>
/// Runs a background ping using <see cref="Ping"/> and emits samples for the
/// in-app telemetry (latency chart). Independent of the visible window.
/// </summary>
public sealed class ContinuousPingMonitor : IContinuousPingMonitor, IDisposable
{
    private const int PingTimeoutMilliseconds = 4000;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly IClock _clock;
    private readonly IAppLogger _logger;
    private readonly object _gate = new();

    private CancellationTokenSource? _cancellation;
    private Task? _loop;

    public ContinuousPingMonitor(IClock clock, IAppLogger logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public event EventHandler<PingSample>? Sampled;

    public bool IsRunning { get; private set; }

    public void Start(string host)
    {
        lock (_gate)
        {
            if (IsRunning)
            {
                return;
            }

            _cancellation = new CancellationTokenSource();
            IsRunning = true;
            _loop = Task.Run(() => RunLoopAsync(host, _cancellation.Token));
        }
    }

    public async Task StopAsync()
    {
        Task? loop;

        lock (_gate)
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            _cancellation?.Cancel();
            loop = _loop;
        }

        if (loop is not null)
        {
            try
            {
                await loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected shutdown.
            }
        }

        lock (_gate)
        {
            _cancellation?.Dispose();
            _cancellation = null;
            _loop = null;
        }
    }

    private async Task RunLoopAsync(string host, CancellationToken cancellationToken)
    {
        using var ping = new Ping();

        while (!cancellationToken.IsCancellationRequested)
        {
            Sampled?.Invoke(this, await SendAsync(ping, host).ConfigureAwait(false));

            try
            {
                await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<PingSample> SendAsync(Ping ping, string host)
    {
        try
        {
            var reply = await ping.SendPingAsync(host, PingTimeoutMilliseconds).ConfigureAwait(false);
            return reply.Status == IPStatus.Success
                ? PingSample.Reply(_clock.Now, reply.RoundtripTime)
                : PingSample.Timeout(_clock.Now);
        }
        catch (Exception exception)
        {
            _logger.Warn($"Ping failed: {exception.Message}");
            return PingSample.Timeout(_clock.Now);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }
}
