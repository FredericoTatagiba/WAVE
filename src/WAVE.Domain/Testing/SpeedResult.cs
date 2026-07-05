namespace WAVE.Domain.Testing;

/// <summary>
/// Resultado de vazão observado no teste (fast.com). Como a medição ocorre no
/// navegador, o valor pode ser registrado pelo técnico para fins de auditoria.
/// </summary>
public readonly record struct SpeedResult(double DownloadMbps, double UploadMbps, DateTimeOffset MeasuredAt);
