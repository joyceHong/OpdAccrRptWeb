using System.Diagnostics;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C4MaterialReportService(IC4MaterialReportRepository repository,
    IOrganizationUnitCodeService organizationUnitCodeService,
    IReportTotalCountCache totalCountCache, IC4TransientFailurePolicy failurePolicy,
    TimeProvider timeProvider, ILogger<C4MaterialReportService> logger,
    IHttpContextAccessor? httpContextAccessor = null) : IC4MaterialReportService
{
    public async Task<C4MaterialReportResult> QueryAsync(C4MaterialReportRequest request,
        CancellationToken cancellationToken = default)
    {
        C4ValidatedRequest validated = request.Validate();
        var stopwatch = Stopwatch.StartNew();
        var rows = new List<C4MaterialReportRow>();
        var mappings = new Dictionary<(string Code, string Room), string>();
        try
        {
            for (DateOnly day = validated.Start; day <= validated.End; day = day.AddDays(1))
            {
                IReadOnlyList<C4MaterialSourceRow> daily = await QueryWithRetryAsync(
                    C4MaterialReportRequest.ToRoc(day), validated.SectionPrefix, cancellationToken);
                foreach (C4MaterialSourceRow source in daily)
                {
                    string legacy = source.LegacySectionCode?.Trim().ToUpperInvariant() ?? string.Empty;
                    string room = source.RoomType.Trim().ToUpperInvariant();
                    var key = (legacy, room);
                    if (!mappings.TryGetValue(key, out string? newCode))
                    {
                        OrganizationUnitMapping? mapping = await organizationUnitCodeService.ResolveNewCodeAsync(
                            legacy, room, OrganizationUnitMappingScope.SectionAndPlace, cancellationToken);
                        newCode = mapping?.NewCode?.Trim() ?? string.Empty;
                        mappings[key] = newCode;
                    }
                    rows.Add(ToReportRow(rows.Count + 1, source, newCode));
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "C4 query failed. UserId={UserId} StartDate={StartDate} EndDate={EndDate} SectionPrefix={SectionPrefix} CorrelationId={CorrelationId} DurationMs={DurationMs}",
                UserId(), validated.Start, validated.End, validated.SectionPrefix, CorrelationId(), stopwatch.ElapsedMilliseconds);
            throw;
        }
        int total = totalCountCache.GetOrCreate("C4", CacheFilters(validated), () => rows.Count);
        List<C4MaterialReportViewModel> pageRows = rows.Skip((validated.PageNumber - 1) * validated.PageSize)
            .Take(validated.PageSize).Select(C4MaterialReportViewModel.From).ToList();
        logger.LogInformation(
            "C4 query completed. UserId={UserId} StartDate={StartDate} EndDate={EndDate} SectionPrefix={SectionPrefix} Count={Count} CorrelationId={CorrelationId} DurationMs={DurationMs}",
            UserId(), validated.Start, validated.End, validated.SectionPrefix, rows.Count,
            CorrelationId(), stopwatch.ElapsedMilliseconds);
        return new(validated, rows, new()
        {
            Columns = ModelDescriptionsHelper.GetPropertyDescriptions<C4MaterialReportViewModel>(),
            Data = pageRows, TotalCount = total, PageNumber = validated.PageNumber,
            PageSize = validated.PageSize,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)validated.PageSize)
        });
    }

    public async Task<C4MaterialPreviewViewModel?> CreatePreviewAsync(C4MaterialReportRequest request,
        string userId, CancellationToken cancellationToken = default)
    {
        C4MaterialReportResult result = await QueryAsync(request with { PageNumber = 1, PageSize = 50 },
            cancellationToken);
        if (result.AllRows.Count == 0) return null;
        DateTimeOffset now = timeProvider.GetLocalNow();
        return new(new(FormatRoc(result.Request.RocStartDate), FormatRoc(result.Request.RocEndDate),
            userId, $"{now.Year - 1911:000}/{now:MM/dd  HH:mm:ss}"), result.AllRows);
    }

    internal static C4MaterialReportRow ToReportRow(int id, C4MaterialSourceRow row, string sectionCode) =>
        new(id, row.Detail?.Trim() == "1" ? "新品" : "舊品", row.MaterialCode?.Trim(),
            row.ClaimCode?.Trim(), row.OrderName?.Trim(), row.Unit?.Trim(), row.ChargeCode?.Trim(),
            row.Total, sectionCode);

    private async Task<IReadOnlyList<C4MaterialSourceRow>> QueryWithRetryAsync(string runDate,
        string? sectionPrefix, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                IReadOnlyList<C4MaterialSourceRow> rows = await repository.QueryDayAsync(
                    runDate, sectionPrefix, cancellationToken);
                return rows.ToArray();
            }
            catch (Exception exception) when (attempt < failurePolicy.MaxAttempts
                && failurePolicy.IsTransient(exception))
            {
                logger.LogWarning(exception, "C4 transient query failure. RunDate={RunDate} Attempt={Attempt}",
                    runDate, attempt);
            }
        }
    }

    private static IReadOnlyDictionary<string, string?> CacheFilters(C4ValidatedRequest request) =>
        new Dictionary<string, string?> { ["StartDate"] = request.Start.ToString("yyyy-MM-dd"),
            ["EndDate"] = request.End.ToString("yyyy-MM-dd"), ["SectionPrefix"] = request.SectionPrefix };
    private static string FormatRoc(string value) => $"{value[..3]}/{value.Substring(3, 2)}/{value.Substring(5, 2)}";
    private string UserId() => httpContextAccessor?.HttpContext?.User.Identity?.Name ?? "anonymous";
    private string CorrelationId() => httpContextAccessor?.HttpContext?.TraceIdentifier ?? string.Empty;
}
