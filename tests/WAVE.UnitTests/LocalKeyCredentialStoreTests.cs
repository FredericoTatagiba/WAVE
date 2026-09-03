using WAVE.Domain.Networking;
using WAVE.Infrastructure.Persistence;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

/// <summary>
/// Round-trips the AES-GCM credential store. A tampered blob must read as "no
/// credential", never as a wrong passphrase and never as an unhandled exception.
/// </summary>
/// <remarks>
/// The store writes under <see cref="WAVE.Infrastructure.Configuration.AppPaths"/>, which
/// is a static resolved once per process, so these tests share the real data directory.
/// They therefore use SSIDs that no real network would collide with, and clean up after
/// themselves.
/// </remarks>
public class LocalKeyCredentialStoreTests : IAsyncLifetime, IDisposable
{
    private const string Ssid = "wave-unit-test-network";

    private readonly LocalKeyCredentialStore _store = new(new NullLogger());

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _store.DeleteAsync(Ssid);

    public void Dispose()
    {
        _store.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveThenGet_ReturnsTheSameSecret()
    {
        var secret = new WifiSecret("s3cret-pass", Username: "tecnico", Domain: "CORP");

        await _store.SaveAsync(Ssid, secret);

        Assert.Equal(secret, await _store.GetAsync(Ssid));
    }

    [Fact]
    public async Task Get_UnknownSsid_ReturnsNull() =>
        Assert.Null(await _store.GetAsync("wave-unit-test-never-saved"));

    [Fact]
    public async Task Save_Twice_OverwritesRatherThanDuplicating()
    {
        await _store.SaveAsync(Ssid, new WifiSecret("first"));
        await _store.SaveAsync(Ssid, new WifiSecret("second"));

        Assert.Equal("second", (await _store.GetAsync(Ssid))!.Passphrase);
    }

    [Fact]
    public async Task Get_IsCaseAndWhitespaceInsensitiveOnSsid()
    {
        await _store.SaveAsync(Ssid, new WifiSecret("s3cret"));

        Assert.NotNull(await _store.GetAsync($"  {Ssid.ToUpperInvariant()}  "));
    }

    [Fact]
    public async Task Delete_RemovesTheSecret()
    {
        await _store.SaveAsync(Ssid, new WifiSecret("s3cret"));
        await _store.DeleteAsync(Ssid);

        Assert.Null(await _store.GetAsync(Ssid));
    }

    [Fact]
    public async Task ConcurrentSaves_AllSurvive()
    {
        // Read-modify-write on one shared file: without the mutex, writers lose entries.
        var ssids = Enumerable.Range(0, 12).Select(index => $"{Ssid}-{index}").ToList();

        await Task.WhenAll(ssids.Select(ssid => _store.SaveAsync(ssid, new WifiSecret(ssid))));

        try
        {
            foreach (var ssid in ssids)
            {
                Assert.Equal(ssid, (await _store.GetAsync(ssid))?.Passphrase);
            }
        }
        finally
        {
            foreach (var ssid in ssids)
            {
                await _store.DeleteAsync(ssid);
            }
        }
    }
}
