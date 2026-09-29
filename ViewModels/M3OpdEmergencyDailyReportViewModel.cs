using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed record M3OpdEmergencyDailyPagedResponse(
    string? RunId,
    IReadOnlyList<M3OpdEmergencyDailyReportRow> Data,
    IReadOnlyList<ReportColumn> Columns,
    M3NineKpis NineKpis,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public sealed record M3OpdEmergencyDailyPreviewViewModel(
    string Title,
    string QueryDate,
    string Weekday,
    string GeneratedAt,
    string ProgramId,
    string UserName,
    IReadOnlyList<M3OpdEmergencyDailyReportRow> Rows,
    M3NineKpis NineKpis);

public sealed record M3RenderedFile(byte[] Content, string ContentType, string FileName);
