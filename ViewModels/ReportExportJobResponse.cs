namespace OpdAccrRptWeb.ViewModels;

public sealed record ReportExportJobResponse(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAt,
    string StatusUrl,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? CompletedAt = null,
    DateTimeOffset? ExpiresAt = null,
    string? DownloadUrl = null,
    string? Message = null);
