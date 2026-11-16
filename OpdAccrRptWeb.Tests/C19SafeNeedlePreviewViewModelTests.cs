using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C19SafeNeedlePreviewViewModelTests
{
    [Fact]
    public void Create_EmergencyPairsTwoNeedlesAndOneInjectionByPosition()
    {
        var rows = new[]
        {
            Row("Y", "SDS3", "7A01", "Y001"),
            Row("X", "SICPU24", "7A01", "X001"),
            Row("X", "SICPU22", "7A02", "X002")
        };

        var preview = C19SafeNeedlePreviewViewModel.Create(
            new DateOnly(2026, 11, 13), EncounterSources.Emergency, rows, DateTimeOffset.Now);

        Assert.Equal("1151113", preview.RocReportDate);
        var page = Assert.Single(preview.Pages);
        Assert.Equal("ER", page.Station);
        var combined = Assert.Single(page.Rows);
        Assert.Equal("X001", combined.FirstNeedle?.MedicalRecordNumber);
        Assert.Equal("X002", combined.SecondNeedle?.MedicalRecordNumber);
        Assert.Equal("Y001", combined.Injection?.MedicalRecordNumber);
    }

    [Fact]
    public void Create_InpatientKeepsStationsSeparateAndRetainsInjectionOnlyRows()
    {
        var rows = new[]
        {
            Row("Y", "SDS3", "8B02", "Y002"),
            Row("X", "SICPU24", "7A01", "X001"),
            Row("Y", "SDS1", "7A02", "Y001")
        };

        var preview = C19SafeNeedlePreviewViewModel.Create(
            new DateOnly(2026, 11, 13), EncounterSources.Inpatient, rows, DateTimeOffset.Now);

        Assert.Equal(new[] { "7A0", "8B0" }, preview.Pages.Select(page => page.Station));
        Assert.NotNull(preview.Pages[0].Rows[0].FirstNeedle);
        Assert.NotNull(preview.Pages[0].Rows[0].Injection);
        Assert.Null(preview.Pages[1].Rows[0].FirstNeedle);
        Assert.Equal("Y002", preview.Pages[1].Rows[0].Injection?.MedicalRecordNumber);
    }

    private static SafeNeedleReportViewModel Row(string category, string code, string bed, string mrn) => new()
    {
        Category = category,
        OrderCode = code,
        OrderDate = "1151113",
        BedNumber = bed,
        MedicalRecordNumber = mrn,
        PatientName = "測試"
    };
}
