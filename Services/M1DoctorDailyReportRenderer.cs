using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class M1DoctorDailyReportRenderer : IM1DoctorDailyReportRenderer
{
    private static readonly string[] Headers =
    [
        "科別", "醫師代號及姓名", "民眾人數", "健保人數", "預約人數",
        "預約比例", "合計", "上午人數", "下午人數", "夜間人數"
    ];

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
            sheetData.Append(Row("亞東紀念醫院", "醫師看診人數日表"));
            sheetData.Append(Row("程式名稱：ReportProject1.OpdDocDay",
                $"處理日期：{ToRocDateTime(snapshot.GeneratedAt)}", $"查詢日期：{ToRocDate(snapshot.ReportDate)}",
                $"製表人：{userName}", "頁次：1"));
            sheetData.Append(Row(Headers.Cast<object>().ToArray()));
            foreach ((string visitType, string label) in new[] { ("R", "門診"), ("E", "急診") })
            {
                M1DoctorDailyReportRow[] rows = snapshot.Rows
                    .Where(item => item.VisitType == visitType).ToArray();
                if (rows.Length == 0) continue;
                sheetData.Append(Row(label));
                foreach (M1DoctorDailyReportRow item in rows)
                {
                    M1DoctorDailyOutputRow output = M1DoctorDailyOutputRow.From(item);
                    sheetData.Append(Row(output.SectionNo, output.DoctorDisplay, output.SelfPayCount,
                        output.InsuranceCount, output.AppointmentCount,
                        M1DoctorDailyOutputRow.FormatAppointmentRatio(output.AppointmentRatio),
                        output.TotalCount, output.MorningCount, output.AfternoonCount,
                        output.NightCount));
                }
            }
            int appointmentTotal = snapshot.Rows.Sum(row => row.AppointmentCount);
            int grandTotal = snapshot.Rows.Sum(row => row.TotalCount);
            sheetData.Append(Row("", "合計",
                snapshot.Rows.Sum(row => row.SelfPayCount),
                snapshot.Rows.Sum(row => row.InsuranceCount), appointmentTotal,
                M1DoctorDailyOutputRow.FormatAppointmentRatio(
                    M1DoctorDailyOutputRow.CalculateAppointmentRatio(appointmentTotal, grandTotal)),
                grandTotal,
                snapshot.Rows.Sum(row => row.MorningCount),
                snapshot.Rows.Sum(row => row.AfternoonCount),
                snapshot.Rows.Sum(row => row.NightCount)));
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

    private static string ToRocDate(DateOnly value) =>
        $"{value.Year - 1911:000}/{value.Month:00}/{value.Day:00}";

    private static string ToRocDateTime(DateTimeOffset value) =>
        $"{value.Year - 1911:000}/{value.Month:00}/{value.Day:00} {value:HH:mm}";

}
