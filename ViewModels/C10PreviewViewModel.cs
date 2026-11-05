using System.Globalization;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C10PreviewViewModel
{
    public required string Title { get; init; }
    public required string DateRange { get; init; }
    public required string PrintedAt { get; init; }
    public required string GeneratedBy { get; init; }
    public required IReadOnlyList<C10PreviewPage> Pages { get; init; }

    public static C10PreviewViewModel Create(
        SearchReportCondition condition,
        IReadOnlyList<C10ReceivableDetailRow> rows,
        string generatedBy,
        DateTime printedAt)
    {
        var pages = rows
            .GroupBy(row => new C10VisitKey(row.VisitDate, row.VisitTime, row.VisitRoom, row.VisitNumber))
            .Select((group, index) => new C10PreviewPage(index + 1, group.ToList()))
            .ToList();

        return new C10PreviewViewModel
        {
            Title = condition.Source == C10Sources.Inpatient
                ? "住院病患應收帳款明細記錄表"
                : "門急診病患應收帳款明細記錄表",
            DateRange = $"{ToRocDate(condition.StartDate)} ～ {ToRocDate(condition.EndDate)}",
            PrintedAt = $"{printedAt.Year - 1911:000}/{printedAt:MM/dd HH:mm:ss}",
            GeneratedBy = generatedBy,
            Pages = pages
        };
    }

    private static string ToRocDate(string? value)
    {
        DateOnly date = DateOnly.ParseExact(value!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{date.Year - 1911:000}/{date.Month:00}/{date.Day:00}";
    }
}

public sealed record C10PreviewPage(int Number, IReadOnlyList<C10ReceivableDetailRow> Rows)
{
    public C10ReceivableDetailRow Visit => Rows[0];
}
