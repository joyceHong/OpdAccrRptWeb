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

public sealed class M1DoctorDailyReportControllerTests
{
    [Fact]
    public void Controller_RequiresOnlyCurrentAuthenticationAndAntiforgery()
    {
        AuthorizeAttribute attribute = Assert.Single(typeof(DoctorDailyReportController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Null(attribute.Policy);
        Assert.Null(attribute.Roles);
        foreach (string method in new[] { nameof(DoctorDailyReportController.Query), nameof(DoctorDailyReportController.Preview) })
            Assert.Single(typeof(DoctorDailyReportController).GetMethod(method)!
                .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true));
    }

    [Fact]
    public async Task Query_ReturnsCanonicalPageAndWritesSafeAudit()
    {
        var audit = new Audit();
        var controller = Create(new Service(), audit);
        IActionResult action = await controller.Query(new(new(2026, 9, 23)), default);

        var ok = Assert.IsType<OkObjectResult>(action);
        Assert.IsType<M1DoctorDailyPagedResponse>(ok.Value);
        Assert.Equal("alice", audit.Value!.Actor);
        Assert.Equal("Query", audit.Value.Operation);
        Assert.Equal("trace-m1", audit.Value.CorrelationId);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task Query_ConfirmationAndMissingRunUseStableStatuses()
    {
        var confirmation = Create(new Service { ThrowConfirmation = true }, new Audit());
        var conflict = Assert.IsType<ConflictObjectResult>(await confirmation.Query(new(new(2026, 9, 24)), default));
        Assert.Equal(409, conflict.StatusCode);
        var details = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(M1FutureDateConfirmationRequiredException.ErrorCode, details.Extensions["code"]);

        var missing = Create(new Service { ThrowMissing = true }, new Audit());
        Assert.IsType<NotFoundObjectResult>(await missing.Query(new(null, RunId: "missing"), default));
    }

    [Fact]
    public async Task PreviewAndExport_ReadSameSnapshotAndRejectUnsupportedFormat()
    {
        var service = new Service();
        var controller = Create(service, new Audit());
        Assert.IsType<ViewResult>(await controller.Preview(new(null, RunId: service.Snapshot.RunId), default));
        var file = Assert.IsType<FileContentResult>(await controller.Export(service.Snapshot.RunId, "xlsx", default));
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.IsType<BadRequestObjectResult>(await controller.Export(service.Snapshot.RunId, "pdf", default));
        Assert.IsType<BadRequestObjectResult>(await controller.Export(service.Snapshot.RunId, "csv", default));
        Assert.Equal(0, service.GenerateCalls);
    }

    [Fact]
    public void Catalog_ExposesM1InMedicalStatistics()
    {
        ReportIndexViewModel catalog = new ReportCatalogService().GetReportIndex();
        var medical = catalog.Categories.Single(category => category.Key == "medical");
        ReportDefinitionViewModel m1 = medical.Groups.SelectMany(group => group.Reports)
            .Single(report => report.Code == "M1");
        Assert.Equal("醫師看診人數日表", m1.Name);
    }

    private static DoctorDailyReportController Create(IM1DoctorDailyReportService service, Audit audit)
    {
        var controller = new DoctorDailyReportController(service, new M1DoctorDailyReportRenderer(), audit,
            new ReportCatalogService(), TimeProvider.System, NullLogger<DoctorDailyReportController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice")], "test")),
                TraceIdentifier = "trace-m1"
            }
        };
        return controller;
    }

    private sealed class Service : IM1DoctorDailyReportService
    {
        public M1DoctorDailyReportSnapshot Snapshot { get; } = M1DoctorDailyReportRendererTests.Snapshot();
        public bool ThrowConfirmation { get; init; }
        public bool ThrowMissing { get; init; }
        public int GenerateCalls { get; private set; }
        public Task<M1DoctorDailyPagedResponse> QueryAsync(M1DoctorDailyReportRequest request, string actor, CancellationToken cancellationToken = default)
        {
            if (ThrowConfirmation) throw new M1FutureDateConfirmationRequiredException(request.ReportDate!.Value);
            if (ThrowMissing) throw new M1ReportRunNotFoundException();
            return Task.FromResult(new M1DoctorDailyPagedResponse(Snapshot.RunId, Snapshot.Rows,
                M1DoctorDailyReportService.Columns, Snapshot.Rows.Count, 1, 10, 1));
        }
        public Task<M1DoctorDailyReportSnapshot?> GenerateAsync(M1DoctorDailyReportRequest request, string actor, CancellationToken cancellationToken = default)
        {
            GenerateCalls++;
            return Task.FromResult<M1DoctorDailyReportSnapshot?>(Snapshot);
        }
        public bool TryGetRun(string runId, string actor, out M1DoctorDailyReportSnapshot snapshot)
        {
            snapshot = Snapshot;
            return runId == Snapshot.RunId && actor == Snapshot.Actor;
        }
    }

    private sealed class Audit : IM1PatientAccessAuditWriter
    {
        public M1PatientAccessAudit? Value { get; private set; }
        public Task WriteAsync(M1PatientAccessAudit audit, CancellationToken cancellationToken = default)
        {
            Value = audit;
            return Task.CompletedTask;
        }
    }
}
