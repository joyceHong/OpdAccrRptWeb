using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C11LegacyCompatibilityTests
{
    [Theory]
    [InlineData("1130101abc", 1130101d)]
    [InlineData("  -12.5xyz", -12.5d)]
    [InlineData("abc", 0d)]
    [InlineData("", 0d)]
    public void VbVal_ReadsNumericPrefix(string value, double expected) =>
        Assert.Equal(expected, LegacyC11NumberConverter.VbVal(value));

    [Fact]
    public void Subtract_UsesAlreadyConvertedSingleOperands()
    {
        decimal total = 16_777_217m;
        decimal period = 1m;
        float expected = (float)((float)total - (float)period);
        Assert.Equal(expected, LegacyC11NumberConverter.Subtract(
            LegacyC11NumberConverter.ToSingle(total), LegacyC11NumberConverter.ToSingle(period)));
        Assert.NotEqual((float)(total - period), expected);
    }
}
