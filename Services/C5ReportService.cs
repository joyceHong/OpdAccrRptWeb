using System.Diagnostics;
using System.Globalization;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C5ReportService(IC5ReportRepository repository,
    IOrganizationUnitCodeService organizationUnitCodeService, IC5ReportResultCache resultCache,
    ILogger<C5ReportService> logger) : IC5ReportService
{
    public async Task<C5ReportResult> QueryAsync(C5ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();
        C5ValidatedRequest validated = request.Validate();
        IReadOnlyDictionary<string, string?> cacheFilters = CacheFilters(validated);
        if (validated.DetailType != C5DetailType.PatientDetail &&
            resultCache.TryGet(cacheFilters, out C5CachedResult cached))
        {
            LogPerformance(logger, true, 0, 0, 0, cached.Rows.Count, 0, 0,
                timer.ElapsedMilliseconds);
            C5ValidatedRequest cachedRequest = cached.Request with
            {
                PageNumber = validated.PageNumber,
                PageSize = validated.PageSize
            };
            return BuildResult(cachedRequest, cached.Rows, cached.QueryIds);
        }
        long mappingElapsedMs = 0;
        int mappingLookupCount = 0;
        if (validated.NewOrganizationUnitCode is not null)
        {
            long mappingStarted = Stopwatch.GetTimestamp();
            OrganizationUnitMapping? mapping = await organizationUnitCodeService.ResolveLegacyCodeAsync(
                validated.NewOrganizationUnitCode, true, cancellationToken);
            mappingElapsedMs += ElapsedMilliseconds(mappingStarted);
            mappingLookupCount++;
            if (mapping is null) throw new ArgumentException("請輸入正確科別代碼");
            validated = validated with { LegacySectionCode = mapping.LegacyCode };
        }
        var rows = new List<C5ReportRow>();
        var queryIds = new List<C5QueryId>();
        var outputMappings = new Dictionary<(string LegacyCode, string RoomType), OrganizationUnitMapping?>();
        long databaseElapsedMs = 0;
        long databaseStarted = Stopwatch.GetTimestamp();
        await using IC5ReportQuerySession session = await repository.OpenSessionAsync(cancellationToken);
        databaseElapsedMs += ElapsedMilliseconds(databaseStarted);
        for (DateOnly day = validated.Start; day <= validated.End; day = day.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            C5QueryId id = SelectQuery(validated);
            queryIds.Add(id);
            databaseStarted = Stopwatch.GetTimestamp();
            IReadOnlyList<C5SourceRow> sourceRows = await session.QueryDayAsync(
                validated, C5ReportRequest.ToRoc(day), id, cancellationToken);
            databaseElapsedMs += ElapsedMilliseconds(databaseStarted);
            foreach (C5SourceRow source in sourceRows)
            {
                var mappingKey = (source.LegacySectionCode.Trim().ToUpperInvariant(),
                    source.RoomType.Trim().ToUpperInvariant());
                if (!outputMappings.TryGetValue(mappingKey, out OrganizationUnitMapping? output))
                {
                    long mappingStarted = Stopwatch.GetTimestamp();
                    output = await organizationUnitCodeService.ResolveNewCodeAsync(mappingKey.Item1,
                        mappingKey.Item2, OrganizationUnitMappingScope.SectionOnly, cancellationToken);
                    mappingElapsedMs += ElapsedMilliseconds(mappingStarted);
                    mappingLookupCount++;
                    outputMappings.Add(mappingKey, output);
                }
                (decimal unitPrice, decimal amount) = C5AmountPolicy.Calculate(source,
                    validated.DataSource == C5DataSource.Outpatient && validated.ChargeKind == C5ChargeKind.Order
                    && validated.LegacySectionCode == "0430");
                bool detail = validated.DetailType == C5DetailType.PatientDetail;
                rows.Add(new(validated.DataSource == C5DataSource.Inpatient
                        ? validated.ChargeKind == C5ChargeKind.Drug ? "住院" : "住" + (validated.LegacySectionCode ?? string.Empty)[..Math.Min(4, (validated.LegacySectionCode ?? string.Empty).Length)]
                        : source.RoomType == "E" ? "急診" : "門診",
                    output?.NewCode ?? string.Empty, output?.DisplayName, source.ChargeCode,
                    source.ChargeName, source.ServiceDate, detail ? source.DoctorId : null,
                    detail ? source.DoctorName : null, detail ? source.MedicalRecordNo : null,
                    detail ? source.PatientName : null, source.Quantity, unitPrice, amount));
            }
        }
        if (validated.DetailType != C5DetailType.PatientDetail)
            resultCache.Set(cacheFilters, new(validated, rows, queryIds));
        LogPerformance(logger, false, queryIds.Count, queryIds.Count, mappingLookupCount,
            rows.Count, databaseElapsedMs, mappingElapsedMs, timer.ElapsedMilliseconds);
        return BuildResult(validated, rows, queryIds);
    }

    private static long ElapsedMilliseconds(long started) =>
        (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

    private static void LogPerformance(ILogger<C5ReportService> target, bool cacheHit,
        int daysQueried, int dailyQueryCount, int mappingLookupCount, int returnedRows,
        long databaseElapsedMs, long mappingElapsedMs, long totalElapsedMs) =>
        target.LogInformation("C5 performance completed. CacheHit={CacheHit} DaysQueried={DaysQueried} DailyQueryCount={DailyQueryCount} MappingLookupCount={MappingLookupCount} ReturnedRows={ReturnedRows} DatabaseElapsedMs={DatabaseElapsedMs} MappingElapsedMs={MappingElapsedMs} TotalElapsedMs={TotalElapsedMs}",
            cacheHit, daysQueried, dailyQueryCount, mappingLookupCount, returnedRows,
            databaseElapsedMs, mappingElapsedMs, totalElapsedMs);

    private static C5ReportResult BuildResult(C5ValidatedRequest validated,
        IReadOnlyList<C5ReportRow> rows, IReadOnlyList<C5QueryId> queryIds)
    {
        int total = rows.Count;
        var pageRows = rows.Skip((validated.PageNumber - 1) * validated.PageSize).Take(validated.PageSize)
            .Select(C5ChargeQuantityReportViewModel.From).ToList();
        var page = new ReportDataAndColumns<C5ChargeQuantityReportViewModel>
        {
            Columns = ModelDescriptionsHelper.GetPropertyDescriptions<C5ChargeQuantityReportViewModel>()
                .Where(x => validated.DetailType == C5DetailType.PatientDetail || x.Key is not "doctorId" and not "doctorName" and not "medicalRecordNo" and not "patientName").ToList(),
            Data = pageRows, TotalCount = total, PageNumber = validated.PageNumber,
            PageSize = validated.PageSize, TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)validated.PageSize)
        };
        return new(validated, rows, page, queryIds);
    }

    internal static C5QueryId SelectQuery(C5ValidatedRequest request)
    {
        bool detail = request.DetailType == C5DetailType.PatientDetail;
        if (request.DataSource == C5DataSource.Inpatient)
            return request.ChargeKind == C5ChargeKind.Drug
                ? detail ? C5QueryId.IpdDrugDetail : C5QueryId.IpdDrugAggregate
                : detail ? C5QueryId.IpdOrderDetail : C5QueryId.IpdOrderAggregate;
        if (request.ChargeKind == C5ChargeKind.Drug)
            return detail ? C5QueryId.OpdDrugDetail : C5QueryId.OpdDrugAggregate;
        if (request.LegacySectionCode == "0430")
            return detail ? C5QueryId.OpdOrder0430Detail : C5QueryId.OpdOrder0430Aggregate;
        return detail ? C5QueryId.OpdOrderDetail : C5QueryId.OpdOrderAggregate;
    }

    private static IReadOnlyDictionary<string, string?> CacheFilters(C5ValidatedRequest r) =>
        new Dictionary<string, string?> { ["StartDate"] = r.RocStartDate, ["EndDate"] = r.RocEndDate,
            ["Source"] = r.DataSource.ToString(), ["DetailType"] = ((int)r.DetailType).ToString(CultureInfo.InvariantCulture),
            ["EncounterType"] = ((int)r.EncounterType).ToString(CultureInfo.InvariantCulture), ["ChargeKind"] = r.ChargeKind.ToString(),
            ["NewSection"] = r.NewOrganizationUnitCode, ["LegacySection"] = r.LegacySectionCode,
            ["Room"] = r.RoomNo, ["Charge"] = r.ChargeCode, ["Identity"] = r.InsuranceIdentityCode };
}
