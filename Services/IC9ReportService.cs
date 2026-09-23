using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC9ReportService
{
    Task<C9ReportResult> QueryAsync(C9ReportRequest request, string actor,
        CancellationToken token = default);
}
