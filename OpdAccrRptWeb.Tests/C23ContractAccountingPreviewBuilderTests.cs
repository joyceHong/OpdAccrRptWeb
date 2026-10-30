using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C23ContractAccountingPreviewBuilderTests
{
    [Fact]
    public void Build_SkipsSummaryRowsAndTotalsEachContractOnItsLastPage()
    {
        var condition = new SearchReportCondition
        {
            StartDate = "2026-11-01", EndDate = "2026-11-02",
            EncounterSource = C23EncounterSources.Outpatient
        };
        var rows = Enumerable.Range(0, 26).Select(_ => new C23ContractAccountingReportViewModel
        {
            ContractCode = "10", ContractName = "董事長(家屬)",
            GrossAmount = 180, DiscountAmount = 20, ContractAmount = 160
        }).ToList();
        rows.Add(new C23ContractAccountingReportViewModel
        {
            ContractCode = "10", ContractName = "董事長(家屬)",
            GrossAmount = 4_680, DiscountAmount = 520, ContractAmount = 4_160,
            ResultType = C23ResultTypes.Summary
        });

        var result = C23ContractAccountingPreviewBuilder.Build(condition, rows,
            new DateTime(2026, 11, 2, 10, 41, 5), "tester");

        Assert.Equal("115/11/01 - 115/11/02", result.DateRange);
        Assert.Equal(2, result.Pages.Count);
        Assert.Equal(16, result.Pages[0].Rows.Count);
        Assert.Null(result.Pages[0].GrossTotal);
        Assert.Equal(10, result.Pages[1].Rows.Count);
        Assert.Equal(4_680m, result.Pages[1].GrossTotal);
        Assert.Equal(520m, result.Pages[1].DiscountTotal);
        Assert.Equal(4_160m, result.Pages[1].ContractTotal);
    }
}
