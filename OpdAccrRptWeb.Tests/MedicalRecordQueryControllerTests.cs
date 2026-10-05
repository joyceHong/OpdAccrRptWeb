using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class MedicalRecordQueryControllerTests
{
    [Fact]
    public void ControllerAndPostActionsRequireAuthorizationAndAntiforgery()
    {
        Assert.NotNull(typeof(MedicalRecordQueryController).GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
        foreach (string methodName in new[] { nameof(MedicalRecordQueryController.Records), nameof(MedicalRecordQueryController.Detail) })
        {
            Assert.NotNull(typeof(MedicalRecordQueryController)
                .GetMethod(methodName)!
                .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true)
                .SingleOrDefault());
        }
    }

    [Fact]
    public async Task Records_ReturnsBadRequestForInvalidConditions()
    {
        var service = new FakeService { QueryException = new ArgumentException("invalid") };
        MedicalRecordQueryController controller = Create(service);

        IActionResult result = await controller.Records(new("", null, null, null, null, null, null, null), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid", badRequest.Value);
    }

    [Fact]
    public async Task Records_ReturnsSafeProblemDetailsForUnexpectedFailure()
    {
        var service = new FakeService { QueryException = new InvalidOperationException("secret sql") };
        MedicalRecordQueryController controller = Create(service);

        IActionResult result = await controller.Records(new("AB123", null, null, null, null, null, null, null), CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, error.StatusCode);
        Assert.DoesNotContain("secret sql", error.Value?.ToString() ?? string.Empty, StringComparison.Ordinal);
        var problem = Assert.IsType<ProblemDetails>(error.Value);
        Assert.Contains("traceId", problem.Extensions.Keys);
    }

    [Fact]
    public async Task Detail_ReturnsNotFoundForUnknownMedicalRecord()
    {
        var service = new FakeService { DetailException = new KeyNotFoundException() };
        MedicalRecordQueryController controller = Create(service);

        IActionResult result = await controller.Detail(new("AB123"), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("查無指定的病歷資料。", notFound.Value);
    }

    [Fact]
    public async Task Records_ReturnsQueryPageAndNoStoreHeader()
    {
        var service = new FakeService { Page = new([], 1, 1, 10, 1) };
        MedicalRecordQueryController controller = Create(service);

        IActionResult result = await controller.Records(new("AB123", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl.ToString());
    }

    private static MedicalRecordQueryController Create(FakeService service)
    {
        var controller = new MedicalRecordQueryController(
            service,
            new ReportCatalogService(),
            NullLogger<MedicalRecordQueryController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "q2-test-trace" }
            }
        };
        return controller;
    }

    private sealed class FakeService : IMedicalRecordQueryService
    {
        public MedicalRecordQueryPage? Page { get; init; }
        public Exception? QueryException { get; init; }
        public Exception? DetailException { get; init; }

        public Task<MedicalRecordQueryPage> QueryAsync(MedicalRecordQueryRequest request, CancellationToken token)
        {
            if (QueryException is not null)
            {
                return Task.FromException<MedicalRecordQueryPage>(QueryException);
            }

            return Task.FromResult(Page ?? new([], 0, 1, 10, 0));
        }

        public Task<MedicalRecordDetail> QueryDetailAsync(MedicalRecordDetailRequest request, CancellationToken token)
        {
            if (DetailException is not null)
            {
                return Task.FromException<MedicalRecordDetail>(DetailException);
            }

            throw new NotSupportedException();
        }
    }
}
