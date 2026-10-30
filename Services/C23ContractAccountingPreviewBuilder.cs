using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public static class C23ContractAccountingPreviewBuilder
{
    private const int RowsPerPage = 16;

    public static C23ContractAccountingPreviewViewModel Build(
        SearchReportCondition condition,
        IReadOnlyList<C23ContractAccountingReportViewModel> rows,
        DateTime processedAt,
        string generatedBy)
    {
        var start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd");
        string FormatDate(DateOnly date) => $"{date.Year - 1911:000}/{date:MM/dd}";
        var pages = new List<C23ContractAccountingPreviewPage>();
        foreach (var group in rows.Where(row => row.ResultType == C23ResultTypes.Detail)
                     .GroupBy(row => new { row.ContractCode, row.ContractName }))
        {
            var detailRows = group.ToList();
            for (var offset = 0; offset < detailRows.Count; offset += RowsPerPage)
            {
                var lastPage = offset + RowsPerPage >= detailRows.Count;
                pages.Add(new C23ContractAccountingPreviewPage(
                    group.Key.ContractCode, group.Key.ContractName,
                    offset / RowsPerPage + 1,
                    detailRows.Skip(offset).Take(RowsPerPage).ToList(),
                    lastPage ? detailRows.Sum(row => row.GrossAmount) : null,
                    lastPage ? detailRows.Sum(row => row.DiscountAmount) : null,
                    lastPage ? detailRows.Sum(row => row.ContractAmount) : null));
            }
        }

        return new C23ContractAccountingPreviewViewModel(
            condition.EncounterSource == C23EncounterSources.Inpatient
                ? "住院合約單位記帳通知單日報表" : "門急診合約單位記帳通知單日報表",
            $"{FormatDate(start)} - {FormatDate(end)}",
            $"{processedAt.Year - 1911:000}/{processedAt:MM/dd HH:mm:ss}",
            generatedBy,
            pages);
    }
}
