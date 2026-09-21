using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C5AmountPolicyTests
{
    [Theory]
    [InlineData("1", 10, 30)]
    [InlineData("0", 20, 40)]
    [InlineData("4", 20, 40)]
    [InlineData("A", 20, 0)]
    [InlineData("N", 20, 0)]
    [InlineData("5", 20, 0)]
    [InlineData("6", 20, 0)]
    [InlineData(null, 20, 0)]
    public void Calculate_AppliesLegacySPay(string? sPay, decimal price, decimal amount)
    {
        Assert.Equal((price, amount), C5AmountPolicy.Calculate(Row(sPay, 5), false));
    }

    [Fact]
    public void Calculate_DeductsSub3OnlyWhenRequested()
    {
        Assert.Equal((10m, 25m), C5AmountPolicy.Calculate(Row("1", 5), true));
        Assert.Equal((10m, 30m), C5AmountPolicy.Calculate(Row("1", 5), false));
        Assert.Equal((10m, 30m), C5AmountPolicy.Calculate(Row("1", null), true));
    }

    private static C5SourceRow Row(string? sPay, decimal? sub3) =>
        new("E", "0430", "X", "Name", "1150918", null, null, null, null,
            sPay, 2, 10, 20, 30, 40, sub3);
}
