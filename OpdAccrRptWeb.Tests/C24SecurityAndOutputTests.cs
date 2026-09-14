using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C24SecurityAndOutputTests
{
    [Theory]
    [InlineData(12170, true)]
    [InlineData(12541, true)]
    [InlineData(904, false)]
    [InlineData(12899, false)]
    [InlineData(1, false)]
    public void RetryPolicy_AllowsOnlyTransientOracleErrors(int code, bool expected) =>
        Assert.Equal(expected, C24OracleFailurePolicy.IsTransient(code));

    [Fact]
    public void RetryPolicy_IsFinite() => Assert.InRange(C24OracleFailurePolicy.MaximumAttempts, 1, 3);

    [Fact]
    public void EveryRenderer_ConsumesTheSameCanonicalCollections()
    {
        var result = new C24CanonicalResult
        {
            Details = [], Summaries = [], RunId = "run", CorrelationId = "trace"
        };
        IC24CanonicalResultRenderer[] renderers =
            [new C24HtmlRenderer(), new C24JsonRenderer(), new C24PdfRenderer(), new C24XlsxRenderer()];
        foreach (var renderer in renderers)
        {
            var payload = renderer.Render(result);
            Assert.Same(result.Details, payload.Details);
            Assert.Same(result.Summaries, payload.Summaries);
        }
    }
}
