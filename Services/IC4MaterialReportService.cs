using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC4MaterialReportService
{
    Task<C4MaterialReportResult> QueryAsync(C4MaterialReportRequest request,
        CancellationToken cancellationToken = default);
    Task<C4MaterialPreviewViewModel?> CreatePreviewAsync(C4MaterialReportRequest request,
        string userId, CancellationToken cancellationToken = default);
}

public interface IC4TransientFailurePolicy
{
    bool IsTransient(Exception exception);
    int MaxAttempts { get; }
}
