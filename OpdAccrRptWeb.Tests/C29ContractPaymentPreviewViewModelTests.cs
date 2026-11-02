using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C29ContractPaymentPreviewViewModelTests
{
    [Fact]
    public void Create_GroupsContractsPaginatesAndTotalsEachContractOnItsLastPage()
    {
        var rows = Enumerable.Range(0, 25)
            .Select(_ => new ContractPaymentDetailReportViewModel
            {
                ContractCode = "A01", ContractName = "甲單位", PaymentAmount = 2
            })
            .Append(new ContractPaymentDetailReportViewModel
            {
                ContractCode = "B02", ContractName = "乙單位", PaymentAmount = 7
            }).ToList();
        var condition = new SearchReportCondition
        {
            StartDate = "2026-08-01", EndDate = "2026-08-31",
            EncounterSource = "Inpatient"
        };

        var preview = C29ContractPaymentPreviewViewModel.Create(
            condition, rows, DateTimeOffset.Now, "tester");

        Assert.Equal("住院合約記帳單位收款明細表", preview.Title);
        Assert.Equal("115/08/01 - 115/08/31", preview.DateRange);
        Assert.Equal(3, preview.Pages.Count);
        Assert.Equal(24, preview.Pages[0].Rows.Count);
        Assert.Null(preview.Pages[0].ContractTotal);
        Assert.Null(preview.Pages[0].GroupTotal);
        Assert.Equal(50, preview.Pages[1].ContractTotal);
        Assert.Equal(50, preview.Pages[1].GroupTotal);
        Assert.Equal(7, preview.Pages[2].ContractTotal);
        Assert.Equal(7, preview.Pages[2].GroupTotal);
    }
}
