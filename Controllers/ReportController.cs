using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using System.Globalization;

namespace OpdAccrRptWeb.Controllers;

public sealed class ReportController : Controller
{
    private readonly IReportCatalogService _reportCatalogService;
    private readonly IReportService _reportService;
    private readonly IReportExportService _reportExportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportCatalogService reportCatalogService,
        IReportService reportService,
        IReportExportService reportExportService,
        ILogger<ReportController> logger)
    {
        _reportCatalogService = reportCatalogService;
        _reportService = reportService;
        _reportExportService = reportExportService;
        _logger = logger;
    }

    [HttpGet("/")]
    public IActionResult Root()
    {
        return Redirect("/Report");
    }

    [HttpGet("Report/{reportCode?}")]
    public IActionResult Index(string? reportCode = null)
    {
        ReportIndexViewModel viewModel = _reportCatalogService.GetReportIndex();
        return View(viewModel);
    }

    [HttpPost("Report/GetReportData")]
    public IActionResult GetReportData([FromBody] SearchReportCondition searchCondition)
    {
        if (searchCondition.ReportCode == "C1")
        {
            IActionResult? validationResult = ValidateC1Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C22")
        {
            IActionResult? validationResult = ValidateC22Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C25")
        {
            IActionResult? validationResult = ValidateC25Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C27")
        {
            IActionResult? validationResult = ValidateC27Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C28")
        {
            IActionResult? validationResult = ValidateC28Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C29")
        {
            IActionResult? validationResult = ValidateC29Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C18")
        {
            IActionResult? validationResult = ValidateC18Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C19")
        {
            IActionResult? validationResult = ValidateC19Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode is "C1" or "C22" or "C25" or "C27" or "C28" or "C29" or "C171" or "C174" or "C18" or "C19")
        {
            searchCondition.PageNumber ??= 1;
            searchCondition.PageSize ??= 10;

            if (searchCondition.PageNumber <= 0)
            {
                return BadRequest("頁碼必須大於零。");
            }

            if (searchCondition.PageSize is not (10 or 30 or 50))
            {
                return BadRequest("每頁筆數僅接受 10、30 或 50。");
            }
        }

        try
        {
            return searchCondition.ReportCode switch
            {
                "C1" => Ok(_reportService.ReportDataAndColumns<SurgicalAccountingReportViewModel>(searchCondition)),
                "C22" => Ok(_reportService.ReportDataAndColumns<CashierCashReportViewModel>(searchCondition)),
                "C25" => Ok(_reportService.ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(searchCondition)),
                "C27" => Ok(_reportService.ReportDataAndColumns<AssistiveDeviceDepositBalanceReportViewModel>(searchCondition)),
                "C28" => Ok(_reportService.ReportDataAndColumns<InpatientReceivableBalanceReportViewModel>(searchCondition)),
                "C29" => Ok(_reportService.ReportDataAndColumns<ContractPaymentDetailReportViewModel>(searchCondition)),
                "C171" => Ok(_reportService.ReportDataAndColumns<HealthCenterDetailViewModel>(searchCondition)),
                "C172" => Ok(_reportService.ReportDataAndColumns<HealthCenterCountViewModel>(searchCondition)),
                "C173" => Ok(_reportService.ReportDataAndColumns<HealthCheckupVisits>(searchCondition)),
                "C174" => Ok(_reportService.ReportDataAndColumns<HealthCenterContractBillingReport>(searchCondition)),
                "C18" => Ok(_reportService.ReportDataAndColumns<ReferralMemberReportViewModel>(searchCondition)),
                "C19" => Ok(_reportService.ReportDataAndColumns<SafeNeedleReportViewModel>(searchCondition)),
                _ => Ok(null)
            };
        }
        catch (Exception exception)
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogError(
                exception,
                "報表查詢失敗。TraceId: {TraceId}, ReportCode: {ReportCode}, StartDate: {StartDate}, EndDate: {EndDate}, EncounterSource: {EncounterSource}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                traceId,
                searchCondition.ReportCode,
                searchCondition.StartDate,
                searchCondition.EndDate,
                searchCondition.EncounterSource,
                searchCondition.PageNumber,
                searchCondition.PageSize);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "查詢報表時發生錯誤，請提供追蹤碼給系統管理人員。"
            };
            problemDetails.Extensions["traceId"] = traceId;
            return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
        }
    }

    [HttpPost("Report/Export")]
    public IActionResult Export([FromBody] SearchReportCondition searchCondition)
    {
        if (searchCondition.ReportCode != "C174"
            || !TryParseDate(searchCondition.StartDate, out var startDate)
            || !TryParseDate(searchCondition.EndDate, out var endDate)
            || startDate > endDate)
        {
            return BadRequest("僅支援有效日期區間的 C174 報表匯出。");
        }

        try
        {
            var result = _reportExportService.Dispatch(searchCondition);
            if (result.QueueFull)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "背景匯出工作繁忙，請稍後再試。"
                });
            }
            if (result.Workbook is not null)
            {
                return File(result.Workbook, ReportExportService.ExcelContentType, result.FileName);
            }

            return Accepted(ToResponse(result.Job!));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            return ExportFailure(exception, searchCondition.ReportCode);
        }
    }

    [HttpGet("Report/Export/{jobId}")]
    public IActionResult GetExportStatus(string jobId)
    {
        if (!Guid.TryParseExact(jobId, "D", out var parsedJobId))
        {
            return BadRequest("匯出工作識別碼格式不正確。");
        }
        var job = _reportExportService.GetJob(parsedJobId);
        return job is null ? NotFound() : Ok(ToResponse(job));
    }

    [HttpGet("Report/Export/{jobId}/download")]
    public IActionResult DownloadExport(string jobId)
    {
        if (!Guid.TryParseExact(jobId, "D", out var parsedJobId))
        {
            return BadRequest("匯出工作識別碼格式不正確。");
        }

        var result = _reportExportService.GetDownload(parsedJobId);
        if (result.Job is null)
        {
            return NotFound();
        }
        if (result.Job.Status == ReportExportJobStatus.Expired)
        {
            return StatusCode(StatusCodes.Status410Gone, "匯出檔案已過期，請重新申請。");
        }
        if (result.Job.Status != ReportExportJobStatus.Ready)
        {
            return Conflict("匯出檔案尚未完成。");
        }
        if (result.Content is null)
        {
            return NotFound();
        }
        return File(result.Content, ReportExportService.ExcelContentType, result.Job.FileName);
    }

    private ReportExportJobResponse ToResponse(ReportExportJob job)
    {
        var statusUrl = $"/Report/Export/{job.JobId:D}";
        var downloadUrl = job.Status == ReportExportJobStatus.Ready
            ? $"/Report/Export/{job.JobId:D}/download"
            : null;
        return new ReportExportJobResponse(
            job.JobId,
            job.Status.ToString(),
            job.CreatedAt,
            statusUrl,
            job.StartedAt,
            job.CompletedAt,
            job.ExpiresAt,
            downloadUrl,
            job.Message);
    }

    private ObjectResult ExportFailure(Exception exception, string? reportCode)
    {
        var traceId = HttpContext.TraceIdentifier;
        _logger.LogError(exception, "報表匯出失敗。TraceId: {TraceId}, ReportCode: {ReportCode}", traceId, reportCode);
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "產生報表匯出檔案時發生錯誤，請提供追蹤碼給系統管理人員。"
        };
        details.Extensions["traceId"] = traceId;
        return StatusCode(StatusCodes.Status500InternalServerError, details);
    }

    private BadRequestObjectResult? ValidateC18Condition(SearchReportCondition searchCondition)
    {
        if (!EncounterSources.IsSupported(searchCondition.EncounterSource))
        {
            return BadRequest("就醫來源僅接受急診或住院。");
        }

        if (!TryParseDate(searchCondition.StartDate, out DateOnly startDate)
            || !TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return BadRequest("請輸入有效的起始日期與截止日期。");
        }

        if (startDate > endDate)
        {
            return BadRequest("起始日期不可晚於截止日期。");
        }

        if (startDate.Year != endDate.Year)
        {
            return BadRequest("C18 起訖日期必須屬於同一民國年度。");
        }

        return null;
    }

    private BadRequestObjectResult? ValidateC1Condition(SearchReportCondition searchCondition)
    {
        if (!TryParseDate(searchCondition.StartDate, out DateOnly startDate)
            || !TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return BadRequest("請輸入有效的 C1 起始日期與截止日期。");
        }

        if (startDate > endDate)
        {
            return BadRequest("C1 起始日期不可晚於截止日期。");
        }

        return null;
    }

    private BadRequestObjectResult? ValidateC22Condition(SearchReportCondition searchCondition)
    {
        if (!TryParseDate(searchCondition.StartDate, out DateOnly startDate)
            || !TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return BadRequest("請輸入有效的 C22 起始日期與截止日期。");
        }
        if (startDate > endDate)
        {
            return BadRequest("C22 起始日期不可晚於截止日期。");
        }
        if (!CashierCashSortTypes.IsSupported(searchCondition.CashierCashSortType))
        {
            return BadRequest("C22 排序方式僅接受依櫃員或依門急診。");
        }
        searchCondition.CashierUserId = string.IsNullOrWhiteSpace(searchCondition.CashierUserId)
            ? null
            : searchCondition.CashierUserId.Trim();
        return null;
    }

    private BadRequestObjectResult? ValidateC25Condition(SearchReportCondition searchCondition)
    {
        if (!TryParseDate(searchCondition.StartDate, out DateOnly startDate)
            || !TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return BadRequest("請輸入有效的 C25 起始日期與截止日期。");
        }

        if (startDate > endDate)
        {
            return BadRequest("C25 起始日期不可晚於截止日期。");
        }

        return null;
    }

    private BadRequestObjectResult? ValidateC27Condition(SearchReportCondition searchCondition)
    {
        if (!TryParseDate(searchCondition.EndDate, out _))
        {
            return BadRequest("請輸入有效的 C27 截止日期。");
        }

        return null;
    }

    private BadRequestObjectResult? ValidateC28Condition(SearchReportCondition searchCondition)
    {
        if (!TryParseDate(searchCondition.EndDate, out _))
        {
            return BadRequest("請輸入有效的 C28 截止日期。");
        }

        return null;
    }

    private BadRequestObjectResult? ValidateC29Condition(SearchReportCondition searchCondition)
    {
        if (!EncounterSources.IsSupported(searchCondition.EncounterSource))
        {
            return BadRequest("C29 就醫來源僅接受門急診或住院。");
        }

        if (!TryParseDate(searchCondition.StartDate, out DateOnly startDate)
            || !TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return BadRequest("請輸入有效的 C29 起始日期與截止日期。");
        }

        if (startDate > endDate)
        {
            return BadRequest("C29 起始日期不可晚於截止日期。");
        }

        searchCondition.BillingCode = string.IsNullOrWhiteSpace(searchCondition.BillingCode)
            ? null
            : searchCondition.BillingCode.Trim();
        return null;
    }

    private BadRequestObjectResult? ValidateC19Condition(SearchReportCondition searchCondition)
    {
        if (!EncounterSources.IsSupported(searchCondition.EncounterSource))
        {
            return BadRequest("C19 就醫來源僅接受門急診或住院。");
        }

        if (!TryParseDate(searchCondition.StartDate, out DateOnly startDate)
            || !TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return BadRequest("請輸入有效的 C19 查詢日期。");
        }

        if (startDate != endDate)
        {
            return BadRequest("C19 僅限查詢單日資料，起始日期與截止日期必須相同。");
        }

        return null;
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        bool parsed = DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
        return parsed && date.Year >= 1912;
    }
}
