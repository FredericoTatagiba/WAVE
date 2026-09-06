using WAVE.Application.Configuration;
using WAVE.Application.Security;
using WAVE.Infrastructure.Security;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Covers the single administrator password that replaced the sign-in screen: it is
/// created on first use, unlocks for the session, and never reaches the settings in clear.
/// </summary>
public class AdminSessionTests
{
    private const string ValidPassword = "senha-forte";

    private static (AdminSession Session, FakeSettingsStore Settings) Build(WaveSettings? initial = null)
    {
        var settings = new FakeSettingsStore(initial);
        return (new AdminSession(settings, new FakePasswordHasher()), settings);
    }

    [Fact]
    public void FreshDevice_IsNeitherConfiguredNorUnlocked()
    {
        var (session, _) = Build();

        Assert.False(session.IsConfigured);
        Assert.False(session.IsUnlocked);
        Assert.True(session.RequireUnlocked().IsFailure);
    }

    [Fact]
    public async Task Configure_StoresHashAndUnlocks()
    {
        var (session, settings) = Build();

        var result = await session.ConfigureAsync(ValidPassword);

        Assert.True(result.IsSuccess);
        Assert.True(session.IsConfigured);
        Assert.True(session.IsUnlocked);
        Assert.True(session.RequireUnlocked().IsSuccess);
        Assert.NotNull(settings.Current.AdminPasswordHash);
    }

    [Fact]
    public async Task Configure_WithRealHasher_NeverStoresThePasswordInClear()
    {
        // Uses the production hasher on purpose: the fake is a reversible prefix, so it
        // could not catch the mistake this test exists to catch.
        var settings = new FakeSettingsStore();
        var session = new AdminSession(settings, new Pbkdf2PasswordHasher());

        await session.ConfigureAsync(ValidPassword);

        var stored = settings.Current.AdminPasswordHash;
        Assert.NotNull(stored);
        Assert.DoesNotContain(ValidPassword, stored!, StringComparison.OrdinalIgnoreCase);

        session.Lock();
        Assert.True((await session.UnlockAsync(ValidPassword)).IsSuccess);
    }

    [Fact]
    public async Task Configure_ShortPassword_IsRejected()
    {
        var (session, settings) = Build();

        var result = await session.ConfigureAsync("curta");

        Assert.True(result.IsFailure);
        Assert.False(session.IsConfigured);
        Assert.Null(settings.Current.AdminPasswordHash);
    }

    [Fact]
    public async Task Configure_WhenAlreadySet_IsRejected()
    {
        // Otherwise anyone could silently replace the password by "configuring" it again.
        var (session, _) = Build();
        await session.ConfigureAsync(ValidPassword);
        session.Lock();

        var result = await session.ConfigureAsync("outra-senha");

        Assert.True(result.IsFailure);
        Assert.False(session.IsUnlocked);
    }

    [Fact]
    public async Task Unlock_WithWrongPassword_StaysLocked()
    {
        var (session, _) = Build();
        await session.ConfigureAsync(ValidPassword);
        session.Lock();

        var result = await session.UnlockAsync("errada");

        Assert.True(result.IsFailure);
        Assert.False(session.IsUnlocked);
    }

    [Fact]
    public async Task Unlock_WithCorrectPassword_Unlocks()
    {
        var (session, _) = Build();
        await session.ConfigureAsync(ValidPassword);
        session.Lock();

        var result = await session.UnlockAsync(ValidPassword);

        Assert.True(result.IsSuccess);
        Assert.True(session.IsUnlocked);
    }

    [Fact]
    public async Task ChangePassword_RequiresTheCurrentOne()
    {
        var (session, settings) = Build();
        await session.ConfigureAsync(ValidPassword);
        var originalHash = settings.Current.AdminPasswordHash;

        var refused = await session.ChangePasswordAsync("errada", "nova-senha-ok");

        Assert.True(refused.IsFailure);
        Assert.Equal(originalHash, settings.Current.AdminPasswordHash);

        var accepted = await session.ChangePasswordAsync(ValidPassword, "nova-senha-ok");

        Assert.True(accepted.IsSuccess);
        Assert.NotEqual(originalHash, settings.Current.AdminPasswordHash);
        Assert.True((await session.UnlockAsync("nova-senha-ok")).IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_ShortNewPassword_KeepsTheOldOne()
    {
        var (session, settings) = Build();
        await session.ConfigureAsync(ValidPassword);
        var originalHash = settings.Current.AdminPasswordHash;

        var result = await session.ChangePasswordAsync(ValidPassword, "curta");

        Assert.True(result.IsFailure);
        Assert.Equal(originalHash, settings.Current.AdminPasswordHash);
    }
}
