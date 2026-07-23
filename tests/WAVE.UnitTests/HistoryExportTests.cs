using WAVE.Application.Abstractions;
using WAVE.Application.History;
using WAVE.Domain.Testing;
using WAVE.UnitTests.Fakes;
using Xunit;

namespace WAVE.UnitTests;

public class HistoryFilterTests
{
    private static TestRun Run(string ssid, DateTimeOffset startedAt) => new()
    {
        Id = Guid.NewGuid(),
        Ssid = ssid,
        OperatorName = "op",
        StartedAt = startedAt
    };

    [Fact]
    public void Apply_NoCriteria_ReturnsAll()
    {
        var runs = new[] { Run("A", DateTimeOffset.UnixEpoch), Run("B", DateTimeOffset.UnixEpoch) };

        Assert.Equal(2, HistoryFilter.None.Apply(runs).Count);
        Assert.False(HistoryFilter.None.IsActive);
    }

    [Fact]
    public void Apply_FiltersBySsid_CaseInsensitiveSubstring()
    {
        var runs = new[]
        {
            Run("Corp-Guest", DateTimeOffset.UnixEpoch),
            Run("Home", DateTimeOffset.UnixEpoch),
            Run("corp-main", DateTimeOffset.UnixEpoch)
        };

        var filtered = new HistoryFilter(Ssid: "corp").Apply(runs);

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, run => Assert.Contains("corp", run.Ssid, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Apply_FiltersByDateRange_Inclusive()
    {
        var day = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var runs = new[]
        {
            Run("before", day.AddDays(-1)),
            Run("inside", day.AddHours(12)),
            Run("after", day.AddDays(2))
        };

        var filter = new HistoryFilter(From: day, To: day.AddDays(1).AddTicks(-1));
        var filtered = filter.Apply(runs);

        Assert.Single(filtered);
        Assert.Equal("inside", filtered[0].Ssid);
        Assert.True(filter.IsActive);
    }
}

public class HistoryExportServiceTests
{
    private sealed class FakeHistoryExporter : IHistoryExporter
    {
        public FakeHistoryExporter(ExportFormat format) => Format = format;

        public ExportFormat Format { get; }

        public string FileExtension => Format.ToString().ToLowerInvariant();

        public string DisplayName => Format.ToString();

        public IReadOnlyList<TestRun>? Received { get; private set; }

        public Task ExportAsync(
            IReadOnlyList<TestRun> runs, Stream output, CancellationToken cancellationToken = default)
        {
            Received = runs;
            return Task.CompletedTask;
        }
    }

    private static TestRun Run(string ssid) => new()
    {
        Id = Guid.NewGuid(),
        Ssid = ssid,
        OperatorName = "op",
        StartedAt = DateTimeOffset.UnixEpoch
    };

    private static (HistoryExportService service, FakeHistoryExporter csv, FakeTestRunRepository repo) Build(
        bool allow = true)
    {
        var repo = new FakeTestRunRepository();
        var history = new TestHistoryService(repo, new FakeAuthorizationService(allow));
        var csv = new FakeHistoryExporter(ExportFormat.Csv);
        var service = new HistoryExportService(history, new IHistoryExporter[] { csv });
        return (service, csv, repo);
    }

    [Fact]
    public async Task ExportAsync_WhenNotAuthorized_Fails()
    {
        var (service, csv, repo) = Build(allow: false);
        repo.Added.Add(Run("A"));

        var result = await service.ExportAsync(HistoryFilter.None, ExportFormat.Csv, new MemoryStream());

        Assert.True(result.IsFailure);
        Assert.Null(csv.Received);
    }

    [Fact]
    public async Task ExportAsync_UnsupportedFormat_Fails()
    {
        var (service, _, repo) = Build();
        repo.Added.Add(Run("A"));

        var result = await service.ExportAsync(HistoryFilter.None, ExportFormat.Pdf, new MemoryStream());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ExportAsync_AppliesFilterAndDelegatesToExporter()
    {
        var (service, csv, repo) = Build();
        repo.Added.Add(Run("Corp"));
        repo.Added.Add(Run("Home"));

        var result = await service.ExportAsync(new HistoryFilter(Ssid: "corp"), ExportFormat.Csv, new MemoryStream());

        Assert.True(result.IsSuccess);
        Assert.NotNull(csv.Received);
        Assert.Single(csv.Received!);
        Assert.Equal("Corp", csv.Received![0].Ssid);
    }

    [Fact]
    public void AvailableExporters_AreOrderedByFormat()
    {
        var repo = new FakeTestRunRepository();
        var history = new TestHistoryService(repo, new FakeAuthorizationService(allow: true));
        var pdf = new FakeHistoryExporter(ExportFormat.Pdf);
        var csv = new FakeHistoryExporter(ExportFormat.Csv);
        var service = new HistoryExportService(history, new IHistoryExporter[] { pdf, csv });

        var formats = service.AvailableExporters.Select(exporter => exporter.Format).ToArray();

        Assert.Equal(new[] { ExportFormat.Csv, ExportFormat.Pdf }, formats);
    }
}
