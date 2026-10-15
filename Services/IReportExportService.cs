using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IReportExportService
{
    ReportExportDispatchResult Dispatch(SearchReportCondition searchCondition, string? ownerIdentity = null);

    ReportExportJob? GetJob(Guid jobId, string? requesterIdentity = null);

    ReportExportDownloadResult GetDownload(Guid jobId, string? requesterIdentity = null);
}

public interface IReportWorkbookGenerator
{
    void GenerateWorkbook(SearchReportCondition searchCondition, Stream destination);
}

public interface IReportExportQueue
{
    bool TryEnqueue(ReportExportJob job);
}

public sealed record ReportExportDispatchResult(
    byte[]? Workbook,
    string? FileName,
    ReportExportJob? Job,
    bool QueueFull = false,
    bool NoData = false);

public sealed record ReportExportDownloadResult(ReportExportJob? Job, Stream? Content);
