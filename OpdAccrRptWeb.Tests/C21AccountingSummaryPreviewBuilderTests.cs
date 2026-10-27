using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C21AccountingSummaryPreviewBuilderTests
{
    [Theory]
    [InlineData(5, "住院總", "住院收入")]
    [InlineData(8, "出院總", "合　　計")]
    public void Build_CreatesFixedPairedLayoutForSelectedInpatientScope(
        int scope, string expectedScope, string expectedSubtotal)
    {
        var condition = new SearchReportCondition
        {
            EncounterSource = C21EncounterSources.Inpatient,
            StartDate = "2026-10-21",
            EndDate = "2026-10-21",
            AccountingScope = scope
        };
        var rows = new List<C21AccountingSummaryReportViewModel>
        {
            new() { GroupCode = scope.ToString(), RowType = "Detail", BillingCode = "01",
                BillingName = "藥品費", SelfPayAmount = 900m },
            new() { GroupCode = scope.ToString(), RowType = "Detail", BillingCode = "02",
                BillingName = "病房費", InsuranceAmount = 330m },
            new() { GroupCode = scope.ToString(), RowType = "Subtotal",
                BillingName = expectedSubtotal, SelfPayAmount = 900m, InsuranceAmount = 330m }
        };

        var result = C21AccountingSummaryPreviewBuilder.Build(condition, scope, rows,
            [new("75", "欠繳記帳")], new DateTimeOffset(2026, 10, 27, 8, 45, 58, TimeSpan.FromHours(8)),
            "20021");

        Assert.Equal(expectedScope, result.ScopeName);
        Assert.Equal("115/10/21 - 115/10/21", result.DateRange);
        Assert.Equal("115/10/27 08:45:58", result.PrintedAt);
        Assert.Equal(25, result.FirstSection.Count);
        Assert.Equal(15, result.SecondSection.Count);
        Assert.Equal("01", result.FirstSection[0].Left.Code);
        Assert.Equal(900m, result.FirstSection[0].Left.SelfPayAmount);
        Assert.Equal("02", result.FirstSection[0].Right.Code);
        Assert.Equal(330m, result.FirstSection[0].Right.InsuranceAmount);
        Assert.Equal("75", result.SecondSection[12].Left.Code);
        Assert.Equal("欠繳記帳", result.SecondSection[12].Left.Name);
        Assert.Equal(expectedSubtotal, result.Subtotal?.Label);
    }

    [Fact]
    public void Build_RejectsOutpatientSource()
    {
        var condition = new SearchReportCondition
        {
            EncounterSource = C21EncounterSources.Outpatient,
            StartDate = "2026-10-21",
            EndDate = "2026-10-21"
        };

        Assert.Throws<ArgumentException>(() => C21AccountingSummaryPreviewBuilder.Build(
            condition, 5, [], [], DateTimeOffset.Now, "user"));
    }

    [Theory]
    [InlineData(0, "門急診,門診,急診")]
    [InlineData(1, "門急診")]
    [InlineData(2, "門診")]
    [InlineData(3, "急診")]
    public void BuildDocument_SelectsOutpatientPagesForRequestedScope(
        int scope, string expectedScopes)
    {
        var condition = new SearchReportCondition
        {
            EncounterSource = C21EncounterSources.Outpatient,
            AccountingScope = scope,
            StartDate = "2026-10-21",
            EndDate = "2026-10-21"
        };
        var rows = new List<C21AccountingSummaryReportViewModel>
        {
            new() { GroupCode = "1", RowType = "Detail", BillingCode = "01", SelfPayAmount = 950m },
            new() { GroupCode = "2", RowType = "Detail", BillingCode = "01", SelfPayAmount = 650m },
            new() { GroupCode = "3", RowType = "Detail", BillingCode = "01", SelfPayAmount = 300m }
        };

        var document = C21AccountingSummaryPreviewBuilder.BuildDocument(condition, rows,
            [new("01", "藥品費")], DateTimeOffset.Now, "20021");

        Assert.Equal(expectedScopes, string.Join(',', document.Pages.Select(page => page.ScopeName)));
        Assert.All(document.Pages, page => Assert.Equal("門急診批價報表", page.Title));
        Assert.Equal(scope == 0 ? 950m : scope switch { 1 => 950m, 2 => 650m, _ => 300m },
            document.Pages[0].FirstSection[0].Left.SelfPayAmount);
    }
}
