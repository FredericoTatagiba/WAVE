using WAVE.Application.Abstractions;
using WAVE.Domain.Common;
using WAVE.Domain.Security;
using WAVE.Domain.Testing;

namespace WAVE.Application.History;

/// <summary>Consulta do histórico de execuções, protegida por <see cref="Permission.ViewHistory"/>.</summary>
public sealed class TestHistoryService
{
    private readonly ITestRunRepository _repository;
    private readonly IAuthorizationService _authorization;

    public TestHistoryService(ITestRunRepository repository, IAuthorizationService authorization)
    {
        _repository = repository;
        _authorization = authorization;
    }

    public async Task<Result<IReadOnlyList<TestRun>>> GetRecentAsync(
        int maxItems, CancellationToken cancellationToken = default)
    {
        var authorization = _authorization.Authorize(Permission.ViewHistory);
        if (authorization.IsFailure)
        {
            return Result<IReadOnlyList<TestRun>>.Failure(authorization.Error);
        }

        var items = await _repository.GetRecentAsync(maxItems, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<TestRun>>.Success(items);
    }
}
