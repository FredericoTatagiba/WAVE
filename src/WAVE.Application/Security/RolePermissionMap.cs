using WAVE.Domain.Security;

namespace WAVE.Application.Security;

/// <summary>
/// Mapa estático papel → permissões (fonte única do RBAC). Alterar aqui reflete
/// em todas as camadas, pois a autorização deriva sempre deste mapa.
/// </summary>
public static class RolePermissionMap
{
    private static readonly IReadOnlyDictionary<UserRole, IReadOnlySet<Permission>> Map =
        new Dictionary<UserRole, IReadOnlySet<Permission>>
        {
            [UserRole.Operator] = new HashSet<Permission>
            {
                Permission.RunTest,
                Permission.ViewHistory
            },
            [UserRole.Administrator] = new HashSet<Permission>
            {
                Permission.RunTest,
                Permission.ViewHistory,
                Permission.ManageProfiles,
                Permission.EditSettings
            }
        };

    public static bool IsGranted(UserRole role, Permission permission) =>
        Map.TryGetValue(role, out var permissions) && permissions.Contains(permission);
}
