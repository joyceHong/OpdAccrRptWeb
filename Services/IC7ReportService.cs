using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;
public interface IC7ReportService
{
    Task<C7ReportResult> QueryAsync(C7ReportRequest request, CancellationToken token = default);
    Task<IReadOnlyList<C7InputUser>> GetEligibleUsersAsync(string endDate, CancellationToken token = default);
}
