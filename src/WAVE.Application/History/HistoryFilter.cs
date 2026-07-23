using WAVE.Domain.Testing;

namespace WAVE.Application.History;

/// <summary>
/// Criteria for narrowing the run history before display or export: an optional date
/// range (inclusive) and an optional SSID (case-insensitive, substring). Keeps the
/// filtering logic in one place so the on-screen list and the export stay consistent.
/// </summary>
public sealed record HistoryFilter(DateTimeOffset? From = null, DateTimeOffset? To = null, string? Ssid = null)
{
    /// <summary>An empty filter that matches every run.</summary>
    public static readonly HistoryFilter None = new();

    /// <summary>Whether any criterion is set.</summary>
    public bool IsActive =>
        From is not null || To is not null || !string.IsNullOrWhiteSpace(Ssid);

    /// <summary>Applies the criteria to a sequence of runs, preserving order.</summary>
    public IReadOnlyList<TestRun> Apply(IEnumerable<TestRun> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);
        return runs.Where(Matches).ToList();
    }

    /// <summary>Whether a single run satisfies every set criterion.</summary>
    public bool Matches(TestRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (From is { } from && run.StartedAt < from)
        {
            return false;
        }

        if (To is { } to && run.StartedAt > to)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Ssid) &&
            run.Ssid.IndexOf(Ssid.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        return true;
    }
}
