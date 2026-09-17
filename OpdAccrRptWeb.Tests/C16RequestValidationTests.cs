using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Tests;

public sealed class C16RequestValidationTests
{
    [Fact]
    public void ValidRequest_ConvertsGregorianDatesAndNormalizesInpatientBasis()
    {
        var request = new C16PreviewRequest("2026-09-01", "2026-09-16", C16Source.Inpatient,
            C16ReportType.All, C16DateBasis.VisitDate, 1, 10);
        C16QueryPeriod period = request.ValidateAndCreatePeriod();
        Assert.Equal(("1150901", "1150916", C16DateBasis.AccountingDate),
            (period.StartDate, period.EndDate, request.EffectiveDateBasis));
    }

    [Theory]
    [InlineData("2026-02-30", "2026-03-01", 1, 10)]
    [InlineData("2026-03-02", "2026-03-01", 1, 10)]
    [InlineData("2026-03-01", "2026-03-02", 0, 10)]
    [InlineData("2026-03-01", "2026-03-02", 1, 20)]
    public void InvalidRequest_IsRejected(string start, string end, int page, int size) =>
        Assert.Throws<ArgumentException>(() => new C16PreviewRequest(start, end,
            C16Source.OutpatientEmergency, C16ReportType.All, C16DateBasis.AccountingDate, page, size)
            .ValidateAndCreatePeriod());
}
