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

    /// <summary>
    /// Runs the connect + validation flow for the given network. A credential just
    /// entered by the operator can be passed in <paramref name="providedSecret"/> for
    /// use during this test only; it must be remembered by the caller only after a
    /// confirmed success.
    /// </summary>
    Task<Result> RunTestAsync(
        WifiNetworkProfile profile,
        WifiSecret? providedSecret = null,
        CancellationToken cancellationToken = default);

    /// <summary>Encerra o teste em andamento e retorna ao estado ocioso.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Reconhece uma falha (após alerta) e retorna ao estado ocioso.</summary>
    void AcknowledgeFailure();
}
