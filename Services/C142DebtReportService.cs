using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Caching.Memory;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC142DebtReportService
{
    object Query(C142Request request, string owner, CancellationToken cancellationToken);
    byte[] Export(C142ExportRequest request, string owner, CancellationToken cancellationToken);
}

public sealed class C142DebtReportService(IC142DebtReportRepository repository, IMemoryCache cache) : IC142DebtReportService
{
    private static readonly string[] Headings = ["病歷號", "姓名", "就診日", "出院日", "自費總額", "已繳金額",
        "折扣金額", "差額", "尚欠金額", "科別名稱", "醫師", "床位號", "卡號"];
    private static readonly IReadOnlyDictionary<string, string> TransactionTypes = new Dictionary<string, string>
    {
        ["1"]="更改身分", ["2"]="更改折扣", ["7"]="更改醫師", ["A"]="沖轉收據", ["B"]="沖轉欠款",
        ["C"]="全部作廢", ["D"]="補發收據", ["E"]="刪除批價內容", ["F"]="拆帳還款", ["G"]="繳費存檔",
        ["H"]="欠款歸還", ["I"]="更改健保卡序", ["J"]="入診察室", ["K"]="入留觀室", ["L"]="入手術室",
        ["M"]="自訂動態", ["N"]="修正成未看診", ["O"]="病患出院", ["P"]="更改部份負擔", ["R"]="急診帳轉住院",
        ["S"]="存檔", ["T"]="刪除欠款", ["U"]="修改批價內容", ["W"]="待床中", ["X"]="自動結帳", ["Y"]="手動結帳"
    };

    public object Query(C142Request request, string owner, CancellationToken cancellationToken)
    {
        Validate(request);
        C142Snapshot snapshot;
        string snapshotId;
        if (request.SnapshotId is { Length: > 0 } existingId)
        {
            snapshot = GetSnapshot(existingId, owner);
            if (snapshot.StartDate != request.StartDate || snapshot.EndDate != request.EndDate ||
                snapshot.Source != request.Source || snapshot.SectionCode != (request.SectionCode?.Trim() ?? ""))
                throw new ArgumentException("查詢條件已變更，請重新查詢。");
            snapshotId = existingId;
        }
        else
        {
            string start = ToRoc(request.StartDate!);
            string end = ToRoc(request.EndDate!);
            var (rows, errors) = repository.Query(start, end, request.Source!, request.SectionCode?.Trim() ?? "", cancellationToken);
            long total = 0;
            foreach (C142DebtRow row in rows)
                total = checked(Convert.ToInt64((decimal)total + row.DifferenceAmount));
            snapshot = new C142Snapshot(owner, request.StartDate!, request.EndDate!, request.Source!,
                request.SectionCode?.Trim() ?? "", rows, errors, total);
            snapshotId = Guid.NewGuid().ToString("N");
            cache.Set(CacheKey(snapshotId), snapshot, TimeSpan.FromMinutes(20));
        }
        int count = snapshot.Rows.Count;
        return new
        {
            columns = ModelDescriptionsHelper.GetPropertyDescriptions<C142DebtRow>()
                .Where(column => Headings.Contains(column.Label)).ToList(),
            data = snapshot.Rows.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList(),
            totalCount = count, pageNumber = request.PageNumber, pageSize = request.PageSize,
            totalPages = count == 0 ? 0 : (int)Math.Ceiling(count / (double)request.PageSize),
            summary = new { totalAmount = snapshot.TotalAmount, errors = snapshot.Errors }, snapshotId
        };
    }

    public byte[] Export(C142ExportRequest request, string owner, CancellationToken cancellationToken)
    {
        if (request.Mode is not ("General" or "Transactions")) throw new ArgumentException("Excel 模式不正確。");
        C142Snapshot snapshot = GetSnapshot(request.SnapshotId ?? "", owner);
        if (snapshot.Rows.Count == 0) throw new ArgumentException("查詢結果沒有可匯出的資料。");
        bool withTransactions = request.Mode == "Transactions";
        var output = new List<Dictionary<string, object?>>();
        foreach (C142DebtRow row in snapshot.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var contact = repository.GetContact(row.MedicalRecordNumber, cancellationToken);
            string? order = snapshot.Source == "Outpatient" ? repository.GetLargeOrder(row, cancellationToken) : null;
            Dictionary<string, object?> record = NewExportRow(withTransactions);
            object?[] values = [row.MedicalRecordNumber, row.PatientName, row.VisitDate, row.DischargeDate,
                row.SelfPayAmount, row.PaidAmount, row.DiscountAmount, row.DifferenceAmount,
                row.OutstandingAmount, row.SectionName, row.DoctorName, row.BedNumber, row.CardNumber];
            for (int index = 0; index < Headings.Length; index++) record[Headings[index]] = values[index];
            record["醫令／查詢鍵"] = snapshot.Source == "Outpatient" ? order : withTransactions ? null : row.EncounterKey;
            if (!withTransactions && snapshot.Source == "Outpatient")
            {
                decimal? orderTotal = repository.GetOrderTotal(row, cancellationToken);
                if (orderTotal > 360) record["醫令金額"] = orderTotal;
            }
            record["電話"] = contact.Phone;
            record["地址"] = contact.Address;
            if (withTransactions)
            {
                IReadOnlyList<C142Transaction> transactions = repository.GetTransactions(snapshot.Source, row, cancellationToken);
                if (transactions.Count > 0) FillTransaction(record, transactions[0]);
                output.Add(record);
                for (int index = 1; index < transactions.Count; index++)
                {
                    Dictionary<string, object?> extra = NewExportRow(true);
                    FillTransaction(extra, transactions[index]);
                    output.Add(extra);
                }
                // VB6 advances by the number of transactions after the last dynamic row.
                for (int index = 0; index < transactions.Count; index++) output.Add(NewExportRow(true));
            }
            else output.Add(record);
        }
        Dictionary<string, object?> totalRow = NewExportRow(withTransactions);
        totalRow["病歷號"] = "合計";
        totalRow["卡號"] = $"共 {snapshot.Rows.Count} 筆";
        totalRow["醫令／查詢鍵"] = snapshot.TotalAmount;
        output.Add(totalRow);
        using var stream = new MemoryStream();
        MiniExcel.SaveAs(stream, output, configuration: new OpenXmlConfiguration
        {
            EnableWriteNullValueCell = true,
            IgnoreEmptyRows = false
        });
        AddGridBorders(stream, output.Count + 1, output[0].Count, cancellationToken);
        return stream.ToArray();
    }

    private static void AddGridBorders(MemoryStream stream, int rowCount, int columnCount,
        CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using SpreadsheetDocument document = SpreadsheetDocument.Open(stream, true);
        WorkbookPart workbookPart = document.WorkbookPart
            ?? throw new InvalidOperationException("Excel 活頁簿缺少 WorkbookPart。");
        WorksheetPart worksheetPart = workbookPart.WorksheetParts.FirstOrDefault()
            ?? throw new InvalidOperationException("Excel 活頁簿缺少工作表。");
        Worksheet worksheet = worksheetPart.Worksheet;
        SheetData sheetData = worksheet.GetFirstChild<SheetData>() ?? worksheet.AppendChild(new SheetData());

        WorkbookStylesPart stylesPart = workbookPart.WorkbookStylesPart
            ?? throw new InvalidOperationException("Excel 活頁簿缺少樣式設定。");
        Stylesheet stylesheet = stylesPart.Stylesheet
            ?? throw new InvalidOperationException("Excel 活頁簿缺少樣式表。");
        Borders borders = stylesheet.Borders ??= new Borders();
        if (!borders.Elements<Border>().Any()) borders.AppendChild(new Border());
        uint borderId = (uint)borders.Elements<Border>().Count();
        borders.AppendChild(new Border(
            new LeftBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FFD9D9D9" } },
            new RightBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FFD9D9D9" } },
            new TopBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FFD9D9D9" } },
            new BottomBorder { Style = BorderStyleValues.Thin, Color = new Color { Rgb = "FFD9D9D9" } },
            new DiagonalBorder()));
        borders.Count = (uint)borders.Elements<Border>().Count();

        CellFormats cellFormats = stylesheet.CellFormats ??= new CellFormats();
        if (!cellFormats.Elements<CellFormat>().Any()) cellFormats.AppendChild(new CellFormat());
        var borderedStyles = new Dictionary<uint, uint>();
        uint GetBorderedStyle(uint styleIndex)
        {
            if (borderedStyles.TryGetValue(styleIndex, out uint borderedStyle)) return borderedStyle;

            uint styleCount = (uint)cellFormats.Elements<CellFormat>().Count();
            uint sourceStyleIndex = styleIndex < styleCount ? styleIndex : 0;
            CellFormat sourceFormat = cellFormats.Elements<CellFormat>().ElementAt((int)sourceStyleIndex);
            var newFormat = (CellFormat)sourceFormat.CloneNode(true);
            newFormat.BorderId = borderId;
            newFormat.ApplyBorder = true;
            borderedStyle = (uint)cellFormats.Elements<CellFormat>().Count();
            cellFormats.AppendChild(newFormat);
            borderedStyles[styleIndex] = borderedStyle;
            return borderedStyle;
        }

        var rowsByIndex = sheetData.Elements<Row>()
            .Where(row => row.RowIndex is not null)
            .ToDictionary(row => row.RowIndex!.Value);
        for (uint rowIndex = 1; rowIndex <= (uint)rowCount; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!rowsByIndex.TryGetValue(rowIndex, out Row? row) || row is null)
            {
                row = new Row { RowIndex = rowIndex };
                Row? nextRow = sheetData.Elements<Row>().FirstOrDefault(existing =>
                    existing.RowIndex is not null && existing.RowIndex.Value > rowIndex);
                if (nextRow is null) sheetData.AppendChild(row);
                else sheetData.InsertBefore(row, nextRow);
                rowsByIndex[rowIndex] = row;
            }

            for (int columnIndex = 1; columnIndex <= columnCount; columnIndex++)
            {
                string cellReference = $"{GetColumnName(columnIndex)}{rowIndex}";
                Cell? cell = row.Elements<Cell>().FirstOrDefault(existing => existing.CellReference?.Value == cellReference);
                if (cell is null)
                {
                    cell = new Cell { CellReference = cellReference };
                    Cell? nextCell = row.Elements<Cell>().FirstOrDefault(existing =>
                        GetColumnIndex(existing.CellReference?.Value) > columnIndex);
                    if (nextCell is null) row.AppendChild(cell);
                    else row.InsertBefore(cell, nextCell);
                }

                cell.StyleIndex = GetBorderedStyle(cell.StyleIndex?.Value ?? 0);
            }
        }

        borders.Count = (uint)borders.Elements<Border>().Count();
        cellFormats.Count = (uint)cellFormats.Elements<CellFormat>().Count();
        stylesheet.Save();
        if (worksheet.SheetDimension is { } dimension)
            dimension.Reference = $"A1:{GetColumnName(columnCount)}{rowCount}";
        worksheet.Save();
    }

    private static string GetColumnName(int columnIndex)
    {
        string columnName = string.Empty;
        while (columnIndex > 0)
        {
            columnIndex--;
            columnName = (char)('A' + columnIndex % 26) + columnName;
            columnIndex /= 26;
        }
        return columnName;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrEmpty(cellReference)) return int.MaxValue;

        int columnIndex = 0;
        foreach (char character in cellReference)
        {
            if (!char.IsLetter(character)) break;
            columnIndex = columnIndex * 26 + char.ToUpperInvariant(character) - 'A' + 1;
        }
        return columnIndex;
    }

    private static Dictionary<string, object?> NewExportRow(bool withTransactions)
    {
        var row = new Dictionary<string, object?>();
        foreach (string heading in Headings) row[heading] = null;
        row["醫令／查詢鍵"] = null;
        if (withTransactions)
        {
            row["狀態"] = null; row["動態備註"] = null;
            row["動態執行時間"] = null; row["動態經手人"] = null;
        }
        else { row["醫令金額"] = null; row["保留16"] = null; row["保留17"] = null; row["保留18"] = null; }
        row["電話"] = null; row["地址"] = null;
        if (withTransactions)
            foreach (string heading in new[] { "處理狀況", "電話通知日期", "催繳函寄出日", "病患主訴", "尚未繳款原因", "備註說明" })
                row[heading] = null;
        return row;
    }
    private static void FillTransaction(Dictionary<string, object?> row, C142Transaction transaction)
    {
        row["狀態"] = TransactionTypes.GetValueOrDefault(transaction.Type ?? "", "");
        row["動態備註"] = transaction.Note;
        string? time = transaction.DateTime;
        row["動態執行時間"] = time is { Length: >= 13 }
            ? $"{time[..3]}/{time.Substring(3, 2)}/{time.Substring(5, 2)} {time.Substring(7, 2)}:{time.Substring(9, 2)}:{time.Substring(11, 2)}"
            : time;
        row["動態經手人"] = transaction.User;
    }
    private C142Snapshot GetSnapshot(string id, string owner)
    {
        if (!Guid.TryParseExact(id, "N", out _) || !cache.TryGetValue(CacheKey(id), out C142Snapshot? snapshot)
            || snapshot is null || snapshot.Owner != owner)
            throw new ArgumentException("查詢資料已過期，請重新查詢。");
        return snapshot;
    }
    private static string CacheKey(string id) => $"C142:{id}";
    private static void Validate(C142Request request)
    {
        if (request.Source is not ("Outpatient" or "Emergency" or "Inpatient"))
            throw new ArgumentException("請選擇門診、急診或住院。");
        if (!DateOnly.TryParseExact(request.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start) ||
            !DateOnly.TryParseExact(request.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end) || start.Year < 1912 || end.Year < 1912 || start > end)
            throw new ArgumentException("請輸入有效的起迄日期。");
        if (request.PageNumber < 1 || request.PageSize is not (10 or 30 or 50))
            throw new ArgumentException("分頁條件不正確。");
    }
    private static string ToRoc(string date)
    {
        DateOnly value = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{value.Year - 1911:000}{value:MMdd}";
    }
}
