using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Tests;

public sealed class C9RequestValidationTests
{
    [Fact]
    public void Validate_ConvertsAcceptedDatesAtRepositoryBoundary()
    {
        var request = new C9ReportRequest(new(2026, 2, 27), new(2026, 3, 1), 1, 30);
        C9ValidatedRequest validated = request.Validate();
        Assert.Equal("1150227", C9ReportRequest.ToRoc(validated.StartDate));
        Assert.Equal("1150301", C9ReportRequest.ToRoc(validated.EndDate));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 20)]
    public void Validate_RejectsInvalidPaging(int page, int size) =>
        Assert.Throws<ArgumentException>(() =>
            new C9ReportRequest(new(2026, 1, 1), new(2026, 1, 1), page, size).Validate());

    [Fact]
    public void Validate_RejectsMissingPre1912AndReversedDates()
    {
        Assert.Throws<ArgumentException>(() => new C9ReportRequest(null, new(2026, 1, 1)).Validate());
        Assert.Throws<ArgumentException>(() => new C9ReportRequest(new(1911, 12, 31), new(2026, 1, 1)).Validate());
        Assert.Throws<ArgumentException>(() => new C9ReportRequest(new(2026, 1, 2), new(2026, 1, 1)).Validate());
    }
}
