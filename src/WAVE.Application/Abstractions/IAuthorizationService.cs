using WAVE.Domain.Common;
using WAVE.Domain.Security;

namespace WAVE.Application.Abstractions;

/// <summary>
/// Checks the current user's permissions. Must be called across all layers
/// (not only in the UI), per the Core security Rules.
/// </summary>
public interface IAuthorizationService
{
    bool HasPermission(Permission permission);

    /// <summary>Returns failure when the permission is not granted.</summary>
    Result Authorize(Permission permission);
}
