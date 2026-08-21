using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

/// <summary>
/// 所有報表服務的基礎類別，提供報表相關的功能和操作。
/// </summary>
public class ReportService : IReportService
{
    private readonly IHealthCenterRepository _healthCenterRepository;
    private readonly IReportTotalCountCache _totalCountCache;

    public ReportService(
        IHealthCenterRepository healthCenterRepository,
        IReportTotalCountCache totalCountCache)
    {
        _healthCenterRepository = healthCenterRepository;
        _totalCountCache = totalCountCache;
    }

    public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition)
    {
        if (searchCondition.StartDate is not null)
        {
            searchCondition.StartDate = DateTimeExtensions.ToRocDateString(DateTime.Parse(searchCondition.StartDate));
        }

        if (searchCondition.EndDate is not null)
        {
            searchCondition.EndDate = DateTimeExtensions.ToRocDateString(DateTime.Parse(searchCondition.EndDate));
        }

        switch (searchCondition.ReportCode)
        {
            case "C171":
                var pagedResult = _healthCenterRepository.GetHealthCenterData<T>(searchCondition);
                var pageNumber = searchCondition.PageNumber!.Value;
                var pageSize = searchCondition.PageSize!.Value;
                return new ReportDataAndColumns<T>
                {
                    Columns = _healthCenterRepository.GetHelthCenterDetailColumns(),
                    Data = pagedResult.Data,
                    TotalCount = pagedResult.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = CalculateTotalPages(pagedResult.TotalCount, pageSize)
                };
            case "C172":
                return CreateUnpagedResult(
                    _healthCenterRepository.GetHelthCenterCountColumns(),
                    _healthCenterRepository.GetHealthCenterCountData<T>(searchCondition));
            case "C173":
                var healthCheckupVisits = _healthCenterRepository
                    .GetHealthCheckupVisitsData<HealthCheckupVisits>(searchCondition)
                    .OrderBy(visit => visit.Chop1date, StringComparer.Ordinal)
                    .ThenBy(visit => visit.Chop1sec, StringComparer.Ordinal)
                    .Cast<T>()
                    .ToList();
                return CreateUnpagedResult(
                    _healthCenterRepository.GetHealthCheckupVisitsColumns(),
                    healthCheckupVisits);
            case "C174":
                var c174PageNumber = searchCondition.PageNumber!.Value;
                var c174PageSize = searchCondition.PageSize!.Value;
                var totalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate
                    },
                    () => _healthCenterRepository.GetHealthCenterContractBillingReportCount(searchCondition));
                var pageData = _healthCenterRepository.GetHealthCenterContractBillingReportPage<T>(searchCondition);
                return new ReportDataAndColumns<T>
                {
                    Columns = _healthCenterRepository.GetHealthCenterContractBillingReportColumns(),
                    Data = pageData,
                    TotalCount = totalCount,
                    PageNumber = c174PageNumber,
                    PageSize = c174PageSize,
                    TotalPages = CalculateTotalPages(totalCount, c174PageSize)
                };
            default:
                throw new ArgumentException($"Invalid report code: {searchCondition.ReportCode}");
        }
    }

    internal static int CalculateTotalPages(int totalCount, int pageSize)
    {
        return totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    private static ReportDataAndColumns<T> CreateUnpagedResult<T>(
        List<ModelDescriptionsHelper.PropertyMetadata> columns,
        List<T> data)
    {
        return new ReportDataAndColumns<T>
        {
            Columns = columns,
            Data = data
        };
    }
}
