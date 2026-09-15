using OpdAccrRptWeb.Services;
namespace OpdAccrRptWeb.Tests;
public sealed class C12LegacyAmountConverterTests
{
    private readonly C12LegacyAmountConverter _subject=new();
    [Theory][InlineData(null,0)][InlineData("0",0)][InlineData("-12",-12)][InlineData("2147483647",int.MaxValue)][InlineData("-2147483648",int.MinValue)]
    public void Convert_AcceptsVerifiedIntegers(string? text,int expected)=>Assert.Equal(expected,_subject.Convert(text is null?null:decimal.Parse(text)));
    [Theory][InlineData("0.5")][InlineData("-0.5")][InlineData("2147483648")]
    public void Convert_RejectsUnverifiedBoundaries(string text)=>Assert.Throws<C12CompatibilityException>(()=>_subject.Convert(decimal.Parse(text)));
}
