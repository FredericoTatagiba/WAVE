using WAVE.Application.Profiles;
using WAVE.Domain.Networking;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Covers profile management and the line between the two ways a network gets persisted:
/// curating the catalog is an administrator action, while remembering a network the
/// operator just connected to is part of running a test and needs no password.
/// </summary>
public class NetworkProfileServiceTests
{
    private static WifiNetworkProfile ProtectedProfile() =>
        new("RedeCorp", "Rede Corp", SecurityType.Wpa2Personal);

    private static WifiNetworkProfile OpenProfile() =>
        new("RedeAberta", "Rede Aberta", SecurityType.Open);

    private static (NetworkProfileService Service, FakeNetworkProfileRepository Repo, FakeCredentialStore Store)
        Build(bool adminUnlocked)
    {
        var repo = new FakeNetworkProfileRepository();
        var store = new FakeCredentialStore();
        var service = new NetworkProfileService(repo, store, new FakeAdminSession(adminUnlocked));
        return (service, repo, store);
    }

    [Fact]
    public async Task RememberForTesting_WhileLocked_PersistsProfileAndCredential()
    {
        var (service, repo, store) = Build(adminUnlocked: false);
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
        var (service, repo, store) = Build(adminUnlocked: false);

        var result = await service.RememberForTestingAsync(ProtectedProfile(), secret: null);

        Assert.True(result.IsFailure);
        Assert.Empty(repo.Profiles);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public async Task RememberForTesting_OpenNetwork_PersistsProfileWithoutCredential()
    {
        var (service, repo, store) = Build(adminUnlocked: false);
        var profile = OpenProfile();

        var result = await service.RememberForTestingAsync(profile, secret: null);

        Assert.True(result.IsSuccess);
        Assert.Contains(repo.Profiles, p => p.Ssid == profile.Ssid);
        Assert.Empty(store.Saved);
    }

    [Fact]
    public async Task Save_WhileLocked_FailsButRememberStillWorks()
    {
        // Curating the catalog needs the administrator password; remembering the network
        // the operator just connected to is part of the test and must not.
        var (service, repo, _) = Build(adminUnlocked: false);
        var profile = ProtectedProfile();
        var secret = new WifiSecret("senha");

        var curate = await service.SaveAsync(profile, secret);
        var remember = await service.RememberForTestingAsync(profile, secret);

        Assert.True(curate.IsFailure);
        Assert.True(remember.IsSuccess);
        Assert.Contains(repo.Profiles, p => p.Ssid == profile.Ssid);
    }

    [Fact]
    public async Task Save_WhenUnlocked_CuratesCatalog()
    {
        var (service, repo, store) = Build(adminUnlocked: true);
        var profile = ProtectedProfile();

        var result = await service.SaveAsync(profile, new WifiSecret("senha"));

        Assert.True(result.IsSuccess);
        Assert.Contains(repo.Profiles, p => p.Ssid == profile.Ssid);
        Assert.True(store.Saved.ContainsKey(profile.Ssid));
    }

    [Fact]
    public async Task Delete_WhileLocked_IsRejected()
    {
        var (service, repo, _) = Build(adminUnlocked: false);
        await service.RememberForTestingAsync(OpenProfile(), secret: null);

        var result = await service.DeleteAsync("RedeAberta");

        Assert.True(result.IsFailure);
        Assert.Contains(repo.Profiles, p => p.Ssid == "RedeAberta");
    }
}
