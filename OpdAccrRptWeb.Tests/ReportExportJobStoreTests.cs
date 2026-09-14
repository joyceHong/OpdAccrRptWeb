using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportExportJobStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "OpdAccrRptWeb.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void JobLifecycle_QueuedRunningReady_PublishesExpiryAndFile()
    {
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 8, 25, 6, 0, 0, TimeSpan.Zero));
        var store = CreateStore(time);
        var job = store.Create(Condition());

        store.MarkRunning(job.JobId);
        time.Advance(TimeSpan.FromMinutes(5));
        var completedPath = Path.Combine(_root, "C174_test.xlsx");
        File.WriteAllBytes(completedPath, [1, 2, 3]);
        store.MarkReady(job.JobId, "C174_test.xlsx");

        var actual = Assert.IsType<ReportExportJob>(store.Get(job.JobId));
        Assert.Equal(ReportExportJobStatus.Ready, actual.Status);
        Assert.Equal(time.GetUtcNow().AddHours(72), actual.ExpiresAt);
        Assert.Equal(completedPath, store.GetCompletedPath(actual));
    }

    [Fact]
    public void RecoverInterruptedJobs_MarksRunningFailedButKeepsReady()
    {
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var store = CreateStore(time);
        var running = store.Create(Condition());
        store.MarkRunning(running.JobId);
        File.WriteAllBytes(store.GetTemporaryPath(running.JobId), [1]);
        var ready = store.Create(Condition());
        store.MarkRunning(ready.JobId);
        File.WriteAllBytes(Path.Combine(_root, "ready.xlsx"), [1]);
        store.MarkReady(ready.JobId, "ready.xlsx");

        CreateStore(time).RecoverInterruptedJobs();

        Assert.Equal(ReportExportJobStatus.Failed, store.Get(running.JobId)!.Status);
        Assert.False(File.Exists(store.GetTemporaryPath(running.JobId)));
        Assert.Equal(ReportExportJobStatus.Ready, store.Get(ready.JobId)!.Status);
    }

    [Fact]
    public void Get_AtExactSeventyTwoHours_MarksJobExpired()
    {
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 8, 25, 6, 5, 0, TimeSpan.Zero));
        var store = CreateStore(time);
        var job = store.Create(Condition());
        store.MarkRunning(job.JobId);
        File.WriteAllBytes(Path.Combine(_root, "expires.xlsx"), [1]);
        store.MarkReady(job.JobId, "expires.xlsx");

        time.Advance(TimeSpan.FromHours(72));

        Assert.Equal(ReportExportJobStatus.Expired, store.Get(job.JobId)!.Status);
    }

    [Fact]
    public void CleanupExpired_DeleteTemporarilyFails_RetriesOnNextCycle()
    {
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var store = CreateStore(time);
        var job = store.Create(Condition());
        store.MarkRunning(job.JobId);
        var workbookPath = Path.Combine(_root, "locked.xlsx");
        File.WriteAllBytes(workbookPath, [1]);
        store.MarkReady(job.JobId, "locked.xlsx");
        time.Advance(TimeSpan.FromHours(72));

        using (File.Open(workbookPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            store.CleanupExpired();
            Assert.NotNull(store.Get(job.JobId));
        }

        store.CleanupExpired();
        Assert.Null(store.Get(job.JobId));
        Assert.False(File.Exists(workbookPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private ReportExportJobStore CreateStore(TimeProvider timeProvider) => new(
        Options.Create(new ReportExportOptions { RootDirectory = _root }),
        new FakeEnvironment { ContentRootPath = _root },
        timeProvider,
        NullLogger<ReportExportJobStore>.Instance);

    private static SearchReportCondition Condition() => new()
    {
        ReportCode = "C174",
        StartDate = "1150801",
        EndDate = "1150831"
    };

    private sealed class MutableTimeProvider(DateTimeOffset current) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan value) => current = current.Add(value);
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
