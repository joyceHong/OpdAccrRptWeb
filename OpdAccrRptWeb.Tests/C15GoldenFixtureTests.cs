using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C15GoldenFixtureTests
{
    [Theory]
    [InlineData("2026-09-16", "2026-09-16", "1150916", "1150916")]
    [InlineData("2026-09-15", "2026-09-16", "1150915", "1150916")]
    [InlineData("2026-08-31", "2026-09-02", "1150831", "1150902")]
    public void DateFixtures_PreserveSingleCrossDayAndCrossMonthRocBoundaries(
        string start,
        string end,
        string rocStart,
        string rocEnd)
    {
        var period = C15AssistiveDeviceDepositDetailRepository.BuildPeriod(new SearchReportCondition
        {
            StartDate = start,
            EndDate = end
        });

        Assert.Equal(rocStart, period.StartDate);
        Assert.Equal(rocEnd, period.EndDate);
    }

    [Fact]
    public void SqlFixture_PreservesDcNullZeroAndSevenCharacterReturnDateRules()
    {
        string sql = C15AssistiveDeviceDepositDetailRepository.QuerySql;

        Assert.Contains("a.chOp4DC IN ('0', '2')", sql);
        Assert.Contains("o.chOp4Stat <> 'DC'", sql);
        Assert.Contains("a.chOp4DCDate BETWEEN :StartDate AND :EndDate", sql);
        Assert.DoesNotContain("EndDate || '9999'", sql);
        Assert.Contains("o.rlOp4Sub5 + o.rlOp4Sub6 <> 0", sql);
        Assert.DoesNotContain("NVL", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrderedSourceFixture_MatchesLegacyLastWriteAndMixedTypeCanonicalRows()
    {
        C15SourceRow[] source =
        [
            Row(1m, "696-001", 100m, "First", "11509010000"),
            Row(1m, "696-001", 250m, "Second", "11509020000"),
            Row(1m, "696-003", 30m, "Third", "11509030000"),
            Row(1m, "696-008", 500m, "Final", "11509041234"),
            Row(2m, "696-007", 50m, "Other", string.Empty)
        ];

        IReadOnlyList<C15WorkingRow> actual = new C15LegacyReducer().Reduce(source);

        Assert.Collection(actual,
            first =>
            {
                Assert.Equal(("MR1", "Final", "1150904", 500m, 30m, "2", 0),
                    (first.MedicalRecordNumber, first.PatientName, first.ReturnDate,
                        first.Rl001, first.Rl003, first.Type, first.EncounterOrdinal));
            },
            second =>
            {
                Assert.Equal(("MR2", 50m, "2", 1),
                    (second.MedicalRecordNumber, second.Rl002, second.Type, second.EncounterOrdinal));
            });
    }

    private static C15SourceRow Row(
        decimal encounter,
        string code,
        decimal amount,
        string patient,
        string returnDate) =>
        new("1150901", "1", "000001", encounter, $"MR{encounter:0}", patient,
            returnDate, code, amount);
}
