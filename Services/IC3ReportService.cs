using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC3ReportService
{
    Task<C3ReportResult> QueryAsync(C3ReportRequest request, CancellationToken cancellationToken = default);
    Task<C3PreviewViewModel?> CreatePreviewAsync(C3ReportRequest request, string userId,
        CancellationToken cancellationToken = default);
}
