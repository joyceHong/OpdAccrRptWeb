using System.Diagnostics;
using System.Globalization;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C3ReportService(
    IC3ReportRepository repository,
    DepartmentFilterResolver filterResolver,
    IOrganizationUnitCodeService organizationUnitCodeService,
    DepartmentAssignmentService assignmentService,
    IReportTotalCountCache totalCountCache,
    ILogger<C3ReportService> logger,
    IHttpContextAccessor? httpContextAccessor = null) : IC3ReportService
{
    public async Task<C3ReportResult> QueryAsync(C3ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        C3ValidatedRequest validated = request.Validate();
        DepartmentFilterMode mode = DepartmentFilterMode.None;
        if (validated.DepartmentCode is not null)
        {
            OrganizationUnitMapping? mapping = await organizationUnitCodeService.ResolveLegacyCodeAsync(
                validated.DepartmentCode, activePlaceOnly: true, cancellationToken);
            if (mapping is null) throw new ArgumentException("請輸入正確科別代碼");
            validated = validated with { DepartmentCode = mapping.LegacyCode };
        }
        if (validated.Source == CareSource.O)
        {
            mode = await filterResolver.ResolveAsync(validated.DepartmentCode, cancellationToken);
        }
        var stopwatch = Stopwatch.StartNew();
        var rows = new List<C3ReportRow>();
        try
        {
            for (DateOnly day = validated.Start; day <= validated.End; day = day.AddDays(1))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string runDate = C3ReportRequest.ToRoc(day);
                IReadOnlyList<C3MovementRow> movements = await repository.QueryDayAsync(
                    validated, runDate, mode, cancellationToken);
                foreach (C3MovementRow movement in movements)
                {
                    DepartmentAssignment assignment = await assignmentService.AssignAsync(
                        validated.Source, movement, cancellationToken);
                    rows.Add(ToReportRow(movement, assignment, validated.DetailType));
                }
                logger.LogInformation("C3 day completed. RunDate={RunDate} Count={Count} DurationMs={DurationMs}",
                    runDate, movements.Count, stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError("C3 query failed. ErrorCategory={ErrorCategory} DurationMs={DurationMs}",
                exception.GetType().Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (validated.DetailType == ReportDetailType.Summary)
        {
            rows = AggregateSummaryRows(rows);
        }
        int total = totalCountCache.GetOrCreate("C3", CacheFilters(validated), () => rows.Count);
        logger.LogInformation(
            "C3 query completed. CorrelationId={CorrelationId} UserId={UserId} Source={Source} DetailType={DetailType} LogisticsType={LogisticsType} Count={Count} DurationMs={DurationMs}",
            httpContextAccessor?.HttpContext?.TraceIdentifier ?? Activity.Current?.TraceId.ToString() ?? string.Empty,
            httpContextAccessor?.HttpContext?.User.Identity?.Name ?? string.Empty,
            validated.Source, validated.DetailType, validated.LogisticsType, total, stopwatch.ElapsedMilliseconds);
        List<C3ReportViewModel> pageRows = rows
            .Skip((validated.PageNumber - 1) * validated.PageSize).Take(validated.PageSize)
            .Select(C3ReportViewModel.From).ToList();
        var page = new ReportDataAndColumns<C3ReportViewModel>
        {
            Columns = ModelDescriptionsHelper.GetPropertyDescriptions<C3ReportViewModel>()
                .Where(x => validated.DetailType == ReportDetailType.Detail ||
                    x.Key is not "dctNo" and not "mrNo" and not "pName" and not "op1Date" and not "drName" and not "sPay")
                .ToList(),
            Data = pageRows, TotalCount = total, PageNumber = validated.PageNumber,
            PageSize = validated.PageSize,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)validated.PageSize)
        };
        return new C3ReportResult(validated, rows, page);
    }

    public async Task<C3PreviewViewModel?> CreatePreviewAsync(C3ReportRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        C3ReportRequest allRequest = request with { PageNumber = 1, PageSize = 50 };
        C3ReportResult first = await QueryAsync(allRequest, cancellationToken);
        var rows = new List<C3ReportRow>(first.AllRows);
        if (rows.Count == 0) return null;
        C3ValidatedRequest validated = first.Request;
        return new C3PreviewViewModel(Title(validated), FormatRoc(validated.RocStartDate),
            FormatRoc(validated.RocEndDate), userId, CurrentRocDateTime(), validated.DetailType, rows);
    }

    internal static C3ReportRow ToReportRow(C3MovementRow movement, DepartmentAssignment assignment,
        ReportDetailType detailType)
    {
        bool detail = detailType == ReportDetailType.Detail;
        return new C3ReportRow(assignment.Diagnose, assignment.Dispensary, assignment.Section,
            movement.ChargeCode, movement.MaterialCode, movement.MaterialName,
            movement.InventoryFlag switch { "0" => "物流", "1" => "AK庫", "2" => "AN庫", _ => string.Empty },
            movement.SignedQuantity, detail ? movement.DctNo : null, detail ? movement.MrNo : null,
            detail && movement.Project?.Trim() == "I" ? movement.PatientName + "(轉住)" : detail ? movement.PatientName : null,
            detail ? movement.RunDate : null, detail ? movement.DoctorName : null, detail ? movement.SelfPay : null);
    }

    internal static List<C3ReportRow> AggregateSummaryRows(IEnumerable<C3ReportRow> rows) => rows
        .GroupBy(row => new
        {
            row.Diagnose,
            row.Dispensary,
            row.Section,
            row.ChargeCode,
            row.MaterialCode,
            row.MaterialName,
            row.InventoryType
        })
        .Select(group => group.First() with { TotalSum = group.Sum(row => row.TotalSum) })
        .ToList();

    private static IReadOnlyDictionary<string, string?> CacheFilters(C3ValidatedRequest request) =>
        new Dictionary<string, string?>
        {
            ["StartDate"] = request.RocStartDate, ["EndDate"] = request.RocEndDate,
            ["Source"] = request.Source.ToString(), ["DetailType"] = ((int)request.DetailType).ToString(CultureInfo.InvariantCulture),
            ["LogisticsType"] = ((int)request.LogisticsType).ToString(CultureInfo.InvariantCulture),
            ["DepartmentCode"] = request.DepartmentCode,
            ["RoomCodes"] = string.Join(',', request.RoomCodes.Order(StringComparer.Ordinal)),
            ["ChargeCodes"] = string.Join(',', request.ChargeCodes.Order(StringComparer.Ordinal))
        };

    internal static string Title(C3ValidatedRequest request) => "亞東紀念醫院"
        + (request.Source == CareSource.I ? "住院" : "門急診") + "各護理站計價品"
        + (request.DetailType == ReportDetailType.Detail ? "明細表" : "彙總表")
        + request.LogisticsType switch { LogisticsType.Logistics => "__物流", LogisticsType.NonLogistics => "__非物流", _ => string.Empty };

    private static string FormatRoc(string value) => $"{value[..3]}/{value.Substring(3, 2)}/{value.Substring(5, 2)}";
    private static string CurrentRocDateTime()
    {
        DateTime now = DateTime.Now;
        return $"{now.Year - 1911:000}/{now:MM/dd HH:mm:ss}";
    }
}
