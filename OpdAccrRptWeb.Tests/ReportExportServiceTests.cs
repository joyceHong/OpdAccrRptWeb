using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportExportServiceTests
{
    [Fact]
    public void GenerateWorkbook_OneC174Row_CreatesReadableHeaderAndData()
    {
        var repository = new FakeHealthCenterRepository
        {
            C174ExportData =
            [
                new HealthCenterContractBillingReport
                {
                    BillingCode = "B01",
                    BillingName = "合約記帳",
                    TotalAmount = 123.45m
                }
            ]
        };
        var service = CreateService(repository, new FakeJobStore(), new FakeExportQueue());

        using var output = new MemoryStream();
        service.GenerateWorkbook(RocCondition(), output);

        output.Position = 0;
        using var document = SpreadsheetDocument.Open(output, false);
        var rows = document.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal("記帳代碼", rows[0].Descendants<DocumentFormat.OpenXml.Spreadsheet.Text>().First().Text);
        Assert.Equal("B01", rows[1].Descendants<DocumentFormat.OpenXml.Spreadsheet.Text>().First().Text);
    }

    [Fact]
    public void GenerateWorkbook_SixtyOneRows_UsesBatchesAndExportsEveryRow()
    {
        var repository = new FakeHealthCenterRepository
        {
            C174ExportData = Enumerable.Range(1, 61)
                .Select(index => new HealthCenterContractBillingReport { BillingCode = $"B{index:000}" })
                .ToList()
        };
        var service = CreateService(
            repository,
            new FakeJobStore(),
            new FakeExportQueue(),
            new ReportExportOptions { BatchSize = 30 });

        using var output = new MemoryStream();
        service.GenerateWorkbook(RocCondition(), output);

        output.Position = 0;
        using var document = SpreadsheetDocument.Open(output, false);
        var rows = document.WorkbookPart!.WorksheetParts.Single().Worksheet.Descendants<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        Assert.Equal(62, rows.Count);
        Assert.Equal([(0, 30), (30, 30), (60, 30)], repository.C174BatchCalls);
        Assert.Equal("B001", rows[1].Descendants<DocumentFormat.OpenXml.Spreadsheet.Text>().First().Text);
        Assert.Equal("B061", rows[^1].Descendants<DocumentFormat.OpenXml.Spreadsheet.Text>().First().Text);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(30_000, false)]
    [InlineData(30_001, true)]
    public void Dispatch_ThresholdBoundary_SelectsExpectedMode(int totalCount, bool background)
    {
        var repository = new FakeHealthCenterRepository { C174TotalCount = totalCount };
        var store = new FakeJobStore();
        var queue = new FakeExportQueue();
        var service = CreateService(repository, store, queue);

        var result = service.Dispatch(WebCondition());

        Assert.Equal(background, result.Job is not null);
        Assert.Equal(background, result.Workbook is null);
        Assert.Equal(background, queue.Calls == 1);
    }

    [Fact]
    public void Dispatch_FullQueue_RemovesCreatedJob()
    {
        var repository = new FakeHealthCenterRepository { C174TotalCount = 30_001 };
        var store = new FakeJobStore();
        var service = CreateService(repository, store, new FakeExportQueue { Accept = false });

        var result = service.Dispatch(WebCondition());

        Assert.True(result.QueueFull);
        Assert.Equal(store.Created!.JobId, store.RemovedJobId);
    }

    private static ReportExportService CreateService(
        FakeHealthCenterRepository repository,
        IReportExportJobStore store,
        IReportExportQueue queue,
        ReportExportOptions? options = null) =>
        new(repository, store, queue, Options.Create(options ?? new ReportExportOptions()), TimeProvider.System);

    private static SearchReportCondition WebCondition() => new()
    {
        ReportCode = "C174",
        StartDate = "2026-08-01",
        EndDate = "2026-08-31"
    };

    private static SearchReportCondition RocCondition() => new()
    {
        ReportCode = "C174",
        StartDate = "1150801",
        EndDate = "1150831"
    };

    private sealed class FakeExportQueue : IReportExportQueue
    {
        public bool Accept { get; init; } = true;

        public int Calls { get; private set; }

        public bool TryEnqueue(ReportExportJob job)
        {
            Calls++;
            return Accept;
        }
    }

    private sealed class FakeJobStore : IReportExportJobStore
    {
        public ReportExportJob? Created { get; private set; }

        public Guid? RemovedJobId { get; private set; }

        public ReportExportJob Create(SearchReportCondition condition) => Created = new ReportExportJob
        {
            JobId = Guid.NewGuid(),
            SearchCondition = condition,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = ReportExportJobStatus.Queued
        };

        public ReportExportJob? Get(Guid jobId) => Created?.JobId == jobId ? Created : null;
        public string GetTemporaryPath(Guid jobId) => throw new NotSupportedException();
        public string GetCompletedPath(ReportExportJob job) => throw new NotSupportedException();
        public void MarkRunning(Guid jobId) => throw new NotSupportedException();
        public void MarkReady(Guid jobId, string fileName) => throw new NotSupportedException();
        public void MarkFailed(Guid jobId, string message) => throw new NotSupportedException();
        public void Remove(Guid jobId) => RemovedJobId = jobId;
        public void RecoverInterruptedJobs() => throw new NotSupportedException();
        public void CleanupExpired() => throw new NotSupportedException();
    }
}
