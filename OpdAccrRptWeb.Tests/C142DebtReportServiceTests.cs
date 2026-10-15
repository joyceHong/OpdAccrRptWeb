using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MiniExcelLibs;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Xunit;

namespace OpdAccrRptWeb.Tests;

public sealed class C142DebtReportServiceTests
{
    [Fact]
    public void QueryKeepsValidRowsAndReportsReceiptErrors()
    {
        var repository = new FakeRepository();
        var service = new C142DebtReportService(repository, new MemoryCache(new MemoryCacheOptions()));
        C142Request request = new("2026-10-01", "2026-10-12", "Outpatient", " 01 ");

        using JsonDocument result = JsonSerializer.SerializeToDocument(service.Query(request, "user-a", default));
        JsonElement root = result.RootElement;
        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(12, root.GetProperty("summary").GetProperty("totalAmount").GetDecimal());
        Assert.Equal("查無收據", root.GetProperty("summary").GetProperty("errors")[0].GetProperty("Message").GetString());
        Assert.Equal("01", repository.LastSectionCode);
        Assert.Equal("1151001", repository.LastStartDate);
        Assert.DoesNotContain(root.GetProperty("columns").EnumerateArray(),
            column => column.GetProperty("Key").GetString() == "encounterKey");
    }

    [Fact]
    public void ExportUsesTheSuccessfulQuerySnapshotAndRejectsAnotherUser()
    {
        var repository = new FakeRepository();
        var service = new C142DebtReportService(repository, new MemoryCache(new MemoryCacheOptions()));
        C142Request request = new("2026-10-01", "2026-10-12", "Outpatient", null);
        using JsonDocument result = JsonSerializer.SerializeToDocument(service.Query(request, "user-a", default));
        string id = result.RootElement.GetProperty("snapshotId").GetString()!;

        Assert.Throws<ArgumentException>(() => service.Export(new(id, "General"), "user-b", default));
        byte[] workbook = service.Export(new(id, "General"), "user-a", default);
        using var stream = new MemoryStream(workbook);
        var rows = MiniExcel.Query(stream, useHeaderRow: true).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Contains("尚欠金額", ((IDictionary<string, object>)rows[0]).Keys);
        Assert.Equal(1, repository.QueryCount);
    }

    [Fact]
    public void TransactionExportKeepsFirstTransactionOnThePatientRow()
    {
        var repository = new FakeRepository {
            Transactions = [new("H", "1151001123456", "A1", "已還款"),
                new("Q", "1151001130000", "A2", "未定義狀態")]
        };
        var service = new C142DebtReportService(repository, new MemoryCache(new MemoryCacheOptions()));
        C142Request request = new("2026-10-01", "2026-10-12", "Outpatient", null);
        using JsonDocument result = JsonSerializer.SerializeToDocument(service.Query(request, "user-a", default));
        string id = result.RootElement.GetProperty("snapshotId").GetString()!;

        using var stream = new MemoryStream(service.Export(new(id, "Transactions"), "user-a", default));
        var rows = MiniExcel.Query(stream, useHeaderRow: true)
            .Cast<IDictionary<string, object>>().ToList();
        Assert.Equal("123", Convert.ToString(rows[0]["病歷號"]));
        Assert.Equal("欠款歸還", Convert.ToString(rows[0]["狀態"]));
        Assert.Equal("", Convert.ToString(rows[1]["狀態"]));
        Assert.Contains("尚欠金額", rows[0].Keys);
        Assert.Contains("備註說明", rows[0].Keys);
    }

    private sealed class FakeRepository : IC142DebtReportRepository
    {
        public int QueryCount { get; private set; }
        public string? LastStartDate { get; private set; }
        public string? LastSectionCode { get; private set; }
        public IReadOnlyList<C142Transaction> Transactions { get; init; } = [];
        public (List<C142DebtRow> Rows, List<C142RowError> Errors) Query(
            string startDate, string endDate, string source, string sectionCode, CancellationToken cancellationToken)
        {
            QueryCount++;
            LastStartDate = startDate;
            LastSectionCode = sectionCode;
            return ([new C142DebtRow {
                MedicalRecordNumber = "123", PatientName = "測試", VisitDate = "1151001",
                DifferenceAmount = 12, OutstandingAmount = 12, VisitTime = "A", RoomNumber = "0001", SequenceNumber = 1
            }], [new C142RowError("1151001,A,0001,2", "查無收據")]);
        }
        public (string? Phone, string? Address) GetContact(string? medicalRecordNumber, CancellationToken cancellationToken) => (null, null);
        public string? GetLargeOrder(C142DebtRow row, CancellationToken cancellationToken) => null;
        public decimal? GetOrderTotal(C142DebtRow row, CancellationToken cancellationToken) => null;
        public IReadOnlyList<C142Transaction> GetTransactions(string source, C142DebtRow row, CancellationToken cancellationToken) => Transactions;
    }
}
