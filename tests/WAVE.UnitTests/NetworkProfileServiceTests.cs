using WAVE.Application.Profiles;
using WAVE.Application.Security;
using WAVE.Domain.Networking;
using WAVE.Domain.Security;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Covers profile management, focused on the "remember the network on selection"
/// improvement: <see cref="NetworkProfileService.RememberForTestingAsync"/> must
/// persist profile + credential for future tests, under operator RBAC.
/// </summary>
public class NetworkProfileServiceTests
{
    private static WifiNetworkProfile ProtectedProfile() =>
        new("RedeCorp", "Rede Corp", SecurityType.Wpa2Personal);

    private static WifiNetworkProfile OpenProfile() =>
        new("RedeAberta", "Rede Aberta", SecurityType.Open);

    private static (NetworkProfileService Service, FakeNetworkProfileRepository Repo, FakeCredentialStore Store)
        BuildFor(UserRole role)
    {
        var context = new CurrentUserContext();
        if (role == UserRole.Administrator)
        {
            context.Set(UserRole.Administrator, "Admin");
        }

        var repo = new FakeNetworkProfileRepository();
        var store = new FakeCredentialStore();
        var service = new NetworkProfileService(repo, store, new AuthorizationService(context));
        return (service, repo, store);
    }

    [Fact]
    public async Task RememberForTesting_AsOperator_PersistsProfileAndCredential()
    {
        var (service, repo, store) = BuildFor(UserRole.Operator);
        var profile = ProtectedProfile();
        var secret = new WifiSecret("senha-super");

        var result = await service.RememberForTestingAsync(profile, secret);

        Assert.True(result.IsSuccess);
        Assert.Contains(repo.Profiles, p => p.Ssid == profile.Ssid);
        Assert.True(store.Saved.ContainsKey(profile.Ssid));
        Assert.Equal("senha-super", store.Saved[profile.Ssid].Passphrase);
    }

    [Fact]
    public async Task RememberForTesting_ProtectedWithoutSecret_FailsAndPersistsNothing()
    {
        var (service, repo, store) = BuildFor(UserRole.Operator);

        var result = await service.RememberForTestingAsync(ProtectedProfile(), secret: null);

        Assert.True(result.IsFailure);
        Assert.Empty(repo.Profiles);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public async Task RememberForTesting_OpenNetwork_PersistsProfileWithoutCredential()
    {
        var (service, repo, store) = BuildFor(UserRole.Operator);
        var profile = OpenProfile();

        var result = await service.RememberForTestingAsync(profile, secret: null);

        Assert.True(result.IsSuccess);
        Assert.Contains(repo.Profiles, p => p.Ssid == profile.Ssid);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public async Task RememberForTesting_WhenUnauthorized_FailsAndPersistsNothing()
    {
        var repo = new FakeNetworkProfileRepository();
        var store = new FakeCredentialStore();
        var service = new NetworkProfileService(repo, store, new FakeAuthorizationService(allow: false));

        var result = await service.RememberForTestingAsync(ProtectedProfile(), new WifiSecret("x"));

        Assert.True(result.IsFailure);
        Assert.Empty(repo.Profiles);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public async Task Operator_CannotCurateCatalog_ButCanRememberNetwork()
    {
        // SaveAsync curates the catalog (ManageProfiles: admin). The operator cannot,
        // but CAN remember a just-selected network (RunTest).
        var (service, repo, _) = BuildFor(UserRole.Operator);
        var profile = ProtectedProfile();
        var secret = new WifiSecret("senha");

        var curate = await service.SaveAsync(profile, secret);
        var remember = await service.RememberForTestingAsync(profile, secret);

        Assert.True(curate.IsFailure);
        Assert.True(remember.IsSuccess);
        Assert.Contains(repo.Profiles, p => p.Ssid == profile.Ssid);
    }

    [Fact]
    public async Task Administrator_CanCurateCatalog()
    {
        var (service, repo, store) = BuildFor(UserRole.Administrator);
        var profile = ProtectedProfile();

        var result = await service.SaveAsync(profile, new WifiSecret("senha"));

        Assert.True(result.IsSuccess);
        Assert.Contains(repo.Profiles, p => p.Ssid == profile.Ssid);
        Assert.True(store.Saved.ContainsKey(profile.Ssid));
    }
}
