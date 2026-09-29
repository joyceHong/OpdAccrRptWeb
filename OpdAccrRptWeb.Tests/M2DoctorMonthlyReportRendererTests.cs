using DocumentFormat.OpenXml.Packaging;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class M2DoctorMonthlyReportRendererTests
{
    [Fact]
    public void XlsxUsesSnapshotOrderDailyValuesAndTotals()
    {
        var snapshot = M2DoctorMonthlyReportControllerTests.SnapshotValue();
        var renderer = new M2DoctorMonthlyReportRenderer();
        var xlsx = renderer.RenderXlsx(snapshot, "alice");
        using var stream = new MemoryStream(xlsx.Content); using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
        string text = document.WorkbookPart!.WorksheetParts.Single().Worksheet.InnerText;
        Assert.Contains("D01", text); Assert.Contains("D31", text); Assert.Contains("D1", text); Assert.Contains("總計", text);
    }
}
