using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C211ContractBalanceReportService(
    IC211ContractBalanceRepository repository,
    TimeProvider timeProvider) : IC211ContractBalanceReportService
{
    public async Task<ReportDataAndColumns<C211ContractBalanceReportViewModel>> CreateAsync(
        SearchReportCondition condition,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var report = await CreateCompleteResultAsync(condition, userId, cancellationToken);
        var pageNumber = condition.PageNumber ?? 1;
        var pageSize = condition.PageSize ?? 10;
        var pageOffset = Math.Min((long)(pageNumber - 1) * pageSize, report.Rows.Count);

        return new ReportDataAndColumns<C211ContractBalanceReportViewModel>
        {
            Columns = ModelDescriptionsHelper.GetPropertyDescriptions<C211ContractBalanceReportViewModel>(),
            Data = report.Rows.Skip((int)pageOffset).Take(pageSize).ToList(),
            TotalCount = report.Rows.Count,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = report.Rows.Count == 0
                ? 0
                : (report.Rows.Count / pageSize) + (report.Rows.Count % pageSize == 0 ? 0 : 1),
            Summary = report.Summary
        };
    }

    public async Task<C211ContractBalancePreviewViewModel> CreatePreviewAsync(
        SearchReportCondition condition,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var report = await CreateCompleteResultAsync(condition, userId, cancellationToken);
        return new C211ContractBalancePreviewViewModel(report.Rows, report.Summary);
    }

    private async Task<(List<C211ContractBalanceReportViewModel> Rows, C211ReportSummary Summary)>
        CreateCompleteResultAsync(
            SearchReportCondition condition,
            string userId,
            CancellationToken cancellationToken)
    {
        var asOfDate = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd");
        var source = condition.EncounterSource!;
        var rows = await repository.GetRowsAsync(
            source, asOfDate, condition.ContractCode, cancellationToken);
        var data = rows.Select(row => new C211ContractBalanceReportViewModel
        {
            ContractCode = row.ContractCode,
            MedicalRecordNo = row.MedicalRecordNo,
            VisitDate = FormatRocDate(row.VisitDateRoc),
            SelfAmount = row.SelfAmount,
            ClaimAmount = row.ClaimAmount
        }).ToList();
        var groups = BuildGroups(rows);
        var now = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TaipeiTimeZone());

        decimal selfGrandTotal = 0m;
        decimal claimGrandTotal = 0m;
        checked
        {
            foreach (var group in groups)
            {
                selfGrandTotal += group.SelfAmount;
                claimGrandTotal += group.ClaimAmount;
            }
        }

        return (data, new C211ReportSummary(
                source == C211Sources.Inpatient ? "合約單位餘額明細表(住院)" : "合約單位餘額明細表(門急)",
                $"資料日期： ~ {FormatRocDate(C211ContractBalanceRepository.ToRocDate(asOfDate))}",
                "OpdAccRpt", "PFin2Balance",
                $"{FormatRocDate(C211ContractBalanceRepository.ToRocDate(DateOnly.FromDateTime(now.DateTime)))}  {now:HH:mm:ss}",
                userId,
                groups,
                selfGrandTotal,
                claimGrandTotal));
    }

    internal static IReadOnlyList<C211ContractSubtotal> BuildGroups(IEnumerable<C211Row> rows)
    {
        var order = new List<string>();
        var totals = new Dictionary<string, (decimal Self, decimal Claim)>(StringComparer.Ordinal);
        checked
        {
            foreach (var row in rows)
            {
                if (!totals.TryGetValue(row.ContractCode, out var total)) order.Add(row.ContractCode);
                totals[row.ContractCode] = (total.Self + row.SelfAmount, total.Claim + row.ClaimAmount);
            }
        }
        return order.Select(code => new C211ContractSubtotal(code, totals[code].Self, totals[code].Claim)).ToList();
    }

    internal static string FormatRocDate(string value) => value.Length == 7
        ? $"{value[..3]}/{value.Substring(3, 2)}/{value.Substring(5, 2)}"
        : value;

    private static TimeZoneInfo TaipeiTimeZone()
    {
        foreach (var id in new[] { "Asia/Taipei", "Taipei Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        throw new TimeZoneNotFoundException("找不到 Asia/Taipei 時區。");
    }
}
