namespace WAVE.Application.Testing;

/// <summary>
/// Parâmetros configuráveis do teste (sem "magic numbers" espalhados).
/// Valores default seguem a especificação; podem vir de configuração externa.
/// </summary>
public sealed class TestRunnerOptions
{
    /// <summary>Host alvo do ping contínuo.</summary>
    public string PingTargetHost { get; init; } = "google.com";

    /// <summary>URL do teste de vazão.</summary>
    public string SpeedTestUrl { get; init; } = "https://fast.com";

    /// <summary>
    /// URL do vídeo de streaming de teste — neutra e configurável
    /// (evita hardcode do exemplo da especificação).
    /// </summary>
    public string StreamingUrl { get; init; } = "https://www.youtube.com/watch?v=aqz-KE-bpKQ";

    /// <summary>Tempo de estabilização do hardware após a associação.</summary>
    public TimeSpan StabilizationDelay { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>Tempo máximo para obter IP via DHCP.</summary>
    public TimeSpan DhcpTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>Intervalo entre verificações de concessão DHCP.</summary>
    public TimeSpan DhcpPollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>Intervalo entre a abertura das ferramentas web.</summary>
    public TimeSpan BetweenLaunchesDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Nomes de processos a encerrar entre execuções (sem extensão).</summary>
    public IReadOnlyList<string> ProcessesToTerminate { get; init; } = new[] { "cmd", "msedge", "chrome" };

    /// <summary>Máximo de itens mantidos no histórico.</summary>
    public int MaxHistoryEntries { get; init; } = 200;
}
