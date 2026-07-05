namespace WAVE.Domain.Security;

/// <summary>Papéis de acesso da aplicação (RBAC).</summary>
public enum UserRole
{
    /// <summary>Executa testes e consulta histórico.</summary>
    Operator = 0,

    /// <summary>Além do operador, gerencia perfis/credenciais e configurações.</summary>
    Administrator = 1
}
