using System.Text.Json;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public enum ReportExportJobStatus
{
    Queued,
    Running,
    Ready,
    Failed,
    Expired
}

public sealed class ReportExportJob
{
    public required Guid JobId { get; init; }

    public required SearchReportCondition SearchCondition { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public ReportExportJobStatus Status { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? Message { get; set; }

    public string? FileName { get; set; }
}

public interface IReportExportJobStore
{
    ReportExportJob Create(SearchReportCondition condition);

    ReportExportJob? Get(Guid jobId);

    string GetTemporaryPath(Guid jobId);

    string GetCompletedPath(ReportExportJob job);

    void MarkRunning(Guid jobId);

    void MarkReady(Guid jobId, string fileName);

    void MarkFailed(Guid jobId, string message);

    void Remove(Guid jobId);

    void RecoverInterruptedJobs();

    void CleanupExpired();
}

public sealed class ReportExportJobStore : IReportExportJobStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _gate = new();
    private readonly string _rootDirectory;
    private readonly TimeSpan _retention;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportExportJobStore> _logger;

    public ReportExportJobStore(
        IOptions<ReportExportOptions> options,
        IWebHostEnvironment environment,
        TimeProvider timeProvider,
        ILogger<ReportExportJobStore> logger)
    {
        var configuredRoot = options.Value.RootDirectory;
        _rootDirectory = Path.GetFullPath(
            Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(environment.ContentRootPath, configuredRoot));
        _retention = TimeSpan.FromHours(options.Value.RetentionHours);
        _timeProvider = timeProvider;
        _logger = logger;
        Directory.CreateDirectory(_rootDirectory);
    }

    public ReportExportJob Create(SearchReportCondition condition)
    {
        var job = new ReportExportJob
        {
            JobId = Guid.NewGuid(),
            SearchCondition = CloneCondition(condition),
            CreatedAt = _timeProvider.GetUtcNow(),
            Status = ReportExportJobStatus.Queued
        };
        lock (_gate)
        {
            Save(job);
        }
        return job;
    }

    public ReportExportJob? Get(Guid jobId)
    {
        lock (_gate)
        {
            var job = Load(jobId);
            if (job?.Status == ReportExportJobStatus.Ready
                && job.ExpiresAt <= _timeProvider.GetUtcNow())
            {
                job.Status = ReportExportJobStatus.Expired;
                Save(job);
            }
            return job;
        }
    }

    public string GetTemporaryPath(Guid jobId) => SafePath($"{jobId:N}.tmp");

    public string GetCompletedPath(ReportExportJob job)
    {
        if (string.IsNullOrWhiteSpace(job.FileName))
        {
            throw new InvalidOperationException("匯出工作尚未產生檔案。");
        }
        return SafePath(job.FileName);
    }

    public void MarkRunning(Guid jobId) => Update(jobId, job =>
    {
        EnsureStatus(job, ReportExportJobStatus.Queued);
        job.Status = ReportExportJobStatus.Running;
        job.StartedAt = _timeProvider.GetUtcNow();
    });

    public void MarkReady(Guid jobId, string fileName) => Update(jobId, job =>
    {
        EnsureStatus(job, ReportExportJobStatus.Running);
        var completedAt = _timeProvider.GetUtcNow();
        job.Status = ReportExportJobStatus.Ready;
        job.CompletedAt = completedAt;
        job.ExpiresAt = completedAt.Add(_retention);
        job.FileName = Path.GetFileName(fileName);
        job.Message = null;
    });

    public void MarkFailed(Guid jobId, string message) => Update(jobId, job =>
    {
        if (job.Status is not (ReportExportJobStatus.Queued or ReportExportJobStatus.Running))
        {
            throw new InvalidOperationException($"無法將 {job.Status} 工作標記為失敗。");
        }
        job.Status = ReportExportJobStatus.Failed;
        job.Message = message;
        DeleteIfExists(GetTemporaryPath(jobId));
    });

    public void Remove(Guid jobId)
    {
        lock (_gate)
        {
            var job = Load(jobId);
            if (job is not null && !string.IsNullOrWhiteSpace(job.FileName))
            {
                DeleteIfExists(GetCompletedPath(job));
            }
            DeleteIfExists(GetTemporaryPath(jobId));
            DeleteIfExists(MetadataPath(jobId));
        }
    }

    public void RecoverInterruptedJobs()
    {
        lock (_gate)
        {
            foreach (var path in Directory.EnumerateFiles(_rootDirectory, "*.json"))
            {
                var job = Deserialize(path);
                if (job?.Status is not (ReportExportJobStatus.Queued or ReportExportJobStatus.Running))
                {
                    continue;
                }
                job.Status = ReportExportJobStatus.Failed;
                job.Message = "網站曾重新啟動，請重新申請匯出。";
                DeleteIfExists(GetTemporaryPath(job.JobId));
                Save(job);
            }
        }
    }

    public void CleanupExpired()
    {
        foreach (var path in Directory.EnumerateFiles(_rootDirectory, "*.json"))
        {
            try
            {
                var job = Deserialize(path);
                if (job?.ExpiresAt <= _timeProvider.GetUtcNow())
                {
                    Remove(job.JobId);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "清除過期報表匯出檔案失敗。MetadataFile={MetadataFile}", Path.GetFileName(path));
            }
        }
    }

    private void Update(Guid jobId, Action<ReportExportJob> update)
    {
        lock (_gate)
        {
            var job = Load(jobId) ?? throw new KeyNotFoundException("找不到匯出工作。");
            update(job);
            Save(job);
        }
    }

    private ReportExportJob? Load(Guid jobId)
    {
        var path = MetadataPath(jobId);
        return File.Exists(path) ? Deserialize(path) : null;
    }

    private static ReportExportJob? Deserialize(string path) =>
        JsonSerializer.Deserialize<ReportExportJob>(File.ReadAllText(path), JsonOptions);

    private void Save(ReportExportJob job)
    {
        var finalPath = MetadataPath(job.JobId);
        var temporaryPath = finalPath + ".new";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(job, JsonOptions));
        File.Move(temporaryPath, finalPath, true);
    }

    private string MetadataPath(Guid jobId) => SafePath($"{jobId:N}.json");

    private string SafePath(string fileName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, Path.GetFileName(fileName)));
        if (!fullPath.StartsWith(_rootDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("匯出檔案路徑超出允許範圍。");
        }
        return fullPath;
    }

    private static void EnsureStatus(ReportExportJob job, ReportExportJobStatus expected)
    {
        if (job.Status != expected)
        {
            throw new InvalidOperationException($"匯出工作狀態必須為 {expected}，目前為 {job.Status}。");
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static SearchReportCondition CloneCondition(SearchReportCondition condition) => new()
    {
        ReportCode = condition.ReportCode,
        StartDate = condition.StartDate,
        EndDate = condition.EndDate,
        Chop1sec = condition.Chop1sec
    };
}
