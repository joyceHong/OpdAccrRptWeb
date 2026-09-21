using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Tests;

public sealed class C5RequestValidationTests
{
    [Theory]
    [InlineData("", "2026-09-18")]
    [InlineData("2026-02-30", "2026-03-01")]
    [InlineData("1911-12-31", "1912-01-01")]
    [InlineData("2026-09-19", "2026-09-18")]
    public void Validate_RejectsInvalidDateRanges(string start, string end)
    {
        Assert.Throws<ArgumentException>(() => new C5ReportRequest(start, end).Validate());
    }

    [Fact]
    public void Validate_RejectsNewAndLegacyCodesTogether()
    {
        var request = new C5ReportRequest("2026-09-18", "2026-09-18",
            NewOrganizationUnitCode: " 11910 ", LegacySectionCode: "0201");

        ArgumentException exception = Assert.Throws<ArgumentException>(request.Validate);

        Assert.Contains("不得同時", exception.Message);
    }

    [Fact]
    public void Validate_NormalizesCodesAndConvertsGregorianDates()
    {
        var request = new C5ReportRequest("2026-09-18", "2026-09-19",
            NewOrganizationUnitCode: " ab12 ", RoomNo: " op_a ", ChargeCode: " x-1 ",
            InsuranceIdentityCode: " a1 ");

        C5ValidatedRequest result = request.Validate();

        Assert.Equal("1150918", result.RocStartDate);
        Assert.Equal("1150919", result.RocEndDate);
        Assert.Equal("AB12", result.NewOrganizationUnitCode);
        Assert.Equal("OP_A", result.RoomNo);
        Assert.Equal("X-1", result.ChargeCode);
        Assert.Equal("A1", result.InsuranceIdentityCode);
    }

    [Fact]
    public void Validate_ClearsOutpatientOnlyFieldsForInpatient()
    {
        var request = new C5ReportRequest("2026-09-18", "2026-09-18",
            C5DataSource.Inpatient, EncounterType: C5EncounterType.Emergency, RoomNo: "0000");

        C5ValidatedRequest result = request.Validate();

        Assert.Equal(C5EncounterType.All, result.EncounterType);
        Assert.Null(result.RoomNo);
    }

    [Fact]
    public void Validate_TreatsEmptyInsuranceIdentityAsAll()
    {
        var request = new C5ReportRequest("2026-09-18", "2026-09-18",
            InsuranceIdentityCode: " ");

        C5ValidatedRequest result = request.Validate();

        Assert.Null(result.InsuranceIdentityCode);
    }
}
