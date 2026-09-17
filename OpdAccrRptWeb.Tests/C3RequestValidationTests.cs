using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Tests;

public sealed class C3RequestValidationTests
{
    [Fact]
    public void Validate_NormalizesDatesDefaultsAndCodes()
    {
        var request = new C3ReportRequest("2026-05-14", "2026-05-15", CareSource.O,
            RoomCodes: [" 5d103 ", "op_1", "5D103"]);

        C3ValidatedRequest actual = request.Validate();

        Assert.Equal("1150514", actual.RocStartDate);
        Assert.Equal("1150515", actual.RocEndDate);
        Assert.Equal(ReportDetailType.Summary, actual.DetailType);
        Assert.Equal(LogisticsType.All, actual.LogisticsType);
        Assert.Equal(["5D103", "OP_1"], actual.RoomCodes);
    }

    [Theory]
    [InlineData("bad", "2026-05-15", 1, 10)]
    [InlineData("2026-05-16", "2026-05-15", 1, 10)]
    [InlineData("2026-05-14", "2026-05-15", 0, 10)]
    [InlineData("2026-05-14", "2026-05-15", 1, 20)]
    public void Validate_RejectsInvalidInputs(string start, string end, int page, int size)
    {
        var request = new C3ReportRequest(start, end, CareSource.O, PageNumber: page, PageSize: size);
        Assert.Throws<ArgumentException>(() => request.Validate());
    }

    [Fact]
    public void Validate_RejectsMoreThanFiftyCodesAndSqlFragments()
    {
        Assert.Throws<ArgumentException>(() => new C3ReportRequest("2026-05-14", "2026-05-15",
            CareSource.O, RoomCodes: Enumerable.Range(1, 51).Select(x => $"R{x}").ToArray()).Validate());
        Assert.Throws<ArgumentException>(() => C3ReportRequest.ParseCodes("A','B"));
    }
}
