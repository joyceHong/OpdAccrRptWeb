using DocumentFormat.OpenXml.Packaging;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class M3OpdEmergencyDailyReportRendererTests
{
    [Fact]
    public void Renderers_UseSnapshotAndExpectedMediaTypes()
    {
        var snapshot = new M3OpdEmergencyDailyReportSnapshot("id","actor",new(2026,9,24),
            DateTimeOffset.Parse("2026-09-24T10:00:00+08:00"),[M3ReportRunStoreTests.Row()],M3ReportRunStoreTests.Kpis(),123);
        var renderer = new M3OpdEmergencyDailyReportRenderer();
        var xlsx=renderer.RenderXlsx(snapshot,"actor");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",xlsx.ContentType);
        using var stream=new MemoryStream(xlsx.Content); using SpreadsheetDocument doc=SpreadsheetDocument.Open(stream,false);
        string text=doc.WorkbookPart!.WorksheetParts.First().Worksheet.InnerText;
        Assert.Equal("M3門急診日報表", doc.WorkbookPart.Workbook.Sheets!.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>().Single().Name!.Value);
        Assert.Contains("0450",text); Assert.Contains("淨預約",text);
        Assert.Equal(31,M3OpdEmergencyDailyReportRenderer.RowValues(snapshot.Rows[0]).Length);
    }
}
