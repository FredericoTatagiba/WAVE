namespace WAVE.Application.Abstractions;

/// <summary>
/// Encerra processos remanescentes (ex.: navegador e terminal de ping) para
/// evitar acúmulo de memória entre execuções, conforme "Pontos de Atenção".
/// </summary>
public interface IProcessTerminator
{
    /// <summary>Encerra todos os processos cujos nomes constem na lista. Retorna a quantidade encerrada.</summary>
    int TerminateByNames(IReadOnlyCollection<string> processNames);
}
