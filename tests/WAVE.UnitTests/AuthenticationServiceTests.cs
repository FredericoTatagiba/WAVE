using WAVE.Application.Security;
using WAVE.Domain.Security;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

public class AuthenticationServiceTests
{
    private static AuthenticationService Build(out CurrentUserContext context)
    {
        context = new CurrentUserContext();
        return new AuthenticationService(new FakeUserRepository(), new FakePasswordHasher(), context);
    }

    [Fact]
    public async Task IsFirstRun_IsTrue_WhenNoUsersExist()
    {
        var authentication = Build(out _);

        Assert.True(await authentication.IsFirstRunAsync());
    }

    [Fact]
    public async Task CreateInitialAdministrator_CreatesAdmin_AndClearsFirstRun()
    {
        var authentication = Build(out _);

        var result = await authentication.CreateInitialAdministratorAsync("admin", "Administrador", "senhaForte1");

        Assert.True(result.IsSuccess);
        Assert.False(await authentication.IsFirstRunAsync());
    }

    [Fact]
    public async Task CreateInitialAdministrator_Fails_WhenPasswordTooShort()
    {
        var authentication = Build(out _);

        var result = await authentication.CreateInitialAdministratorAsync("admin", "Administrador", "123");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Authenticate_Succeeds_AndSetsCurrentUser()
    {
        var authentication = Build(out var context);
        await authentication.CreateInitialAdministratorAsync("admin", "Administrador", "senhaForte1");

        var result = await authentication.AuthenticateAsync("admin", "senhaForte1");

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Administrator, context.Role);
        Assert.Equal("Administrador", context.UserName);
    }

    [Fact]
    public async Task Authenticate_Fails_WithWrongPassword()
    {
        var authentication = Build(out _);
        await authentication.CreateInitialAdministratorAsync("admin", "Administrador", "senhaForte1");

        var result = await authentication.AuthenticateAsync("admin", "errada");

        Assert.True(result.IsFailure);
    }
}
