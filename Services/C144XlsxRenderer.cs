using System.Globalization;
using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C144XlsxRenderer : IC144XlsxRenderer
{
    public byte[] Render(
        IReadOnlyList<C144DebtDetailReportViewModel> rows,
        string worksheetName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream();
        using (SpreadsheetDocument document = SpreadsheetDocument.Create(
                   stream, SpreadsheetDocumentType.Workbook, true))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = SanitizeWorksheetName(worksheetName)
            });

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;
            List<ModelDescriptionsHelper.PropertyMetadata> columns =
                C144DebtDetailReportService.GetColumns();
            var properties = typeof(C144DebtDetailReportViewModel)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(
                    property => char.ToLowerInvariant(property.Name[0]) + property.Name[1..],
                    StringComparer.OrdinalIgnoreCase);
            sheetData.Append(CreateRow(columns.Select(column => (object?)column.Label)));
            foreach (C144DebtDetailReportViewModel item in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sheetData.Append(CreateRow(columns.Select(column => properties[column.Key].GetValue(item))));
            }
            worksheetPart.Worksheet.Save();
            workbookPart.Workbook.Save();
        }
        return stream.ToArray();
    }

    internal static string SanitizeWorksheetName(string value)
    {
        char[] invalid = ['[', ']', ':', '*', '?', '/', '\\'];
        string sanitized = string.Concat(value.Select(character =>
            invalid.Contains(character) ? '_' : character)).Trim('\'');
        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "C144";
        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }

    private static Row CreateRow(IEnumerable<object?> values)
    {
        var row = new Row();
        foreach (object? value in values) row.Append(CreateCell(value));
        return row;
    }

    private static Cell CreateCell(object? value)
    {
        if (value is decimal or byte or sbyte or short or ushort or int or uint or long or ulong)
        {
            return new Cell
            {
                DataType = CellValues.Number,
                CellValue = new CellValue(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture))
            };
        }
        return new Cell
        {
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(value?.ToString() ?? string.Empty))
        };
    }
}
