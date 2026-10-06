using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class RegistrationQueryControllerTests
{
    [Fact]
    public void ControllerAndQueryActionRequireAuthorizationAndAntiforgery()
    {
        Assert.NotNull(typeof(RegistrationQueryController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .SingleOrDefault());
        Assert.NotNull(typeof(RegistrationQueryController)
            .GetMethod(nameof(RegistrationQueryController.Query))!
            .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true)
            .SingleOrDefault());
    }

    [Fact]
    public async Task Query_ReturnsBadRequestForInvalidConditions()
    {
        var service = new FakeService { QueryException = new ArgumentException("invalid") };
        RegistrationQueryController controller = Create(service);

        IActionResult result = await controller.Query(
            new("registered", "2026-10-05", null, null, null, null, null, null, null, "bad", null),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid", badRequest.Value);
    }

    [Fact]
    public async Task Query_ReturnsSafeProblemDetailsAndNoStoreForUnexpectedFailure()
    {
        var service = new FakeService { QueryException = new InvalidOperationException("secret sql") };
        RegistrationQueryController controller = Create(service);

        IActionResult result = await controller.Query(
            new("registered", "2026-10-05", null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, error.StatusCode);
        Assert.DoesNotContain("secret sql", error.Value?.ToString() ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl.ToString());
        var problem = Assert.IsType<ProblemDetails>(error.Value);
        Assert.Contains("traceId", problem.Extensions.Keys);
    }

    [Fact]
    public async Task Query_ReturnsResultAndNoStoreHeader()
    {
        var service = new FakeService
        {
            Result = new("registered", [], 0, 1, 10, 0, null)
        };
        RegistrationQueryController controller = Create(service);

        IActionResult result = await controller.Query(
            new("registered", "2026-10-05", null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task LookupEndpointsReturnServiceOptions()
    {
        var service = new FakeService
        {
            Sections = [new("01", "N01", "內科")],
            Doctors = [new("D01", "陳醫師")]
        };
        RegistrationQueryController controller = Create(service);

        var sections = Assert.IsType<OkObjectResult>(
            await controller.Sections("內", CancellationToken.None));
        var doctors = Assert.IsType<OkObjectResult>(
            await controller.Doctors("陳", CancellationToken.None));

        Assert.Same(service.Sections, sections.Value);
        Assert.Same(service.Doctors, doctors.Value);
    }

    private static RegistrationQueryController Create(FakeService service)
    {
        return new RegistrationQueryController(
            service,
            new ReportCatalogService(),
            NullLogger<RegistrationQueryController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "q3-test-trace" }
            }
        };
    }

    private sealed class FakeService : IRegistrationQueryService
    {
        public RegistrationQueryResult? Result { get; init; }
        public Exception? QueryException { get; init; }
        public IReadOnlyList<RegistrationSectionOption> Sections { get; init; } = [];
        public IReadOnlyList<RegistrationDoctorOption> Doctors { get; init; } = [];

        public Task<RegistrationQueryResult> QueryAsync(
            RegistrationQueryRequest request,
            CancellationToken token)
        {
            if (QueryException is not null)
            {
                return Task.FromException<RegistrationQueryResult>(QueryException);
            }

            return Task.FromResult(Result ?? new("registered", [], 0, 1, 10, 0, null));
        }

        public Task<IReadOnlyList<RegistrationSectionOption>> SearchSectionsAsync(
            string query,
            CancellationToken token) => Task.FromResult(Sections);

        public Task<IReadOnlyList<RegistrationDoctorOption>> SearchDoctorsAsync(
            string query,
            CancellationToken token) => Task.FromResult(Doctors);
    }
}
