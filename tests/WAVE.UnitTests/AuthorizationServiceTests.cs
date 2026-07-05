using WAVE.Application.Security;
using WAVE.Domain.Security;
using Xunit;

namespace WAVE.UnitTests;

public class AuthorizationServiceTests
{
    [Fact]
    public void Operator_CanRunTestAndViewHistory_ButNotManageProfiles()
    {
        var context = new CurrentUserContext(); // inicia como Operador
        var authorization = new AuthorizationService(context);

        Assert.True(authorization.HasPermission(Permission.RunTest));
        Assert.True(authorization.HasPermission(Permission.ViewHistory));
        Assert.False(authorization.HasPermission(Permission.ManageProfiles));
        Assert.True(authorization.Authorize(Permission.ManageProfiles).IsFailure);
    }

    [Fact]
    public void Administrator_HasAllPermissions()
    {
        var context = new CurrentUserContext();
        context.Set(UserRole.Administrator, "Admin");
        var authorization = new AuthorizationService(context);

        Assert.True(authorization.HasPermission(Permission.ManageProfiles));
        Assert.True(authorization.HasPermission(Permission.EditSettings));
        Assert.True(authorization.Authorize(Permission.EditSettings).IsSuccess);
    }
}
