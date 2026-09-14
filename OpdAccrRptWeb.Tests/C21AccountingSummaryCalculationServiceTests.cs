using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C21AccountingSummaryCalculationServiceTests
{
    private readonly C21AccountingSummaryCalculationService _service = new();

    [Fact]
    public void Calculate_ScopeZeroBuildsTotalOutpatientAndEmergencyGroups()
    {
        var rows = _service.Calculate(Condition(C21EncounterSources.Outpatient, 0),
        [
            new(0, "01", "01", 100m),
            new(1, "01", "01", 40m)
        ], [new("01", "掛號費")]);

        Assert.Equal(140m, Detail(rows, "1", "01").SelfPayAmount);
        Assert.Equal(100m, Detail(rows, "2", "01").SelfPayAmount);
        Assert.Equal(40m, Detail(rows, "3", "01").SelfPayAmount);
    }

    [Fact]
    public void Calculate_AppliesSpecialDisplayAndDoesNotEmitOriginalDebtTwice()
    {
        var rows = _service.Calculate(Condition(C21EncounterSources.Inpatient, 5),
        [
            new(2, "56", "30", 10m), new(2, "57", "30", 2m),
            new(2, "69", "30", 3m), new(2, "59", "30", 4m),
            new(2, "64", "30", 5m), new(2, "75", "30", 7m)
        ], []);

        Assert.Equal(24m, Detail(rows, "5", "56").InsuranceAmount);
        Assert.Single(rows, row => row.GroupCode == "5" && row.BillingCode == "75");
        Assert.All(rows, row => Assert.IsType<decimal>(row.SelfPayAmount));
    }

    [Fact]
    public void Calculate_OrdersGroupsAndRowsDeterministically()
    {
        var rows = _service.Calculate(Condition(C21EncounterSources.Outpatient, 0),
            [new(0, "02", "01", 1m), new(0, "01", "01", 1m)], []);

        Assert.Equal(["1", "2", "3"], rows.Select(row => row.GroupCode).Distinct());
        Assert.All(rows.GroupBy(row => row.GroupCode), group =>
            Assert.Equal(group.Select(row => row.RowOrder).Order(), group.Select(row => row.RowOrder)));
    }

    [Theory]
    [InlineData("Outpatient", 1, "1")]
    [InlineData("Outpatient", 2, "2")]
    [InlineData("Outpatient", 3, "3")]
    [InlineData("Inpatient", 5, "5")]
    [InlineData("Inpatient", 8, "8")]
    public void Calculate_MapsEverySelectableScopeToItsLegacyGroup(
        string source, int scope, string expectedGroup)
    {
        var room = scope switch { 2 => 0, 3 => 1, 8 => 4, _ => 2 };
        var rows = _service.Calculate(Condition(source, scope),
            [new(room, "01", "01", 1m)], []);

        Assert.All(rows, row => Assert.Equal(expectedGroup, row.GroupCode));
    }

    [Fact]
    public void Calculate_GoldenMasterAppliesSubtotalIncomeAndDifferenceRules()
    {
        var rows = _service.Calculate(Condition(C21EncounterSources.Outpatient, 2),
        [
            new(0, "01", "01", 100m), new(0, "49", "30", 20m),
            new(0, "51", "01", 10m), new(0, "59", "01", 3m),
            new(0, "60", "01", 2m), new(0, "67", "30", 5m)
        ], []);

        var subtotal = Assert.Single(rows, row => row.RowType == "Subtotal");
        var income = Assert.Single(rows, row => row.RowType == "Income");
        var difference = Assert.Single(rows, row => row.RowType == "Difference");
        Assert.Equal(100m, subtotal.SelfPayAmount);
        Assert.Equal(0m, subtotal.InsuranceAmount);
        Assert.Equal(-5m, income.InsuranceAmount);
        Assert.Equal(91m, difference.SelfPayAmount);
        Assert.Equal(-5m, difference.InsuranceAmount);
        Assert.Single(rows, row => row.RowType == "Blank");
    }

    [Fact]
    public void Calculate_ZeroDifferenceDoesNotEmitDifferenceRow()
    {
        var rows = _service.Calculate(Condition(C21EncounterSources.Outpatient, 2), [], []);
        Assert.DoesNotContain(rows, row => row.RowType == "Difference");
    }

    [Fact]
    public void Calculate_Outpatient56Includes80AndInpatient62Includes51And63()
    {
        var outpatient = _service.Calculate(Condition(C21EncounterSources.Outpatient, 2),
            [new(0, "56", "01", 10m), new(0, "80", "01", 4m)], []);
        var inpatient = _service.Calculate(Condition(C21EncounterSources.Inpatient, 5),
            [new(2, "62", "30", 10m), new(2, "51", "30", 4m), new(2, "63", "30", 3m)], []);

        Assert.Equal(14m, Detail(outpatient, "2", "56").SelfPayAmount);
        Assert.Equal(17m, Detail(inpatient, "5", "62").InsuranceAmount);
    }

    private static SearchReportCondition Condition(string source, int scope) => new()
    {
        EncounterSource = source,
        AccountingScope = scope
    };

    private static C21AccountingSummaryReportViewModel Detail(
        IEnumerable<C21AccountingSummaryReportViewModel> rows, string group, string code) =>
        Assert.Single(rows, row => row.GroupCode == group && row.BillingCode == code);
}
