using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C1SurgicalPrintServiceTests
{
    [Fact]
    public void CreatePreview_LoadsEntireRangeAndGroupsEmergencyBeforeOutpatient()
    {
        var repository = new FakeSurgicalAccountingRepository
        {
            Data =
            [
                new() { EncounterType = "o門診(3F1)", EncounterDate = "1151015", SurgicalOrderCode = "64202B" },
                new() { EncounterType = "e急診(4F8)", EncounterDate = "1151015", SurgicalOrderCode = "64164B" },
                new() { EncounterType = "o門診(3F1)", EncounterDate = "1151015", SurgicalOrderCode = "86007C" }
            ]
        };

        C1SurgicalPrintPreviewViewModel preview = new C1SurgicalPrintService(repository)
            .CreatePreview(new SearchReportCondition
            {
                StartDate = "2026-10-01",
                EndDate = "2026-10-19"
            }, "20021");

        Assert.Equal("1151001", repository.LastStartDate);
        Assert.Equal("1151019", repository.LastEndDate);
        Assert.Equal("115/10/01", preview.StartDate);
        Assert.Equal("115/10/19", preview.EndDate);
        Assert.Collection(preview.Pages,
            page =>
            {
                Assert.Equal("e急診(4F8)", page.EncounterType);
                Assert.Equal((1, 2), (page.PageNumber, page.TotalPages));
                Assert.Equal("64164B", Assert.Single(page.Rows).SurgicalOrderCode);
            },
            page =>
            {
                Assert.Equal("o門診(3F1)", page.EncounterType);
                Assert.Equal((2, 2), (page.PageNumber, page.TotalPages));
                Assert.Equal(["64202B", "86007C"], page.Rows.Select(row => row.SurgicalOrderCode));
            });
        Assert.Equal("20021", preview.GeneratedBy);
    }

    [Fact]
    public void CreatePreview_SplitsLongEncounterGroupAcrossPrintedPages()
    {
        var repository = new FakeSurgicalAccountingRepository
        {
            Data = Enumerable.Range(1, 25)
                .Select(number => new SurgicalAccountingReportViewModel
                {
                    EncounterType = "o門診(3F1)", SurgicalOrderCode = number.ToString()
                }).ToList()
        };

        C1SurgicalPrintPreviewViewModel preview = new C1SurgicalPrintService(repository)
            .CreatePreview(new SearchReportCondition
            {
                StartDate = "2026-10-01", EndDate = "2026-10-19"
            }, "20021");

        Assert.Equal([16, 9], preview.Pages.Select(page => page.Rows.Count));
        Assert.All(preview.Pages, page => Assert.Equal(2, page.TotalPages));
    }
}
