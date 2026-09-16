using System.Globalization;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C143AccountingBalanceDebtReportService(
    IC143AccountingBalanceDebtRepository repository,
    IReportTotalCountCache totalCountCache) : IC143AccountingBalanceDebtReportService
{
    internal const string OutpatientAllStartDate = "1040101";
    internal const string InpatientAllStartDate = "0970331";

    public Task<ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel>> QueryAsync(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        C143Query query = CreateQuery(condition);
        var filters = new Dictionary<string, string?>
        {
            [nameof(C143Query.StartDate)] = query.StartDate,
            [nameof(C143Query.EndDate)] = query.EndDate,
            [nameof(C143Query.Source)] = query.Source,
            [nameof(C143Query.ReportType)] = query.ReportType
        };

        int dischargedCount = query.Source == C143Sources.Inpatient
            ? totalCountCache.GetOrCreate("C143:Discharged", filters,
                () => repository.GetInpatientCount(query, 1, cancellationToken))
            : 0;
        bool includeInHospital = query.Source == C143Sources.Inpatient
            && query.ReportType == C143ReportTypes.All;
        int totalCount = totalCountCache.GetOrCreate("C143", filters, () =>
            query.Source == C143Sources.OpdEr
                ? repository.GetOutpatientEmergencyCount(query, cancellationToken)
                : dischargedCount + (includeInHospital
                    ? repository.GetInpatientCount(query, 2, cancellationToken)
                    : 0));

        List<C143AccountingBalanceDebtReportViewModel> rows;
        int offset = (query.PageNumber - 1) * query.PageSize;
        if (query.Source == C143Sources.OpdEr)
        {
            rows = repository.GetOutpatientEmergencyPage(query, offset, query.PageSize, cancellationToken);
        }
        else
        {
            rows = [];
            if (offset < dischargedCount)
            {
                rows.AddRange(repository.GetInpatientPage(
                    query, 1, offset, query.PageSize, cancellationToken));
            }
            int remaining = query.PageSize - rows.Count;
            if (includeInHospital && remaining > 0)
            {
                int inHospitalOffset = Math.Max(0, offset - dischargedCount);
                rows.AddRange(repository.GetInpatientPage(
                    query, 2, inHospitalOffset, remaining, cancellationToken));
            }
        }

        return Task.FromResult(new ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel>
        {
            Columns = GetColumns(query.Source),
            Data = rows,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        });
    }

    internal static C143Query CreateQuery(SearchReportCondition condition)
    {
        if (!C143Sources.IsSupported(condition.Source))
            throw new ArgumentException("C143 資料來源不正確。", nameof(condition));
        if (!C143ReportTypes.IsSupported(condition.ReportType))
            throw new ArgumentException("C143 報表類型不正確。", nameof(condition));
        if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start)
            || !DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end)
            || start.Year < 1912 || start > end)
            throw new ArgumentException("C143 日期格式或範圍不正確。", nameof(condition));
        int pageNumber = condition.PageNumber ?? 1;
        int pageSize = condition.PageSize ?? 10;
        if (pageNumber <= 0 || pageSize is not (10 or 30 or 50))
            throw new ArgumentException("C143 分頁條件不正確。", nameof(condition));

        string startDate = condition.ReportType == C143ReportTypes.All
            ? condition.Source == C143Sources.Inpatient ? InpatientAllStartDate : OutpatientAllStartDate
            : ToRocDate(start);
        return new C143Query(startDate, ToRocDate(end), condition.Source!,
            condition.ReportType!, pageNumber, pageSize);
    }

    internal static List<ModelDescriptionsHelper.PropertyMetadata> GetColumns(string source)
    {
        List<ModelDescriptionsHelper.PropertyMetadata> all =
            ModelDescriptionsHelper.GetPropertyDescriptions<C143AccountingBalanceDebtReportViewModel>();
        string[] names = source == C143Sources.OpdEr
            ? [nameof(C143AccountingBalanceDebtReportViewModel.EncounterType),
                nameof(C143AccountingBalanceDebtReportViewModel.VisitDate),
                nameof(C143AccountingBalanceDebtReportViewModel.MedicalRecordNumber),
                nameof(C143AccountingBalanceDebtReportViewModel.EmergencyDepartureDate),
                nameof(C143AccountingBalanceDebtReportViewModel.AccountingGeneralSelfPay),
                nameof(C143AccountingBalanceDebtReportViewModel.AccountingInsuranceSelfPay),
                nameof(C143AccountingBalanceDebtReportViewModel.AccountingOutstanding),
                nameof(C143AccountingBalanceDebtReportViewModel.BillingDebt),
                nameof(C143AccountingBalanceDebtReportViewModel.BillingOutstanding),
                nameof(C143AccountingBalanceDebtReportViewModel.Difference)]
            : [nameof(C143AccountingBalanceDebtReportViewModel.MedicalRecordNumber),
                nameof(C143AccountingBalanceDebtReportViewModel.VisitDate),
                nameof(C143AccountingBalanceDebtReportViewModel.SequenceNumber),
                nameof(C143AccountingBalanceDebtReportViewModel.DischargeDate),
                nameof(C143AccountingBalanceDebtReportViewModel.AccountingGeneralSelfPay),
                nameof(C143AccountingBalanceDebtReportViewModel.AccountingInsuranceSelfPay),
                nameof(C143AccountingBalanceDebtReportViewModel.AccountingCopayment),
                nameof(C143AccountingBalanceDebtReportViewModel.AccountingOutstanding),
                nameof(C143AccountingBalanceDebtReportViewModel.AccountingMergedOutstanding),
                nameof(C143AccountingBalanceDebtReportViewModel.BillingDebt),
                nameof(C143AccountingBalanceDebtReportViewModel.BillingOutstanding),
                nameof(C143AccountingBalanceDebtReportViewModel.Difference)];
        return names.Select(name => all.Single(column =>
            column.Key == char.ToLowerInvariant(name[0]) + name[1..])).ToList();
    }

    private static string ToRocDate(DateOnly date) => $"{date.Year - 1911:000}{date:MMdd}";
}
