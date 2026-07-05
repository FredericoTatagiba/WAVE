namespace WAVE.Domain.Testing;

/// <summary>Estados da operação, conforme seção 5 da especificação técnica.</summary>
public enum TestOperationState
{
    /// <summary>Interface carregada, aguardando interação.</summary>
    Idle,

    /// <summary>Conexão enviada; autenticação/DHCP em andamento.</summary>
    Connecting,

    /// <summary>Conexão estabelecida; rotinas de validação em execução.</summary>
    TestRunning,

    /// <summary>Falha na associação ou timeout de DHCP.</summary>
    Failed
}
