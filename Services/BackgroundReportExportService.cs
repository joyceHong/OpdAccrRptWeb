using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Services;

internal interface IReportExportWorkQueue : IReportExportQueue
{
    IAsyncEnumerable<ReportExportJob> ReadAllAsync(CancellationToken cancellationToken);
}

internal sealed class ReportExportWorkQueue : IReportExportWorkQueue
{
    private readonly Channel<ReportExportJob> _channel;

    public ReportExportWorkQueue(IOptions<ReportExportOptions> options)
    {
        _channel = Channel.CreateBounded<ReportExportJob>(new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool TryEnqueue(ReportExportJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<ReportExportJob> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

internal sealed class BackgroundReportExportService : BackgroundService
{
    private readonly IReportExportWorkQueue _workQueue;
    private readonly IReportExportJobStore _jobStore;
    private readonly IReportWorkbookGenerator _workbookGenerator;
    private readonly ReportExportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BackgroundReportExportService> _logger;

    public BackgroundReportExportService(
        IOptions<ReportExportOptions> options,
        IReportExportWorkQueue workQueue,
        IReportExportJobStore jobStore,
        IReportWorkbookGenerator workbookGenerator,
        TimeProvider timeProvider,
        ILogger<BackgroundReportExportService> logger)
    {
        _options = options.Value;
        _workQueue = workQueue;
        _jobStore = jobStore;
        _workbookGenerator = workbookGenerator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _jobStore.RecoverInterruptedJobs();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var cleanupTimer = new PeriodicTimer(
            TimeSpan.FromMinutes(_options.CleanupIntervalMinutes),
            _timeProvider);
        var processTask = ProcessQueue(stoppingToken);
        var cleanupTask = RunCleanup(cleanupTimer, stoppingToken);
        await Task.WhenAll(processTask, cleanupTask);
    }

    private async Task ProcessQueue(CancellationToken stoppingToken)
    {
        await foreach (var job in _workQueue.ReadAllAsync(stoppingToken))
        {
            var temporaryPath = _jobStore.GetTemporaryPath(job.JobId);
            try
            {
                _jobStore.MarkRunning(job.JobId);
                await using (var output = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 65_536,
                    useAsync: true))
                {
                    _workbookGenerator.GenerateWorkbook(job.SearchCondition, output);
                    await output.FlushAsync(stoppingToken);
                }

                var fileName = $"C174_{_timeProvider.GetUtcNow():yyyyMMdd_HHmmss}_{job.JobId:N}.xlsx";
                var completedJob = new ReportExportJob
                {
                    JobId = job.JobId,
                    SearchCondition = job.SearchCondition,
                    CreatedAt = job.CreatedAt,
                    Status = ReportExportJobStatus.Running,
                    FileName = fileName
                };
                var completedPath = _jobStore.GetCompletedPath(completedJob);
                File.Move(temporaryPath, completedPath, true);
                _jobStore.MarkReady(job.JobId, fileName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                TryMarkFailed(job.JobId, "網站已停止，請重新申請匯出。");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "背景報表匯出失敗。JobId={JobId}, ReportCode={ReportCode}", job.JobId, job.SearchCondition.ReportCode);
                TryMarkFailed(job.JobId, "報表匯出失敗，請稍後重新申請。");
            }
        }
    }

    private async Task RunCleanup(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _jobStore.CleanupExpired();
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "報表匯出定期清理失敗，下一週期將重試。");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private void TryMarkFailed(Guid jobId, string message)
    {
        try
        {
            _jobStore.MarkFailed(jobId, message);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "無法更新失敗的報表匯出工作。JobId={JobId}", jobId);
        }
    }
}
