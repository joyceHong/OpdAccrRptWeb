using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class M3OpdEmergencyDailyReportService(
    IM3OpdEmergencyDailyReportRepository repository,
    ISectionMappingRepository sectionMappings,
    IM3ReportRunStore runStore) : IM3OpdEmergencyDailyReportService
{
    internal const string CutoffRocDate = "1000901";
    internal static readonly IReadOnlyList<ReportColumn> Columns =
    [
        new("departmentId", "科別代碼"), new("departmentName", "科別名稱"),
        new("op1SQty", "門診初診自費"), new("op1HQty", "門診初診健保"),
        new("op2SQty", "門診複診自費"), new("op2HQty", "門診複診健保"),
        new("em1SQty", "急診初診自費"), new("em1HQty", "急診初診健保"),
        new("em2SQty", "急診複診自費"), new("em2HQty", "急診複診健保"),
        new("oeSQty", "當日自費"), new("oeHQty", "當日健保"),
        new("op1MonQty", "月門診初診"), new("op2MonQty", "月門診複診"),
        new("em1MonQty", "月急診初診"), new("em2MonQty", "月急診複診"),
        new("oeMonSQty", "月自費"), new("oeMonHQty", "月健保"),
        new("op1YearQty", "年門診初診"), new("op2YearQty", "年門診複診"),
        new("em1YearQty", "年急診初診"), new("em2YearQty", "年急診複診"),
        new("oeYearSQty", "年自費"), new("oeYearHQty", "年健保"),
        new("op1DaySum", "日門診初診合計"), new("op2DaySum", "日門診複診合計"),
        new("em1DaySum", "日急診初診合計"), new("em2DaySum", "日急診複診合計"),
        new("oeDaySum", "日總計"), new("oeMonSum", "月總計"), new("oeYearSum", "年總計")
    ];

    public async Task<M3OpdEmergencyDailyPagedResponse> QueryAsync(
        M3OpdEmergencyDailyReportRequest request, string actor,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(request.PageNumber, request.PageSize);
        M3OpdEmergencyDailyReportSnapshot snapshot;
        if (!string.IsNullOrWhiteSpace(request.RunId))
        {
            if (!runStore.TryGet(request.RunId, actor, out snapshot!))
                throw new M3ReportRunNotFoundException();
        }
        else snapshot = await GenerateAsync(request, actor, cancellationToken);

        int totalPages = (int)Math.Ceiling(snapshot.Rows.Count / (double)request.PageSize);
        int pageNumber = Math.Min(request.PageNumber, Math.Max(1, totalPages));
        M3OpdEmergencyDailyReportRow[] page = snapshot.Rows
            .Skip((pageNumber - 1) * request.PageSize).Take(request.PageSize).ToArray();
        return new(snapshot.RunId, page, Columns, snapshot.NineKpis, snapshot.Rows.Count,
            pageNumber, request.PageSize, totalPages);
    }

    public async Task<M3OpdEmergencyDailyReportSnapshot> GenerateAsync(
        M3OpdEmergencyDailyReportRequest request, string actor,
        CancellationToken cancellationToken = default)
    {
        DateOnly date = ValidateDate(request.ReportDate);
        string reportDate = ToRocDate(date);
        string monthStart = $"{reportDate[..5]}01";
        string yearStart = $"{reportDate[..3]}0101";
        M3RepositoryResult source = await repository.QueryAsync(reportDate, monthStart, yearStart,
            cancellationToken);
        IReadOnlyList<M3OpdEmergencyDailyReportRow> rows = await TransformAsync(
            reportDate, source, cancellationToken);
        M3NineKpis kpis = CalculateKpis(source.KpiSource, source.EmergencyShifts);
        string runId = runStore.Save(actor, date, rows, kpis);
        return runStore.TryGet(runId, actor, out M3OpdEmergencyDailyReportSnapshot snapshot)
            ? snapshot : throw new InvalidOperationException("M3 快照建立失敗。");
    }

    public bool TryGetRun(string runId, string actor, out M3OpdEmergencyDailyReportSnapshot snapshot) =>
        runStore.TryGet(runId, actor, out snapshot);

    internal async Task<IReadOnlyList<M3OpdEmergencyDailyReportRow>> TransformAsync(
        string reportDate, M3RepositoryResult source, CancellationToken cancellationToken)
    {
        List<WorkRow> daily = Map(source.Daily, reportDate);
        List<WorkRow> monthly = Map(source.Monthly, reportDate);
        List<WorkRow> yearly = Map(source.Yearly, reportDate);
        Merge0420Into0450(daily);
        Merge0420Into0450(monthly);
        Merge0420Into0450(yearly);
        Attach(daily, monthly, true);
        Attach(daily, yearly, false);

        var result = new List<M3OpdEmergencyDailyReportRow>(daily.Count);
        foreach (WorkRow row in daily)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string oldCode = row.DepartmentId;
            string name = await repository.ResolveDepartmentNameAsync(oldCode, cancellationToken);
            SectionMapping mapping = await sectionMappings.TranslateAsync(oldCode, string.Empty,
                cancellationToken);
            string newCode = oldCode == "0201" ? "11910" :
                string.IsNullOrWhiteSpace(mapping.NewCode) ? oldCode : mapping.NewCode.Trim();
            result.Add(row.ToResult(newCode, name));
        }
        return result.OrderBy(row => row.DepartmentId, StringComparer.Ordinal).ToArray();
    }

    internal static M3NineKpis CalculateKpis(M3KpiSource source, M3EmergencyShiftCounts shifts)
    {
        long morning = source.MorningTotal ?? 0;
        long afternoon = source.AfternoonTotal ?? 0;
        long night = source.NightTotal ?? 0;
        long appointment = source.AppointmentTotal ?? 0;
        long noShow = source.NoShowTotal ?? 0;
        return new(checked(morning - shifts.Day), checked(afternoon - shifts.Evening),
            checked(night - shifts.Night), shifts.Day, shifts.Evening, shifts.Night,
            appointment, noShow, checked(appointment - noShow));
    }

    internal static List<WorkRow> Map(IReadOnlyList<M3DepartmentAggregate> source, string reportDate) =>
        source.Select(item => WorkRow.From(item, string.CompareOrdinal(reportDate, CutoffRocDate) >= 0))
            .ToList();

    internal static void Merge0420Into0450(List<WorkRow> rows)
    {
        WorkRow? source = rows.FirstOrDefault(row => row.DepartmentId == "0420");
        if (source is null) return;
        WorkRow? target = rows.FirstOrDefault(row => row.DepartmentId == "0450");
        if (target is not null) target.AddDaily(source);
        rows.RemoveAll(row => row.DepartmentId == "0420");
    }

    internal static void Attach(List<WorkRow> daily, IReadOnlyList<WorkRow> cumulative, bool monthly)
    {
        foreach (WorkRow source in cumulative)
        {
            WorkRow? target = daily.FirstOrDefault(row => row.DepartmentId == source.DepartmentId);
            if (target is null)
            {
                target = WorkRow.Empty(source.DepartmentId);
                daily.Add(target);
            }
            target.Attach(source, monthly);
        }
    }

    internal static string ToRocDate(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";
    private static DateOnly ValidateDate(DateOnly? value) => value is null || value.Value.Year < 1912
        ? throw new ArgumentException("請輸入有效的 M3 查詢日期。") : value.Value;
    private static void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize is not (10 or 30 or 50))
            throw new ArgumentException("M3 查詢或分頁條件不正確。");
    }

    internal sealed class WorkRow
    {
        private WorkRow(string departmentId) => DepartmentId = departmentId;
        public string DepartmentId { get; }
        public long Op1S, Op1H, Op2S, Op2H, Em1S, Em1H, Em2S, Em2H, OES, OEH;
        public long Op1Mon, Op2Mon, Em1Mon, Em2Mon, OEMonS, OEMonH;
        public long Op1Year, Op2Year, Em1Year, Em2Year, OEYearS, OEYearH;

        public static WorkRow Empty(string departmentId) => new(departmentId);
        public static WorkRow From(M3DepartmentAggregate value, bool afterCutoff)
        {
            var row = new WorkRow(value.DepartmentId.Trim());
            if (afterCutoff)
            {
                row.Op1S = checked(value.S1 + value.S2); row.Op1H = value.S3;
                row.Op2S = checked(value.S4 + value.S5); row.Op2H = value.S6;
                row.Em1S = checked(value.S7 + value.S8); row.Em1H = value.S9;
                row.Em2S = checked(value.S10 + value.S11); row.Em2H = value.S12;
            }
            else
            {
                row.Op1S = value.S1; row.Op1H = checked(value.S2 + value.S3);
                row.Op2S = value.S4; row.Op2H = checked(value.S5 + value.S6);
                row.Em1S = value.S7; row.Em1H = checked(value.S8 + value.S9);
                row.Em2S = value.S10; row.Em2H = checked(value.S11 + value.S12);
            }
            row.OES = checked(row.Op1S + row.Op2S + row.Em1S + row.Em2S);
            row.OEH = checked(row.Op1H + row.Op2H + row.Em1H + row.Em2H);
            return row;
        }
        public void AddDaily(WorkRow value)
        {
            Op1S = checked(Op1S + value.Op1S); Op1H = checked(Op1H + value.Op1H);
            Op2S = checked(Op2S + value.Op2S); Op2H = checked(Op2H + value.Op2H);
            Em1S = checked(Em1S + value.Em1S); Em1H = checked(Em1H + value.Em1H);
            Em2S = checked(Em2S + value.Em2S); Em2H = checked(Em2H + value.Em2H);
            OES = checked(OES + value.OES); OEH = checked(OEH + value.OEH);
        }
        public void Attach(WorkRow value, bool monthly)
        {
            if (monthly)
            {
                Op1Mon = checked(value.Op1S + value.Op1H); Op2Mon = checked(value.Op2S + value.Op2H);
                Em1Mon = checked(value.Em1S + value.Em1H); Em2Mon = checked(value.Em2S + value.Em2H);
                OEMonS = checked(value.Op1S + value.Op2S + value.Em1S + value.Em2S);
                OEMonH = checked(value.Op1H + value.Op2H + value.Em1H + value.Em2H);
            }
            else
            {
                Op1Year = checked(value.Op1S + value.Op1H); Op2Year = checked(value.Op2S + value.Op2H);
                Em1Year = checked(value.Em1S + value.Em1H); Em2Year = checked(value.Em2S + value.Em2H);
                OEYearS = checked(value.Op1S + value.Op2S + value.Em1S + value.Em2S);
                OEYearH = checked(value.Op1H + value.Op2H + value.Em1H + value.Em2H);
            }
        }
        public M3OpdEmergencyDailyReportRow ToResult(string code, string name) => new(code, name,
            Op1S, Op1H, Op2S, Op2H, Em1S, Em1H, Em2S, Em2H, OES, OEH,
            Op1Mon, Op2Mon, Em1Mon, Em2Mon, OEMonS, OEMonH,
            Op1Year, Op2Year, Em1Year, Em2Year, OEYearS, OEYearH);
    }
}
