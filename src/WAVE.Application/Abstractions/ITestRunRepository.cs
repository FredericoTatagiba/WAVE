using WAVE.Domain.Testing;

namespace WAVE.Application.Abstractions;

/// <summary>Histórico de execuções de teste para auditoria.</summary>
public interface ITestRunRepository
{
    Task AddAsync(TestRun run, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestRun>> GetRecentAsync(int maxItems, CancellationToken cancellationToken = default);
}
