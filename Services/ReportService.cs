using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Oracle.ManagedDataAccess.Client;

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
    private readonly IC21AccountingSummaryRepository? _c21AccountingSummaryRepository;
    private readonly IC21AccountingSummaryCalculationService? _c21CalculationService;
    private readonly IC21RebuildService? _c21RebuildService;
    private readonly IC23ContractAccountingRepository? _c23ContractAccountingRepository;
    private readonly IC23RebuildService? _c23RebuildService;
    private readonly IC24DebtPaymentRepository? _c24Repository;
    private readonly IC24DebtPaymentCalculationService? _c24CalculationService;

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
        IOutpatientReceivableBalanceRepository? outpatientReceivableBalanceRepository = null,
        IC21AccountingSummaryRepository? c21AccountingSummaryRepository = null,
        IC21AccountingSummaryCalculationService? c21CalculationService = null,
        IC21RebuildService? c21RebuildService = null,
        IC23ContractAccountingRepository? c23ContractAccountingRepository = null,
        IC23RebuildService? c23RebuildService = null,
        IC24DebtPaymentRepository? c24Repository = null,
        IC24DebtPaymentCalculationService? c24CalculationService = null)
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
        _c21AccountingSummaryRepository = c21AccountingSummaryRepository;
        _c21CalculationService = c21CalculationService;
        _c21RebuildService = c21RebuildService;
        _c23ContractAccountingRepository = c23ContractAccountingRepository;
        _c23RebuildService = c23RebuildService;
        _c24Repository = c24Repository;
        _c24CalculationService = c24CalculationService;
    }

    public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition)
    {
        if (searchCondition.ReportCode == "C21")
        {
            return CreateC21Result<T>(searchCondition);
        }

        if (searchCondition.ReportCode == "C23")
        {
            return CreateC23Result<T>(searchCondition);
        }

        if (searchCondition.ReportCode == "C24")
        {
            return CreateC24Result<T>(searchCondition);
        }

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

    private ReportDataAndColumns<T> CreateC21Result<T>(SearchReportCondition condition)
    {
        var repository = _c21AccountingSummaryRepository
            ?? throw new InvalidOperationException("C21 repository 尚未設定。");
        var calculationService = _c21CalculationService
            ?? throw new InvalidOperationException("C21 calculation service 尚未設定。");
        var rebuildService = _c21RebuildService
            ?? throw new InvalidOperationException("C21 rebuild service 尚未設定。");

        if (rebuildService.EnsureData(condition))
        {
            _totalCountCache.Invalidate("C21");
        }

        var canonicalRows = calculationService.Calculate(
            condition,
            repository.GetSourceAmounts(condition),
            repository.GetBillingItems());
        var filters = new Dictionary<string, string?>
        {
            [nameof(condition.StartDate)] = condition.StartDate,
            [nameof(condition.EndDate)] = condition.EndDate,
            [nameof(condition.EncounterSource)] = condition.EncounterSource,
            [nameof(condition.AccountingScope)] = condition.AccountingScope?.ToString(),
            [nameof(condition.BillingCode)] = condition.BillingCode
        };
        var totalCount = _totalCountCache.GetOrCreate("C21", filters, () => canonicalRows.Count);
        var pageNumber = condition.PageNumber!.Value;
        var pageSize = condition.PageSize!.Value;
        var page = canonicalRows.Skip((pageNumber - 1) * pageSize).Take(pageSize).Cast<T>().ToList();
        return new ReportDataAndColumns<T>
        {
            Columns = repository.GetColumns(),
            Data = page,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = CalculateTotalPages(totalCount, pageSize)
        };
    }

    private ReportDataAndColumns<T> CreateC23Result<T>(SearchReportCondition condition)
    {
        var repository = _c23ContractAccountingRepository
            ?? throw new InvalidOperationException("C23 repository 尚未設定。");
        var rebuildService = _c23RebuildService
            ?? throw new InvalidOperationException("C23 rebuild service 尚未設定。");
        if (rebuildService.EnsureData(condition)) _totalCountCache.Invalidate("C23");
        var filters = new Dictionary<string, string?>
        {
            [nameof(condition.StartDate)] = condition.StartDate,
            [nameof(condition.EndDate)] = condition.EndDate,
            [nameof(condition.EncounterSource)] = condition.EncounterSource,
            [nameof(condition.DateMode)] = condition.DateMode,
            [nameof(condition.InpatientType)] = condition.InpatientType,
            [nameof(condition.ContractCode)] = condition.ContractCode
        };
        var totalCount = _totalCountCache.GetOrCreate("C23", filters, () => repository.GetCount(condition));
        var pageNumber = condition.PageNumber!.Value;
        var pageSize = condition.PageSize!.Value;
        return new ReportDataAndColumns<T>
        {
            Columns = repository.GetColumns(),
            Data = repository.GetPage(condition).Cast<T>().ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = CalculateTotalPages(totalCount, pageSize)
        };
    }

    private ReportDataAndColumns<T> CreateC24Result<T>(SearchReportCondition condition)
    {
        var repository = _c24Repository ?? throw new InvalidOperationException("C24 repository 尚未設定。");
        var calculation = _c24CalculationService ?? throw new InvalidOperationException("C24 calculation service 尚未設定。");
        var start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd");
        C24CanonicalResult canonical;
        if (condition.Mode == C24Modes.Billing)
        {
            var source = ExecuteC24ReadWithRetry(() => repository.Load(condition), "BillingSource");
            canonical = calculation.Calculate(condition, source);
        }
        else
        {
            var isSingleDay = start == end;
            var hasLegacy = isSingleDay && !condition.ForceRebuild
                && ExecuteC24ReadWithRetry(() => repository.HasLegacyResult(condition.Source!, start),
                    "LegacyExists");
            if (isSingleDay && !hasLegacy)
            {
                var source = ExecuteC24ReadWithRetry(() => repository.Load(condition), "AccountingSource");
                var rebuilt = calculation.Calculate(condition, source);
                ValidateLegacyPublication(rebuilt);
                PublishLegacyWithRetry(repository, condition.Source!, start,
                    rebuilt.LegacyDetails.Where(x => x.AccountingDate == start).ToList(),
                    rebuilt.LegacySummaries.Where(x => x.AccountingDate == start).ToList());
            }
            var legacy = ExecuteC24ReadWithRetry(
                () => repository.LoadLegacy(condition.Source!, start, end), "LegacyRead");
            canonical = CreateCanonicalFromLegacy(condition, legacy);
        }
        var pageNumber = condition.PageNumber!.Value;
        var pageSize = condition.PageSize!.Value;
        var page = canonical.Details.Skip((pageNumber - 1) * pageSize).Take(pageSize).Cast<T>().ToList();
        return new ReportDataAndColumns<T>
        {
            Columns = ModelDescriptionsHelper.GetPropertyDescriptions<C24DebtPaymentDetail>()
                .Where(x => x.Key is not "sourceBusinessKey" and not "legacyRoomType"
                    and not "legacyDischargeFlag").ToList(),
            Data = page,
            Summary = canonical.Summaries,
            TotalCount = canonical.Details.Count,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = CalculateTotalPages(canonical.Details.Count, pageSize)
        };
    }

    private T ExecuteC24ReadWithRetry<T>(Func<T> operation, string stage)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                return operation();
            }
            catch (OracleException exception) when (attempt < C24OracleFailurePolicy.MaximumAttempts
                                                     && C24OracleFailurePolicy.IsTransient(exception.Number))
            {
                _logger.LogWarning("C24-DB-001 ReportCode=C24 Stage={Stage} OracleCode={OracleCode} Action=Retry RetryCount={RetryCount}",
                    stage, exception.Number, attempt);
            }
            catch (OracleException exception)
            {
                _logger.LogError("C24-DB-002 ReportCode=C24 Stage={Stage} OracleCode={OracleCode} ExceptionType={ExceptionType} Action=Fail RetryCount={RetryCount}",
                    stage, exception.Number, exception.GetType().Name, attempt - 1);
                throw;
            }
        }
    }

    private static C24CanonicalResult CreateCanonicalFromLegacy(
        SearchReportCondition condition, C24LegacyResult legacy)
    {
        var details = legacy.Details.Select(ToC24Detail)
            .Where(x => MatchesC24Filter(condition, x))
            .OrderBy(x => x.RoomCategory).ThenBy(x => x.MedicalRecordNo, StringComparer.Ordinal)
            .ThenBy(x => x.DepartmentCode, StringComparer.Ordinal)
            .ThenBy(x => x.ChargeItemCode, StringComparer.Ordinal).ThenBy(x => x.VisitDate)
            .ThenBy(x => x.SourceBusinessKey, StringComparer.Ordinal).ToList();
        IReadOnlyList<C24Summary> summaries = condition.RoomScope == C24RoomScopes.All
                                               && condition.MedicalRecordNo is null
            ? legacy.Summaries.GroupBy(x => RoomCategory(x.RoomType, condition.Source!))
                .OrderBy(x => x.Key)
                .Select(x => new C24Summary(x.Key, x.Sum(y => y.DebtAmount),
                    decimal.ToInt32(x.Sum(y => y.DebtCount)), x.Sum(y => y.PaymentAmount),
                    decimal.ToInt32(x.Sum(y => y.PaymentCount)), x.Sum(y => y.OutstandingAmount),
                    decimal.ToInt32(x.Sum(y => y.OutstandingCount)))).ToList()
            : C24DebtPaymentCalculationService.Summarize(details, C24Modes.Accounting).ToList();
        var id = Guid.NewGuid().ToString("N");
        return new C24CanonicalResult
        {
            Details = details, Summaries = summaries, LegacyDetails = legacy.Details,
            LegacySummaries = legacy.Summaries, RunId = id, CorrelationId = id
        };
    }

    private static C24DebtPaymentDetail ToC24Detail(C24LegacyDetailRow row)
    {
        var sourceKind = row.ChargeItemCode == "69" ? C24SourceKind.Acc69 : C24SourceKind.Ord;
        return new C24DebtPaymentDetail
        {
            AccountingDate = row.AccountingDate, VisitDate = row.VisitDate,
            RoomCategory = RoomCategory(row.RoomType, null), MedicalRecordNo = row.MedicalRecordNo,
            PatientName = row.PatientName, MaskedPhone = row.Phone, DepartmentCode = row.DepartmentCode,
            PayerClassCode = row.PayerClassCode, CardSequenceNo = row.CardSequenceNo,
            ChargeItemCode = row.ChargeItemCode, ChargeItemName = row.ChargeItemName,
            EventAmount = row.SignedAmount, DiscountAmount = row.SignedDiscount,
            AmountDue = row.AmountDue, CreatedBy = row.CreatedBy, SourceKind = sourceKind,
            SourceBusinessKey = string.Join('|', row.AccountingDate, row.VisitDate,
                row.MedicalRecordNo, row.ChargeItemCode, row.CreatedBy),
            LegacyRoomType = row.RoomType, LegacyDischargeFlag = row.DischargeFlag
        };
    }

    private static bool MatchesC24Filter(SearchReportCondition condition, C24DebtPaymentDetail row) =>
        (condition.RoomScope == C24RoomScopes.All
         || condition.RoomScope == C24RoomScopes.Emergency && row.RoomCategory == C24RoomCategory.Emergency
         || condition.RoomScope == C24RoomScopes.NonEmergency && row.RoomCategory != C24RoomCategory.Emergency)
        && (condition.MedicalRecordNo is null || condition.MedicalRecordNo == row.MedicalRecordNo);

    private static C24RoomCategory RoomCategory(string? roomType, string? source) =>
        source == C24Sources.Inpatient || roomType == "I" ? C24RoomCategory.Inpatient
        : roomType == "E" ? C24RoomCategory.Emergency : C24RoomCategory.Outpatient;

    internal static void ValidateLegacyPublication(C24CanonicalResult result)
    {
        if (result.LegacyDetails.Any(x => string.IsNullOrWhiteSpace(x.RoomType)
                                          || string.IsNullOrWhiteSpace(x.RoomTypeName)
                                          || string.IsNullOrWhiteSpace(x.MedicalRecordNo)
                                          || string.IsNullOrWhiteSpace(x.PatientName)))
            throw new C24LegacyPublicationException("C24 Legacy 明細必要欄位不完整。");

        var expected = C24DebtPaymentCalculationService.SummarizeLegacy(result.LegacyDetails)
            .OrderBy(x => x.AccountingDate).ThenBy(x => x.RoomType, StringComparer.Ordinal).ToList();
        var actual = result.LegacySummaries
            .OrderBy(x => x.AccountingDate).ThenBy(x => x.RoomType, StringComparer.Ordinal).ToList();
        if (!expected.SequenceEqual(actual))
            throw new C24LegacyPublicationException("C24 Legacy 明細與摘要不一致。");
    }

    private void PublishLegacyWithRetry(IC24DebtPaymentRepository repository, string source,
        DateOnly accountingDate, IReadOnlyList<C24LegacyDetailRow> details,
        IReadOnlyList<C24LegacySummaryRow> summaries)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                repository.PublishLegacy(source, accountingDate, details, summaries);
                return;
            }
            catch (OracleException exception) when (attempt < C24OracleFailurePolicy.MaximumAttempts
                                                     && C24OracleFailurePolicy.IsTransient(exception.Number))
            {
                _logger.LogWarning("C24-DB-003 ReportCode=C24 Stage=LegacyPublish OracleCode={OracleCode} Action=Retry RetryCount={RetryCount}",
                    exception.Number, attempt);
            }
            catch (OracleException exception)
            {
                _logger.LogError("C24-DB-004 ReportCode=C24 Stage=LegacyPublish OracleCode={OracleCode} ExceptionType={ExceptionType} Action=Fail RetryCount={RetryCount}",
                    exception.Number, exception.GetType().Name, attempt - 1);
                throw;
            }
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

public sealed class C24LegacyPublicationException(string message) : Exception(message);
