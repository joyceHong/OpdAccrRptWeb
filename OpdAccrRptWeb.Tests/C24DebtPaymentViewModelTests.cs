using System.ComponentModel;
using System.Reflection;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C24DebtPaymentViewModelTests
{
    [Fact]
    public void Detail_ExposesFixedDescriptionsAndNullableItemName()
    {
        var property = typeof(C24DebtPaymentDetail).GetProperty(nameof(C24DebtPaymentDetail.ChargeItemName))!;
        Assert.Equal("科目名稱", property.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>().Single().Description);
        Assert.True(new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable);
    }

    [Fact]
    public void Summary_UsesDecimalAmountsAndIndependentCollection()
    {
        Assert.Equal(typeof(decimal), typeof(C24Summary).GetProperty(nameof(C24Summary.DebtAmount))!.PropertyType);
        var result = new C24CanonicalResult
        {
            Details = [], Summaries = [new(C24RoomCategory.Inpatient, 0m, 0, 0m, 0, 0m, 0)],
            RunId = "run", CorrelationId = "trace"
        };
        Assert.Empty(result.Details);
        Assert.Single(result.Summaries);
    }

    [Fact]
    public void Preview_UsesFullResultAndShowsSummaryOnlyOnFinalRoomPage()
    {
        var date = new DateOnly(2026, 9, 1);
        var result = new C24CanonicalResult
        {
            Details = Enumerable.Range(0, 30).Select(_ => new C24DebtPaymentDetail
            {
                AccountingDate = date, RoomCategory = C24RoomCategory.Outpatient,
                EventAmount = 100m
            }).ToList(),
            Summaries = [new(C24RoomCategory.Outpatient, 3000m, 30, 300m, 1, 2700m, 29)],
            RunId = "run", CorrelationId = "trace"
        };
        var condition = new SearchReportCondition
        {
            StartDate = "2026-09-01", EndDate = "2026-09-01", Mode = C24Modes.Accounting
        };

        var preview = C24PreviewViewModel.Create(condition, result, "20021", date.ToDateTime(new TimeOnly(12, 0)));

        Assert.Equal(2, preview.Pages.Count);
        Assert.Equal(28, preview.Pages[0].Rows.Count);
        Assert.Null(preview.Pages[0].Summary);
        Assert.Equal(2700m, preview.Pages[1].Summary?.OutstandingAmount);
        Assert.Equal("115/09/01 - 115/09/01", preview.DateRange);
    }
}
