using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C141LineBotControllerTests
{
    [Fact]
    public async Task Send_RejectsAnonymousBeforeCallingService()
    {
        var service = new FakeService();
        C141LineBotController controller = CreateController(service, new ClaimsPrincipal(new ClaimsIdentity()));

        IActionResult result = await controller.Send(
            new(C141LineBotMessageKind.PaymentNotice, [Key]),
            default);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task Send_UsesAuthenticatedServerIdentityAndValidatedFourKey()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "server-user")], "test");
        var service = new FakeService();
        C141LineBotController controller = CreateController(service, new ClaimsPrincipal(identity));

        IActionResult result = await controller.Send(
            new(C141LineBotMessageKind.CardReminder, [Key]),
            default);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("server-user", service.UserId);
        Assert.Equal(Key, Assert.Single(service.Cases));
    }

    [Fact]
    public async Task Send_RejectsMalformedKeyWithoutCallingService()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "server-user")], "test");
        var service = new FakeService();
        C141LineBotController controller = CreateController(service, new ClaimsPrincipal(identity));

        IActionResult result = await controller.Send(
            new(C141LineBotMessageKind.PaymentNotice, [Key with { AdmissionTime = "12" }]),
            default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    private static readonly C141AdmissionKey Key = new("1150101", "1", "A12", 1m);

    private static C141LineBotController CreateController(IC141LineBotService service, ClaimsPrincipal user)
    {
        var controller = new C141LineBotController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };
        return controller;
    }

    private sealed class FakeService : IC141LineBotService
    {
        public int Calls { get; private set; }
        public string? UserId { get; private set; }
        public IReadOnlyList<C141AdmissionKey> Cases { get; private set; } = [];

        public Task<C141LineBotBatchResult> SendAsync(
            C141LineBotMessageKind kind,
            IReadOnlyList<C141AdmissionKey> cases,
            string userId,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            UserId = userId;
            Cases = cases;
            return Task.FromResult(new C141LineBotBatchResult([]));
        }
    }
}
