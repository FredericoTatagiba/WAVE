using WAVE.Domain.Common;
using WAVE.Domain.Security;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Verifica permissões do usuário atual. Deve ser chamado em todas as camadas
/// (não apenas na UI), conforme as Regras Primordiais de segurança.
/// </summary>
public interface IAuthorizationService
{
    bool HasPermission(Permission permission);

    /// <summary>Retorna falha quando a permissão não é concedida.</summary>
    Result Authorize(Permission permission);
}
