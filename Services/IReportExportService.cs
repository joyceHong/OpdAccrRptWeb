using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IReportExportService
{
    ReportExportDispatchResult Dispatch(SearchReportCondition searchCondition);

    ReportExportJob? GetJob(Guid jobId);

    ReportExportDownloadResult GetDownload(Guid jobId);
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
    bool QueueFull = false);

public sealed record ReportExportDownloadResult(ReportExportJob? Job, Stream? Content);
