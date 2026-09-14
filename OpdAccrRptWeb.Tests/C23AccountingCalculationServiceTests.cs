using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C23AccountingCalculationServiceTests
{
    [Theory]
    [InlineData("EEEE", "01", "00-039", null, "ZZ")]
    [InlineData("AAAA", "01", "00-039", null, "XX")]
    [InlineData("AAAA", "01", "64-003", null, "YY")]
    [InlineData("AAAA", "01", "102-23", null, "WW")]
    [InlineData("AAAA", "01", "S001A", null, "42")]
    [InlineData("AAAA", "01", "normal", "6", "VV")]
    [InlineData("AAAA", "01", "normal", "7", "TT")]
    public void Calculate_AppliesSpecialCodePrecedence(
        string room, string contract, string order, string? selfPay, string expected)
    {
        var result = C23AccountingCalculationService.Calculate(
            new("1150908", room, contract, order, selfPay, null, false, 20m, 100m, false));
        Assert.Equal(expected, result.ContractCode);
        Assert.Equal(120m, result.GrossAmount);
    }

    [Theory]
    [InlineData("0930531", "VV")]
    [InlineData("0930601", "UU")]
    public void Calculate_MapsContractZeroAcrossHistoricalCutoff(string date, string expected)
    {
        var result = C23AccountingCalculationService.Calculate(
            new(date, "AAAA", "00", "normal", null, null, false, 0m, 100m, false));
        Assert.Equal(expected, result.ContractCode);
    }

    [Fact]
    public void Calculate_CancellationReversesSub3AndSub5()
    {
        var result = C23AccountingCalculationService.Calculate(
            new("1150908", "AAAA", "01", "normal", null, null, false, 20m, 100m, true));
        Assert.Equal(-100m, result.ContractAmount);
        Assert.Equal(-20m, result.DiscountAmount);
        Assert.Equal(-120m, result.GrossAmount);
    }
}
