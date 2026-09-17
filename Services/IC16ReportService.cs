using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC16ReportService
{
    Task<C16ReportResult> QueryAsync(C16PreviewRequest request, CancellationToken cancellationToken = default);
}
