using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class M1DoctorDailyReportRenderer : IM1DoctorDailyReportRenderer
{
    public M1RenderedFile RenderXlsx(M1DoctorDailyReportSnapshot snapshot, string userName)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var stream = new MemoryStream();
        using (SpreadsheetDocument document = SpreadsheetDocument.Create(
                   stream, SpreadsheetDocumentType.Workbook, true))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "M1醫師看診人數日表"
            });
            sheetData.Append(Row("醫師看診人數日表", $"查詢日期：{snapshot.ReportDate:yyyy-MM-dd}",
                $"產表時間：{snapshot.GeneratedAt:yyyy-MM-dd HH:mm:ss}", $"使用者：{userName}",
                "程式：OpdAccrRptWeb.M1"));
            sheetData.Append(Row("科別代碼", "科別名稱", "醫師代碼", "醫師姓名", "門急診",
                "自費", "健保", "上午", "下午", "夜間", "預約量", "合計"));
            foreach (M1DoctorDailyReportRow item in snapshot.Rows)
                sheetData.Append(Row(item.SectionNo, item.SectionName, item.DoctorNo, item.DoctorName,
                    item.VisitType, item.SelfPayCount, item.InsuranceCount, item.MorningCount,
                    item.AfternoonCount, item.NightCount, item.AppointmentCount, item.TotalCount));
            sheetData.Append(Row("總計", "", "", "", "",
                snapshot.Rows.Sum(row => row.SelfPayCount),
                snapshot.Rows.Sum(row => row.InsuranceCount),
                snapshot.Rows.Sum(row => row.MorningCount),
                snapshot.Rows.Sum(row => row.AfternoonCount),
                snapshot.Rows.Sum(row => row.NightCount),
                snapshot.Rows.Sum(row => row.AppointmentCount),
                snapshot.Rows.Sum(row => row.TotalCount)));
            worksheetPart.Worksheet.Save();
            workbookPart.Workbook.Save();
        }
        return new(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"M1-doctor-daily-{snapshot.ReportDate:yyyyMMdd}.xlsx");
    }

    private static DocumentFormat.OpenXml.Spreadsheet.Row Row(params object[] values)
    {
        var row = new DocumentFormat.OpenXml.Spreadsheet.Row();
        foreach (object value in values) row.Append(Cell(value));
        return row;
    }

    private static Cell Cell(object value) => value is int number
        ? new Cell { DataType = CellValues.Number, CellValue = new CellValue(number.ToString(CultureInfo.InvariantCulture)) }
        : new Cell { DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value.ToString() ?? string.Empty)) };

}
