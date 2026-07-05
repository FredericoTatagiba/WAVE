using WAVE.Domain.Security;

namespace WAVE.Application.Abstractions;

/// <summary>Contexto do usuário atual (papel e identidade) para o RBAC.</summary>
public interface ICurrentUserContext
{
    UserRole Role { get; }

    string UserName { get; }

    event EventHandler? Changed;

    /// <summary>Define papel/identidade atuais. Uso interno da elevação de papel.</summary>
    void Set(UserRole role, string userName);
}
