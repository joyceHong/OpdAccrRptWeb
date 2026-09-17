using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C16LegacyReducerTests
{
    private readonly C16LegacyReducer _reducer = new();

    [Fact]
    public void Reduce_CombinesOnlyAdjacentKeysAndPreservesOrder()
    {
        C16SourceRow[] rows = [Row(0, 1, "1-25-99", 10), Row(1, 1, "1-49-99", 20),
            Row(2, 2, "1-25-99", 30), Row(3, 1, "1-50-99", 40)];
        IReadOnlyList<C16ReportRow> result = _reducer.Reduce(rows, C16Source.OutpatientEmergency);
        Assert.Equal(3, result.Count);
        Assert.Equal((10m, 20m), (result[0].Rl25, result[0].Rl49));
        Assert.Equal([0, 1, 2], result.Select(row => row.EncounterOrdinal));
    }

    [Theory]
    [InlineData("30", "104", "A")]
    [InlineData("30", "105", "B")]
    [InlineData("30", "106", "C")]
    [InlineData("99", "104", "D")]
    [InlineData("99", "105", "E")]
    [InlineData("30", "137", "F")]
    [InlineData("30", "139", "G")]
    [InlineData("30", "999", "X")]
    [InlineData("30", "107", "")]
    public void Classify_MatchesLegacy(string p1, string p2, string expected) =>
        Assert.Equal(expected, C16LegacyReducer.Classify(p1, p2));

    [Fact]
    public void Reduce_InpatientUsesFirstDiagnosisAndDaysExcludeDischargeDay()
    {
        C16ReportRow result = Assert.Single(_reducer.Reduce(
            [Row(0, 1, "1-49-99", 20) with { VisitDate = "1150901", DischargeDate = "1150903", FirstInpatientDiagnosis = "A01" }],
            C16Source.Inpatient));
        Assert.Equal((2, "A01"), (result.Days, result.Diagnosis));
    }

    [Fact]
    public void Reduce_DrugPartPayAndSourceFormulasMatchContract()
    {
        C16ReportRow result = Assert.Single(_reducer.Reduce(
            [Row(0, 1, "1-25-99", 10), Row(1, 1, "1-50-99", 5), Row(2, 1, "49-U1", 0, 30)],
            C16Source.OutpatientEmergency));
        Assert.Equal((30m, -30m, 15m), (result.Rl49, result.Rl49Drug, result.OutpatientTotal));
    }

    [Fact]
    public void Reduce_UnknownOrderDoesNotCreateOrMutateOutput() =>
        Assert.Empty(_reducer.Reduce([Row(0,1,"UNKNOWN",999)],C16Source.OutpatientEmergency));

    private static C16SourceRow Row(int ordinal, decimal encounter, string order, decimal sub5, decimal sub2 = 0) =>
        new(ordinal, "Name", "A123", "0900101", "1150901", "1150902", "Sec", "Z00", null,
            "30", "104", order, "1", "000001", encounter, sub5, sub2);
}
