using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportControllerTests
{
    [Fact]
    public void GetReportData_ValidC212EndDate_UsesTypedDispatchRequestAbortedAndNoStore()
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>(), "c212-trace");
        using var source = new CancellationTokenSource();
        controller.HttpContext.RequestAborted = source.Token;

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C212",
            EndDate = "2026-09-11"
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.C212Calls);
        Assert.Equal(new DateOnly(2026, 9, 11), service.LastC212Query!.EndDate);
        Assert.Equal("c212-trace", service.LastCorrelationId);
        Assert.Equal(source.Token, service.LastCancellationToken);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "2026-02-30")]
    [InlineData(null, "2911-01-01")]
    [InlineData("2026-09-01", "2026-09-11")]
    public void GetReportData_InvalidC212Condition_ReturnsBadRequestWithoutDispatch(
        string? startDate,
        string? endDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C212",
            StartDate = startDate,
            EndDate = endDate
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.C212Calls);
    }

    [Fact]
    public void GetReportData_CancelledC212Query_ReturnsClientClosedStatus()
    {
        var controller = CreateController(
            new ThrowingC212ReportService(new OperationCanceledException()),
            new CapturingLogger<ReportController>());

        var result = Assert.IsType<StatusCodeResult>(controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C212", EndDate = "2026-09-11"
        }));

        Assert.Equal(499, result.StatusCode);
    }

    [Fact]
    public void GetReportData_UnexpectedC212Failure_ReturnsSafeProblemWithTraceId()
    {
        var controller = CreateController(
            new ThrowingC212ReportService(new InvalidOperationException("sensitive")),
            new CapturingLogger<ReportController>(), "safe-trace");

        var result = Assert.IsType<ObjectResult>(controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C212", EndDate = "2026-09-11"
        }));
        var problem = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal("safe-trace", problem.Extensions["traceId"]);
        Assert.DoesNotContain("sensitive", problem.Title);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Index_ProjectsOnlyC21RebuildCapabilityFromConfiguration(bool rebuildEnabled)
    {
        var options = Options.Create(new C21Options
        {
            CurrentUserId = "C21-SECRET",
            RebuildEnabled = rebuildEnabled
        });
        var controller = new ReportController(
            new FakeReportCatalogService(),
            new CountingReportService(),
            new FakeReportExportService(),
            new CapturingLogger<ReportController>(),
            c21Options: options);

        var result = Assert.IsType<ViewResult>(controller.Index());
        var model = Assert.IsType<ReportIndexViewModel>(result.Model);

        Assert.Equal(rebuildEnabled, model.C21RebuildEnabled);
        Assert.DoesNotContain("C21-SECRET", System.Text.Json.JsonSerializer.Serialize(model));
    }

    [Theory]
    [InlineData(null, null, null, 1, 10, ReceivableBalanceTypes.SelfPay)]
    [InlineData(ReceivableBalanceTypes.Insurance, 2, 30, 2, 30, ReceivableBalanceTypes.Insurance)]
    public void GetReportData_ValidC214Condition_AppliesDefaultsPagingAndTypedDispatch(
        string? balanceType,
        int? pageNumber,
        int? pageSize,
        int expectedPageNumber,
        int expectedPageSize,
        string expectedBalanceType)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C214",
            EndDate = "2026-08-31",
            ReceivableBalanceType = balanceType,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        Assert.IsType<OkObjectResult>(controller.GetReportData(condition));
        Assert.Equal(typeof(OutpatientReceivableBalanceReportViewModel), service.LastRequestedType);
        Assert.Equal(expectedBalanceType, condition.ReceivableBalanceType);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(null, ReceivableBalanceTypes.SelfPay, 1, 10)]
    [InlineData("bad", ReceivableBalanceTypes.SelfPay, 1, 10)]
    [InlineData("2014-12-31", ReceivableBalanceTypes.SelfPay, 1, 10)]
    [InlineData("2026-08-31", "Unknown", 1, 10)]
    [InlineData("2026-08-31", ReceivableBalanceTypes.SelfPay, 0, 10)]
    [InlineData("2026-08-31", ReceivableBalanceTypes.Insurance, 1, 25)]
    public void GetReportData_InvalidC214Condition_ReturnsBadRequestWithoutCallingService(
        string? endDate,
        string balanceType,
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C214",
            EndDate = endDate,
            ReceivableBalanceType = balanceType,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.False(string.IsNullOrWhiteSpace(problemDetails.Title));
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(2, 30, 2, 30)]
    public void GetReportData_ValidC213Condition_AppliesPaginationAndTypedDispatch(
        int? pageNumber, int? pageSize, int expectedPageNumber, int expectedPageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C213",
            StartDate = "2026-08-01",
            EndDate = "2026-08-03",
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        Assert.IsType<OkObjectResult>(controller.GetReportData(condition));
        Assert.Equal(typeof(CashierCashSummaryReportViewModel), service.LastRequestedType);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(null, "2026-08-03", 1, 10)]
    [InlineData("bad", "2026-08-03", 1, 10)]
    [InlineData("2026-08-04", "2026-08-03", 1, 10)]
    [InlineData("2026-08-01", "2026-08-03", 0, 10)]
    [InlineData("2026-08-01", "2026-08-03", 1, 25)]
    public void GetReportData_InvalidC213Condition_ReturnsBadRequestWithoutCallingService(
        string? startDate, string endDate, int pageNumber, int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C213",
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(2, 30, 2, 30)]
    public void GetReportData_ValidC22Condition_AppliesPaginationAndTypedDispatch(
        int? pageNumber, int? pageSize, int expectedPageNumber, int expectedPageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C22", StartDate = "2026-08-01", EndDate = "2026-08-03",
            CashierCashSortType = CashierCashSortTypes.Cashier,
            CashierUserId = " A123 ", PageNumber = pageNumber, PageSize = pageSize
        };
        Assert.IsType<OkObjectResult>(controller.GetReportData(condition));
        Assert.Equal(typeof(CashierCashReportViewModel), service.LastRequestedType);
        Assert.Equal("A123", condition.CashierUserId);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(null, "2026-08-03", "Cashier", 1, 10)]
    [InlineData("2026-08-04", "2026-08-03", "Cashier", 1, 10)]
    [InlineData("0090-08-26", "0090-08-26", "Cashier", 1, 10)]
    [InlineData("2026-08-01", "2026-08-03", "Unknown", 1, 10)]
    [InlineData("2026-08-01", "2026-08-03", "Cashier", 0, 10)]
    [InlineData("2026-08-01", "2026-08-03", "Encounter", 1, 25)]
    public void GetReportData_InvalidC22Condition_ReturnsBadRequest(
        string? startDate, string endDate, string sortType, int pageNumber, int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C22", StartDate = startDate, EndDate = endDate,
            CashierCashSortType = sortType, PageNumber = pageNumber, PageSize = pageSize
        });
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(2, 30, 2, 30)]
    [InlineData(2, 50, 2, 50)]
    public void GetReportData_ValidC1Range_AppliesPaginationAndTypedDispatch(
        int? pageNumber,
        int? pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C1",
            StartDate = "2026-08-01",
            EndDate = "2026-08-03",
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(typeof(SurgicalAccountingReportViewModel), service.LastRequestedType);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(null, "2026-08-03")]
    [InlineData("bad", "2026-08-03")]
    [InlineData("2026-08-04", "2026-08-03")]
    public void GetReportData_InvalidC1Dates_ReturnsBadRequestWithoutCallingService(
        string? startDate,
        string endDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C1",
            StartDate = startDate,
            EndDate = endDate
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC1Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C1",
            StartDate = "2026-08-01",
            EndDate = "2026-08-03",
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void Export_ThirtyThousandRows_ReturnsExcelFile()
    {
        var exportService = new FakeReportExportService
        {
            DispatchResult = new ReportExportDispatchResult([1, 2, 3], "C174_test.xlsx", null)
        };
        var controller = CreateController(new CountingReportService(), new CapturingLogger<ReportController>(), exportService: exportService);

        var result = controller.Export(ValidExportCondition());

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(ReportExportService.ExcelContentType, file.ContentType);
        Assert.Equal("C174_test.xlsx", file.FileDownloadName);
    }

    [Fact]
    public void Export_ThirtyThousandOneRows_ReturnsAcceptedJobContract()
    {
        var job = CreateJob(ReportExportJobStatus.Queued);
        var exportService = new FakeReportExportService
        {
            DispatchResult = new ReportExportDispatchResult(null, null, job)
        };
        var controller = CreateController(new CountingReportService(), new CapturingLogger<ReportController>(), exportService: exportService);

        var result = controller.Export(ValidExportCondition());

        var accepted = Assert.IsType<AcceptedResult>(result);
        var response = Assert.IsType<ReportExportJobResponse>(accepted.Value);
        Assert.Equal(job.JobId, response.JobId);
        Assert.Equal("Queued", response.Status);
        Assert.Equal($"/Report/Export/{job.JobId:D}", response.StatusUrl);
    }

    [Theory]
    [InlineData("C171", "2026-08-01", "2026-08-31")]
    [InlineData("C174", "bad", "2026-08-31")]
    [InlineData("C174", "2026-09-01", "2026-08-31")]
    public void Export_InvalidRequest_ReturnsBadRequestWithoutDispatch(string code, string startDate, string endDate)
    {
        var exportService = new FakeReportExportService();
        var controller = CreateController(new CountingReportService(), new CapturingLogger<ReportController>(), exportService: exportService);

        var result = controller.Export(new SearchReportCondition { ReportCode = code, StartDate = startDate, EndDate = endDate });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, exportService.DispatchCalls);
    }

    [Fact]
    public void Export_FullQueue_ReturnsServiceUnavailable()
    {
        var exportService = new FakeReportExportService
        {
            DispatchResult = new ReportExportDispatchResult(null, null, null, QueueFull: true)
        };
        var controller = CreateController(new CountingReportService(), new CapturingLogger<ReportController>(), exportService: exportService);

        var result = Assert.IsType<ObjectResult>(controller.Export(ValidExportCondition()));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
    }

    [Fact]
    public void Export_UnexpectedFailure_LogsSecretButReturnsSafeProblem()
    {
        const string secret = "SELECT patient connection password";
        var logger = new CapturingLogger<ReportController>();
        var exportService = new FakeReportExportService { DispatchException = new InvalidOperationException(secret) };
        var controller = CreateController(new CountingReportService(), logger, "export-trace", exportService);

        var result = Assert.IsType<ObjectResult>(controller.Export(ValidExportCondition()));
        var problem = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.DoesNotContain(secret, problem.Title);
        Assert.Equal("export-trace", problem.Extensions["traceId"]);
        Assert.Contains(secret, Assert.Single(logger.Entries).Exception!.Message);
    }

    [Theory]
    [InlineData(ReportExportJobStatus.Queued, 409)]
    [InlineData(ReportExportJobStatus.Running, 409)]
    [InlineData(ReportExportJobStatus.Expired, 410)]
    public void DownloadExport_NonReadyStatus_ReturnsContractStatus(ReportExportJobStatus status, int expectedStatus)
    {
        var job = CreateJob(status);
        var exportService = new FakeReportExportService
        {
            DownloadResult = new ReportExportDownloadResult(job, null)
        };
        var controller = CreateController(new CountingReportService(), new CapturingLogger<ReportController>(), exportService: exportService);

        var result = Assert.IsAssignableFrom<ObjectResult>(controller.DownloadExport(job.JobId.ToString("D")));

        Assert.Equal(expectedStatus, result.StatusCode);
    }

    [Fact]
    public void DownloadExport_ReadyJob_ReturnsExcelStream()
    {
        var job = CreateJob(ReportExportJobStatus.Ready);
        var exportService = new FakeReportExportService
        {
            DownloadResult = new ReportExportDownloadResult(job, new MemoryStream([1, 2, 3]))
        };
        var controller = CreateController(new CountingReportService(), new CapturingLogger<ReportController>(), exportService: exportService);

        var result = Assert.IsType<FileStreamResult>(controller.DownloadExport(job.JobId.ToString("D")));

        Assert.Equal(ReportExportService.ExcelContentType, result.ContentType);
        Assert.Equal("C174_test.xlsx", result.FileDownloadName);
    }

    [Fact]
    public void GetExportStatus_MalformedAndUnknownIds_ReturnExpectedStatus()
    {
        var controller = CreateController(new CountingReportService(), new CapturingLogger<ReportController>());

        Assert.IsType<BadRequestObjectResult>(controller.GetExportStatus("../secret.xlsx"));
        Assert.IsType<NotFoundResult>(controller.GetExportStatus(Guid.NewGuid().ToString("D")));
    }

    [Theory]
    [InlineData(EncounterSources.Emergency)]
    [InlineData(EncounterSources.Inpatient)]
    public void GetReportData_ValidC19Condition_UsesTypedDispatch(string source)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C19",
            StartDate = "2026-08-24",
            EndDate = "2026-08-24",
            EncounterSource = source,
            StationOrBedPrefix = "7A"
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(typeof(SafeNeedleReportViewModel), service.LastRequestedType);
        Assert.Equal(1, service.LastCondition!.PageNumber);
        Assert.Equal(10, service.LastCondition.PageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC19Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C19",
            StartDate = "2026-08-24",
            EndDate = "2026-08-24",
            EncounterSource = EncounterSources.Emergency,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Outpatient")]
    public void GetReportData_InvalidC19Source_ReturnsBadRequestWithoutCallingService(string? source)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C19",
            StartDate = "2026-08-24",
            EndDate = "2026-08-24",
            EncounterSource = source
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, "2026-08-24")]
    [InlineData("not-a-date", "2026-08-24")]
    [InlineData("2026-08-24", "2026-08-25")]
    [InlineData("2026-08-25", "2026-08-24")]
    public void GetReportData_InvalidC19Dates_ReturnsBadRequestWithoutCallingService(
        string? startDate,
        string endDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C19",
            StartDate = startDate,
            EndDate = endDate,
            EncounterSource = EncounterSources.Emergency
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(EncounterSources.Emergency)]
    [InlineData(EncounterSources.Inpatient)]
    public void GetReportData_ValidC18Condition_AppliesPaginationAndTypedDispatch(string source)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C18",
            StartDate = "2026-01-01",
            EndDate = "2026-12-31",
            EncounterSource = source
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(typeof(ReferralMemberReportViewModel), service.LastRequestedType);
        Assert.Equal(1, condition.PageNumber);
        Assert.Equal(10, condition.PageSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Outpatient")]
    public void GetReportData_InvalidC18Source_ReturnsBadRequestWithoutCallingService(string? source)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C18",
            StartDate = "2026-01-01",
            EndDate = "2026-01-31",
            EncounterSource = source
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, "2026-01-31")]
    [InlineData("not-a-date", "2026-01-31")]
    [InlineData("2026-02-01", "2026-01-31")]
    [InlineData("2025-12-31", "2026-01-01")]
    public void GetReportData_InvalidC18Dates_ReturnsBadRequestWithoutCallingService(
        string? startDate,
        string endDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C18",
            StartDate = startDate,
            EndDate = endDate,
            EncounterSource = EncounterSources.Emergency
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC18Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C18",
            StartDate = "2026-01-01",
            EndDate = "2026-01-31",
            EncounterSource = EncounterSources.Emergency,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(2, 10, 2, 10)]
    [InlineData(2, 30, 2, 30)]
    [InlineData(2, 50, 2, 50)]
    public void GetReportData_ValidC171Pagination_AppliesValues(
        int? pageNumber,
        int? pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C171",
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC171Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var logger = new CapturingLogger<ReportController>();
        var controller = CreateController(service, logger);

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C171",
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(2, 30, 2, 30)]
    public void GetReportData_ValidC174Pagination_AppliesValues(
        int? pageNumber,
        int? pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C174",
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC174Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C174",
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void GetReportData_UnexpectedC174Failure_ReturnsSanitizedProblemDetails()
    {
        const string traceId = "trace-c174-001";
        const string secretMessage = "SELECT patient password";
        var controller = CreateController(
            new ThrowingReportService(new InvalidOperationException(secretMessage)),
            new CapturingLogger<ReportController>(),
            traceId);

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C174",
            StartDate = "2026-08-01",
            EndDate = "2026-08-31",
            PageNumber = 1,
            PageSize = 10
        });

        var objectResult = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal(traceId, problem.Extensions["traceId"]);
        Assert.DoesNotContain(secretMessage, problem.Title);
    }

    [Fact]
    public void GetReportData_UnexpectedC171Failure_LogsOriginalExceptionOnceAndReturnsSafeTraceId()
    {
        const string traceId = "trace-c171-001";
        const string secretExceptionMessage = "SQL SELECT patient 王小明 MRNO A123 connection password";
        var exception = new InvalidOperationException(secretExceptionMessage);
        var logger = new CapturingLogger<ReportController>();
        var controller = CreateController(new ThrowingReportService(exception), logger, traceId);

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C171",
            StartDate = "2026-08-01",
            EndDate = "2026-08-18",
            Chop1sec = "A123 王小明",
            PageNumber = 2,
            PageSize = 30
        });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(traceId, problem.Extensions["traceId"]);
        Assert.DoesNotContain(secretExceptionMessage, problem.Title);

        var entry = Assert.Single(logger.Entries);
        Assert.Same(exception, entry.Exception);
        Assert.Contains(traceId, entry.Message);
        Assert.Contains("C171", entry.Message);
        Assert.Contains("PageNumber: 2", entry.Message);
        Assert.Contains("PageSize: 30", entry.Message);
        Assert.DoesNotContain("王小明", entry.Message);
        Assert.DoesNotContain("A123", entry.Message);
        Assert.DoesNotContain("SELECT", entry.Message);
        Assert.DoesNotContain("password", entry.Message);
    }

    [Fact]
    public void GetReportData_ValidC25Condition_DispatchesC25ViewModelAndDefaultsPagination()
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C25",
            StartDate = "2026-08-01",
            EndDate = "2026-08-31"
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(typeof(InpatientAdvancePaymentBalanceReportViewModel), service.LastRequestedType);
        Assert.Equal(1, condition.PageNumber);
        Assert.Equal(10, condition.PageSize);
    }

    [Theory]
    [InlineData(null, "2026-08-31")]
    [InlineData("not-a-date", "2026-08-31")]
    [InlineData("2026-08-01", null)]
    [InlineData("2026-09-01", "2026-08-31")]
    public void GetReportData_InvalidC25Dates_ReturnsBadRequestWithoutCallingService(
        string? startDate,
        string? endDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C25",
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = 1,
            PageSize = 10
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC25Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C25",
            StartDate = "2026-08-01",
            EndDate = "2026-08-31",
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(2, 30, 2, 30)]
    [InlineData(2, 50, 2, 50)]
    public void GetReportData_ValidC27Cutoff_AppliesPaginationAndTypedDispatch(
        int? pageNumber,
        int? pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C27",
            EndDate = "2026-08-31",
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(typeof(AssistiveDeviceDepositBalanceReportViewModel), service.LastRequestedType);
        Assert.Null(condition.StartDate);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("1911-12-31")]
    public void GetReportData_InvalidC27Cutoff_ReturnsBadRequestWithoutCallingService(string? endDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C27",
            EndDate = endDate
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("C27 截止日期", Assert.IsType<string>(badRequest.Value));
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC27Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C27",
            EndDate = "2026-08-31",
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(2, 30, 2, 30)]
    [InlineData(2, 50, 2, 50)]
    public void GetReportData_ValidC28Cutoff_AppliesPaginationAndTypedDispatch(
        int? pageNumber,
        int? pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C28",
            EndDate = "2026-08-31",
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(typeof(InpatientReceivableBalanceReportViewModel), service.LastRequestedType);
        Assert.Null(condition.StartDate);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("1911-12-31")]
    public void GetReportData_InvalidC28Cutoff_ReturnsBadRequestWithoutCallingService(string? endDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C28",
            EndDate = endDate
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("C28 截止日期", Assert.IsType<string>(badRequest.Value));
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC28Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C28",
            EndDate = "2026-08-31",
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(null, null, 1, 10)]
    [InlineData(2, 30, 2, 30)]
    public void GetReportData_ValidC29_AppliesPaginationTrimsContractAndTypedDispatch(
        int? pageNumber,
        int? pageSize,
        int expectedPageNumber,
        int expectedPageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C29",
            StartDate = "2026-08-01",
            EndDate = "2026-08-31",
            EncounterSource = EncounterSources.Emergency,
            BillingCode = "  A01  ",
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(typeof(ContractPaymentDetailReportViewModel), service.LastRequestedType);
        Assert.Equal("A01", condition.BillingCode);
        Assert.Equal(expectedPageNumber, condition.PageNumber);
        Assert.Equal(expectedPageSize, condition.PageSize);
    }

    [Theory]
    [InlineData(null, "2026-08-01", "2026-08-31")]
    [InlineData("Unknown", "2026-08-01", "2026-08-31")]
    [InlineData("Emergency", null, "2026-08-31")]
    [InlineData("Emergency", "2026-09-01", "2026-08-31")]
    public void GetReportData_InvalidC29Condition_ReturnsBadRequestWithoutCallingService(
        string? source,
        string? startDate,
        string? endDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C29",
            StartDate = startDate,
            EndDate = endDate,
            EncounterSource = source
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 25)]
    public void GetReportData_InvalidC29Pagination_ReturnsBadRequestWithoutCallingService(
        int pageNumber,
        int pageSize)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C29",
            StartDate = "2026-08-01",
            EndDate = "2026-08-31",
            EncounterSource = EncounterSources.Inpatient,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void GetReportData_ValidC21DefaultsScopeAndDispatchesNormalizedViewModel()
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C21", StartDate = "2026-09-08", EndDate = "2026-09-08",
            EncounterSource = C21EncounterSources.Outpatient
        };

        var result = controller.GetReportData(condition);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(typeof(C21AccountingSummaryReportViewModel), service.LastRequestedType);
        Assert.Equal(0, condition.AccountingScope);
        Assert.Equal(1, condition.PageNumber);
        Assert.Equal(10, condition.PageSize);
    }

    [Theory]
    [InlineData("Outpatient", 5, false)]
    [InlineData("Inpatient", 2, false)]
    [InlineData("Outpatient", 0, true)]
    [InlineData("Inpatient", 4, true)]
    public void GetReportData_InvalidC21ScopeOrRebuildBoundary_DoesNotCallService(
        string source, int scope, bool forceRebuild)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C21", StartDate = "2026-09-08",
            EndDate = forceRebuild && source == "Inpatient" ? "2026-09-09" : "2026-09-08",
            EncounterSource = source, AccountingScope = scope, ForceRebuild = forceRebuild
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("123")]
    [InlineData("A1")]
    public void GetReportData_InvalidC21BillingCode_DoesNotCallService(string code)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C21", StartDate = "2026-09-08", EndDate = "2026-09-08",
            EncounterSource = C21EncounterSources.Outpatient, BillingCode = code
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void GetReportData_C21AuthorizationFailure_ReturnsForbiddenWithoutLeakingDetails()
    {
        var controller = CreateController(
            new ThrowingReportService(new C21RebuildForbiddenException("C21 重新計算需要固定帳號。")),
            new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C21", StartDate = "2026-09-08", EndDate = "2026-09-08",
            EncounterSource = C21EncounterSources.Inpatient, AccountingScope = 4
        });

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal("C21 重新計算需要固定帳號。", Assert.IsType<ProblemDetails>(forbidden.Value).Title);
    }

    [Fact]
    public void GetReportData_ValidC23Condition_NormalizesAndDispatchesTypedResult()
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C23", StartDate = "2026-09-08", EndDate = "2026-09-08",
            EncounterSource = C23EncounterSources.Outpatient
        };

        Assert.IsType<OkObjectResult>(controller.GetReportData(condition));
        Assert.Equal(typeof(C23ContractAccountingReportViewModel), service.LastRequestedType);
        Assert.Equal(C23DateModes.General, condition.DateMode);
        Assert.Equal(1, condition.PageNumber);
        Assert.Equal(10, condition.PageSize);
    }

    [Theory]
    [InlineData("2026-09-30", "2026-10-01", "Outpatient", "EncounterDate", null)]
    [InlineData("2026-09-08", "2026-09-08", "Unknown", "General", null)]
    [InlineData("2026-09-08", "2026-09-08", "Inpatient", "General", null)]
    public void GetReportData_InvalidC23Condition_DoesNotCallService(
        string start, string end, string source, string mode, string? inpatientType)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C23", StartDate = start, EndDate = end,
            EncounterSource = source, DateMode = mode, InpatientType = inpatientType
        });
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void GetReportData_GeneralC23Condition_AllowsCrossMonthRange()
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C23", StartDate = "2026-09-30", EndDate = "2026-10-01",
            EncounterSource = C23EncounterSources.Outpatient, DateMode = C23DateModes.General
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
    }

    [Fact]
    public void GetReportData_DisabledC23ForcedRebuild_IsRejectedBeforeService()
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C23", StartDate = "2026-09-08", EndDate = "2026-09-08",
            EncounterSource = C23EncounterSources.Outpatient, DateMode = C23DateModes.General,
            ForceRebuild = true
        });
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void GetReportData_ValidC211_UsesCutoffOnlyTrimsContractAndDisablesSharedCache()
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());
        var condition = new SearchReportCondition
        {
            ReportCode = "C211", EndDate = "2026-08-31", EncounterSource = "O",
            ContractCode = "  OUTSIDE  "
        };

        Assert.IsType<OkObjectResult>(controller.GetReportData(condition));
        Assert.Equal(1, service.C211Calls);
        Assert.Equal("OUTSIDE", condition.ContractCode);
        Assert.Null(condition.StartDate);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
        Assert.Null(condition.PageNumber);
        Assert.Null(condition.PageSize);
    }

    [Theory]
    [InlineData("X", "2026-08-31", null)]
    [InlineData("O", "invalid", null)]
    [InlineData("I", "2026-08-31", "2026-08-01")]
    public void GetReportData_InvalidC211_DoesNotDispatch(string source, string endDate, string? startDate)
    {
        var service = new CountingReportService();
        var controller = CreateController(service, new CapturingLogger<ReportController>());

        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C211", StartDate = startDate, EndDate = endDate, EncounterSource = source
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.C211Calls);
    }

    private static ReportController CreateController(
        IReportService service,
        CapturingLogger<ReportController> logger,
        string traceId = "test-trace",
        IReportExportService? exportService = null)
    {
        return new ReportController(new FakeReportCatalogService(), service, exportService ?? new FakeReportExportService(), logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = traceId }
            }
        };
    }

    private static SearchReportCondition ValidExportCondition() => new()
    {
        ReportCode = "C174",
        StartDate = "2026-08-01",
        EndDate = "2026-08-31"
    };

    private static ReportExportJob CreateJob(ReportExportJobStatus status) => new()
    {
        JobId = Guid.NewGuid(),
        SearchCondition = ValidExportCondition(),
        CreatedAt = DateTimeOffset.UtcNow,
        Status = status,
        FileName = status == ReportExportJobStatus.Ready ? "C174_test.xlsx" : null
    };

    private sealed class CountingReportService : IReportService
    {
        public int Calls { get; private set; }

        public Type? LastRequestedType { get; private set; }

        public SearchReportCondition? LastCondition { get; private set; }

        public int C211Calls { get; private set; }

        public int C212Calls { get; private set; }
        public C212Query? LastC212Query { get; private set; }
        public string? LastCorrelationId { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition)
        {
            Calls++;
            LastRequestedType = typeof(T);
            LastCondition = searchCondition;
            return new ReportDataAndColumns<T> { Columns = [], Data = [] };
        }

        public Task<ReportDataAndColumns<C211ContractBalanceReportViewModel>> ReportC211Async(
            SearchReportCondition searchCondition, string userId,
            CancellationToken cancellationToken = default)
        {
            C211Calls++;
            LastRequestedType = typeof(C211ContractBalanceReportViewModel);
            LastCondition = searchCondition;
            return Task.FromResult(new ReportDataAndColumns<C211ContractBalanceReportViewModel>
            {
                Columns = [], Data = []
            });
        }

        public Task<ReportDataAndColumns<C212BoneBankBalanceReportViewModel>> ReportC212Async(
            C212Query query,
            string userId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            C212Calls++;
            LastC212Query = query;
            LastCorrelationId = correlationId;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(new ReportDataAndColumns<C212BoneBankBalanceReportViewModel>
            {
                Columns = [], Data = []
            });
        }
    }

    private sealed class ThrowingReportService(Exception exception) : IReportService
    {
        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition)
        {
            throw exception;
        }
    }

    private sealed class ThrowingC212ReportService(Exception exception) : IReportService
    {
        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition) =>
            throw new NotSupportedException();

        public Task<ReportDataAndColumns<C212BoneBankBalanceReportViewModel>> ReportC212Async(
            C212Query query, string userId, string correlationId,
            CancellationToken cancellationToken = default) => Task.FromException<ReportDataAndColumns<C212BoneBankBalanceReportViewModel>>(exception);
    }
}
