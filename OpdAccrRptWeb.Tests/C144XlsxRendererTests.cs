using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C144XlsxRendererTests
{
    [Fact]
    public void Render_WritesExactHeadersAndPreservesNumericTextAndNullCells()
    {
        var renderer = new C144XlsxRenderer();
        byte[] content = renderer.Render([new C144DebtDetailReportViewModel
        {
            EncounterType = "E", PatientName = "測試病人", OutstandingAmount = -12m,
            DrugCopaymentAmount = null, InsuredRegistrationAmount = 25m
        }], "門急 1150901/1150916");

        using var stream = new MemoryStream(content);
        using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, false);
        WorksheetPart worksheet = document.WorkbookPart!.WorksheetParts.Single();
        Row[] rows = worksheet.Worksheet.Descendants<Row>().ToArray();
        Cell[] headers = rows[0].Elements<Cell>().ToArray();
        Cell[] values = rows[1].Elements<Cell>().ToArray();
        Assert.Equal(31, headers.Length);
        Assert.Equal("診別", headers[0].InnerText);
        Assert.Equal("健保身分掛號費", headers[^1].InnerText);
        Assert.Equal(CellValues.Number, values[13].DataType!.Value);
        Assert.Equal("-12", values[13].CellValue!.Text);
        Assert.Equal(string.Empty, values[18].InnerText);
        Assert.Equal("門急 1150901_1150916", document.WorkbookPart.Workbook.Sheets!.Elements<Sheet>().Single().Name!.Value);
    }
}
