using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class SapInterfaceServiceTests
{
    [Fact]
    public async Task RunAsync_NoDataResultIsReportedAndLaterSelectedEventContinues()
    {
        var repository = new FakeRepository
        {
            Results =
            {
                ["SAPCASH"] = new(SapInterfaceRepositoryRunStatus.NoData, 0),
                ["SAPCONS"] = new(SapInterfaceRepositoryRunStatus.Completed, 3)
            }
        };
        var service = new SapInterfaceService(repository, NullLogger<SapInterfaceService>.Instance);

        SapInterfaceRunResult result = await service.RunAsync(new SapInterfaceRequest
        {
            BusinessDate = "2026-10-06",
            RunCash = true,
            RunContract = true
        }, CancellationToken.None);

        Assert.Equal(new[] { "SAPCASH", "SAPCONS" }, repository.RunEvents);
        Assert.Equal(new[] { "noData", "completed" }, result.Events.Select(item => item.Status));
        Assert.Empty(result.ConfirmationRequired);

        IReadOnlyList<SapInterfaceEventStatus> statuses = await service.GetStatusAsync(
            "2026-10-06", CancellationToken.None);
        Assert.False(statuses.Single(item => item.Event == "SAPCASH").Completed);
    }

    [Fact]
    public async Task RunAsync_ConfirmedEmptyRerunKeepsPriorCompletionStatus()
    {
        var repository = new FakeRepository
        {
            Results = { ["SAPCASH"] = new(SapInterfaceRepositoryRunStatus.NoData, 0) },
            Completed = { ["SAPCASH"] = true }
        };
        var service = new SapInterfaceService(repository, NullLogger<SapInterfaceService>.Instance);

        SapInterfaceRunResult result = await service.RunAsync(new SapInterfaceRequest
        {
            BusinessDate = "2026-10-06",
            RunCash = true,
            ConfirmRerun = ["SAPCASH"]
        }, CancellationToken.None);

        Assert.Equal("noData", Assert.Single(result.Events).Status);
        IReadOnlyList<SapInterfaceEventStatus> statuses = await service.GetStatusAsync(
            "2026-10-06", CancellationToken.None);
        Assert.True(statuses.Single(item => item.Event == "SAPCASH").Completed);
    }

    [Fact]
    public async Task RunAsync_WhenEventThrows_ReportsFailureAndContinuesLaterEvents()
    {
        var repository = new FakeRepository
        {
            Failures = { ["SAPCASH"] = new InvalidOperationException("ORA-01031") },
            Results = { ["SAPCONS"] = new(SapInterfaceRepositoryRunStatus.Completed, 1) }
        };
        var service = new SapInterfaceService(repository, NullLogger<SapInterfaceService>.Instance);

        SapInterfaceRunResult result = await service.RunAsync(new SapInterfaceRequest
        {
            BusinessDate = "2026-10-06",
            RunCash = true,
            RunContract = true
        }, CancellationToken.None);

        Assert.Equal(new[] { "SAPCASH", "SAPCONS" }, repository.RunEvents);
        Assert.Equal(new[] { "failed", "completed" }, result.Events.Select(item => item.Status));
        Assert.Equal("執行失敗，該項資料已回復。", result.Events[0].Message);
    }

    private sealed class FakeRepository : ISapInterfaceRepository
    {
        public Dictionary<string, SapInterfaceRepositoryRunResult> Results { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, Exception> Failures { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, bool> Completed { get; } = new(StringComparer.Ordinal);
        public List<string> RunEvents { get; } = [];

        public Task<string?> GetDefaultRocDateAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);

        public Task<IReadOnlyDictionary<string, bool>> GetCompletedAsync(string rocDate,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                new Dictionary<string, bool>(Completed, StringComparer.Ordinal));

        public Task<SapInterfaceRepositoryRunResult> RunEventAsync(string eventCode, string rocDate,
            string gregorianDate, bool confirmRerun, CancellationToken cancellationToken)
        {
            RunEvents.Add(eventCode);
            if (Failures.TryGetValue(eventCode, out Exception? exception))
                throw exception;
            return Task.FromResult(Results.GetValueOrDefault(eventCode)
                ?? new SapInterfaceRepositoryRunResult(SapInterfaceRepositoryRunStatus.Completed, 1));
        }
    }
}
