namespace WAVE.Application.Abstractions;

/// <summary>
/// Encerra apenas os processos que o próprio WAVE iniciou, rastreados por PID
/// (ex.: a janela de ping). Evita o dano colateral de encerrar por nome, que
/// fecharia navegadores/terminais do usuário. Substitui a antiga terminação por
/// nome, agora desnecessária (a medição de vazão/streaming roda no app).
/// </summary>
public interface IProcessTerminator
{
    /// <summary>Passa a rastrear um processo iniciado pelo WAVE (por PID).</summary>
    void Track(int processId);

    /// <summary>Encerra os processos rastreados que ainda estão vivos e limpa o registro. Retorna a quantidade encerrada.</summary>
    int TerminateTracked();
}
