using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Security;

namespace WAVE.Application.Security;

/// <summary>Serviço de autorização baseado no papel do usuário atual.</summary>
public sealed class AuthorizationService : IAuthorizationService
{
    private readonly ICurrentUserContext _currentUser;

    public AuthorizationService(ICurrentUserContext currentUser) => _currentUser = currentUser;

    public bool HasPermission(Permission permission) =>
        RolePermissionMap.IsGranted(_currentUser.Role, permission);

    public Result Authorize(Permission permission) =>
        HasPermission(permission)
            ? Result.Success()
            : Result.Failure($"Acesso negado: a ação requer a permissão '{permission}'.");
}
