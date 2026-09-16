using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using OpdAccrRptWeb.Help;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Controllers;

public sealed class ReportController : Controller
{
    private readonly IReportCatalogService _reportCatalogService;
    private readonly IReportService _reportService;
    private readonly IReportExportService _reportExportService;
    private readonly ILogger<ReportController> _logger;
    private readonly IC21AccountingSummaryRepository? _c21Repository;
    private readonly IOptions<C21Options>? _c21Options;
    private readonly IC23ContractAccountingRepository? _c23Repository;
    private readonly IOptions<C23Options>? _c23Options;
    private readonly IC211ContractBalanceRepository? _c211Repository;
    private readonly IC12ReportService? _c12ReportService;
    private readonly IC12ReportRepository? _c12Repository;
    private readonly IC13HighRiskEmergencyReportService? _c13ReportService;

    public ReportController(
        IReportCatalogService reportCatalogService,
        IReportService reportService,
        IReportExportService reportExportService,
        ILogger<ReportController> logger,
        IC21AccountingSummaryRepository? c21Repository = null,
        IOptions<C21Options>? c21Options = null,
        IC23ContractAccountingRepository? c23Repository = null,
        IOptions<C23Options>? c23Options = null,
        IC211ContractBalanceRepository? c211Repository = null,
        IC12ReportService? c12ReportService = null,
        IC13HighRiskEmergencyReportService? c13ReportService = null,
        IC12ReportRepository? c12Repository = null)
    {
        _reportCatalogService = reportCatalogService;
        _reportService = reportService;
        _reportExportService = reportExportService;
        _logger = logger;
        _c21Repository = c21Repository;
        _c21Options = c21Options;
        _c23Repository = c23Repository;
        _c23Options = c23Options;
        _c211Repository = c211Repository;
        _c12ReportService = c12ReportService;
        _c12Repository = c12Repository;
        _c13ReportService = c13ReportService;
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
        viewModel.C21RebuildEnabled = _c21Options?.Value.RebuildEnabled ?? false;
        viewModel.C23RebuildEnabled = _c23Options?.Value.RebuildEnabled ?? false;
        return View(viewModel);
    }

    [HttpPost("Report/GetReportData")]
    public IActionResult GetReportData([FromBody] SearchReportCondition searchCondition)
    {
        if (searchCondition.ReportCode == "C10")
        {
            IActionResult? validationResult = ValidateC10Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }

        if (searchCondition.ReportCode == "C11")
        {
            IActionResult? validationResult = ValidateC11Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }

        if (searchCondition.ReportCode == "C12")
        {
            IActionResult? validationResult = ValidateC12Condition(searchCondition, out _);
            if (validationResult is not null) return validationResult;
        }

        if (searchCondition.ReportCode == "C13")
        {
            IActionResult? validationResult = ValidateC13Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }

        if (searchCondition.ReportCode == "C15")
        {
            IActionResult? validationResult = ValidateC15Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }

        if (searchCondition.ReportCode == "C143")
        {
            IActionResult? validationResult = ValidateC143Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }

        if (searchCondition.ReportCode == "C144")
        {
            IActionResult? validationResult = ValidateC144Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }

        if (searchCondition.ReportCode == "C211")
        {
            IActionResult? validationResult = ValidateC211Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }
        if (searchCondition.ReportCode == "C212")
        {
            IActionResult? validationResult = ValidateC212Condition(searchCondition, out _);
            if (validationResult is not null) return validationResult;
        }
        if (searchCondition.ReportCode == "C21")
        {
            IActionResult? validationResult = ValidateC21Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C23")
        {
            IActionResult? validationResult = ValidateC23Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }

        if (searchCondition.ReportCode == "C24")
        {
            IActionResult? validationResult = ValidateC24Condition(searchCondition);
            if (validationResult is not null) return validationResult;
        }

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

        if (searchCondition.ReportCode == "C213")
        {
            IActionResult? validationResult = ValidateC213Condition(searchCondition);
            if (validationResult is not null)
            {
                return validationResult;
            }
        }

        if (searchCondition.ReportCode == "C214")
        {
            IActionResult? validationResult = ValidateC214Condition(searchCondition);
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

        if (searchCondition.ReportCode is "C1" or "C10" or "C11" or "C12" or "C13" or "C15" or "C143" or "C144" or "C21" or "C22" or "C23" or "C24" or "C213" or "C214" or "C25" or "C27" or "C28" or "C29" or "C171" or "C174" or "C18" or "C19")
        {
            searchCondition.PageNumber ??= 1;
            searchCondition.PageSize ??= 10;

            if (searchCondition.PageNumber <= 0)
            {
                if (searchCondition.ReportCode == "C214")
                {
                    return C214ValidationProblem("頁碼必須大於零。");
                }

                return BadRequest("頁碼必須大於零。");
            }

            bool isC15CompletePreview = searchCondition.ReportCode == "C15"
                && searchCondition.PageNumber == 1
                && searchCondition.PageSize > 0;
            if (!isC15CompletePreview && searchCondition.PageSize is not (10 or 30 or 50))
            {
                if (searchCondition.ReportCode == "C214")
                {
                    return C214ValidationProblem("每頁筆數僅接受 10、30 或 50。");
                }

                return BadRequest("每頁筆數僅接受 10、30 或 50。");
            }
        }

        try
        {
            if (searchCondition.ReportCode == "C13")
            {
                Response.Headers.CacheControl = "private, no-store";
                Response.Headers.Pragma = "no-cache";
            }
            if (searchCondition.ReportCode == "C15")
            {
                Response.Headers.CacheControl = "private, no-store";
                Response.Headers.Pragma = "no-cache";
                return Ok(_reportService.ReportC15Async(
                    searchCondition, HttpContext.RequestAborted).GetAwaiter().GetResult());
            }
            if (searchCondition.ReportCode == "C143")
            {
                Response.Headers.CacheControl = "private, no-store";
                return Ok(_reportService.ReportC143Async(
                    searchCondition, HttpContext.RequestAborted).GetAwaiter().GetResult());
            }
            if (searchCondition.ReportCode == "C144")
            {
                Response.Headers.CacheControl = "private, no-store";
                Response.Headers.Pragma = "no-cache";
                return Ok(_reportService.ReportC144Async(
                    searchCondition, HttpContext.RequestAborted).GetAwaiter().GetResult());
            }
            if (searchCondition.ReportCode == "C10")
            {
                Response.Headers.CacheControl = "private, no-store";
                return Ok(_reportService.ReportC10Async(
                    searchCondition,
                    HttpContext.RequestAborted).GetAwaiter().GetResult());
            }
            if (searchCondition.ReportCode == "C11")
            {
                Response.Headers.CacheControl = "private, no-store";
                string generatedBy = User.Identity?.IsAuthenticated == true
                    ? User.Identity.Name ?? string.Empty
                    : string.Empty;
                return Ok(_reportService.ReportC11Async(
                    searchCondition, generatedBy, HttpContext.RequestAborted).GetAwaiter().GetResult());
            }
            if (searchCondition.ReportCode == "C12")
            {
                Response.Headers.CacheControl = "private, no-store";
                ValidateC12Condition(searchCondition, out C12ReportRequest request);
                string userId = User.Identity?.IsAuthenticated == true ? User.Identity.Name ?? string.Empty : string.Empty;
                C12MedicalReceiptSummaryViewModel result = (_c12ReportService ?? throw new InvalidOperationException("C12 report service 尚未設定。"))
                    .CreateAsync(request,userId,HttpContext.RequestAborted).GetAwaiter().GetResult();
                return result.HasVisits ? Ok(result) : NotFound(new ProblemDetails { Status=404,Title="找不到資料！" });
            }
            if (searchCondition.ReportCode == "C211")
            {
                Response.Headers.CacheControl = "private, no-store";
                var userId = User.Identity?.IsAuthenticated == true
                    ? User.Identity.Name ?? string.Empty
                    : string.Empty;
                return Ok(_reportService.ReportC211Async(
                    searchCondition, userId, HttpContext.RequestAborted).GetAwaiter().GetResult());
            }
            if (searchCondition.ReportCode == "C212")
            {
                Response.Headers.CacheControl = "private, no-store";
                ValidateC212Condition(searchCondition, out C212Query query);
                var userId = User.Identity?.IsAuthenticated == true
                    ? User.Identity.Name ?? string.Empty
                    : string.Empty;
                return Ok(_reportService.ReportC212Async(
                    query, userId, HttpContext.TraceIdentifier, HttpContext.RequestAborted)
                    .GetAwaiter().GetResult());
            }
            return searchCondition.ReportCode switch
            {
                "C1" => Ok(_reportService.ReportDataAndColumns<SurgicalAccountingReportViewModel>(searchCondition)),
                "C13" => Ok(_reportService.ReportDataAndColumns<C13HighRiskEmergencyReportViewModel>(searchCondition)),
                "C21" => Ok(_reportService.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(searchCondition)),
                "C23" => Ok(_reportService.ReportDataAndColumns<C23ContractAccountingReportViewModel>(searchCondition)),
                "C24" => Ok(_reportService.ReportDataAndColumns<C24DebtPaymentDetail>(searchCondition)),
                "C22" => Ok(_reportService.ReportDataAndColumns<CashierCashReportViewModel>(searchCondition)),
                "C213" => Ok(_reportService.ReportDataAndColumns<CashierCashSummaryReportViewModel>(searchCondition)),
                "C214" => Ok(_reportService.ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(searchCondition)),
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
        catch (C21RebuildForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = exception.Message
            });
        }
        catch (C23RebuildForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = exception.Message
            });
        }
        catch (C12AccessDeniedException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden,new ProblemDetails{Status=403,Title=exception.Message});
        }
        catch (OperationCanceledException) when (searchCondition.ReportCode is "C10" or "C11" or "C12" or "C13" or "C15" or "C143" or "C144" or "C211" or "C212")
        {
            return StatusCode(499);
        }
        catch (OracleException exception) when (searchCondition.ReportCode is "C10" or "C11" or "C12" or "C13" or "C15" or "C143" or "C144" or "C211" or "C212")
        {
            var traceId = HttpContext.TraceIdentifier;
            var unavailable = IsOracleConnectionFailure(exception.Number);
            _logger.LogWarning(
                "{ReportCode} Oracle 查詢失敗。TraceId: {TraceId}, ErrorNumber: {ErrorNumber}, Category: {Category}",
                searchCondition.ReportCode, traceId, exception.Number, unavailable ? "Unavailable" : "TimeoutOrQueryFailure");
            var status = unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status504GatewayTimeout;
            var details = new ProblemDetails
            {
                Status = status,
                Title = unavailable ? "資料庫服務暫時無法使用，請稍後再試。" : "查詢逾時，請稍後再試。"
            };
            details.Extensions["traceId"] = traceId;
            return StatusCode(status, details);
        }
        catch (Exception exception)
        {
            var traceId = HttpContext.TraceIdentifier;
            if (searchCondition.ReportCode is "C10" or "C11" or "C12" or "C13" or "C15" or "C143" or "C144" or "C211" or "C212")
            {
                _logger.LogError(
                    "{ReportCode} 查詢發生未預期錯誤。TraceId: {TraceId}, ExceptionType: {ExceptionType}",
                    searchCondition.ReportCode, traceId, exception.GetType().FullName);
                var safeDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "查詢報表時發生錯誤，請提供追蹤碼給系統管理人員。"
                };
                safeDetails.Extensions["traceId"] = traceId;
                return StatusCode(StatusCodes.Status500InternalServerError, safeDetails);
            }
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

    [HttpGet("Report/GetC12SectionOptions")]
    public async Task<IActionResult> GetC12SectionOptions(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<C12SectionOption> options = await (_c12Repository
                ?? throw new InvalidOperationException("C12 report repository 尚未設定。"))
                .QuerySectionOptionsAsync(cancellationToken);
            return Ok(options);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (OracleException exception)
        {
            string traceId = HttpContext.TraceIdentifier;
            _logger.LogWarning("C12 科別候選查詢失敗。TraceId: {TraceId}, ErrorNumber: {ErrorNumber}", traceId, exception.Number);
            var details = new ProblemDetails { Status = StatusCodes.Status503ServiceUnavailable, Title = "無法載入科別清單，仍可直接輸入科別代碼。" };
            details.Extensions["traceId"] = traceId;
            return StatusCode(details.Status.Value, details);
        }
        catch (Exception exception)
        {
            string traceId = HttpContext.TraceIdentifier;
            _logger.LogError("C12 科別候選發生未預期錯誤。TraceId: {TraceId}, ExceptionType: {ExceptionType}", traceId, exception.GetType().FullName);
            var details = new ProblemDetails { Status = StatusCodes.Status503ServiceUnavailable, Title = "無法載入科別清單，仍可直接輸入科別代碼。" };
            details.Extensions["traceId"] = traceId;
            return StatusCode(details.Status.Value, details);
        }
    }

    [HttpPost("Report/C13/Preview")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult PreviewC13([FromBody] SearchReportCondition condition)
    {
        condition.ReportCode = "C13";
        IActionResult? validationResult = ValidateC13Condition(condition);
        if (validationResult is not null) return validationResult;

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
        try
        {
            string generatedBy = User.Identity?.IsAuthenticated == true
                ? User.Identity.Name ?? string.Empty
                : string.Empty;
            C13PreviewViewModel preview = (_c13ReportService
                ?? throw new InvalidOperationException("C13 report service 尚未設定。"))
                .CreatePreview(condition, generatedBy, HttpContext.RequestAborted);
            if (preview.Rows.Count == 0)
                return NotFound(new ProblemDetails { Status = 404, Title = "查無此筆資料" });
            return PartialView("_C13HighRiskEmergencyPreview", preview);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499);
        }
        catch (OracleException exception)
        {
            string traceId = HttpContext.TraceIdentifier;
            bool unavailable = IsOracleConnectionFailure(exception.Number);
            _logger.LogWarning(
                "C13 preview failed. TraceId={TraceId} ErrorNumber={ErrorNumber} Category={Category}",
                traceId, exception.Number, unavailable ? "Unavailable" : "TimeoutOrQueryFailure");
            int status = unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status504GatewayTimeout;
            var details = new ProblemDetails { Status = status, Title = "無法建立報表預覽，請稍後再試。" };
            details.Extensions["traceId"] = traceId;
            return StatusCode(status, details);
        }
        catch (Exception exception)
        {
            string traceId = HttpContext.TraceIdentifier;
            _logger.LogError(
                "C13 preview failed. TraceId={TraceId} ExceptionType={ExceptionType}",
                traceId, exception.GetType().FullName);
            var details = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "無法建立報表預覽，請提供追蹤碼給系統管理人員。"
            };
            details.Extensions["traceId"] = traceId;
            return StatusCode(StatusCodes.Status500InternalServerError, details);
        }
    }

    [HttpGet("Report/C21/BillingItems")]
    public IActionResult GetC21BillingItems()
    {
        if (_c21Repository is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        return Ok(_c21Repository.GetBillingItems());
    }

    [HttpGet("Report/C23/Contracts")]
    public IActionResult GetC23Contracts()
    {
        if (_c23Repository is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
        return Ok(_c23Repository.GetContracts());
    }

    [HttpGet("Report/C211/Contracts")]
    public async Task<IActionResult> GetC211Contracts(CancellationToken cancellationToken)
    {
        if (_c211Repository is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
        Response.Headers.CacheControl = "private, no-store";
        return Ok(await _c211Repository.GetContractChoicesAsync(cancellationToken));
    }

    private BadRequestObjectResult? ValidateC211Condition(SearchReportCondition condition)
    {
        if (!C211Sources.IsSupported(condition.EncounterSource))
            return BadRequest("C211 資料來源僅接受門急或住院。");
        if (!TryParseDate(condition.EndDate, out _))
            return BadRequest("請輸入有效的資料迄日。");
        if (!string.IsNullOrWhiteSpace(condition.StartDate))
            return BadRequest("C211 不接受起始日期。");
        condition.StartDate = null;
        condition.ContractCode = string.IsNullOrWhiteSpace(condition.ContractCode)
            ? null
            : condition.ContractCode.Trim();
        return null;
    }

    private BadRequestObjectResult? ValidateC212Condition(
        SearchReportCondition condition,
        out C212Query query)
    {
        query = default!;
        if (!string.IsNullOrWhiteSpace(condition.StartDate))
        {
            return BadRequest("C212 不接受起始日期。");
        }
        condition.StartDate = null;
        if (!TryParseDate(condition.EndDate, out DateOnly endDate)
            || endDate.Year > 2910)
        {
            return BadRequest("請輸入有效的 C212 資料迄日。");
        }

        query = new C212Query(endDate);
        return null;
    }

    private BadRequestObjectResult? ValidateC23Condition(SearchReportCondition condition)
    {
        if (!TryParseDate(condition.StartDate, out var startDate) || !TryParseDate(condition.EndDate, out var endDate))
            return BadRequest("請輸入有效的 C23 起始日期與截止日期。");
        if (startDate > endDate) return BadRequest("C23 起始日期不可晚於截止日期。");
        if (!C23EncounterSources.IsSupported(condition.EncounterSource))
            return BadRequest("C23 來源僅接受門急診或住院。");
        condition.DateMode = string.IsNullOrWhiteSpace(condition.DateMode) ? C23DateModes.General : condition.DateMode;
        if (!C23DateModes.IsSupported(condition.DateMode)) return BadRequest("C23 日期模式不正確。");
        if (condition.DateMode == C23DateModes.EncounterDate)
        {
            if (startDate.Year != endDate.Year || startDate.Month != endDate.Month)
                return BadRequest("C23 就診日模式起訖日期必須在同一月份。");
            condition.InpatientType = null;
        }
        else if (condition.EncounterSource == C23EncounterSources.Inpatient
                 && !C23InpatientTypes.IsSupported(condition.InpatientType))
        {
            return BadRequest("C23 一般住院查詢必須選擇住院或出院。");
        }
        else if (condition.EncounterSource == C23EncounterSources.Outpatient)
        {
            condition.InpatientType = null;
        }
        condition.ContractCode = string.IsNullOrWhiteSpace(condition.ContractCode) ? null : condition.ContractCode.Trim();
        if (condition.ForceRebuild && (condition.DateMode != C23DateModes.General || startDate != endDate))
            return BadRequest("C23 手動重建僅適用於一般模式單日查詢。");
        if (condition.ForceRebuild && !(_c23Options?.Value.RebuildEnabled ?? false))
            return BadRequest("C23 手動重建功能未開放。");
        return null;
    }

    private BadRequestObjectResult? ValidateC24Condition(SearchReportCondition condition)
    {
        if (!TryParseDate(condition.StartDate, out var startDate)
            || !TryParseDate(condition.EndDate, out var endDate))
            return BadRequest("請輸入有效的 C24 ISO 起始日期與截止日期。");
        if (startDate > endDate) return BadRequest("C24 起始日期不可晚於截止日期。");
        if (!C24Sources.IsSupported(condition.Source))
            return BadRequest("C24 來源僅接受門急診或住院。");
        if (!C24Modes.IsSupported(condition.Mode))
            return BadRequest("C24 模式僅接受 Accounting 或 Billing。");
        condition.RoomScope = string.IsNullOrWhiteSpace(condition.RoomScope)
            ? C24RoomScopes.All : condition.RoomScope;
        if (!C24RoomScopes.IsSupported(condition.RoomScope)
            || condition.Source == C24Sources.Inpatient && condition.RoomScope != C24RoomScopes.All)
            return BadRequest("C24 來源與房別範圍不相容。");
        if (condition.ForceRebuild && (condition.Mode != C24Modes.Accounting || startDate != endDate))
            return BadRequest("C24 手動重建僅適用於 Accounting 單日查詢。");
        condition.MedicalRecordNo = string.IsNullOrWhiteSpace(condition.MedicalRecordNo)
            ? null : condition.MedicalRecordNo.Trim().ToUpperInvariant();
        if (condition.MedicalRecordNo is { Length: > 10 })
            return BadRequest("C24 病歷號不得超過 10 個字元。");
        return null;
    }

    private BadRequestObjectResult? ValidateC11Condition(SearchReportCondition condition)
    {
        condition.StartDate = condition.StartDate?.Trim();
        condition.EndDate = condition.EndDate?.Trim();
        condition.Source = string.IsNullOrWhiteSpace(condition.Source)
            ? C10Sources.OpdEr
            : condition.Source.Trim();
        if (string.IsNullOrEmpty(condition.StartDate) || string.IsNullOrEmpty(condition.EndDate))
            return BadRequest("C11 起始日期與截止日期不得空白。");
        if (!C10Sources.IsSupported(condition.Source))
            return BadRequest("C11 來源僅接受門急診或住院。");
        if (LegacyC11NumberConverter.VbVal(condition.StartDate) >
            LegacyC11NumberConverter.VbVal(condition.EndDate))
            return BadRequest("C11 起始日期不可晚於截止日期。");
        return null;
    }

    private BadRequestObjectResult? ValidateC10Condition(SearchReportCondition condition)
    {
        if (!TryParseDate(condition.StartDate, out var startDate)
            || !TryParseDate(condition.EndDate, out var endDate))
            return BadRequest("請輸入有效的 C10 ISO 起始日期與截止日期。");
        if (startDate > endDate) return BadRequest("C10 起始日期不可晚於截止日期。");
        if (!C10Sources.IsSupported(condition.Source))
            return BadRequest("C10 來源僅接受門急診或住院。");

        condition.RoomScope = string.IsNullOrWhiteSpace(condition.RoomScope)
            ? C10RoomScopes.All
            : condition.RoomScope;
        if (!C10RoomScopes.IsSupported(condition.RoomScope)
            || condition.Source == C10Sources.Inpatient && condition.RoomScope != C10RoomScopes.All)
            return BadRequest("C10 來源與門急診別不相容。");

        condition.MedicalRecordNo = string.IsNullOrWhiteSpace(condition.MedicalRecordNo)
            ? null
            : condition.MedicalRecordNo.Trim().ToUpperInvariant();
        if (condition.MedicalRecordNo is { Length: > 10 })
            return BadRequest("C10 病歷號不得超過 10 個字元。");
        return null;
    }

    private BadRequestObjectResult? ValidateC12Condition(SearchReportCondition condition, out C12ReportRequest request)
    {
        request = new C12ReportRequest(string.Empty,string.Empty,C12Source.OutpatientAndEmergency,0,string.Empty,null);
        if (!TryParseDate(condition.StartDate,out DateOnly startDate) || !TryParseDate(condition.EndDate,out DateOnly endDate))
            return BadRequest("請輸入有效的 C12 起始日期與截止日期。");
        if (startDate>endDate) return BadRequest("C12 起始日期不可晚於截止日期。");
        condition.Source=condition.Source?.Trim();
        condition.RoomScope=string.IsNullOrWhiteSpace(condition.RoomScope)?C12RoomScopes.All:condition.RoomScope.Trim();
        condition.MedicalRecordNo=condition.MedicalRecordNo?.Trim().ToUpperInvariant();
        condition.NewSectionCode=string.IsNullOrWhiteSpace(condition.NewSectionCode)?null:condition.NewSectionCode.Trim().ToUpperInvariant();
        if(!C12Sources.IsSupported(condition.Source)) return BadRequest("C12 來源僅接受門急診或住院。");
        if(!C12RoomScopes.IsSupported(condition.RoomScope) || condition.Source==C12Sources.Inpatient && condition.RoomScope!=C12RoomScopes.All)
            return BadRequest("C12 來源與門急診別不相容。");
        if(string.IsNullOrWhiteSpace(condition.MedicalRecordNo)) return BadRequest("請輸入病歷號！");
        if(condition.MedicalRecordNo.Length>10) return BadRequest("C12 病歷號或身分證號不得超過 10 個字元。");
        int roomType=condition.RoomScope switch { C12RoomScopes.Emergency=>1,C12RoomScopes.Outpatient=>2,_=>0 };
        string startRoc=DateTimeExtensions.ToRocDateString(startDate.ToDateTime(TimeOnly.MinValue));
        string endRoc=DateTimeExtensions.ToRocDateString(endDate.ToDateTime(TimeOnly.MinValue));
        request=new(startRoc,endRoc,condition.Source==C12Sources.Inpatient?C12Source.Inpatient:C12Source.OutpatientAndEmergency,roomType,condition.MedicalRecordNo,condition.NewSectionCode);
        return null;
    }

    private BadRequestObjectResult? ValidateC13Condition(SearchReportCondition condition)
    {
        condition.StartDate = condition.StartDate?.Trim();
        condition.EndDate = condition.EndDate?.Trim();
        if (!TryParseDate(condition.StartDate, out DateOnly startDate)
            || !TryParseDate(condition.EndDate, out DateOnly endDate))
            return BadRequest("請輸入有效的 C13 起始日期與截止日期。");
        if (startDate > endDate) return BadRequest("C13 起始日期不可晚於截止日期。");
        return null;
    }

    private BadRequestObjectResult? ValidateC143Condition(SearchReportCondition condition)
    {
        condition.StartDate = condition.StartDate?.Trim();
        condition.EndDate = condition.EndDate?.Trim();
        condition.Source = condition.Source?.Trim();
        condition.ReportType = condition.ReportType?.Trim();
        if (!TryParseDate(condition.StartDate, out DateOnly startDate)
            || !TryParseDate(condition.EndDate, out DateOnly endDate))
            return BadRequest("請輸入有效的 C143 起始日期與截止日期。");
        if (startDate.Year < 1912 || startDate > endDate)
            return BadRequest("C143 起始日期不可晚於截止日期。");
        if (!C143Sources.IsSupported(condition.Source))
            return BadRequest("C143 來源僅接受門急診或住院。");
        if (!C143ReportTypes.IsSupported(condition.ReportType))
            return BadRequest("C143 報表類型僅接受差異或全部。");
        return null;
    }

    private BadRequestObjectResult? ValidateC144Condition(SearchReportCondition condition)
    {
        condition.StartDate = condition.StartDate?.Trim();
        condition.EndDate = condition.EndDate?.Trim();
        condition.Source = condition.Source?.Trim();
        if (!TryParseDate(condition.StartDate, out DateOnly startDate)
            || !TryParseDate(condition.EndDate, out DateOnly endDate))
            return BadRequest("請輸入有效的 C144 起始日期與截止日期。");
        if (startDate.Year < 1912 || startDate > endDate)
            return BadRequest("C144 起始日期不可晚於截止日期。");
        if (!C144Sources.IsSupported(condition.Source))
            return BadRequest("C144 來源僅接受門急診或住院。");
        return null;
    }

    [HttpPost("Report/Export")]
    public IActionResult Export([FromBody] SearchReportCondition searchCondition)
    {
        IActionResult? c10Validation = searchCondition.ReportCode == "C10"
            ? ValidateC10Condition(searchCondition)
            : null;
        if (c10Validation is not null) return c10Validation;
        IActionResult? c144Validation = searchCondition.ReportCode == "C144"
            ? ValidateC144Condition(searchCondition)
            : null;
        if (c144Validation is not null) return c144Validation;
        if (searchCondition.ReportCode is not ("C10" or "C144" or "C174")
            || !TryParseDate(searchCondition.StartDate, out var startDate)
            || !TryParseDate(searchCondition.EndDate, out var endDate)
            || startDate > endDate)
        {
            return BadRequest("僅支援有效日期區間的 C10、C144 或 C174 報表匯出。");
        }

        try
        {
            if (searchCondition.ReportCode == "C144")
            {
                Response.Headers.CacheControl = "private, no-store";
                Response.Headers.Pragma = "no-cache";
            }
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

    private BadRequestObjectResult? ValidateC21Condition(SearchReportCondition condition)
    {
        if (!TryParseDate(condition.StartDate, out var startDate)
            || !TryParseDate(condition.EndDate, out var endDate))
        {
            return BadRequest("請輸入有效的 C21 起始日期與截止日期。");
        }
        if (startDate > endDate)
        {
            return BadRequest("C21 起始日期不可晚於截止日期。");
        }
        if (!C21EncounterSources.IsSupported(condition.EncounterSource))
        {
            return BadRequest("C21 來源僅接受門急診或住院。");
        }

        condition.AccountingScope ??= condition.EncounterSource == C21EncounterSources.Inpatient ? 4 : 0;
        if (!C21EncounterSources.IsScopeSupported(condition.EncounterSource!, condition.AccountingScope.Value))
        {
            return BadRequest("C21 來源與帳務範圍不相容。");
        }
        condition.BillingCode = string.IsNullOrWhiteSpace(condition.BillingCode)
            ? null
            : condition.BillingCode.Trim();
        if (condition.BillingCode is not null
            && (condition.BillingCode.Length != 2 || !condition.BillingCode.All(char.IsDigit)))
        {
            return BadRequest("C21 收費科目僅接受兩碼數字代碼。");
        }
        if (condition.ForceRebuild
            && (condition.EncounterSource != C21EncounterSources.Inpatient || startDate != endDate))
        {
            return BadRequest("C21 重新計算僅適用於單日住院查詢。");
        }
        return null;
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

    private BadRequestObjectResult? ValidateC213Condition(SearchReportCondition searchCondition)
    {
        if (!TryParseDate(searchCondition.StartDate, out DateOnly startDate)
            || !TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return BadRequest("請輸入有效的 C213 起始日期與截止日期。");
        }

        if (startDate > endDate)
        {
            return BadRequest("C213 起始日期不可晚於截止日期。");
        }

        return null;
    }

    private IActionResult? ValidateC214Condition(SearchReportCondition searchCondition)
    {
        if (!TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return C214ValidationProblem("請輸入有效的 C214 截止日期。");
        }

        if (endDate < new DateOnly(2015, 1, 1))
        {
            return C214ValidationProblem("C214 截止日期不可早於 2015-01-01。");
        }

        searchCondition.ReceivableBalanceType = string.IsNullOrWhiteSpace(
            searchCondition.ReceivableBalanceType)
            ? ReceivableBalanceTypes.SelfPay
            : searchCondition.ReceivableBalanceType;
        if (!ReceivableBalanceTypes.IsSupported(searchCondition.ReceivableBalanceType))
        {
            return C214ValidationProblem("C214 餘額類型僅接受 SelfPay 或 Insurance。");
        }

        return null;
    }

    private ObjectResult C214ValidationProblem(string title)
    {
        return Problem(title: title, statusCode: StatusCodes.Status400BadRequest);
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

    private BadRequestObjectResult? ValidateC15Condition(SearchReportCondition searchCondition)
    {
        if (!TryParseDate(searchCondition.StartDate, out DateOnly startDate))
        {
            return BadRequest("請輸入有效的 C15 起始日期。");
        }

        if (!TryParseDate(searchCondition.EndDate, out DateOnly endDate))
        {
            return BadRequest("請輸入有效的 C15 截止日期。");
        }

        if (startDate > endDate)
        {
            return BadRequest("C15 起始日期不可晚於截止日期。");
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

    private static bool IsOracleConnectionFailure(int number) =>
        number is 12154 or 12170 or 12514 or 12537 or 12541 or 12543 or 12545 or 12547 or 12560;
}
