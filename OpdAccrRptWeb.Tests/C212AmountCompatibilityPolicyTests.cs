using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C212AmountCompatibilityPolicyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("123.45")]
    [InlineData("-987.65")]
    public void Convert_PreservesDecimalUntilLegacyPolicyIsApproved(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(amount, new C212PreserveDecimalAmountPolicy().Convert(amount));
    }
}
