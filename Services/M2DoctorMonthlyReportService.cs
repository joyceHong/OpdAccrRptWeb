using System.Globalization;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class M2DoctorMonthlyReportService(
    IM2DoctorMonthlyReportRepository repository,
    IM2ReportRunStore runStore) : IM2DoctorMonthlyReportService
{
    internal static readonly IReadOnlyList<ReportColumn> Columns =
        [new("sectionNo", "科別代碼"), new("sectionName", "科別名稱"),
         new("doctorNo", "醫師代碼"), new("doctorName", "醫師姓名"),
         .. Enumerable.Range(1, 31).Select(day => new ReportColumn($"d{day:00}", $"{day:00}")),
         new("monthlyTotal", "合計")];

    public async Task<M2DoctorMonthlyPagedResponse> QueryAsync(M2DoctorMonthlyReportRequest request,
        string actor, CancellationToken cancellationToken = default)
    {
        ValidatePaging(request.PageNumber, request.PageSize);
        M2DoctorMonthlyReportSnapshot? snapshot;
        if (!string.IsNullOrWhiteSpace(request.RunId))
        {
            if (!runStore.TryGet(request.RunId, actor, out snapshot!))
                throw new M2ReportRunNotFoundException();
        }
        else snapshot = await GenerateAsync(request, actor, cancellationToken);

        if (snapshot is null) return new(null, [], Columns, 0, 1, request.PageSize, 0);
        int totalPages = (int)Math.Ceiling(snapshot.Rows.Count / (double)request.PageSize);
        int pageNumber = Math.Min(request.PageNumber, Math.Max(1, totalPages));
        M2DoctorMonthlyReportRow[] page = snapshot.Rows.Skip((pageNumber - 1) * request.PageSize)
            .Take(request.PageSize).ToArray();
        return new(snapshot.RunId, page, Columns, snapshot.Rows.Count, pageNumber,
            request.PageSize, totalPages);
    }

    public async Task<M2DoctorMonthlyReportSnapshot?> GenerateAsync(
        M2DoctorMonthlyReportRequest request, string actor,
        CancellationToken cancellationToken = default)
    {
        DateOnly month = Validate(request);
        string rocMonth = ToRocMonth(month);
        var source = new List<M2DoctorMonthlySourceRow>();
        if (request.CalculationBasis == M2CalculationBasis.Statistics)
            source.AddRange(await repository.QueryStatisticsAsync(rocMonth, request.VisitScope,
                request.TimeSlot, cancellationToken));
        else
            for (int day = 1; day <= DateTime.DaysInMonth(month.Year, month.Month); day++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                source.AddRange(await repository.QueryActualVisitDayAsync(rocMonth, day,
                    request.VisitScope, request.TimeSlot, cancellationToken));
            }

        IReadOnlyList<M2DoctorMonthlyReportRow> rows = Transform(source);
        if (rows.Count == 0) return null;
        string runId = runStore.Save(actor, month, request.CalculationBasis,
            request.VisitScope, request.TimeSlot, rows);
        return runStore.TryGet(runId, actor, out M2DoctorMonthlyReportSnapshot snapshot)
            ? snapshot : throw new InvalidOperationException("M2 快照建立失敗。");
    }

    public bool TryGetRun(string runId, string actor, out M2DoctorMonthlyReportSnapshot snapshot) =>
        runStore.TryGet(runId, actor, out snapshot);

    internal static IReadOnlyList<M2DoctorMonthlyReportRow> Transform(
        IReadOnlyList<M2DoctorMonthlySourceRow> source)
    {
        var groups = new Dictionary<(string Section, string Doctor), MutableRow>();
        foreach (M2DoctorMonthlySourceRow item in source)
        {
            if (item.Day is < 1 or > 31) throw new InvalidOperationException("Oracle 回傳無效的日期欄位。");
            string section = item.SectionNo.Trim();
            string doctor = item.DoctorNo.Trim();
            var key = (section, doctor);
            if (!groups.TryGetValue(key, out MutableRow? row))
                groups[key] = row = new(section, item.SectionName.Trim(), doctor,
                    item.DoctorName.Trim(), new int[31]);
            row.Counts[item.Day - 1] = checked(row.Counts[item.Day - 1] + item.Count);
        }

        Merge0420(groups);
        Merge0221To0227(groups);
        return groups.Values.Where(row => row.Counts.Sum() != 0)
            .OrderBy(row => row.SectionNo, StringComparer.Ordinal)
            .ThenBy(row => row.DoctorNo, StringComparer.Ordinal)
            .Select(row => M2DoctorMonthlyReportRow.Create(row.SectionNo, row.SectionName,
                row.DoctorNo, row.DoctorName, row.Counts)).ToArray();
    }

    private static void Merge0420(Dictionary<(string Section, string Doctor), MutableRow> groups)
    {
        foreach (var entry in groups.Where(pair => pair.Key.Section == "0420").ToArray())
        {
            if (groups.TryGetValue(("0450", entry.Key.Doctor), out MutableRow? target))
                Add(target.Counts, entry.Value.Counts);
            groups.Remove(entry.Key);
        }
    }

    private static void Merge0221To0227(Dictionary<(string Section, string Doctor), MutableRow> groups)
    {
        foreach (IGrouping<string, KeyValuePair<(string Section, string Doctor), MutableRow>> doctorGroup
                 in groups.Where(pair => string.CompareOrdinal(pair.Key.Section, "0221") >= 0 &&
                     string.CompareOrdinal(pair.Key.Section, "0228") < 0).GroupBy(pair => pair.Key.Doctor).ToArray())
        {
            KeyValuePair<(string Section, string Doctor), MutableRow>[] entries = doctorGroup.ToArray();
            if (!groups.TryGetValue(("0220", doctorGroup.Key), out MutableRow? target))
            {
                MutableRow first = entries[0].Value;
                groups[("0220", doctorGroup.Key)] = target = new("0220", "", first.DoctorNo,
                    first.DoctorName, new int[31]);
            }
            foreach (var entry in entries) { Add(target.Counts, entry.Value.Counts); groups.Remove(entry.Key); }
        }
    }

    private static void Add(int[] target, int[] source)
    {
        for (int index = 0; index < 31; index++) target[index] = checked(target[index] + source[index]);
    }

    internal static string ToRocMonth(DateOnly value) => $"{value.Year - 1911:000}{value:MM}";

    private static DateOnly Validate(M2DoctorMonthlyReportRequest request)
    {
        if (!Enum.IsDefined(request.CalculationBasis) || !Enum.IsDefined(request.VisitScope) ||
            !Enum.IsDefined(request.TimeSlot)) throw new ArgumentException("M2 查詢選項不正確。");
        if (!DateOnly.TryParseExact($"{request.ReportMonth}-01", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly month) || month.Year < 1912)
            throw new ArgumentException("請輸入有效的 M2 西元年月。");
        return month;
    }

    private static void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize is not (10 or 30 or 50))
            throw new ArgumentException("M2 查詢或分頁條件不正確。");
    }

    private sealed record MutableRow(string SectionNo, string SectionName, string DoctorNo,
        string DoctorName, int[] Counts);
}
