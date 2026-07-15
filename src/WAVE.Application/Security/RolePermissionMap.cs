using WAVE.Domain.Security;

namespace WAVE.Application.Security;

/// <summary>
/// Static role -> permissions map (single source of RBAC). Changing it here reflects
/// across all layers, since authorization always derives from this map.
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
                Permission.EditSettings,
                Permission.ManageUsers
            }
        };

    public static bool IsGranted(UserRole role, Permission permission) =>
        Map.TryGetValue(role, out var permissions) && permissions.Contains(permission);
}
