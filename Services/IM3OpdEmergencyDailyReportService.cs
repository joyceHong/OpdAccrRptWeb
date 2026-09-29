using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IM3OpdEmergencyDailyReportService
{
    Task<M3OpdEmergencyDailyPagedResponse> QueryAsync(M3OpdEmergencyDailyReportRequest request,
        string actor, CancellationToken cancellationToken = default);
    Task<M3OpdEmergencyDailyReportSnapshot> GenerateAsync(M3OpdEmergencyDailyReportRequest request,
        string actor, CancellationToken cancellationToken = default);
    bool TryGetRun(string runId, string actor, out M3OpdEmergencyDailyReportSnapshot snapshot);
}

public sealed class M3ReportRunNotFoundException()
    : Exception("M3 查詢結果不存在或已逾期，請重新查詢。");
