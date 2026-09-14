using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C212BoneBankBalanceReportService(
    IC212BoneBankBalanceRepository repository,
    IC212AmountCompatibilityPolicy amountPolicy,
    TimeProvider timeProvider) : IC212BoneBankBalanceReportService
{
    public async Task<ReportDataAndColumns<C212BoneBankBalanceReportViewModel>> CreateAsync(
        C212Query query,
        string userId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        C212ReportResult result = await CreateResultAsync(
            query, userId, correlationId, cancellationToken);
        return new ReportDataAndColumns<C212BoneBankBalanceReportViewModel>
        {
            Columns = ModelDescriptionsHelper.GetPropertyDescriptions<C212BoneBankBalanceReportViewModel>(),
            Data = result.Rows.Select(row => new C212BoneBankBalanceReportViewModel
            {
                AccountingDate = FormatRocDate(row.AccountingDateRoc),
                MedicalRecordNo = row.MedicalRecordNo,
                PatientName = row.PatientName,
                Amount = row.Amount
            }).ToList(),
            TotalCount = result.Rows.Count,
            Summary = new C212ReportSummary(
                "骨庫餘額明細表",
                $"資料日期：{FormatRocDate(ToRocDate(result.AsOfDate))}",
                "OpdAccRpt",
                "PFin2Balance42",
                result.ReportProcessDateTimeRoc,
                result.GeneratedBy,
                result.TotalAmount,
                result.DataStatus,
                "目前無法確認資料完整性",
                result.CorrelationId)
        };
    }

    public async Task<C212ReportResult> CreateResultAsync(
        C212Query query,
        string userId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        string endDateRoc = ToRocDate(query.EndDate);
        string monthFirstDayRoc = $"{endDateRoc[..5]}01";
        IReadOnlyList<C212RawRow> rawRows = await repository.GetRowsAsync(
            endDateRoc, monthFirstDayRoc, cancellationToken);
        DateTime oracleNow = await repository.GetOracleNowAsync(cancellationToken);

        var rows = new List<C212ReportRow>(rawRows.Count);
        decimal total = 0m;
        checked
        {
            foreach (C212RawRow rawRow in rawRows)
            {
                decimal amount = amountPolicy.Convert(rawRow.RawOracleAmount);
                total += amount;
                rows.Add(new C212ReportRow(
                    rawRow.Kind,
                    rawRow.AccountingDateRoc,
                    rawRow.MedicalRecordNo,
                    rawRow.PatientName,
                    rawRow.RawOracleAmount,
                    amount));
            }
        }

        return new C212ReportResult(
            query.EndDate,
            monthFirstDayRoc,
            rows,
            total,
            C212DataStatus.Unknown,
            timeProvider.GetUtcNow(),
            $"{FormatRocDate(ToRocDate(DateOnly.FromDateTime(oracleNow)))}  {oracleNow:HH:mm:ss}",
            userId,
            correlationId);
    }

    internal static string ToRocDate(DateOnly date)
    {
        int rocYear = date.Year - 1911;
        if (rocYear is < 0 or > 999)
            throw new ArgumentOutOfRangeException(nameof(date), "日期無法轉成三碼民國年。");
        return $"{rocYear:000}{date:MMdd}";
    }

    internal static string FormatRocDate(string value) => value.Length == 7
        ? $"{value[..3]}/{value.Substring(3, 2)}/{value.Substring(5, 2)}"
        : value;
}
