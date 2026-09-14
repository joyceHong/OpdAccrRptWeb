using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C10SecurityAndOutputTests
{
    [Fact]
    public void ExportNormalization_PreservesCanonicalFiltersAndRequestsCompleteResult()
    {
        SearchReportCondition normalized = ReportExportService.NormalizeC10(new SearchReportCondition
        {
            ReportCode = "C10",
            StartDate = "2026-09-01",
            EndDate = "2026-09-02",
            Source = C10Sources.OpdEr,
            RoomScope = C10RoomScopes.Outpatient,
            MedicalRecordNo = " ab12 "
        });

        Assert.Equal("AB12", normalized.MedicalRecordNo);
        Assert.Equal(C10RoomScopes.Outpatient, normalized.RoomScope);
        Assert.Equal(1, normalized.PageNumber);
        Assert.Equal(int.MaxValue, normalized.PageSize);
    }

    [Theory]
    [InlineData("Inpatient", "Emergency")]
    [InlineData("Unknown", "All")]
    public void ExportNormalization_RejectsInvalidSourceScopeMatrix(string source, string scope)
    {
        var condition = new SearchReportCondition
        {
            ReportCode = "C10",
            StartDate = "2026-09-01",
            EndDate = "2026-09-02",
            Source = source,
            RoomScope = scope
        };

        Assert.Throws<ArgumentException>(() => ReportExportService.NormalizeC10(condition));
    }

    [Fact]
    public void RuntimeSource_DoesNotReferenceLegacyMdbDaoOrCrystal()
    {
        string[] c10Files =
        [
            "Repositories/C10Sql.cs",
            "Repositories/C10ReceivableDetailRepository.cs",
            "Services/C10AmountCalculationService.cs",
            "ViewModels/C10ReceivableDetailReportViewModel.cs"
        ];
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        string source = string.Join('\n', c10Files.Select(path =>
            File.ReadAllText(Path.Combine(projectRoot, path))));

        Assert.DoesNotContain("ReportDB.mdb", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DAO", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Crystal", source, StringComparison.OrdinalIgnoreCase);
    }
}
