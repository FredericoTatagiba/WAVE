using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Testing;

namespace WAVE.Application.Testing;

/// <summary>
/// Orquestra o ciclo de vida de um teste de conectividade (máquina de estados).
/// A UI depende desta abstração, não da implementação.
/// </summary>
public interface IWifiTestOrchestrator
{
    TestOperationState CurrentState { get; }

    string? ActiveSsid { get; }

    event EventHandler<TestStateChangedEventArgs>? StateChanged;

    event EventHandler<PingSample>? PingSampled;

    /// <summary>Executa o fluxo de conexão + validação para a rede informada.</summary>
    Task<Result> RunTestAsync(WifiNetworkProfile profile, CancellationToken cancellationToken = default);

    /// <summary>Encerra o teste em andamento e retorna ao estado ocioso.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Reconhece uma falha (após alerta) e retorna ao estado ocioso.</summary>
    void AcknowledgeFailure();
}
