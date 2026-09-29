using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class M1DoctorDailyReportService(
    IM1DoctorDailyReportRepository repository,
    ISectionMappingRepository sectionMappings,
    IM1ReportRunStore runStore,
    TimeProvider timeProvider) : IM1DoctorDailyReportService
{
    private static readonly TimeZoneInfo TaipeiTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    internal static readonly IReadOnlyList<ReportColumn> Columns =
    [
        new("sectionNo", "科別代碼"), new("sectionName", "科別名稱"),
        new("doctorNo", "醫師代碼"), new("doctorName", "醫師姓名"),
        new("visitType", "門急診"), new("selfPayCount", "自費"),
        new("insuranceCount", "健保"), new("morningCount", "上午"),
        new("afternoonCount", "下午"), new("nightCount", "夜間"),
        new("appointmentCount", "預約量"), new("totalCount", "合計")
    ];

    public async Task<M1DoctorDailyPagedResponse> QueryAsync(
        M1DoctorDailyReportRequest request,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(request.PageNumber, request.PageSize);
        M1DoctorDailyReportSnapshot? snapshot;
        if (!string.IsNullOrWhiteSpace(request.RunId))
        {
            if (!runStore.TryGet(request.RunId, actor, out snapshot!))
                throw new M1ReportRunNotFoundException();
        }
        else
        {
            snapshot = await GenerateAsync(request, actor, cancellationToken);
        }

        if (snapshot is null)
            return new(null, [], Columns, 0, 1, request.PageSize, 0);

        int totalPages = (int)Math.Ceiling(snapshot.Rows.Count / (double)request.PageSize);
        int pageNumber = Math.Min(request.PageNumber, Math.Max(1, totalPages));
        M1DoctorDailyReportRow[] page = snapshot.Rows
            .Skip((pageNumber - 1) * request.PageSize).Take(request.PageSize).ToArray();
        return new(snapshot.RunId, page, Columns, snapshot.Rows.Count,
            pageNumber, request.PageSize, totalPages);
    }

    public async Task<M1DoctorDailyReportSnapshot?> GenerateAsync(
        M1DoctorDailyReportRequest request,
        string actor,
        CancellationToken cancellationToken = default)
    {
        DateOnly reportDate = ValidateDate(request);
        IReadOnlyList<M1DoctorDailyAggregateRow> source = await repository.QueryAsync(
            ToRocDate(reportDate), cancellationToken);
        IReadOnlyList<M1DoctorDailyReportRow> rows = await TransformAsync(source, cancellationToken);
        if (rows.Count == 0) return null;
        string runId = runStore.Save(actor, reportDate, rows);
        return runStore.TryGet(runId, actor, out M1DoctorDailyReportSnapshot snapshot)
            ? snapshot
            : throw new InvalidOperationException("M1 快照建立失敗。");
    }

    public bool TryGetRun(string runId, string actor, out M1DoctorDailyReportSnapshot snapshot) =>
        runStore.TryGet(runId, actor, out snapshot);

    internal async Task<IReadOnlyList<M1DoctorDailyReportRow>> TransformAsync(
        IReadOnlyList<M1DoctorDailyAggregateRow> source,
        CancellationToken cancellationToken)
    {
        var rows = new List<M1DoctorDailyReportRow>();
        var translations = new Dictionary<(string, string), SectionMapping>();
        foreach (M1DoctorDailyAggregateRow item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.S1 + item.S2 != 0)
            {
                SectionMapping section = await TranslateAsync(item.SectionNo, string.Empty);
                rows.Add(M1DoctorDailyReportRow.Create(section.NewCode, Name(section, item),
                    item.DoctorNo, item.DoctorName, "R", item.S1, item.S2,
                    item.S3, item.S4, item.S5, item.S6));
            }
            if (item.S7 + item.S8 != 0)
            {
                SectionMapping section = await TranslateAsync(item.SectionNo, "E");
                rows.Add(M1DoctorDailyReportRow.Create(section.NewCode, Name(section, item),
                    item.DoctorNo, item.DoctorName, "E", item.S7, item.S8,
                    item.S9, item.S10, item.S11, 0));
            }
        }

        return Merge0420Into0450(rows)
            .OrderBy(row => row.SectionNo, StringComparer.Ordinal)
            .ThenBy(row => row.DoctorNo, StringComparer.Ordinal)
            .ThenBy(row => row.VisitType, StringComparer.Ordinal)
            .ToArray();

        async Task<SectionMapping> TranslateAsync(string sectionNo, string roomType)
        {
            string normalized = NormalizeSection(sectionNo);
            var key = (normalized, roomType);
            if (translations.TryGetValue(key, out SectionMapping? mapping)) return mapping;
            mapping = await sectionMappings.TranslateAsync(normalized, roomType, cancellationToken);
            translations[key] = mapping;
            return mapping;
        }
    }

    internal static IReadOnlyList<M1DoctorDailyReportRow> Merge0420Into0450(
        IReadOnlyList<M1DoctorDailyReportRow> rows)
    {
        var result = rows.Where(row => row.SectionNo != "0420").ToList();
        foreach (M1DoctorDailyReportRow source in rows.Where(row => row.SectionNo == "0420"))
        {
            int index = result.FindIndex(target => target.SectionNo == "0450" &&
                target.DoctorNo == source.DoctorNo && target.VisitType == source.VisitType);
            if (index < 0) continue;
            M1DoctorDailyReportRow target = result[index];
            result[index] = target with
            {
                SelfPayCount = checked(target.SelfPayCount + source.SelfPayCount),
                InsuranceCount = checked(target.InsuranceCount + source.InsuranceCount),
                MorningCount = checked(target.MorningCount + source.MorningCount),
                AfternoonCount = checked(target.AfternoonCount + source.AfternoonCount),
                NightCount = checked(target.NightCount + source.NightCount),
                AppointmentCount = checked(target.AppointmentCount + source.AppointmentCount)
            };
        }
        return result;
    }

    internal static string NormalizeSection(string value)
    {
        string section = value.Trim();
        if (section.StartsWith("0212", StringComparison.Ordinal)) return "0212*";
        if (section.Length >= 5 && "ABCDEFM*".Contains(section[^1], StringComparison.Ordinal))
            return section[..^1];
        return section;
    }

    internal static string ToRocDate(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";

    private DateOnly ValidateDate(M1DoctorDailyReportRequest request)
    {
        if (request.ReportDate is null || request.ReportDate.Value.Year < 1912)
            throw new ArgumentException("請輸入有效的 M1 查詢日期。");
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            timeProvider.GetUtcNow(), TaipeiTimeZone).DateTime);
        if (request.ReportDate.Value >= today && !request.FutureDateConfirmed)
            throw new M1FutureDateConfirmationRequiredException(request.ReportDate.Value);
        return request.ReportDate.Value;
    }

    private static void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize is not (10 or 30 or 50))
            throw new ArgumentException("M1 查詢或分頁條件不正確。");
    }

    private static string Name(SectionMapping mapping, M1DoctorDailyAggregateRow source) =>
        string.IsNullOrWhiteSpace(mapping.DisplayName) ? source.SectionName : mapping.DisplayName;
}
