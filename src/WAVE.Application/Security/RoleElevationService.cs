using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Security;

namespace WAVE.Application.Security;

/// <summary>Eleva o papel do usuário mediante senha administrativa válida.</summary>
public sealed class RoleElevationService : IRoleElevationService
{
    private const string AdministratorName = "Administrador";
    private const string OperatorName = "Operador";

    private readonly IAdminPasswordVerifier _verifier;
    private readonly ICurrentUserContext _currentUser;

    public RoleElevationService(IAdminPasswordVerifier verifier, ICurrentUserContext currentUser)
    {
        _verifier = verifier;
        _currentUser = currentUser;
    }

    public Result ElevateToAdministrator(string password)
    {
        if (!_verifier.IsConfigured)
        {
            return Result.Failure("Nenhuma senha administrativa configurada.");
        }

        if (string.IsNullOrEmpty(password) || !_verifier.Verify(password))
        {
            return Result.Failure("Senha administrativa inválida.");
        }

        _currentUser.Set(UserRole.Administrator, AdministratorName);
        return Result.Success();
    }

    public void ReturnToOperator() => _currentUser.Set(UserRole.Operator, OperatorName);
}
