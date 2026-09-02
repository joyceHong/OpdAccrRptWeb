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
    private readonly ISurgicalAccountingRepository? _surgicalAccountingRepository;
    private readonly ICashierCashRepository? _cashierCashRepository;
    private readonly ICashierCashSummaryRepository? _cashierCashSummaryRepository;
    private readonly IOutpatientReceivableBalanceRepository? _outpatientReceivableBalanceRepository;
    private readonly IInpatientAdvancePaymentBalanceRepository? _inpatientAdvancePaymentBalanceRepository;
    private readonly IAssistiveDeviceDepositBalanceRepository? _assistiveDeviceDepositBalanceRepository;
    private readonly IInpatientReceivableBalanceRepository? _inpatientReceivableBalanceRepository;
    private readonly IContractPaymentDetailRepository? _contractPaymentDetailRepository;

    public ReportService(
        IHealthCenterRepository healthCenterRepository,
        IReferralMemberRepository referralMemberRepository,
        ISafeNeedleRepository safeNeedleRepository,
        IReportTotalCountCache totalCountCache,
        ILogger<ReportService> logger,
        ISurgicalAccountingRepository? surgicalAccountingRepository = null,
        ICashierCashRepository? cashierCashRepository = null,
        IInpatientAdvancePaymentBalanceRepository? inpatientAdvancePaymentBalanceRepository = null,
        IAssistiveDeviceDepositBalanceRepository? assistiveDeviceDepositBalanceRepository = null,
        IInpatientReceivableBalanceRepository? inpatientReceivableBalanceRepository = null,
        IContractPaymentDetailRepository? contractPaymentDetailRepository = null,
        ICashierCashSummaryRepository? cashierCashSummaryRepository = null,
        IOutpatientReceivableBalanceRepository? outpatientReceivableBalanceRepository = null)
    {
        _healthCenterRepository = healthCenterRepository;
        _referralMemberRepository = referralMemberRepository;
        _safeNeedleRepository = safeNeedleRepository;
        _totalCountCache = totalCountCache;
        _logger = logger;
        _surgicalAccountingRepository = surgicalAccountingRepository;
        _cashierCashRepository = cashierCashRepository;
        _cashierCashSummaryRepository = cashierCashSummaryRepository;
        _outpatientReceivableBalanceRepository = outpatientReceivableBalanceRepository;
        _inpatientAdvancePaymentBalanceRepository = inpatientAdvancePaymentBalanceRepository;
        _assistiveDeviceDepositBalanceRepository = assistiveDeviceDepositBalanceRepository;
        _inpatientReceivableBalanceRepository = inpatientReceivableBalanceRepository;
        _contractPaymentDetailRepository = contractPaymentDetailRepository;
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
            case "C1":
                var surgicalAccountingRepository = _surgicalAccountingRepository
                    ?? throw new InvalidOperationException("C1 repository 尚未設定。");
                var c1PageNumber = searchCondition.PageNumber!.Value;
                var c1PageSize = searchCondition.PageSize!.Value;
                var c1TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate
                    },
                    () => surgicalAccountingRepository.GetCount(searchCondition));
                var c1PageData = surgicalAccountingRepository
                    .GetPage(searchCondition)
                    .Cast<T>()
                    .ToList();
                return new ReportDataAndColumns<T>
                {
                    Columns = surgicalAccountingRepository.GetColumns(),
                    Data = c1PageData,
                    TotalCount = c1TotalCount,
                    PageNumber = c1PageNumber,
                    PageSize = c1PageSize,
                    TotalPages = CalculateTotalPages(c1TotalCount, c1PageSize)
                };
            case "C22":
                var cashierCashRepository = _cashierCashRepository
                    ?? throw new InvalidOperationException("C22 repository 尚未設定。");
                var c22PageNumber = searchCondition.PageNumber!.Value;
                var c22PageSize = searchCondition.PageSize!.Value;
                var c22TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate,
                        [nameof(SearchReportCondition.CashierUserId)] = string.IsNullOrWhiteSpace(searchCondition.CashierUserId) ? null : searchCondition.CashierUserId.Trim()
                    },
                    () => cashierCashRepository.GetCount(searchCondition));
                return new ReportDataAndColumns<T>
                {
                    Columns = cashierCashRepository.GetColumns(),
                    Data = cashierCashRepository.GetPage(searchCondition).Cast<T>().ToList(),
                    TotalCount = c22TotalCount,
                    PageNumber = c22PageNumber,
                    PageSize = c22PageSize,
                    TotalPages = CalculateTotalPages(c22TotalCount, c22PageSize)
                };
            case "C213":
                var cashierCashSummaryRepository = _cashierCashSummaryRepository
                    ?? throw new InvalidOperationException("C213 repository 尚未設定。");
                var c213PageNumber = searchCondition.PageNumber!.Value;
                var c213PageSize = searchCondition.PageSize!.Value;
                var c213TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate
                    },
                    () => cashierCashSummaryRepository.GetCount(searchCondition));
                return new ReportDataAndColumns<T>
                {
                    Columns = cashierCashSummaryRepository.GetColumns(),
                    Data = cashierCashSummaryRepository.GetPage(searchCondition).Cast<T>().ToList(),
                    TotalCount = c213TotalCount,
                    PageNumber = c213PageNumber,
                    PageSize = c213PageSize,
                    TotalPages = CalculateTotalPages(c213TotalCount, c213PageSize)
                };
            case "C214":
                var outpatientReceivableBalanceRepository = _outpatientReceivableBalanceRepository
                    ?? throw new InvalidOperationException("C214 repository 尚未設定。");
                var c214PageNumber = searchCondition.PageNumber!.Value;
                var c214PageSize = searchCondition.PageSize!.Value;
                var c214TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate,
                        [nameof(SearchReportCondition.ReceivableBalanceType)] = searchCondition.ReceivableBalanceType
                    },
                    () => outpatientReceivableBalanceRepository.GetCount(searchCondition));
                return new ReportDataAndColumns<T>
                {
                    Columns = outpatientReceivableBalanceRepository.GetColumns(),
                    Data = outpatientReceivableBalanceRepository.GetPage(searchCondition).Cast<T>().ToList(),
                    TotalCount = c214TotalCount,
                    PageNumber = c214PageNumber,
                    PageSize = c214PageSize,
                    TotalPages = CalculateTotalPages(c214TotalCount, c214PageSize)
                };
            case "C25":
                var inpatientAdvancePaymentBalanceRepository = _inpatientAdvancePaymentBalanceRepository
                    ?? throw new InvalidOperationException("C25 repository 尚未設定。");
                var c25PageNumber = searchCondition.PageNumber!.Value;
                var c25PageSize = searchCondition.PageSize!.Value;
                var c25TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate
                    },
                    () => inpatientAdvancePaymentBalanceRepository.GetCount(searchCondition));
                return new ReportDataAndColumns<T>
                {
                    Columns = inpatientAdvancePaymentBalanceRepository.GetColumns(),
                    Data = inpatientAdvancePaymentBalanceRepository.GetPage(searchCondition).Cast<T>().ToList(),
                    TotalCount = c25TotalCount,
                    PageNumber = c25PageNumber,
                    PageSize = c25PageSize,
                    TotalPages = CalculateTotalPages(c25TotalCount, c25PageSize)
                };
            case "C27":
                var assistiveDeviceDepositBalanceRepository = _assistiveDeviceDepositBalanceRepository
                    ?? throw new InvalidOperationException("C27 repository 尚未設定。");
                var c27PageNumber = searchCondition.PageNumber!.Value;
                var c27PageSize = searchCondition.PageSize!.Value;
                var c27TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate
                    },
                    () => assistiveDeviceDepositBalanceRepository.GetCount(searchCondition));
                return new ReportDataAndColumns<T>
                {
                    Columns = assistiveDeviceDepositBalanceRepository.GetColumns(),
                    Data = assistiveDeviceDepositBalanceRepository.GetPage(searchCondition).Cast<T>().ToList(),
                    TotalCount = c27TotalCount,
                    PageNumber = c27PageNumber,
                    PageSize = c27PageSize,
                    TotalPages = CalculateTotalPages(c27TotalCount, c27PageSize)
                };
            case "C28":
                var inpatientReceivableBalanceRepository = _inpatientReceivableBalanceRepository
                    ?? throw new InvalidOperationException("C28 repository 尚未設定。");
                var c28PageNumber = searchCondition.PageNumber!.Value;
                var c28PageSize = searchCondition.PageSize!.Value;
                var c28TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate
                    },
                    () => inpatientReceivableBalanceRepository.GetCount(searchCondition));
                return new ReportDataAndColumns<T>
                {
                    Columns = inpatientReceivableBalanceRepository.GetColumns(),
                    Data = inpatientReceivableBalanceRepository.GetPage(searchCondition).Cast<T>().ToList(),
                    TotalCount = c28TotalCount,
                    PageNumber = c28PageNumber,
                    PageSize = c28PageSize,
                    TotalPages = CalculateTotalPages(c28TotalCount, c28PageSize)
                };
            case "C29":
                var contractPaymentDetailRepository = _contractPaymentDetailRepository
                    ?? throw new InvalidOperationException("C29 repository 尚未設定。");
                var c29PageNumber = searchCondition.PageNumber!.Value;
                var c29PageSize = searchCondition.PageSize!.Value;
                searchCondition.BillingCode = string.IsNullOrWhiteSpace(searchCondition.BillingCode)
                    ? null
                    : searchCondition.BillingCode.Trim();
                var c29TotalCount = _totalCountCache.GetOrCreate(
                    searchCondition.ReportCode,
                    new Dictionary<string, string?>
                    {
                        [nameof(SearchReportCondition.StartDate)] = searchCondition.StartDate,
                        [nameof(SearchReportCondition.EndDate)] = searchCondition.EndDate,
                        [nameof(SearchReportCondition.EncounterSource)] = searchCondition.EncounterSource,
                        [nameof(SearchReportCondition.BillingCode)] = searchCondition.BillingCode
                    },
                    () => contractPaymentDetailRepository.GetCount(searchCondition));
                return new ReportDataAndColumns<T>
                {
                    Columns = contractPaymentDetailRepository.GetColumns(),
                    Data = contractPaymentDetailRepository.GetPage(searchCondition).Cast<T>().ToList(),
                    TotalCount = c29TotalCount,
                    PageNumber = c29PageNumber,
                    PageSize = c29PageSize,
                    TotalPages = CalculateTotalPages(c29TotalCount, c29PageSize)
                };
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
