namespace WAVE.Domain.Testing;

/// <summary>
/// Throughput result observed during the test (fast.com). Since the measurement
/// happens in the browser, the value can be recorded by the technician for auditing.
/// </summary>
public readonly record struct SpeedResult(double DownloadMbps, double UploadMbps, DateTimeOffset MeasuredAt);
