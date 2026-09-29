using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IM2DoctorMonthlyReportService
{
    Task<M2DoctorMonthlyPagedResponse> QueryAsync(M2DoctorMonthlyReportRequest request,
        string actor, CancellationToken cancellationToken = default);
    Task<M2DoctorMonthlyReportSnapshot?> GenerateAsync(M2DoctorMonthlyReportRequest request,
        string actor, CancellationToken cancellationToken = default);
    bool TryGetRun(string runId, string actor, out M2DoctorMonthlyReportSnapshot snapshot);
}

public sealed class M2ReportRunNotFoundException()
    : Exception("M2 查詢結果不存在或已逾期，請重新查詢。");
