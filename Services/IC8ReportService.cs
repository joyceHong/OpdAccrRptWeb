using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC8ReportService
{
    Task<C8ReportResult> QueryAsync(C8ReportRequest request, string actor,
        CancellationToken token = default);
}
