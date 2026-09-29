using DocumentFormat.OpenXml.Packaging;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class M1DoctorDailyReportRendererTests
{
    [Fact]
    public void RenderXlsx_UsesSnapshotOrderAndTotals()
    {
        M1DoctorDailyReportSnapshot snapshot = Snapshot();
        var renderer = new M1DoctorDailyReportRenderer();

        var xlsx = renderer.RenderXlsx(snapshot, "alice");

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", xlsx.ContentType);
        using var stream = new MemoryStream(xlsx.Content);
        using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
        string text = document.WorkbookPart!.WorksheetParts.Single().Worksheet.InnerText;
        Assert.Contains("D1", text);
        Assert.Contains("D2", text);
        Assert.True(text.IndexOf("D1", StringComparison.Ordinal) < text.IndexOf("D2", StringComparison.Ordinal));
        Assert.Contains("總計", text);
    }

    internal static M1DoctorDailyReportSnapshot Snapshot() => new(
        "0123456789abcdef0123456789abcdef0123456789abcdef", "alice", new(2026, 9, 23),
        new DateTimeOffset(2026, 9, 24, 1, 2, 3, TimeSpan.Zero),
        [
            M1DoctorDailyReportRow.Create("0450", "急診", "D1", "甲醫師", "R", 1, 2, 3, 4, 5, 6),
            M1DoctorDailyReportRow.Create("0450", "急診", "D2", "乙醫師", "E", 7, 8, 9, 10, 11, 0)
        ]);
}
