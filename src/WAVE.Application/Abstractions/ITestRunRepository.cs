using WAVE.Domain.Testing;

namespace WAVE.Application.Abstractions;

/// <summary>History of test runs for auditing.</summary>
public interface ITestRunRepository
{
    Task AddAsync(TestRun run, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestRun>> GetRecentAsync(int maxItems, CancellationToken cancellationToken = default);
}
