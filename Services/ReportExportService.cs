using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniExcelLibs;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class ReportExportService : IReportExportService, IReportWorkbookGenerator
{
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly HashSet<string> HealthReportCodes =
        ["C171", "C172", "C173", "C174", "C18", "C19", "C144"];

    private static readonly HashSet<string> OutpatientReportCodes =
        ["C1", "C21", "C22", "C23", "C24", "C25", "C27", "C28", "C29", "C143", "C213", "C214"];

    private readonly IReportExportJobStore _jobStore;
    private readonly IReportExportQueue _queue;
    private readonly ReportExportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IReadOnlyDictionary<string, IReportExportDefinition> _definitions;

    public ReportExportService(
        IHealthCenterRepository healthCenterRepository,
        IReportExportJobStore jobStore,
        IReportExportQueue queue,
        IOptions<ReportExportOptions> options,
        TimeProvider timeProvider,
        IServiceScopeFactory? scopeFactory = null,
        IReferralMemberRepository? referralMemberRepository = null,
        ISafeNeedleRepository? safeNeedleRepository = null,
        ISurgicalAccountingRepository? surgicalAccountingRepository = null,
        ICashierCashRepository? cashierCashRepository = null,
        ICashierCashSummaryRepository? cashierCashSummaryRepository = null,
        IInpatientAdvancePaymentBalanceRepository? inpatientAdvancePaymentBalanceRepository = null,
        IAssistiveDeviceDepositBalanceRepository? assistiveDeviceDepositBalanceRepository = null,
        IInpatientReceivableBalanceRepository? inpatientReceivableBalanceRepository = null,
        IContractPaymentDetailRepository? contractPaymentDetailRepository = null,
        IOutpatientReceivableBalanceRepository? outpatientReceivableBalanceRepository = null,
        IC23ContractAccountingRepository? c23ContractAccountingRepository = null,
        IC23RebuildService? c23RebuildService = null)
    {
        _jobStore = jobStore;
        _queue = queue;
        _options = options.Value;
        _timeProvider = timeProvider;
        _scopeFactory = scopeFactory;

        var definitions = new List<IReportExportDefinition>
        {
            new ReportExportDefinition<HealthCenterDetailViewModel>(
                "C171", _ => healthCenterRepository.GetHelthCenterDetailColumns(),
                healthCenterRepository.GetHealthCenterDataCount,
                healthCenterRepository.GetHealthCenterDataBatch),
            new ReportExportDefinition<HealthCenterCountViewModel>(
                "C172", _ => healthCenterRepository.GetHelthCenterCountColumns(),
                healthCenterRepository.GetHealthCenterCountDataCount,
                healthCenterRepository.GetHealthCenterCountDataBatch),
            new ReportExportDefinition<HealthCheckupVisits>(
                "C173", _ => healthCenterRepository.GetHealthCheckupVisitsColumns(),
                healthCenterRepository.GetHealthCheckupVisitsCount,
                healthCenterRepository.GetHealthCheckupVisitsBatch),
            new ReportExportDefinition<HealthCenterContractBillingReport>(
                "C174", _ => healthCenterRepository.GetHealthCenterContractBillingReportColumns(),
                healthCenterRepository.GetHealthCenterContractBillingReportCount,
                healthCenterRepository.GetHealthCenterContractBillingReportBatch)
        };

        if (surgicalAccountingRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<SurgicalAccountingReportViewModel>(
                "C1",
                _ => surgicalAccountingRepository.GetColumns(),
                condition => surgicalAccountingRepository.GetCount(ToRocDateCondition(condition)),
                (condition, offset, batchSize) => surgicalAccountingRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }
        if (cashierCashRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<CashierCashReportViewModel>(
                "C22",
                _ => cashierCashRepository.GetColumns(),
                condition => cashierCashRepository.GetCount(ToRocDateCondition(condition)),
                (condition, offset, batchSize) => cashierCashRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }
        if (c23ContractAccountingRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<C23ContractAccountingReportViewModel>(
                "C23",
                _ => c23ContractAccountingRepository.GetColumns(),
                condition =>
                {
                    c23RebuildService?.EnsureData(condition);
                    return c23ContractAccountingRepository.GetCount(ToRocDateCondition(condition));
                },
                (condition, offset, batchSize) => c23ContractAccountingRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }
        if (inpatientAdvancePaymentBalanceRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<InpatientAdvancePaymentBalanceReportViewModel>(
                "C25",
                _ => inpatientAdvancePaymentBalanceRepository.GetColumns(),
                condition => inpatientAdvancePaymentBalanceRepository.GetCount(ToRocDateCondition(condition)),
                (condition, offset, batchSize) => inpatientAdvancePaymentBalanceRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }
        if (assistiveDeviceDepositBalanceRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<AssistiveDeviceDepositBalanceReportViewModel>(
                "C27",
                _ => assistiveDeviceDepositBalanceRepository.GetColumns(),
                condition => assistiveDeviceDepositBalanceRepository.GetCount(ToRocDateCondition(condition)),
                (condition, offset, batchSize) => assistiveDeviceDepositBalanceRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }
        if (inpatientReceivableBalanceRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<InpatientReceivableBalanceReportViewModel>(
                "C28",
                _ => inpatientReceivableBalanceRepository.GetColumns(),
                condition => inpatientReceivableBalanceRepository.GetCount(ToRocDateCondition(condition)),
                (condition, offset, batchSize) => inpatientReceivableBalanceRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }
        if (contractPaymentDetailRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<ContractPaymentDetailReportViewModel>(
                "C29",
                _ => contractPaymentDetailRepository.GetColumns(),
                condition => contractPaymentDetailRepository.GetCount(ToRocDateCondition(condition)),
                (condition, offset, batchSize) => contractPaymentDetailRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }
        if (cashierCashSummaryRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<CashierCashSummaryReportViewModel>(
                "C213",
                _ => cashierCashSummaryRepository.GetColumns(),
                condition => cashierCashSummaryRepository.GetCount(ToRocDateCondition(condition)),
                (condition, offset, batchSize) => cashierCashSummaryRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }
        if (outpatientReceivableBalanceRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<OutpatientReceivableBalanceReportViewModel>(
                "C214",
                _ => outpatientReceivableBalanceRepository.GetColumns(),
                condition => outpatientReceivableBalanceRepository.GetCount(ToRocDateCondition(condition)),
                (condition, offset, batchSize) => outpatientReceivableBalanceRepository.GetPage(
                    ForBatch(ToRocDateCondition(condition), offset, batchSize))));
        }

        if (referralMemberRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<ReferralMemberReportViewModel>(
                "C18", _ => referralMemberRepository.GetColumns(),
                referralMemberRepository.GetCount, referralMemberRepository.GetBatch));
        }
        if (safeNeedleRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<SafeNeedleReportViewModel>(
                "C19", _ => safeNeedleRepository.GetColumns(),
                safeNeedleRepository.GetCount, safeNeedleRepository.GetBatch));
        }
        if (scopeFactory is not null)
        {
            definitions.Add(new ReportExportDefinition<C21AccountingSummaryReportViewModel>(
                "C21",
                _ => ModelDescriptionsHelper.GetPropertyDescriptions<C21AccountingSummaryReportViewModel>(),
                condition => WithReportService(scopeFactory, service =>
                    service.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(
                        ForBatch(condition, 0, 1)).TotalCount ?? 0),
                (condition, offset, batchSize) => WithReportService(scopeFactory, service =>
                    service.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(
                        ForBatch(condition, offset, batchSize)).Data ?? [])));

            definitions.Add(new ReportExportDefinition<C24DebtPaymentDetail>(
                "C24",
                _ => GetC24ExportColumns(),
                condition => WithReportService(scopeFactory, service =>
                    service.ReportDataAndColumns<C24DebtPaymentDetail>(
                        ForBatch(condition, 0, 1)).TotalCount ?? 0),
                (condition, offset, batchSize) => WithReportService(scopeFactory, service =>
                    service.ReportDataAndColumns<C24DebtPaymentDetail>(
                        ForBatch(condition, offset, batchSize)).Data ?? [])));

            definitions.Add(new ReportExportDefinition<C143AccountingBalanceDebtReportViewModel>(
                "C143",
                condition => C143AccountingBalanceDebtReportService.GetColumns(condition.Source!),
                condition => GetC143Count(scopeFactory, condition),
                (condition, offset, batchSize) => GetC143Batch(
                    scopeFactory, condition, offset, batchSize)));

            definitions.Add(new ReportExportDefinition<C144DebtDetailReportViewModel>(
                "C144",
                _ => C144DebtDetailReportService.GetColumns(),
                condition => WithC144Repository(scopeFactory, condition,
                    static (repository, query) => repository.GetCount(query)),
                (condition, offset, batchSize) => WithC144Repository(scopeFactory, condition,
                    (repository, query) => repository.GetBatch(query, offset, batchSize))));
        }
        _definitions = definitions.ToDictionary(definition => definition.ReportCode, StringComparer.Ordinal);
    }

    public ReportExportDispatchResult Dispatch(SearchReportCondition searchCondition)
    {
        if (searchCondition.ReportCode == "C10")
        {
            using var stream = new MemoryStream();
            GenerateC10Workbook(searchCondition, stream);
            return new ReportExportDispatchResult(
                stream.ToArray(), CreateFileName("C10", _timeProvider.GetUtcNow()), null);
        }

        SearchReportCondition normalized = searchCondition.ReportCode == "C144"
            ? NormalizeC144(searchCondition)
            : HealthReportCodes.Contains(searchCondition.ReportCode ?? string.Empty)
                ? NormalizeHealthReport(searchCondition)
                : NormalizeOutpatientReport(searchCondition);
        IReportExportDefinition definition = ResolveDefinition(normalized.ReportCode);
        int totalCount = definition.GetCount(normalized);
        if (totalCount <= _options.SynchronousRowLimit)
        {
            using var stream = new MemoryStream();
            GenerateWorkbook(normalized, stream);
            return new ReportExportDispatchResult(
                stream.ToArray(), CreateFileName(normalized.ReportCode!, _timeProvider.GetUtcNow()), null);
        }

        ReportExportJob job = _jobStore.Create(normalized);
        if (!_queue.TryEnqueue(job))
        {
            _jobStore.Remove(job.JobId);
            return new ReportExportDispatchResult(null, null, null, QueueFull: true);
        }
        return new ReportExportDispatchResult(null, null, job);
    }

    public ReportExportJob? GetJob(Guid jobId) => _jobStore.Get(jobId);

    public ReportExportDownloadResult GetDownload(Guid jobId)
    {
        ReportExportJob? job = _jobStore.Get(jobId);
        if (job?.Status != ReportExportJobStatus.Ready)
        {
            return new ReportExportDownloadResult(job, null);
        }
        string path = _jobStore.GetCompletedPath(job);
        return File.Exists(path)
            ? new ReportExportDownloadResult(job, new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            : new ReportExportDownloadResult(job, null);
    }

    public void GenerateWorkbook(SearchReportCondition searchCondition, Stream destination)
    {
        if (searchCondition.ReportCode == "C10")
        {
            GenerateC10Workbook(searchCondition, destination);
            return;
        }

        IReportExportDefinition definition = ResolveDefinition(searchCondition.ReportCode);
        IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata> columns = definition.GetColumns(searchCondition);
        destination.SaveAs(
            ProjectRows(definition.ReadRows(searchCondition, _options.BatchSize), columns),
            sheetName: definition.ReportCode);
    }

    private void GenerateC10Workbook(SearchReportCondition condition, Stream destination)
    {
        if (_scopeFactory is null)
        {
            throw new InvalidOperationException("C10 匯出服務尚未設定 scope factory。");
        }
        SearchReportCondition normalized = NormalizeC10(condition);
        using IServiceScope scope = _scopeFactory.CreateScope();
        IReportService reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        ReportDataAndColumns<C10ReceivableDetailRow> result = reportService.ReportC10Async(normalized)
            .GetAwaiter().GetResult();
        destination.SaveAs(ProjectRows(result.Data ?? [], result.Columns ?? []), sheetName: "C10");
    }

    private IReportExportDefinition ResolveDefinition(string? reportCode)
    {
        if (reportCode is null || !_definitions.TryGetValue(reportCode, out IReportExportDefinition? definition))
        {
            throw new ArgumentException("不支援此報表的 Excel 匯出。", nameof(reportCode));
        }
        return definition;
    }

    internal static SearchReportCondition NormalizeHealthReport(SearchReportCondition condition)
    {
        if (condition.ReportCode is null || !HealthReportCodes.Contains(condition.ReportCode))
        {
            throw new ArgumentException("不支援此報表的 Excel 匯出。", nameof(condition));
        }
        if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly startDate)
            || !DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly endDate)
            || startDate > endDate)
        {
            throw new ArgumentException("請輸入有效的起始日期與截止日期。", nameof(condition));
        }
        if (condition.ReportCode == "C18"
            && (!EncounterSources.IsSupported(condition.EncounterSource) || startDate.Year != endDate.Year))
        {
            throw new ArgumentException("C18 匯出條件不正確。", nameof(condition));
        }
        if (condition.ReportCode == "C19"
            && (!EncounterSources.IsSupported(condition.EncounterSource) || startDate != endDate))
        {
            throw new ArgumentException("C19 匯出條件不正確。", nameof(condition));
        }
        return new SearchReportCondition
        {
            ReportCode = condition.ReportCode,
            StartDate = DateTimeExtensions.ToRocDateString(startDate.ToDateTime(TimeOnly.MinValue)),
            EndDate = DateTimeExtensions.ToRocDateString(endDate.ToDateTime(TimeOnly.MinValue)),
            EncounterSource = condition.EncounterSource,
            StationOrBedPrefix = string.IsNullOrWhiteSpace(condition.StationOrBedPrefix)
                ? null
                : condition.StationOrBedPrefix.Trim()
        };
    }

    internal static SearchReportCondition NormalizeC144(SearchReportCondition condition)
    {
        _ = C144DebtDetailReportService.CreateQuery(condition, allowUnpaged: true);
        return new SearchReportCondition
        {
            ReportCode = "C144", StartDate = condition.StartDate, EndDate = condition.EndDate,
            Source = condition.Source, PageNumber = 1, PageSize = 10
        };
    }

    internal static SearchReportCondition NormalizeOutpatientReport(SearchReportCondition condition)
    {
        if (condition.ReportCode is null || !OutpatientReportCodes.Contains(condition.ReportCode))
        {
            throw new ArgumentException("不支援此報表的 Excel 匯出。", nameof(condition));
        }

        if (!DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly endDate))
        {
            throw new ArgumentException("請輸入有效的起始日期與截止日期。", nameof(condition));
        }

        bool endDateOnly = condition.ReportCode is "C27" or "C28" or "C214";
        if (!endDateOnly)
        {
            if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateOnly startDate)
                || startDate > endDate)
            {
                throw new ArgumentException("請輸入有效的起始日期與截止日期。", nameof(condition));
            }
        }

        return CopyCondition(condition, pageNumber: 1, pageSize: 10);
    }

    private static TResult WithReportService<TResult>(
        IServiceScopeFactory scopeFactory,
        Func<IReportService, TResult> action)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<IReportService>());
    }

    private static IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata> GetC24ExportColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<C24DebtPaymentDetail>()
            .Where(column => column.Key is not "sourceBusinessKey"
                and not "legacyRoomType"
                and not "legacyDischargeFlag")
            .ToList();

    private static int GetC143Count(
        IServiceScopeFactory scopeFactory, SearchReportCondition condition)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IC143AccountingBalanceDebtRepository repository =
            scope.ServiceProvider.GetRequiredService<IC143AccountingBalanceDebtRepository>();
        C143Query query = C143AccountingBalanceDebtReportService.CreateQuery(
            condition, allowExportBatch: true);
        if (query.Source == C143Sources.OpdEr)
        {
            return repository.GetOutpatientEmergencyCount(query);
        }

        int dischargedCount = repository.GetInpatientCount(query, 1);
        return query.ReportType == C143ReportTypes.All
            ? dischargedCount + repository.GetInpatientCount(query, 2)
            : dischargedCount;
    }

    private static IReadOnlyList<C143AccountingBalanceDebtReportViewModel> GetC143Batch(
        IServiceScopeFactory scopeFactory,
        SearchReportCondition condition,
        int offset,
        int batchSize)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IC143AccountingBalanceDebtRepository repository =
            scope.ServiceProvider.GetRequiredService<IC143AccountingBalanceDebtRepository>();
        C143Query query = C143AccountingBalanceDebtReportService.CreateQuery(
            ForBatch(condition, offset, batchSize), allowExportBatch: true);
        if (query.Source == C143Sources.OpdEr)
        {
            return repository.GetOutpatientEmergencyPage(query, offset, batchSize);
        }

        int dischargedCount = repository.GetInpatientCount(query, 1);
        var rows = new List<C143AccountingBalanceDebtReportViewModel>(batchSize);
        if (offset < dischargedCount)
        {
            rows.AddRange(repository.GetInpatientPage(
                query, 1, offset, Math.Min(batchSize, dischargedCount - offset)));
        }

        int remaining = batchSize - rows.Count;
        if (query.ReportType == C143ReportTypes.All && remaining > 0)
        {
            int inHospitalOffset = Math.Max(0, offset - dischargedCount);
            rows.AddRange(repository.GetInpatientPage(
                query, 2, inHospitalOffset, remaining));
        }

        return rows;
    }

    private static TResult WithC144Repository<TResult>(
        IServiceScopeFactory scopeFactory,
        SearchReportCondition condition,
        Func<IC144DebtDetailReportRepository, C144Query, TResult> action)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IC144DebtDetailReportRepository repository =
            scope.ServiceProvider.GetRequiredService<IC144DebtDetailReportRepository>();
        C144Query query = C144DebtDetailReportService.CreateQuery(condition, allowUnpaged: true);
        return action(repository, query);
    }

    private static SearchReportCondition ToRocDateCondition(SearchReportCondition condition) =>
        CopyCondition(
            condition,
            startDate: ToRocDateIfIso(condition.StartDate),
            endDate: ToRocDateIfIso(condition.EndDate));

    private static SearchReportCondition ForBatch(
        SearchReportCondition condition, int offset, int batchSize)
    {
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        return CopyCondition(condition, pageNumber: offset / batchSize + 1, pageSize: batchSize);
    }

    private static string? ToRocDateIfIso(string? value)
    {
        if (value is null) return null;
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateOnly date)
            ? DateTimeExtensions.ToRocDateString(date.ToDateTime(TimeOnly.MinValue))
            : value;
    }

    private static SearchReportCondition CopyCondition(
        SearchReportCondition source,
        string? startDate = null,
        string? endDate = null,
        int? pageNumber = null,
        int? pageSize = null) => new()
        {
            ReportCode = source.ReportCode,
            StartDate = startDate ?? source.StartDate,
            EndDate = endDate ?? source.EndDate,
            EncounterSource = source.EncounterSource,
            StationOrBedPrefix = source.StationOrBedPrefix,
            CashierUserId = source.CashierUserId,
            CashierCashSortType = source.CashierCashSortType,
            BillingCode = source.BillingCode,
            AccountingScope = source.AccountingScope,
            ForceRebuild = source.ForceRebuild,
            DateMode = source.DateMode,
            InpatientType = source.InpatientType,
            ContractCode = source.ContractCode,
            Source = source.Source,
            Mode = source.Mode,
            ReportType = source.ReportType,
            DetailType = source.DetailType,
            LogisticsType = source.LogisticsType,
            DepartmentCode = source.DepartmentCode,
            RoomCodes = source.RoomCodes,
            ChargeCodes = source.ChargeCodes,
            RoomScope = source.RoomScope,
            MedicalRecordNo = source.MedicalRecordNo,
            NewSectionCode = source.NewSectionCode,
            ReceivableBalanceType = source.ReceivableBalanceType,
            Chop1sec = source.Chop1sec,
            PageNumber = pageNumber ?? source.PageNumber,
            PageSize = pageSize ?? source.PageSize
        };

    internal static SearchReportCondition NormalizeC10(SearchReportCondition condition)
    {
        if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start)
            || !DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end)
            || start > end || !C10Sources.IsSupported(condition.Source)
            || !C10RoomScopes.IsSupported(condition.RoomScope)
            || condition.Source == C10Sources.Inpatient && condition.RoomScope != C10RoomScopes.All)
        {
            throw new ArgumentException("C10 匯出條件不正確。", nameof(condition));
        }
        return new SearchReportCondition
        {
            ReportCode = "C10", StartDate = condition.StartDate, EndDate = condition.EndDate,
            Source = condition.Source, RoomScope = condition.RoomScope,
            MedicalRecordNo = string.IsNullOrWhiteSpace(condition.MedicalRecordNo)
                ? null : condition.MedicalRecordNo.Trim().ToUpperInvariant(),
            PageNumber = 1, PageSize = int.MaxValue
        };
    }

    internal static string CreateFileName(string reportCode, DateTimeOffset timestamp) =>
        $"{reportCode}_{timestamp:yyyyMMdd_HHmmss}.xlsx";

    internal static string CreateFileName(DateTimeOffset timestamp) => CreateFileName("C174", timestamp);

    private static IEnumerable<IDictionary<string, object?>> ProjectRows<T>(
        IEnumerable<T> rows,
        IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata> columns)
    {
        Dictionary<string, PropertyInfo> properties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => char.ToLowerInvariant(property.Name[0]) + property.Name[1..],
                StringComparer.OrdinalIgnoreCase);
        foreach (T row in rows)
        {
            var projected = new Dictionary<string, object?>(columns.Count, StringComparer.Ordinal);
            foreach (ModelDescriptionsHelper.PropertyMetadata column in columns)
            {
                projected[column.Label] = row is null
                    ? string.Empty
                    : ToExportValue(properties[column.Key].GetValue(row));
            }
            yield return projected;
        }
    }

    private static IEnumerable<IDictionary<string, object?>> ProjectRows(
        IEnumerable<object> rows,
        IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata> columns)
    {
        Dictionary<Type, Dictionary<string, PropertyInfo>> propertyCache = [];
        foreach (object row in rows)
        {
            Type type = row.GetType();
            if (!propertyCache.TryGetValue(type, out Dictionary<string, PropertyInfo>? properties))
            {
                properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .ToDictionary(property => char.ToLowerInvariant(property.Name[0]) + property.Name[1..],
                        StringComparer.OrdinalIgnoreCase);
                propertyCache[type] = properties;
            }
            var projected = new Dictionary<string, object?>(columns.Count, StringComparer.Ordinal);
            foreach (ModelDescriptionsHelper.PropertyMetadata column in columns)
            {
                projected[column.Label] = ToExportValue(properties[column.Key].GetValue(row));
            }
            yield return projected;
        }
    }

    private static object ToExportValue(object? value) => value ?? string.Empty;
}
