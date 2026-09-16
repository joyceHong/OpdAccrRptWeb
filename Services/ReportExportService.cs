using System.Globalization;
using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class ReportExportService : IReportExportService, IReportWorkbookGenerator
{
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IHealthCenterRepository _repository;
    private readonly IReportExportJobStore _jobStore;
    private readonly IReportExportQueue _queue;
    private readonly ReportExportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly IServiceScopeFactory? _scopeFactory;

    public ReportExportService(
        IHealthCenterRepository repository,
        IReportExportJobStore jobStore,
        IReportExportQueue queue,
        IOptions<ReportExportOptions> options,
        TimeProvider timeProvider,
        IServiceScopeFactory? scopeFactory = null)
    {
        _repository = repository;
        _jobStore = jobStore;
        _queue = queue;
        _options = options.Value;
        _timeProvider = timeProvider;
        _scopeFactory = scopeFactory;
    }

    public ReportExportDispatchResult Dispatch(SearchReportCondition searchCondition)
    {
        if (searchCondition.ReportCode == "C144")
        {
            using var stream = new MemoryStream();
            GenerateC144Workbook(searchCondition, stream);
            string source = searchCondition.Source == C144Sources.Inpatient ? "I" : "O";
            return new ReportExportDispatchResult(
                stream.ToArray(),
                $"C144_{source}_{searchCondition.StartDate}_{searchCondition.EndDate}.xlsx",
                null);
        }
        if (searchCondition.ReportCode == "C10")
        {
            using var stream = new MemoryStream();
            GenerateC10Workbook(searchCondition, stream);
            return new ReportExportDispatchResult(
                stream.ToArray(),
                $"C10_{_timeProvider.GetUtcNow():yyyyMMdd_HHmmss}.xlsx",
                null);
        }
        var normalized = Normalize(searchCondition);
        var totalCount = _repository.GetHealthCenterContractBillingReportCount(normalized);
        if (totalCount <= _options.SynchronousRowLimit)
        {
            using var stream = new MemoryStream();
            GenerateWorkbook(normalized, stream);
            return new ReportExportDispatchResult(
                stream.ToArray(),
                CreateFileName(_timeProvider.GetUtcNow()),
                null);
        }

        var job = _jobStore.Create(normalized);
        if (!_queue.TryEnqueue(job))
        {
            _jobStore.Remove(job.JobId);
            return new ReportExportDispatchResult(null, null, null, QueueFull: true);
        }
        return new ReportExportDispatchResult(null, null, job);
    }

    public ReportExportJob? GetJob(Guid jobId) => _jobStore.Get(jobId);

    public ReportExportDownloadResult GetDownload(Guid jobId)
    {
        var job = _jobStore.Get(jobId);
        if (job?.Status != ReportExportJobStatus.Ready)
        {
            return new ReportExportDownloadResult(job, null);
        }

        var path = _jobStore.GetCompletedPath(job);
        return File.Exists(path)
            ? new ReportExportDownloadResult(job, new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            : new ReportExportDownloadResult(job, null);
    }

    public void GenerateWorkbook(SearchReportCondition searchCondition, Stream destination)
    {
        if (searchCondition.ReportCode == "C144")
        {
            GenerateC144Workbook(searchCondition, destination);
            return;
        }
        if (searchCondition.ReportCode == "C10")
        {
            GenerateC10Workbook(searchCondition, destination);
            return;
        }
        using var document = SpreadsheetDocument.Create(destination, SpreadsheetDocumentType.Workbook, true);
        var workbookPart = document.AddWorkbookPart();
        using (var workbookWriter = OpenXmlWriter.Create(workbookPart))
        {
            workbookWriter.WriteStartElement(new Workbook());
            workbookWriter.WriteStartElement(new Sheets());
            workbookWriter.WriteElement(new Sheet { Name = "C174", SheetId = 1U, Id = "rId1" });
            workbookWriter.WriteEndElement();
            workbookWriter.WriteEndElement();
        }

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>("rId1");
        using var worksheetWriter = OpenXmlWriter.Create(worksheetPart);
        worksheetWriter.WriteStartElement(new Worksheet());
        worksheetWriter.WriteStartElement(new SheetData());

        var columns = _repository.GetHealthCenterContractBillingReportColumns();
        WriteRow(worksheetWriter, columns.Select(column => (object?)column.Label));

        var properties = typeof(HealthCenterContractBillingReport)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(
                property => char.ToLowerInvariant(property.Name[0]) + property.Name[1..],
                StringComparer.OrdinalIgnoreCase);
        var offset = 0;
        while (true)
        {
            var batch = _repository.GetHealthCenterContractBillingReportBatch(
                searchCondition,
                offset,
                _options.BatchSize);
            foreach (var item in batch)
            {
                WriteRow(worksheetWriter, columns.Select(column => properties[column.Key].GetValue(item)));
            }

            if (batch.Count < _options.BatchSize)
            {
                break;
            }
            offset += batch.Count;
        }

        worksheetWriter.WriteEndElement();
        worksheetWriter.WriteEndElement();
    }

    private void GenerateC10Workbook(SearchReportCondition condition, Stream destination)
    {
        if (_scopeFactory is null)
            throw new InvalidOperationException("C10 匯出服務尚未設定 scope factory。");
        SearchReportCondition normalized = NormalizeC10(condition);
        using IServiceScope scope = _scopeFactory.CreateScope();
        IReportService reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        ReportDataAndColumns<C10ReceivableDetailRow> result = reportService.ReportC10Async(normalized)
            .GetAwaiter().GetResult();

        using var document = SpreadsheetDocument.Create(destination, SpreadsheetDocumentType.Workbook, true);
        var workbookPart = document.AddWorkbookPart();
        using (var workbookWriter = OpenXmlWriter.Create(workbookPart))
        {
            workbookWriter.WriteStartElement(new Workbook());
            workbookWriter.WriteStartElement(new Sheets());
            workbookWriter.WriteElement(new Sheet { Name = "C10", SheetId = 1U, Id = "rId1" });
            workbookWriter.WriteEndElement();
            workbookWriter.WriteEndElement();
        }
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>("rId1");
        using var worksheetWriter = OpenXmlWriter.Create(worksheetPart);
        worksheetWriter.WriteStartElement(new Worksheet());
        worksheetWriter.WriteStartElement(new SheetData());
        List<ModelDescriptionsHelper.PropertyMetadata> columns = result.Columns ?? [];
        WriteRow(worksheetWriter, columns.Select(column => (object?)column.Label));
        var properties = typeof(C10ReceivableDetailRow).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => char.ToLowerInvariant(property.Name[0]) + property.Name[1..],
                StringComparer.OrdinalIgnoreCase);
        foreach (C10ReceivableDetailRow row in result.Data ?? [])
            WriteRow(worksheetWriter, columns.Select(column => properties[column.Key].GetValue(row)));
        worksheetWriter.WriteEndElement();
        worksheetWriter.WriteEndElement();
    }

    private void GenerateC144Workbook(SearchReportCondition condition, Stream destination)
    {
        if (_scopeFactory is null)
            throw new InvalidOperationException("C144 匯出服務尚未設定 scope factory。");
        SearchReportCondition normalized = NormalizeC144(condition);
        using IServiceScope scope = _scopeFactory.CreateScope();
        IC144DebtDetailReportService reportService =
            scope.ServiceProvider.GetRequiredService<IC144DebtDetailReportService>();
        IC144XlsxRenderer renderer = scope.ServiceProvider.GetRequiredService<IC144XlsxRenderer>();
        IReadOnlyList<C144DebtDetailReportViewModel> rows = reportService
            .QueryAllAsync(normalized).GetAwaiter().GetResult();
        string sourceLabel = normalized.Source == C144Sources.Inpatient ? "住院" : "門急";
        C144Query query = C144DebtDetailReportService.CreateQuery(normalized, allowUnpaged: true);
        byte[] workbook = renderer.Render(rows, $"{sourceLabel} {query.StartDate}~{query.EndDate}");
        destination.Write(workbook);
    }

    internal static SearchReportCondition NormalizeC144(SearchReportCondition condition)
    {
        _ = C144DebtDetailReportService.CreateQuery(condition, allowUnpaged: true);
        return new SearchReportCondition
        {
            ReportCode = "C144",
            StartDate = condition.StartDate,
            EndDate = condition.EndDate,
            Source = condition.Source,
            PageNumber = 1,
            PageSize = 10
        };
    }

    internal static SearchReportCondition NormalizeC10(SearchReportCondition condition)
    {
        if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start)
            || !DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end)
            || start > end
            || !C10Sources.IsSupported(condition.Source)
            || !C10RoomScopes.IsSupported(condition.RoomScope)
            || condition.Source == C10Sources.Inpatient && condition.RoomScope != C10RoomScopes.All)
            throw new ArgumentException("C10 匯出條件不正確。", nameof(condition));
        return new SearchReportCondition
        {
            ReportCode = "C10",
            StartDate = condition.StartDate,
            EndDate = condition.EndDate,
            Source = condition.Source,
            RoomScope = condition.RoomScope,
            MedicalRecordNo = string.IsNullOrWhiteSpace(condition.MedicalRecordNo)
                ? null
                : condition.MedicalRecordNo.Trim().ToUpperInvariant(),
            PageNumber = 1,
            PageSize = int.MaxValue
        };
    }

    internal static SearchReportCondition Normalize(SearchReportCondition condition)
    {
        if (condition.ReportCode != "C174")
        {
            throw new ArgumentException("僅支援 C174 報表匯出。", nameof(condition));
        }
        if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate)
            || !DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate)
            || startDate > endDate)
        {
            throw new ArgumentException("請輸入有效的起始日期與截止日期。", nameof(condition));
        }

        return new SearchReportCondition
        {
            ReportCode = "C174",
            StartDate = DateTimeExtensions.ToRocDateString(startDate.ToDateTime(TimeOnly.MinValue)),
            EndDate = DateTimeExtensions.ToRocDateString(endDate.ToDateTime(TimeOnly.MinValue))
        };
    }

    internal static string CreateFileName(DateTimeOffset timestamp) =>
        $"C174_{timestamp:yyyyMMdd_HHmmss}.xlsx";

    private static void WriteRow(OpenXmlWriter writer, IEnumerable<object?> values)
    {
        writer.WriteStartElement(new Row());
        foreach (var value in values)
        {
            if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
            {
                writer.WriteElement(new Cell
                {
                    DataType = CellValues.Number,
                    CellValue = new CellValue(FormatCellValue(value))
                });
            }
            else
            {
                writer.WriteElement(new Cell
                {
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(FormatCellValue(value)))
                });
            }
        }
        writer.WriteEndElement();
    }

    private static string FormatCellValue(object? value) => value switch
    {
        null => string.Empty,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };
}
