using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C15LegacyReducerTests
{
    private readonly C15LegacyReducer _reducer = new();

    [Fact]
    public void VisitKey_SupportsEncounterNumberBeyondInt64()
    {
        decimal encounter = decimal.Parse("9999999999999999999999");
        var row = Source("696-001", 100m, encounterNumber: encounter);

        C15VisitKey key = C15VisitKey.From(row);

        Assert.Equal(encounter, key.EncounterNumber);
    }

    [Fact]
    public void Reduce_TypeOneCodes_MapToFourFieldsOnOneVisit()
    {
        var rows = new[]
        {
            Source("696-001", 100m), Source("696-002", 20m),
            Source("696-003", 30m), Source("696-004", 40m)
        };

        C15WorkingRow result = Assert.Single(_reducer.Reduce(rows));

        Assert.Equal((100m, 20m, 30m, 40m, "1"),
            (result.Rl001, result.Rl002, result.Rl003, result.Rl004, result.Type));
    }

    [Fact]
    public void Reduce_TypeTwoAndRepeatedCode_UseLastWriteWins()
    {
        var rows = new[]
        {
            Source("696-008", 100m), Source("696-008", 250m), Source("696-007", 50m)
        };

        C15WorkingRow result = Assert.Single(_reducer.Reduce(rows));

        Assert.Equal(250m, result.Rl001);
        Assert.Equal(50m, result.Rl002);
        Assert.Equal("2", result.Type);
    }

    [Fact]
    public void Reduce_MixedTypesRemainOneRowAndFinalSourceOverwritesSharedFields()
    {
        var rows = new[]
        {
            Source("696-003", 30m, patientName: "First", returnDate: "11509010000"),
            Source("696-008", 500m, patientName: "Final", returnDate: "11509161234")
        };

        C15WorkingRow result = Assert.Single(_reducer.Reduce(rows));

        Assert.Equal("Final", result.PatientName);
        Assert.Equal("1150916", result.ReturnDate);
        Assert.Equal(30m, result.Rl003);
        Assert.Equal(500m, result.Rl001);
        Assert.Equal("2", result.Type);
    }

    [Fact]
    public void Reduce_DifferentVisitKeyCreatesAnotherCanonicalRow()
    {
        var rows = new[]
        {
            Source("696-001", 10m),
            Source("696-002", 20m, encounterNumber: 2m)
        };

        IReadOnlyList<C15WorkingRow> result = _reducer.Reduce(rows);

        Assert.Equal(2, result.Count);
        Assert.Equal([0, 1], result.Select(row => row.EncounterOrdinal));
    }

    [Fact]
    public void Reduce_UnexpectedOrderCodeFailsContract()
    {
        Assert.Throws<C15UnexpectedOrderCodeException>(() =>
            _reducer.Reduce([Source("999-999", 10m)]));
    }

    private static C15SourceRow Source(
        string code,
        decimal amount,
        decimal encounterNumber = 1m,
        string patientName = "Patient",
        string returnDate = "11509160000") =>
        new(" 1150901 ", " 1 ", " 000001 ", encounterNumber, " MR001 ",
            patientName, returnDate, code, amount);
}
