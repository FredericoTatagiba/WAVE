using WAVE.Domain.Testing;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Mede a vazão da conexão (download e, opcionalmente, upload) diretamente no app,
/// sem depender do navegador. A implementação faz a transferência HTTP e converte
/// para Mbps; lança em caso de falha de rede (o orquestrador trata e segue).
/// </summary>
public interface ISpeedMeter
{
    Task<SpeedResult> MeasureAsync(CancellationToken cancellationToken = default);
}
