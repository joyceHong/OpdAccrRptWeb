using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC5ReportService
{
    Task<C5ReportResult> QueryAsync(C5ReportRequest request, CancellationToken cancellationToken = default);
}
