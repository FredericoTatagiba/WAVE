using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Networking;
using WAVE.Domain.Security;
using WAVE.Domain.Testing;

namespace WAVE.UnitTests.Fakes;

internal sealed class FakeAuthorizationService : IAuthorizationService
{
    private readonly bool _allow;

    public FakeAuthorizationService(bool allow) => _allow = allow;

    public bool HasPermission(Permission permission) => _allow;

    public Result Authorize(Permission permission) =>
        _allow ? Result.Success() : Result.Failure("Acesso negado.");
}

internal sealed class FakeCredentialStore : ICredentialStore
{
    private readonly WifiSecret? _secret;

    public FakeCredentialStore(WifiSecret? secret = null) => _secret = secret;

    public Task SaveAsync(string ssid, WifiSecret secret, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<WifiSecret?> GetAsync(string ssid, CancellationToken cancellationToken = default) =>
        Task.FromResult(_secret);

    public Task DeleteAsync(string ssid, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class FakeWifiConnector : IWifiConnector
{
    public Result EnsureResult { get; set; } = Result.Success();

    public Result ConnectResult { get; set; } = Result.Success();

    public bool Connected { get; private set; }

    public Task<Result> EnsureProfileAsync(
        WifiNetworkProfile profile, WifiSecret? secret, CancellationToken cancellationToken = default) =>
        Task.FromResult(EnsureResult);

    public Task<Result> ConnectAsync(string ssid, CancellationToken cancellationToken = default)
    {
        Connected = true;
        return Task.FromResult(ConnectResult);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal sealed class FakeWifiProfileCatalog : IWifiProfileCatalog
{
    private readonly bool _exists;

    public FakeWifiProfileCatalog(bool exists = false) => _exists = exists;

    public Task<IReadOnlyList<string>> GetSavedProfileNamesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<bool> ExistsAsync(string ssid, CancellationToken cancellationToken = default) =>
        Task.FromResult(_exists);
}

internal sealed class FakeDhcpValidator : IDhcpAddressValidator
{
    private readonly bool _hasLease;

    public FakeDhcpValidator(bool hasLease) => _hasLease = hasLease;

    public Task<bool> HasValidLeaseAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_hasLease);
}

internal sealed class FakeProcessTerminator : IProcessTerminator
{
    public int Calls { get; private set; }

    public int TerminateByNames(IReadOnlyCollection<string> processNames)
    {
        Calls++;
        return 0;
    }
}

internal sealed class FakeVisiblePingTerminal : IVisiblePingTerminal
{
    public string? LaunchedHost { get; private set; }

    public bool Closed { get; private set; }

    public void Launch(string host) => LaunchedHost = host;

    public void Close() => Closed = true;
}

internal sealed class FakePingMonitor : IContinuousPingMonitor
{
    public event EventHandler<PingSample>? Sampled;

    public bool IsRunning { get; private set; }

    public bool Started { get; private set; }

    public void Start(string host)
    {
        Started = true;
        IsRunning = true;
    }

    public Task StopAsync()
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public void Emit(PingSample sample) => Sampled?.Invoke(this, sample);
}

internal sealed class FakeBrowserLauncher : IPrivateBrowserLauncher
{
    public List<string> LaunchedUrls { get; } = new();

    public void Launch(string url) => LaunchedUrls.Add(url);
}

internal sealed class FakeTestRunRepository : ITestRunRepository
{
    public List<TestRun> Added { get; } = new();

    public Task AddAsync(TestRun run, CancellationToken cancellationToken = default)
    {
        Added.Add(run);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TestRun>> GetRecentAsync(int maxItems, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TestRun>>(Added.Take(maxItems).ToList());
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "hash:";

    public string Hash(string password) => Prefix + password;

    public bool Verify(string password, string hash) => hash == Prefix + password;
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<(UserAccount User, string Hash)> _users = new();

    public Task<bool> HasAnyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Count > 0);

    public Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(_users.Select(u => u.User).ToList());

    public Task<UserAccount?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users
            .Where(u => string.Equals(u.User.Username, username, StringComparison.OrdinalIgnoreCase))
            .Select(u => u.User)
            .FirstOrDefault());

    public Task<string?> GetPasswordHashAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Where(u => u.User.Id == userId).Select(u => u.Hash).FirstOrDefault());

    public Task UpsertAsync(UserAccount user, string passwordHash, CancellationToken cancellationToken = default)
    {
        _users.RemoveAll(u => u.User.Id == user.Id);
        _users.Add((user, passwordHash));
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _users.RemoveAll(u => u.User.Id == userId);
        return Task.CompletedTask;
    }
}

/// <summary>Relógio que avança um passo fixo a cada leitura (para testar timeouts).</summary>
internal sealed class AdvancingClock : IClock
{
    private readonly TimeSpan _step;
    private DateTimeOffset _now = DateTimeOffset.UnixEpoch;

    public AdvancingClock(TimeSpan step) => _step = step;

    public DateTimeOffset Now
    {
        get
        {
            var current = _now;
            _now = _now.Add(_step);
            return current;
        }
    }
}

internal sealed class NullLogger : IAppLogger
{
    public void Info(string message)
    {
    }

    public void Warn(string message)
    {
    }

    public void Error(string message, Exception? exception = null)
    {
    }
}
