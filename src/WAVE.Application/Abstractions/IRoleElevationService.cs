using WAVE.Domain.Common;

namespace WAVE.Application.Abstractions;

/// <summary>Eleva/rebaixa o papel do usuário atual mediante autenticação.</summary>
public interface IRoleElevationService
{
    /// <summary>Eleva para Administrador se a senha administrativa for válida.</summary>
    Result ElevateToAdministrator(string password);

    /// <summary>Retorna ao papel de Operador (menor privilégio).</summary>
    void ReturnToOperator();
}
