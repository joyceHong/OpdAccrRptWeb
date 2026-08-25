using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace OpdAccrRptWeb.Services;

/// <summary>
/// 所有報表服務的基礎類別，提供報表相關的功能和操作。
/// </summary>
public class ReportService : IReportService
{
    private readonly IHealthCenterRepository _healthCenterRepository;
    private readonly IReferralMemberRepository _referralMemberRepository;
    private readonly ISafeNeedleRepository _safeNeedleRepository;
    private readonly IReportTotalCountCache _totalCountCache;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IHealthCenterRepository healthCenterRepository,
        IReferralMemberRepository referralMemberRepository,
        ISafeNeedleRepository safeNeedleRepository,
        IReportTotalCountCache totalCountCache,
        ILogger<ReportService> logger)
    {
        _healthCenterRepository = healthCenterRepository;
        _referralMemberRepository = referralMemberRepository;
        _safeNeedleRepository = safeNeedleRepository;
        _totalCountCache = totalCountCache;
        _logger = logger;
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
                var countQueryExecuted = false;
                var totalCountStopwatch = Stopwatch.StartNew();
                var c171TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate
                    },
                    () =>
                    {
                        countQueryExecuted = true;
                        return _healthCenterRepository.GetHealthCenterDataCount(searchCondition);
                    });
                totalCountStopwatch.Stop();
                _logger.LogInformation(
                    "{ReportCode} total count resolved in {TotalCountResolutionElapsedMs} ms for {StartDate} through {EndDate}; CacheHit={CacheHit}, TotalCount={TotalCount}",
                    searchCondition.ReportCode,
                    totalCountStopwatch.ElapsedMilliseconds,
                    searchCondition.StartDate,
                    searchCondition.EndDate,
                    !countQueryExecuted,
                    c171TotalCount);
                var c171PageData = _healthCenterRepository.GetHealthCenterDataPage<T>(searchCondition);
                var pageNumber = searchCondition.PageNumber!.Value;
                var pageSize = searchCondition.PageSize!.Value;
                return new ReportDataAndColumns<T>
                {
                    Columns = _healthCenterRepository.GetHelthCenterDetailColumns(),
                    Data = c171PageData,
                    TotalCount = c171TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = CalculateTotalPages(c171TotalCount, pageSize)
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
            case "C18":
                var c18PageNumber = searchCondition.PageNumber!.Value;
                var c18PageSize = searchCondition.PageSize!.Value;
                var c18TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate,
                        [nameof(SearchReportCondition.EncounterSource)] = searchCondition.EncounterSource
                    },
                    () => _referralMemberRepository.GetCount(searchCondition));
                var c18PageData = _referralMemberRepository
                    .GetPage(searchCondition)
                    .Cast<T>()
                    .ToList();
                return new ReportDataAndColumns<T>
                {
                    Columns = _referralMemberRepository.GetColumns(),
                    Data = c18PageData,
                    TotalCount = c18TotalCount,
                    PageNumber = c18PageNumber,
                    PageSize = c18PageSize,
                    TotalPages = CalculateTotalPages(c18TotalCount, c18PageSize)
                };
            case "C19":
                var c19PageNumber = searchCondition.PageNumber!.Value;
                var c19PageSize = searchCondition.PageSize!.Value;
                var normalizedPrefix = string.IsNullOrWhiteSpace(searchCondition.StationOrBedPrefix)
                    ? null
                    : searchCondition.StationOrBedPrefix.Trim();
                var c19TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate,
                        [nameof(SearchReportCondition.EncounterSource)] = searchCondition.EncounterSource,
                        [nameof(SearchReportCondition.StationOrBedPrefix)] = normalizedPrefix
                    },
                    () => _safeNeedleRepository.GetCount(searchCondition));
                var c19PageData = _safeNeedleRepository
                    .GetPage(searchCondition)
                    .Cast<T>()
                    .ToList();
                return new ReportDataAndColumns<T>
                {
                    Columns = _safeNeedleRepository.GetColumns(),
                    Data = c19PageData,
                    TotalCount = c19TotalCount,
                    PageNumber = c19PageNumber,
                    PageSize = c19PageSize,
                    TotalPages = CalculateTotalPages(c19TotalCount, c19PageSize)
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
