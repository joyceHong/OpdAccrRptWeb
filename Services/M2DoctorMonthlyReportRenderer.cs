using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class M2DoctorMonthlyReportRenderer : IM2DoctorMonthlyReportRenderer
{
    public M2RenderedFile RenderXlsx(M2DoctorMonthlyReportSnapshot snapshot, string userName)
    {
        Validate(snapshot);
        using var stream = new MemoryStream();
        using (SpreadsheetDocument document = SpreadsheetDocument.Create(stream,
                   SpreadsheetDocumentType.Workbook, true))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart(); workbookPart.Workbook = new Workbook();
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData(); worksheetPart.Worksheet = new Worksheet(sheetData);
            workbookPart.Workbook.AppendChild(new Sheets()).Append(new Sheet
            { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "M2醫師看診人數月表" });
            sheetData.Append(Row("醫師看診人數月表", $"查詢月份：{snapshot.ReportMonth:yyyy-MM}",
                $"口徑：{snapshot.CalculationBasis}", $"診別：{snapshot.VisitScope}",
                $"時段：{snapshot.TimeSlot}", $"產表時間：{snapshot.GeneratedAt:yyyy-MM-dd HH:mm:ss}",
                $"使用者：{userName}", "程式：OpdAccrRptWeb.M2"));
            sheetData.Append(Row(["科別代碼", "科別名稱", "醫師代碼", "醫師姓名",
                .. Enumerable.Range(1, 31).Select(day => $"D{day:00}"), "合計"]));
            foreach (M2DoctorMonthlyReportRow item in snapshot.Rows)
                sheetData.Append(Row([item.SectionNo, item.SectionName, item.DoctorNo, item.DoctorName,
                    .. item.DailyCounts.Cast<object>(), item.MonthlyTotal]));
            sheetData.Append(Row(["總計", "", "", "",
                .. Enumerable.Range(0, 31).Select(day => (object)snapshot.Rows.Sum(row => row.DailyCounts[day])),
                snapshot.Rows.Sum(row => row.MonthlyTotal)]));
            worksheetPart.Worksheet.Save(); workbookPart.Workbook.Save();
        }
        return new(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"M2-doctor-monthly-{snapshot.ReportMonth:yyyyMM}.xlsx");
    }

    private static void Validate(M2DoctorMonthlyReportSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Rows.Count == 0) throw new ArgumentException("M2 空結果不得輸出。", nameof(snapshot));
    }
    private static DocumentFormat.OpenXml.Spreadsheet.Row Row(params object[] values)
    {
        var row = new DocumentFormat.OpenXml.Spreadsheet.Row(); foreach (object value in values) row.Append(Cell(value)); return row;
    }
    private static Cell Cell(object value) => value is int number
        ? new Cell { DataType = CellValues.Number, CellValue = new CellValue(number.ToString(CultureInfo.InvariantCulture)) }
        : new Cell { DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value?.ToString() ?? string.Empty)) };
}
