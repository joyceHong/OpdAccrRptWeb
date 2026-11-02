using System.Globalization;

namespace OpdAccrRptWeb.ViewModels;

public sealed record C29ContractPaymentPreviewPage(
    int PageNumber,
    string ContractCode,
    string ContractName,
    IReadOnlyList<ContractPaymentDetailReportViewModel> Rows,
    decimal? ContractTotal,
    decimal? GroupTotal);

public sealed record C29ContractPaymentPreviewViewModel(
    string Title,
    string DateRange,
    DateTimeOffset PrintedAt,
    string GeneratedBy,
    IReadOnlyList<C29ContractPaymentPreviewPage> Pages)
{
    public string RocPrintedAt =>
        $"{PrintedAt.Year - 1911:000}/{PrintedAt:MM/dd HH:mm:ss}";

    public static C29ContractPaymentPreviewViewModel Create(
        SearchReportCondition condition,
        IReadOnlyList<ContractPaymentDetailReportViewModel> rows,
        DateTimeOffset printedAt,
        string generatedBy)
    {
        var pages = new List<C29ContractPaymentPreviewPage>();
        foreach (var group in rows.GroupBy(row => row.ContractCode))
        {
            var groupRows = group.ToList();
            decimal groupTotal = groupRows.Sum(row => row.PaymentAmount);
            for (int start = 0; start < groupRows.Count; start += 24)
            {
                var pageRows = groupRows.Skip(start).Take(24).ToList();
                bool lastGroupPage = start + pageRows.Count == groupRows.Count;
                pages.Add(new C29ContractPaymentPreviewPage(
                    pages.Count + 1, group.Key ?? string.Empty,
                    groupRows[0].ContractName ?? string.Empty, pageRows,
                    lastGroupPage ? groupTotal : null,
                    lastGroupPage ? groupTotal : null));
            }
        }

        return new C29ContractPaymentPreviewViewModel(
            condition.EncounterSource == "Inpatient"
                ? "住院合約記帳單位收款明細表"
                : "門急診合約記帳單位收款明細表",
            $"{RocDate(condition.StartDate)} - {RocDate(condition.EndDate)}",
            printedAt, generatedBy, pages);
    }

    private static string RocDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? $"{date.Year - 1911:000}/{date.Month:00}/{date.Day:00}"
            : value ?? string.Empty;
}
