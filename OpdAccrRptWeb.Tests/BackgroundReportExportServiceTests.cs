using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class BackgroundReportExportServiceTests
{
    [Fact]
    public void TryEnqueue_WhenBoundedQueueIsFull_RejectsWithoutDroppingAcceptedJob()
    {
        var queue = new ReportExportWorkQueue(Options.Create(new ReportExportOptions { QueueCapacity = 1 }));
        var first = CreateJob();
        var second = CreateJob();

        Assert.True(queue.TryEnqueue(first));
        Assert.False(queue.TryEnqueue(second));
    }

    [Fact]
    public async Task ExecuteAsync_AcceptedJobsRunSequentialLifecycleToReady()
    {
        var root = Path.Combine(Path.GetTempPath(), "OpdAccrRptWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var options = Options.Create(new ReportExportOptions { QueueCapacity = 2, CleanupIntervalMinutes = 60 });
            var queue = new ReportExportWorkQueue(options);
            var store = new RecordingStore(root);
            var service = new BackgroundReportExportService(
                options,
                queue,
                store,
                new RecordingWorkbookGenerator(),
                TimeProvider.System,
                NullLogger<BackgroundReportExportService>.Instance);
            var job = CreateJob();

            await service.StartAsync(CancellationToken.None);
            Assert.True(queue.TryEnqueue(job));
            await store.Ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await service.StopAsync(CancellationToken.None);

            Assert.Equal(["Recovered", "Running", "Ready"], store.Events);
            Assert.True(File.Exists(store.CompletedPath!));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_MiniExcelGenerator_WritesWorkbookToTemporaryFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "OpdAccrRptWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var options = Options.Create(new ReportExportOptions { QueueCapacity = 1, CleanupIntervalMinutes = 60 });
            var queue = new ReportExportWorkQueue(options);
            var store = new RecordingStore(root);
            var exportService = new ReportExportService(
                new FakeHealthCenterRepository(),
                store,
                queue,
                options,
                TimeProvider.System);
            var service = new BackgroundReportExportService(
                options,
                queue,
                store,
                exportService,
                TimeProvider.System,
                NullLogger<BackgroundReportExportService>.Instance);

            await service.StartAsync(CancellationToken.None);
            Assert.True(queue.TryEnqueue(CreateJob()));
            await store.Completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await service.StopAsync(CancellationToken.None);

            Assert.Equal(["Recovered", "Running", "Ready"], store.Events);
            Assert.True(File.Exists(store.CompletedPath!));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_FirstJobFails_ContinuesAndCompletesNextJob()
    {
        var root = Path.Combine(Path.GetTempPath(), "OpdAccrRptWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var options = Options.Create(new ReportExportOptions { QueueCapacity = 2, CleanupIntervalMinutes = 60 });
            var queue = new ReportExportWorkQueue(options);
            var store = new RecordingStore(root);
            var service = new BackgroundReportExportService(
                options, queue, store, new FailFirstWorkbookGenerator(), TimeProvider.System,
                NullLogger<BackgroundReportExportService>.Instance);

            await service.StartAsync(CancellationToken.None);
            Assert.True(queue.TryEnqueue(CreateJob()));
            Assert.True(queue.TryEnqueue(CreateJob()));
            await store.Ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await service.StopAsync(CancellationToken.None);

            Assert.Equal(2, store.Events.Count(value => value == "Running"));
            Assert.Contains("Failed", store.Events);
            Assert.Equal("Ready", store.Events[^1]);
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_PublishStateFails_RemovesPublishedFileAndCompletesNextJob()
    {
        var root = Path.Combine(Path.GetTempPath(), "OpdAccrRptWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var options = Options.Create(new ReportExportOptions { QueueCapacity = 2, CleanupIntervalMinutes = 60 });
            var queue = new ReportExportWorkQueue(options);
            var store = new RecordingStore(root) { FailFirstMarkReady = true };
            var service = new BackgroundReportExportService(
                options, queue, store, new RecordingWorkbookGenerator(), TimeProvider.System,
                NullLogger<BackgroundReportExportService>.Instance);

            await service.StartAsync(CancellationToken.None);
            Assert.True(queue.TryEnqueue(CreateJob()));
            Assert.True(queue.TryEnqueue(CreateJob()));
            await store.Ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await service.StopAsync(CancellationToken.None);

            Assert.Contains("Failed", store.Events);
            Assert.Equal("Ready", store.Events[^1]);
            Assert.Single(Directory.GetFiles(root, "*.xlsx"));
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_StopWhileQueueIsIdle_CompletesWithoutCancellationFailure()
    {
        var root = Path.Combine(Path.GetTempPath(), "OpdAccrRptWeb.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var options = Options.Create(new ReportExportOptions { QueueCapacity = 1, CleanupIntervalMinutes = 60 });
            var service = new BackgroundReportExportService(
                options,
                new ReportExportWorkQueue(options),
                new RecordingStore(root),
                new RecordingWorkbookGenerator(),
                TimeProvider.System,
                NullLogger<BackgroundReportExportService>.Instance);

            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static ReportExportJob CreateJob() => new()
    {
        JobId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        Status = ReportExportJobStatus.Queued,
        SearchCondition = new SearchReportCondition
        {
            ReportCode = "C174",
            StartDate = "1150801",
            EndDate = "1150831"
        }
    };

    private sealed class RecordingWorkbookGenerator : IReportWorkbookGenerator
    {
        public void GenerateWorkbook(SearchReportCondition searchCondition, Stream destination) =>
            destination.Write([1, 2, 3]);
    }

    private sealed class FailFirstWorkbookGenerator : IReportWorkbookGenerator
    {
        private int _calls;

        public void GenerateWorkbook(SearchReportCondition searchCondition, Stream destination)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                destination.Write([1, 2, 3]);
                throw new IOException("simulated write failure");
            }
            destination.Write([4, 5, 6]);
        }
    }

    private sealed class RecordingStore(string root) : IReportExportJobStore
    {
        private int _markReadyCalls;

        public List<string> Events { get; } = [];

        public bool FailFirstMarkReady { get; init; }

        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string? CompletedPath { get; private set; }

        public ReportExportJob Create(SearchReportCondition condition) => throw new NotSupportedException();
        public ReportExportJob? Get(Guid jobId) => throw new NotSupportedException();
        public string GetTemporaryPath(Guid jobId) => Path.Combine(root, $"{jobId:N}.tmp");
        public string GetCompletedPath(ReportExportJob job) => CompletedPath = Path.Combine(root, job.FileName!);
        public void MarkRunning(Guid jobId) => Events.Add("Running");
        public void MarkReady(Guid jobId, string fileName)
        {
            if (FailFirstMarkReady && Interlocked.Increment(ref _markReadyCalls) == 1)
            {
                throw new IOException("simulated state publish failure");
            }
            Events.Add("Ready");
            Ready.TrySetResult();
            Completed.TrySetResult();
        }
        public void MarkFailed(Guid jobId, string message)
        {
            Events.Add("Failed");
            Completed.TrySetResult();
        }
        public void Remove(Guid jobId) => throw new NotSupportedException();
        public void RecoverInterruptedJobs() => Events.Add("Recovered");
        public void CleanupExpired() => Events.Add("Cleanup");
    }
}
