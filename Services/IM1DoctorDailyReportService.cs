using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IM1DoctorDailyReportService
{
    Task<M1DoctorDailyPagedResponse> QueryAsync(
        M1DoctorDailyReportRequest request,
        string actor,
        CancellationToken cancellationToken = default);
    Task<M1DoctorDailyReportSnapshot?> GenerateAsync(
        M1DoctorDailyReportRequest request,
        string actor,
        CancellationToken cancellationToken = default);
    bool TryGetRun(string runId, string actor, out M1DoctorDailyReportSnapshot snapshot);
}

public sealed class M1FutureDateConfirmationRequiredException(DateOnly reportDate)
    : Exception("查詢日期為今天或未來日期，請確認後再執行。")
{
    public const string ErrorCode = "M1_FUTURE_DATE_CONFIRMATION_REQUIRED";
    public DateOnly ReportDate { get; } = reportDate;
}

public sealed class M1ReportRunNotFoundException()
    : Exception("M1 查詢結果不存在或已逾期，請重新查詢。");

