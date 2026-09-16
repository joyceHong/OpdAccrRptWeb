using System.Globalization;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C144DebtDetailReportService(
    IC144DebtDetailReportRepository repository,
    IReportTotalCountCache totalCountCache) : IC144DebtDetailReportService
{
    public Task<ReportDataAndColumns<C144DebtDetailReportViewModel>> QueryAsync(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        C144Query query = CreateQuery(condition);
        var filters = new Dictionary<string, string?>
        {
            [nameof(C144Query.StartDate)] = query.StartDate,
            [nameof(C144Query.EndDate)] = query.EndDate,
            [nameof(C144Query.Source)] = query.Source
        };
        int totalCount = totalCountCache.GetOrCreate(
            "C144", filters, () => repository.GetCount(query, cancellationToken));
        int offset = checked((query.PageNumber - 1) * query.PageSize);
        List<C144DebtDetailReportViewModel> rows = repository.GetPage(
            query, offset, query.PageSize, cancellationToken);

        return Task.FromResult(new ReportDataAndColumns<C144DebtDetailReportViewModel>
        {
            Columns = GetColumns(),
            Data = rows,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        });
    }

    public Task<IReadOnlyList<C144DebtDetailReportViewModel>> QueryAllAsync(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        C144Query query = CreateQuery(condition, allowUnpaged: true);
        return Task.FromResult<IReadOnlyList<C144DebtDetailReportViewModel>>(
            repository.GetAll(query, cancellationToken));
    }

    internal static C144Query CreateQuery(SearchReportCondition condition, bool allowUnpaged = false)
    {
        if (!C144Sources.IsSupported(condition.Source))
            throw new ArgumentException("C144 資料來源不正確。", nameof(condition));
        if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start)
            || !DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end)
            || start.Year < 1912 || start > end)
            throw new ArgumentException("C144 日期格式或範圍不正確。", nameof(condition));

        int pageNumber = condition.PageNumber ?? 1;
        int pageSize = condition.PageSize ?? 10;
        if (pageNumber <= 0 || (!allowUnpaged && pageSize is not (10 or 30 or 50)))
            throw new ArgumentException("C144 分頁條件不正確。", nameof(condition));

        return new C144Query(
            ToRocDate(start), ToRocDate(end), condition.Source!, pageNumber, pageSize);
    }

    internal static List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<C144DebtDetailReportViewModel>();

    private static string ToRocDate(DateOnly date) => $"{date.Year - 1911:000}{date:MMdd}";
}
