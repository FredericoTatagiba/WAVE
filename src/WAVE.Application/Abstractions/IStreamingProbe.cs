namespace WAVE.Application.Abstractions;

/// <summary>
/// Sonda de streaming: baixa um fluxo sustentado por um período e retorna a vazão
/// (Mbps) medida em cada intervalo. A classificação de estabilidade é feita por
/// <see cref="Testing.StreamingStabilityEvaluator"/> (lógica pura), mantendo esta
/// abstração restrita ao IO.
/// </summary>
public interface IStreamingProbe
{
    Task<IReadOnlyList<double>> SampleAsync(CancellationToken cancellationToken = default);
}
