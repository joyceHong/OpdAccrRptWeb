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
        ISafeNeedleRepository? safeNeedleRepository = null)
    {
        _jobStore = jobStore;
        _queue = queue;
        _options = options.Value;
        _timeProvider = timeProvider;
        _scopeFactory = scopeFactory;

        var definitions = new List<IReportExportDefinition>
        {
            new ReportExportDefinition<HealthCenterDetailViewModel>(
                "C171", healthCenterRepository.GetHelthCenterDetailColumns,
                healthCenterRepository.GetHealthCenterDataCount,
                healthCenterRepository.GetHealthCenterDataBatch),
            new ReportExportDefinition<HealthCenterCountViewModel>(
                "C172", healthCenterRepository.GetHelthCenterCountColumns,
                healthCenterRepository.GetHealthCenterCountDataCount,
                healthCenterRepository.GetHealthCenterCountDataBatch),
            new ReportExportDefinition<HealthCheckupVisits>(
                "C173", healthCenterRepository.GetHealthCheckupVisitsColumns,
                healthCenterRepository.GetHealthCheckupVisitsCount,
                healthCenterRepository.GetHealthCheckupVisitsBatch),
            new ReportExportDefinition<HealthCenterContractBillingReport>(
                "C174", healthCenterRepository.GetHealthCenterContractBillingReportColumns,
                healthCenterRepository.GetHealthCenterContractBillingReportCount,
                healthCenterRepository.GetHealthCenterContractBillingReportBatch)
        };

        if (referralMemberRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<ReferralMemberReportViewModel>(
                "C18", referralMemberRepository.GetColumns,
                referralMemberRepository.GetCount, referralMemberRepository.GetBatch));
        }
        if (safeNeedleRepository is not null)
        {
            definitions.Add(new ReportExportDefinition<SafeNeedleReportViewModel>(
                "C19", safeNeedleRepository.GetColumns,
                safeNeedleRepository.GetCount, safeNeedleRepository.GetBatch));
        }
        if (scopeFactory is not null)
        {
            definitions.Add(new ReportExportDefinition<C144DebtDetailReportViewModel>(
                "C144",
                C144DebtDetailReportService.GetColumns,
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
            : NormalizeHealthReport(searchCondition);
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
        IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata> columns = definition.GetColumns();
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
                projected[column.Label] = row is null ? null : properties[column.Key].GetValue(row);
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
                projected[column.Label] = properties[column.Key].GetValue(row);
            }
            yield return projected;
        }
    }
}
