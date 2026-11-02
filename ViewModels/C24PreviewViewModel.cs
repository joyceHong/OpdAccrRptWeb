using System.Globalization;

namespace OpdAccrRptWeb.ViewModels;

public sealed record C24PreviewPage(int Number, string RoomName,
    IReadOnlyList<C24DebtPaymentDetail> Rows, C24Summary? Summary);

public sealed record C24PreviewViewModel(string Title, string DateRange, string PrintedAt,
    string GeneratedBy, IReadOnlyList<C24PreviewPage> Pages)
{
    public static C24PreviewViewModel Create(SearchReportCondition condition,
        C24CanonicalResult result, string generatedBy, DateTime printedAt)
    {
        const int rowsPerPage = 28;
        var pages = new List<C24PreviewPage>();
        foreach (var group in result.Details.GroupBy(row => row.RoomCategory).OrderBy(group => group.Key))
        {
            var rows = group.ToList();
            for (var index = 0; index < rows.Count; index += rowsPerPage)
            {
                var end = Math.Min(index + rowsPerPage, rows.Count);
                pages.Add(new C24PreviewPage(pages.Count + 1, RoomName(group.Key),
                    rows.GetRange(index, end - index), end == rows.Count
                        ? result.Summaries.FirstOrDefault(summary => summary.RoomCategory == group.Key)
                        : null));
            }
        }
        var start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDate = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var sourceName = condition.Source == C24Sources.Inpatient ? "住院" : "門急診";
        return new C24PreviewViewModel(
            condition.Mode == C24Modes.Billing ? $"{sourceName}病患欠繳款核帳日報表（批價作業）"
                : $"{sourceName}病患欠繳款核帳日報表",
            $"{RocDate(start)} - {RocDate(endDate)}", printedAt.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture),
            generatedBy, pages);
    }

    private static string RocDate(DateOnly date) =>
        $"{date.Year - 1911:000}/{date.Month:00}/{date.Day:00}";

    private static string RoomName(C24RoomCategory category) => category switch
    {
        C24RoomCategory.Emergency => "急診",
        C24RoomCategory.Inpatient => "住院",
        _ => "門診"
    };
}
