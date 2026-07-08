namespace WAVE.Application.Testing;

/// <summary>
/// Parâmetros configuráveis do teste (sem "magic numbers" espalhados).
/// Valores default seguem a especificação; podem vir de configuração externa.
/// </summary>
public sealed class TestRunnerOptions
{
    /// <summary>Host alvo do ping contínuo.</summary>
    public string PingTargetHost { get; init; } = "google.com";

    /// <summary>Endpoint de download para medir vazão (retorna N bytes).</summary>
    public string SpeedDownloadUrl { get; init; } = "https://speed.cloudflare.com/__down?bytes=52428800";

    /// <summary>Endpoint de upload para medir vazão (aceita POST).</summary>
    public string SpeedUploadUrl { get; init; } = "https://speed.cloudflare.com/__up";

    /// <summary>Quantidade de bytes enviados na medição de upload.</summary>
    public long SpeedUploadBytes { get; init; } = 10_485_760;

    /// <summary>Se false, mede apenas o download.</summary>
    public bool MeasureUpload { get; init; } = true;

    /// <summary>Tempo máximo para cada medição de vazão.</summary>
    public TimeSpan SpeedTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Endpoint baixado de forma sustentada para sondar o streaming.</summary>
    public string StreamingProbeUrl { get; init; } = "https://speed.cloudflare.com/__down?bytes=104857600";

    /// <summary>Duração total da sonda de streaming.</summary>
    public TimeSpan StreamingDuration { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Intervalo de amostragem da vazão durante a sonda de streaming.</summary>
    public TimeSpan StreamingSampleInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Bitrate-alvo (Mbps) para sustentar a qualidade (ex.: ~8 Mbps p/ 1080p).</summary>
    public double StreamingTargetMbps { get; init; } = 8;

    /// <summary>Tempo de estabilização do hardware após a associação.</summary>
    public TimeSpan StabilizationDelay { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>Tempo máximo para obter IP via DHCP.</summary>
    public TimeSpan DhcpTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Intervalo entre verificações de concessão DHCP.</summary>
    public TimeSpan DhcpPollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Máximo de itens mantidos no histórico.</summary>
    public int MaxHistoryEntries { get; init; } = 200;
}
