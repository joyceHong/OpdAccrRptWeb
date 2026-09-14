using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C10AmountCalculationServiceTests
{
    [Theory]
    [InlineData("1.5", 2)]
    [InlineData("-1.5", -2)]
    [InlineData("0.5", 1)]
    [InlineData("-0.5", -1)]
    public void RoundLegacy_UsesMidpointAwayFromZero(string input, long expected) =>
        Assert.Equal(expected, C10AmountCalculationService.RoundLegacy(decimal.Parse(input)));

    [Fact]
    public void Outpatient_CalculatesSpecialItemsAndAsymmetricOther()
    {
        C10RepositoryResult source = Data(100m,
            Charge("一般", sub6: 30m, sub3: 2m, sub1: 10m),
            Charge("欠繳記帳", sub6: 5m, sub1: 0m),
            Charge("醫療補助", sub3: -3m, sub1: 3m));

        IReadOnlyList<C10ReceivableDetailRow> rows = Service().Calculate(C10Sources.OpdEr, source);

        Assert.Single(rows);
        Assert.Equal(2, rows[0].PaidAmount);
        Assert.Equal(102, rows[0].AmountDue);
        Assert.Equal(2, rows[0].DiscountAmount);
        Assert.Equal("其他", rows[0].ItemName3);
        Assert.Equal(67, rows[0].SelfPayAmount3);
    }

    [Fact]
    public void Inpatient_HiddenSubsidyAffectsTotalsAndBasicCopaymentHasZeroInsurance()
    {
        C10RepositoryResult source = Data(20m,
            Charge("醫療補助", sub1: 4m, sub3: -1m),
            Charge("基本部份負擔", sub1: 10m, sub3: 0m, sub25: 99m));

        IReadOnlyList<C10ReceivableDetailRow> rows = Service().Calculate(C10Sources.Inpatient, source);

        Assert.Equal("基本部份負擔", rows[0].ItemName1);
        Assert.Equal(10, rows[0].SelfPayAmount1);
        Assert.Equal(0, rows[0].InsuranceAmount1);
        Assert.Equal(6, rows[0].PaidAmount);
        Assert.Equal(26, rows[0].AmountDue);
        Assert.Equal(3, rows[0].DiscountAmount);
        Assert.Equal("其他", rows[0].ItemName2);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    public void Output_ChunksAtThreeItems(int itemCount, int expectedRows)
    {
        var charges = Enumerable.Range(1, itemCount)
            .Select(index => Charge($"項目{index}", sub6: 1m))
            .ToArray();
        C10RepositoryResult source = Data(itemCount, charges);

        IReadOnlyList<C10ReceivableDetailRow> rows = Service().Calculate(C10Sources.OpdEr, source);

        Assert.Equal(expectedRows, rows.Count);
        string[] names = rows.SelectMany(row => new[] { row.ItemName1, row.ItemName2, row.ItemName3 })
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();
        Assert.Equal(itemCount, names.Length);
        Assert.Equal(itemCount, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void NullAmounts_RemainSafeAndDoNotCreateAnItemWhenDebtIsZero()
    {
        C10RepositoryResult source = Data(0m, Charge("Null", null, null, null, null));

        Assert.Empty(Service().Calculate(C10Sources.OpdEr, source));
    }

    [Fact]
    public void NegativeAndLargeValues_RoundAtEachLegacyAssignmentWithoutOverflow()
    {
        C10RepositoryResult source = Data(2_000_000_000m,
            Charge("大額", sub6: 1_999_999_999.5m, sub3: -0.5m, sub1: -1.5m));

        C10ReceivableDetailRow row = Assert.Single(
            Service().Calculate(C10Sources.OpdEr, source));

        Assert.Equal(2_000_000_000, row.SelfPayAmount1);
        Assert.Equal(-2, row.PaidAmount);
        Assert.Equal(-1, row.DiscountAmount);
        Assert.Equal(1_999_999_998, row.AmountDue);
    }

    private static C10AmountCalculationService Service() => new();

    private static C10RepositoryResult Data(decimal debt, params C10ChargeAggregate[] charges) => new()
    {
        Visits = [Visit(debt)],
        Charges = charges
    };

    private static C10DebtVisit Visit(decimal debt) => new(
        Key(), "1", "O", "M1", "Patient", "Doctor", "Dept", null,
        null, null, null, null, null, null, debt);

    private static C10ChargeAggregate Charge(
        string name,
        decimal? sub6 = 0m,
        decimal? sub3 = 0m,
        decimal? sub1 = 0m,
        decimal? sub25 = 0m) =>
        new(Key(), C10ChargeSource.Order, name, sub6, sub3, sub1, sub25);

    private static C10VisitKey Key() => new("1150901", "080000", "A", 1);
}
