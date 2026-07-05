using WAVE.Application.Security;
using WAVE.Application.Users;
using WAVE.Domain.Security;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

public class UserManagementServiceTests
{
    private static UserManagementService Build(out FakeUserRepository repository, out CurrentUserContext context)
    {
        repository = new FakeUserRepository();
        context = new CurrentUserContext();
        var authorization = new AuthorizationService(context);
        return new UserManagementService(repository, new FakePasswordHasher(), authorization);
    }

    [Fact]
    public async Task Operator_CannotCreateUsers()
    {
        var service = Build(out _, out _); // contexto inicia como Operador

        var result = await service.CreateAsync("tech", "Técnico", UserRole.Operator, "senhaForte1");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Administrator_CanCreateUser()
    {
        var service = Build(out var repository, out var context);
        context.Set(UserRole.Administrator, "Admin");

        var result = await service.CreateAsync("tech", "Técnico", UserRole.Operator, "senhaForte1");

        Assert.True(result.IsSuccess);
        Assert.Single(await repository.GetAllAsync());
    }

    [Fact]
    public async Task CannotDeleteLastAdministrator()
    {
        var service = Build(out var repository, out var context);
        context.Set(UserRole.Administrator, "Admin");
        var admin = new UserAccount(Guid.NewGuid(), "admin", "Admin", UserRole.Administrator);
        await repository.UpsertAsync(admin, "hash:qualquer");

        var result = await service.DeleteAsync(admin.Id);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CannotDemoteLastAdministrator()
    {
        var service = Build(out var repository, out var context);
        context.Set(UserRole.Administrator, "Admin");
        var admin = new UserAccount(Guid.NewGuid(), "admin", "Admin", UserRole.Administrator);
        await repository.UpsertAsync(admin, "hash:qualquer");

        var result = await service.ChangeRoleAsync(admin.Id, UserRole.Operator);

        Assert.True(result.IsFailure);
    }
}
