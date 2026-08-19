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

    public ReportService(IHealthCenterRepository healthCenterRepository)
    {
        _healthCenterRepository = healthCenterRepository;
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
                return CreateUnpagedResult(
                    _healthCenterRepository.GetHealthCheckupVisitsColumns(),
                    _healthCenterRepository.GetHealthCheckupVisitsData<T>(searchCondition));
            case "C174":
                return CreateUnpagedResult(
                    _healthCenterRepository.GetHealthCenterContractBillingReportColumns(),
                    _healthCenterRepository.GetHealthCenterContractBillingReport<T>(searchCondition));
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
