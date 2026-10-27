using System.Globalization;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public static class C21AccountingSummaryPreviewBuilder
{
    public static C21AccountingSummaryPreviewDocumentViewModel BuildDocument(
        SearchReportCondition condition,
        IReadOnlyList<C21AccountingSummaryReportViewModel> rows,
        IReadOnlyList<C21BillingItem> billingItems,
        DateTimeOffset printedAt,
        string generatedBy)
    {
        int[] scopes = (condition.EncounterSource, condition.AccountingScope) switch
        {
            (C21EncounterSources.Outpatient, 0) => [1, 2, 3],
            (C21EncounterSources.Outpatient, 1 or 2 or 3) => [condition.AccountingScope!.Value],
            (C21EncounterSources.Inpatient, 4) => [5, 8],
            (C21EncounterSources.Inpatient, 5 or 8) => [condition.AccountingScope!.Value],
            _ => throw new ArgumentException("C21 預覽範圍與就醫來源不相容。", nameof(condition))
        };
        return new C21AccountingSummaryPreviewDocumentViewModel(scopes
            .Select(scope => Build(condition, scope, rows, billingItems, printedAt, generatedBy))
            .ToList());
    }

    public static C21AccountingSummaryPreviewViewModel Build(
        SearchReportCondition condition,
        int scope,
        IReadOnlyList<C21AccountingSummaryReportViewModel> rows,
        IReadOnlyList<C21BillingItem> billingItems,
        DateTimeOffset printedAt,
        string generatedBy)
    {
        bool inpatient = condition.EncounterSource == C21EncounterSources.Inpatient;
        if (inpatient ? scope is not (5 or 8)
            : condition.EncounterSource != C21EncounterSources.Outpatient || scope is not (1 or 2 or 3))
            throw new ArgumentException("C21 預覽範圍與就醫來源不相容。", nameof(scope));

        string groupCode = scope.ToString(CultureInfo.InvariantCulture);
        var groupRows = rows.Where(row => row.GroupCode == groupCode).ToList();
        var detail = groupRows.Where(row => row.RowType == "Detail" && row.BillingCode is not null)
            .GroupBy(row => row.BillingCode!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var names = billingItems
            .Where(item => !string.IsNullOrWhiteSpace(item.Code))
            .GroupBy(item => item.Code.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.Ordinal);

        C21PrintCell Cell(int number)
        {
            string code = number.ToString("00", CultureInfo.InvariantCulture);
            detail.TryGetValue(code, out var value);
            return new C21PrintCell(code, value?.BillingName ?? names.GetValueOrDefault(code, string.Empty),
                value?.SelfPayAmount, value?.InsuranceAmount, value?.InsuranceWithoutCardAmount);
        }

        IReadOnlyList<C21PrintRow> Section(int first, int last) =>
            Enumerable.Range(0, (last - first + 1) / 2)
                .Select(index => new C21PrintRow(Cell(first + index * 2), Cell(first + index * 2 + 1)))
                .ToList();

        C21PrintSummary? Summary(string rowType)
        {
            var row = groupRows.FirstOrDefault(item => item.RowType == rowType);
            return row is null ? null : new C21PrintSummary(row.BillingName,
                row.SelfPayAmount, row.InsuranceAmount, row.InsuranceWithoutCardAmount);
        }

        static string RocDate(DateOnly date) =>
            $"{date.Year - 1911:000}/{date.Month:00}/{date.Day:00}";

        var start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var end = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new C21AccountingSummaryPreviewViewModel(
            inpatient ? "住院批價報表" : "門急診批價報表",
            scope switch
            {
                1 => "門急診",
                2 => "門診",
                3 => "急診",
                5 => "住院總",
                _ => "出院總"
            },
            $"{RocDate(start)} - {RocDate(end)}",
            $"{RocDate(DateOnly.FromDateTime(printedAt.DateTime))} {printedAt:HH:mm:ss}",
            generatedBy,
            Section(1, 50), Section(51, 80),
            Summary("Subtotal"), Summary("Income"), Summary("Difference"));
    }
}
