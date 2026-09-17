using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C3ReportControllerTests
{
    [Fact]
    public void GetReportData_ValidRequestReturnsPageAndNoStore()
    {
        var service = new FakeC3Service();
        var controller = Create(service);
        IActionResult action = controller.GetReportData(Condition());
        Assert.IsType<OkObjectResult>(action);
        Assert.Equal(1, service.QueryCalls);
        Assert.Equal("no-store, private", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public void GetReportData_InvalidSourceDoesNotDispatch()
    {
        var service = new FakeC3Service(); var condition = Condition(); condition.Source = "OpdEr";
        Assert.IsType<BadRequestObjectResult>(Create(service).GetReportData(condition));
        Assert.Equal(0, service.QueryCalls);
    }

    [Fact]
    public void GetReportData_InvalidDepartmentReturnsValidationError()
    {
        var service = new FakeC3Service { ValidationError = "查無此科別。" };

        BadRequestObjectResult result = Assert.IsType<BadRequestObjectResult>(
            Create(service).GetReportData(Condition()));

        Assert.Equal("查無此科別。", Assert.IsType<ProblemDetails>(result.Value).Title);
    }

    [Fact]
    public void Preview_ReturnsNotFoundForZeroRowsAndPartialForRows()
    {
        var empty = new FakeC3Service();
        Assert.IsType<NotFoundObjectResult>(Create(empty).PreviewC3(Condition()));
        var populated = new FakeC3Service { Preview = new("title", "115/05/14", "115/05/15", "U", "115/05/15 10:00:00", ReportDetailType.Summary, [Row()]) };
        PartialViewResult result = Assert.IsType<PartialViewResult>(Create(populated).PreviewC3(Condition()));
        Assert.Equal("_C3NursingStationChargePreview", result.ViewName);
    }

    [Fact]
    public void Preview_AcceptsJsonForModalPaperRendering()
    {
        var preview = new C3PreviewViewModel("title", "115/05/14", "115/05/15", "U",
            "115/05/15 10:00:00", ReportDetailType.Summary, [Row()]);
        var controller = Create(new FakeC3Service { Preview = preview });
        controller.Request.Headers.Accept = "application/json";

        OkObjectResult result = Assert.IsType<OkObjectResult>(controller.PreviewC3(Condition()));

        Assert.Same(preview, result.Value);
        Assert.Equal("no-store, private", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public void Preview_InvalidDepartmentReturnsValidationError()
    {
        var service = new FakeC3Service { ValidationError = "查無此科別。" };

        BadRequestObjectResult result = Assert.IsType<BadRequestObjectResult>(
            Create(service).PreviewC3(Condition()));

        Assert.Equal("查無此科別。", Assert.IsType<ProblemDetails>(result.Value).Title);
    }

    [Fact]
    public void C3_IsRegisteredAndUsesSharedUi()
    {
        string root = FindProjectRoot();
        string program = File.ReadAllText(Path.Combine(root, "Program.cs"));
        string registrations = File.ReadAllText(Path.Combine(root, "Services", "C3ServiceCollectionExtensions.cs"));
        Assert.Contains("AddC3ReportServices()", program);
        Assert.Contains("AddScoped<IC3ReportService, C3ReportService>()", registrations);
        Assert.Contains("AddScoped<IC3ReportRepository, C3ReportRepository>()", registrations);
    }

    private static SearchReportCondition Condition() => new() { ReportCode = "C3", StartDate = "2026-05-14", EndDate = "2026-05-15", Source = "O", DetailType = 0, LogisticsType = 0, PageNumber = 1, PageSize = 10 };
    private static string FindProjectRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Program.cs"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Project root not found.");
    }
    private static C3ReportRow Row() => new("門診", "部門", "100", "C", "M", "材料", "物流", 1, null, null, null, null, null, null);
    private static ReportController Create(IC3ReportService service) => new(new FakeReportCatalogService(), new EmptyReportService(), new FakeReportExportService(), new CapturingLogger<ReportController>(), c3ReportService: service)
    { ControllerContext = new() { HttpContext = new DefaultHttpContext { TraceIdentifier = "c3-trace" } } };

    private sealed class EmptyReportService : IReportService { public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition condition) => new(); }
    private sealed class FakeC3Service : IC3ReportService
    {
        public int QueryCalls { get; private set; }
        public C3PreviewViewModel? Preview { get; init; }
        public string? ValidationError { get; init; }
        public Task<C3ReportResult> QueryAsync(C3ReportRequest request, CancellationToken cancellationToken = default)
        {
            QueryCalls++;
            if (ValidationError is not null) throw new ArgumentException(ValidationError);
            var validated = request.Validate();
            return Task.FromResult(new C3ReportResult(validated, [], new() { Columns = [], Data = [], TotalCount = 0, PageNumber = 1, PageSize = 10, TotalPages = 0 }));
        }
        public Task<C3PreviewViewModel?> CreatePreviewAsync(C3ReportRequest request, string userId, CancellationToken cancellationToken = default) => ValidationError is not null
            ? throw new ArgumentException(ValidationError)
            : Task.FromResult(Preview);
    }
}
