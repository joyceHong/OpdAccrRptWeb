using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class M2DoctorMonthlyReportControllerTests
{
    [Fact]
    public void CatalogExposesM2InMedicalStatistics()
    {
        ReportIndexViewModel catalog = new ReportCatalogService().GetReportIndex();
        ReportDefinitionViewModel report = catalog.Categories.Single(category => category.Key == "medical")
            .Groups.SelectMany(group => group.Reports).Single(item => item.Code == "M2");
        Assert.Equal("醫師看診人數月表", report.Name);
    }

    [Fact]
    public void ControllerRequiresAuthenticationAndAntiforgery()
    {
        Assert.Single(typeof(DoctorMonthlyReportController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        foreach (string name in new[] { nameof(DoctorMonthlyReportController.Query), nameof(DoctorMonthlyReportController.Preview) })
            Assert.Single(typeof(DoctorMonthlyReportController).GetMethod(name)!
                .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true));
    }

    [Fact]
    public async Task QueryReturnsPageNoStoreAndSafeAudit()
    {
        var audit = new Audit(); var controller = Create(new Service(), audit);
        var ok = Assert.IsType<OkObjectResult>(await controller.Query(new("2026-08"), default));
        Assert.IsType<M2DoctorMonthlyPagedResponse>(ok.Value);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
        Assert.Equal("alice", audit.Value!.Actor); Assert.Equal(3, audit.Value.NumericChecksum);
    }

    [Fact]
    public async Task MissingRunAndFailureUseStableStatuses()
    {
        var missing = Create(new Service { Missing = true }, new Audit());
        Assert.IsType<NotFoundObjectResult>(await missing.Query(new(null, RunId: "missing"), default));
        var failed = Create(new Service { Failed = true }, new Audit());
        var result = Assert.IsType<ObjectResult>(await failed.Query(new("2026-08"), default));
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("trace-m2", Assert.IsType<ProblemDetails>(result.Value).Extensions["traceId"]);
    }

    [Fact]
    public async Task Export_AllowsXlsxAndRejectsPdf()
    {
        var controller = Create(new Service(), new Audit());

        Assert.IsType<FileContentResult>(await controller.Export("run", "xlsx", default));
        Assert.IsType<BadRequestObjectResult>(await controller.Export("run", "pdf", default));
    }

    private static DoctorMonthlyReportController Create(IM2DoctorMonthlyReportService service, Audit audit)
    {
        var controller = new DoctorMonthlyReportController(service, new Renderer(), audit,
            new ReportCatalogService(), TimeProvider.System, NullLogger<DoctorMonthlyReportController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "test")), TraceIdentifier = "trace-m2" } };
        return controller;
    }

    private sealed class Service : IM2DoctorMonthlyReportService
    {
        public bool Missing { get; init; } public bool Failed { get; init; }
        public M2DoctorMonthlyReportSnapshot Snapshot { get; } = SnapshotValue();
        public Task<M2DoctorMonthlyPagedResponse> QueryAsync(M2DoctorMonthlyReportRequest request, string actor, CancellationToken cancellationToken = default)
        {
            if (Missing) throw new M2ReportRunNotFoundException(); if (Failed) throw new InvalidOperationException();
            return Task.FromResult(new M2DoctorMonthlyPagedResponse(Snapshot.RunId, Snapshot.Rows,
                M2DoctorMonthlyReportService.Columns, 1, 1, 10, 1));
        }
        public Task<M2DoctorMonthlyReportSnapshot?> GenerateAsync(M2DoctorMonthlyReportRequest request, string actor, CancellationToken cancellationToken = default) => Task.FromResult<M2DoctorMonthlyReportSnapshot?>(Snapshot);
        public bool TryGetRun(string runId, string actor, out M2DoctorMonthlyReportSnapshot snapshot) { snapshot = Snapshot; return !Missing && actor == "alice"; }
    }
    private sealed class Renderer : IM2DoctorMonthlyReportRenderer
    {
        public M2RenderedFile RenderXlsx(M2DoctorMonthlyReportSnapshot snapshot, string userName) => new([], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "x.xlsx");
    }
    private sealed class Audit : IM2PatientAccessAuditWriter
    {
        public M2PatientAccessAudit? Value { get; private set; }
        public Task WriteAsync(M2PatientAccessAudit audit, CancellationToken cancellationToken = default) { Value = audit; return Task.CompletedTask; }
    }
    internal static M2DoctorMonthlyReportSnapshot SnapshotValue()
    {
        int[] counts = new int[31]; counts[0] = 3;
        return new("0123456789abcdef0123456789abcdef0123456789abcdef", "alice", new(2026, 8, 1),
            M2CalculationBasis.Statistics, M2VisitScope.All, M2TimeSlot.All,
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            [M2DoctorMonthlyReportRow.Create("0450", "急診", "D1", "醫師", counts)], 3);
    }
}
