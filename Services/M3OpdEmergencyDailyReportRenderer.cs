using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class M3OpdEmergencyDailyReportRenderer : IM3OpdEmergencyDailyReportRenderer
{
    private static readonly string[] Headers =
    [
        "科別代碼", "科別名稱", "門診初診自費", "門診初診健保", "門診複診自費", "門診複診健保",
        "急診初診自費", "急診初診健保", "急診複診自費", "急診複診健保", "當日自費", "當日健保",
        "月門診初診", "月門診複診", "月急診初診", "月急診複診", "月自費", "月健保",
        "年門診初診", "年門診複診", "年急診初診", "年急診複診", "年自費", "年健保",
        "日門診初診合計", "日門診複診合計", "日急診初診合計", "日急診複診合計", "日總計", "月總計", "年總計"
    ];

    public M3RenderedFile RenderXlsx(M3OpdEmergencyDailyReportSnapshot snapshot, string userName)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var stream = new MemoryStream();
        using (SpreadsheetDocument document = SpreadsheetDocument.Create(stream,
                   SpreadsheetDocumentType.Workbook, true))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);
            workbookPart.Workbook.AppendChild(new Sheets()).Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "M3門急診日報表"
            });
            sheetData.Append(Row("亞東紀念醫院門急診日報表", $"查詢日期：{snapshot.ReportDate:yyyy-MM-dd}",
                $"星期：{Weekday(snapshot.ReportDate)}", $"產表時間：{snapshot.GeneratedAt:yyyy-MM-dd HH:mm:ss}",
                "程式：OpdAccrRptWeb.M3", $"使用者：{userName}"));
            sheetData.Append(Row("門診早", snapshot.NineKpis.OutpatientMorning, "門診午", snapshot.NineKpis.OutpatientAfternoon,
                "門診夜", snapshot.NineKpis.OutpatientNight, "急診白", snapshot.NineKpis.EmergencyDay,
                "急診小夜", snapshot.NineKpis.EmergencyEvening, "急診大夜", snapshot.NineKpis.EmergencyNight,
                "預約", snapshot.NineKpis.Appointment, "未到", snapshot.NineKpis.NoShow, "淨預約", snapshot.NineKpis.NetAppointment));
            sheetData.Append(Row(Headers.Cast<object>().ToArray()));
            foreach (M3OpdEmergencyDailyReportRow item in snapshot.Rows) sheetData.Append(Row(RowValues(item)));
            worksheetPart.Worksheet.Save(); workbookPart.Workbook.Save();
        }
        return new(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"M3-opd-emergency-daily-{snapshot.ReportDate:yyyyMMdd}.xlsx");
    }

    internal static object[] RowValues(M3OpdEmergencyDailyReportRow r) =>
    [
        r.DepartmentId, r.DepartmentName, r.Op1SQty, r.Op1HQty, r.Op2SQty, r.Op2HQty,
        r.Em1SQty, r.Em1HQty, r.Em2SQty, r.Em2HQty, r.OESQty, r.OEHQty,
        r.Op1MonQty, r.Op2MonQty, r.Em1MonQty, r.Em2MonQty, r.OEMonSQty, r.OEMonHQty,
        r.Op1YearQty, r.Op2YearQty, r.Em1YearQty, r.Em2YearQty, r.OEYearSQty, r.OEYearHQty,
        r.Op1DaySum, r.Op2DaySum, r.Em1DaySum, r.Em2DaySum, r.OEDaySum, r.OEMonSum, r.OEYearSum
    ];
    private static DocumentFormat.OpenXml.Spreadsheet.Row Row(params object[] values)
    {
        var row = new DocumentFormat.OpenXml.Spreadsheet.Row();
        foreach (object value in values) row.Append(Cell(value));
        return row;
    }
    private static Cell Cell(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or decimal
        ? new Cell { DataType = CellValues.Number, CellValue = new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0") }
        : new Cell { DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value.ToString() ?? string.Empty)) };
    internal static string Weekday(DateOnly date) => date.DayOfWeek switch
    { DayOfWeek.Sunday => "星期日", DayOfWeek.Monday => "星期一", DayOfWeek.Tuesday => "星期二",
      DayOfWeek.Wednesday => "星期三", DayOfWeek.Thursday => "星期四", DayOfWeek.Friday => "星期五", _ => "星期六" };
}
