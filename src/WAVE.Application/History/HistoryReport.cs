using WAVE.Domain.Testing;

namespace WAVE.Application.History;

/// <summary>One export column: a header and a typed value selector over a <see cref="TestRun"/>.</summary>
public sealed record HistoryColumn(string Header, Func<TestRun, object?> Value);

/// <summary>
/// Single source of truth for the history export layout: the ordered columns shared by
/// every exporter (CSV/XLSX/PDF), so the schema is defined once. Values are returned as
/// typed objects (string/number/date/null) and each exporter renders them appropriately.
/// Headers and human-facing labels are in Portuguese (report is read by the operator).
/// </summary>
public static class HistoryReport
{
    public static IReadOnlyList<HistoryColumn> Columns { get; } = new[]
    {
        new HistoryColumn("Rede", run => run.Ssid),
        new HistoryColumn("Meio", run => MediumText(run.Medium)),
        new HistoryColumn("Dispositivo", run => run.DeviceName),
        new HistoryColumn("Início", run => run.StartedAt),
        new HistoryColumn("Fim", run => run.FinishedAt),
        new HistoryColumn("Resultado", run => run.Succeeded ? "Sucesso" : "Falha"),
        new HistoryColumn("Motivo da falha", run => FailureText(run.FailureReason)),
        new HistoryColumn("Latência média (ms)", run => run.Ping.Received > 0 ? run.Ping.AvgMs : (object?)null),
        new HistoryColumn("Alvo do ping", run => run.PingTarget),
        new HistoryColumn("Jitter (ms)", run => run.Ping.JitterMs),
        new HistoryColumn("Latência p95 (ms)", run => run.Ping.P95Ms),
        new HistoryColumn("Latência ociosa (ms)", run => Average(run.PingIdle)),
        new HistoryColumn("Latência sob carga (ms)", run => Average(run.PingUnderLoad)),
        new HistoryColumn("Bufferbloat (ms)", run => run.BufferbloatMs),
        new HistoryColumn("Perda de pacotes (%)", run => run.Ping.PacketLossPercent),
        new HistoryColumn("Pacotes enviados", run => run.Ping.Sent),
        new HistoryColumn("Pacotes recebidos", run => run.Ping.Received),
        new HistoryColumn("Download (Mbps)", run => run.Speed?.DownloadMbps),
        new HistoryColumn("Upload (Mbps)", run => run.Speed?.UploadMbps),
        new HistoryColumn("Streaming", run => run.Streaming is { } streaming ? StreamingText(streaming.Stability) : null)
    };

    /// <summary>Average of a phase, or null when the phase produced no replies.</summary>
    private static object? Average(PingStatistics? statistics) =>
        statistics is { Received: > 0 } value ? value.AvgMs : null;

    public static string MediumText(TestMedium medium) => medium switch
    {
        TestMedium.Ethernet => "Cabo",
        _ => "Wi-Fi"
    };

    private static string FailureText(TestFailureReason reason) => reason switch
    {
        TestFailureReason.None => string.Empty,
        TestFailureReason.Unauthorized => "Não autorizado",
        TestFailureReason.AlreadyRunning => "Teste já em execução",
        TestFailureReason.MissingCredential => "Credencial ausente",
        TestFailureReason.ProfileCreationFailed => "Falha ao criar perfil",
        TestFailureReason.AuthenticationFailed => "Falha de autenticação",
        TestFailureReason.NoLink => "Sem link (cabo desconectado)",
        TestFailureReason.DhcpTimeout => "Timeout de DHCP",
        TestFailureReason.Unexpected => "Erro inesperado",
        _ => reason.ToString()
    };

    private static string StreamingText(StreamingStability stability) => stability switch
    {
        StreamingStability.Smooth => "Estável",
        StreamingStability.MinorBuffering => "Travadas leves",
        StreamingStability.Unstable => "Instável",
        _ => "Desconhecido"
    };
}
