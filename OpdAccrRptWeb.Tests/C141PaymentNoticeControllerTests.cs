using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C141PaymentNoticeControllerTests
{
    private static readonly C141AdmissionKey Key = new("1150101", "1", "A12", 1m);

    [Fact]
    public async Task Preview_RecordsBeforeRenderingAndAppliesEntryDateAfterDocumentGeneration()
    {
        var events = new List<string>();
        var service = new FakeNoticeService(events, CreateDocument(Key, partialAvailable: true));
        var renderer = new FakeRenderer(events);
        var controller = CreateController(service, renderer, new FakeTicketService(true), AuthenticatedUser);

        IActionResult result = await controller.Preview(new([Key], "valid-ticket"), default);

        var response = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html; charset=utf-8", response.ContentType);
        Assert.Equal(
            ["calculate", "before", "render-document", "after", "render-page"],
            events);
        Assert.Equal("1151013", service.RecordedEntryDate);
        Assert.Equal("srv-user", service.RecordedUserId);
    }

    [Fact]
    public async Task Preview_BlocksInsuredCaseUntilPartialPaymentCanBeCalculated()
    {
        var events = new List<string>();
        var service = new FakeNoticeService(events, CreateDocument(Key, partialAvailable: false));
        var renderer = new FakeRenderer(events);
        var controller = CreateController(service, renderer, new FakeTicketService(true), AuthenticatedUser);

        IActionResult result = await controller.Preview(new([Key], "valid-ticket"), default);

        Assert.IsType<ContentResult>(result);
        Assert.Equal(["calculate", "render-page"], events);
        Assert.Equal("失敗", renderer.LastPage!.Items.Single().Status);
        Assert.Contains("部分負擔計算尚未完成", renderer.LastPage.Items.Single().Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preview_RejectsTicketForAnotherPageSessionBeforeDatabaseReads()
    {
        var events = new List<string>();
        var service = new FakeNoticeService(events, CreateDocument(Key, partialAvailable: true));
        var controller = CreateController(service, new FakeRenderer(events), new FakeTicketService(false), AuthenticatedUser);

        IActionResult result = await controller.Preview(new([Key], "invalid-ticket"), default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(events);
    }

    [Fact]
    public async Task Preview_RejectsAnonymousBeforeDatabaseReads()
    {
        var events = new List<string>();
        var service = new FakeNoticeService(events, CreateDocument(Key, partialAvailable: true));
        var controller = CreateController(service, new FakeRenderer(events), new FakeTicketService(true), new ClaimsPrincipal(new ClaimsIdentity()));

        IActionResult result = await controller.Preview(new([Key], "valid-ticket"), default);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(events);
    }

    [Fact]
    public async Task Preview_PreWriteFailureDoesNotRenderOrApplyAfterDateUpdate()
    {
        var events = new List<string>();
        var service = new FakeNoticeService(events, CreateDocument(Key, partialAvailable: true)) { ThrowBeforeWrite = true };
        var renderer = new FakeRenderer(events);
        var controller = CreateController(service, renderer, new FakeTicketService(true), AuthenticatedUser);

        IActionResult result = await controller.Preview(new([Key], "valid-ticket"), default);

        Assert.IsType<ContentResult>(result);
        Assert.Equal(["calculate", "before", "render-page"], events);
        Assert.Equal("失敗", renderer.LastPage!.Items.Single().Status);
        Assert.Equal("", service.RecordedEntryDate);
    }

    private static ClaimsPrincipal AuthenticatedUser =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "srv-user")], "test"));

    private static C141PaymentNoticeController CreateController(
        IC141PaymentNoticeService service,
        IC141RazorViewRenderer renderer,
        IC141EntryDateTicketService tickets,
        ClaimsPrincipal user) =>
        new(service, tickets, renderer, NullLogger<C141PaymentNoticeController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } }
        };

    private static C141PaymentNoticeDocument CreateDocument(C141AdmissionKey key, bool partialAvailable) =>
        new(
            key,
            new("C36979", "測試", "01", "內科", "A12", "1150101", "000", "01", "1", "醫師", "1", null),
            [],
            new(0m, 0m, 0m, 0m, 0m, 0m, 0m),
            null,
            null,
            null)
        {
            PartialPaymentCalculationAvailable = partialAvailable
        };

    private sealed class FakeTicketService(bool valid) : IC141EntryDateTicketService
    {
        public string Create(DateOnly entryDate, string userId) => "valid-ticket";
        public bool TryRead(string? ticket, string userId, out DateOnly entryDate)
        {
            entryDate = new DateOnly(2026, 10, 13);
            return valid && ticket == "valid-ticket" && userId == "srv-user";
        }
    }

    private sealed class FakeNoticeService(
        ICollection<string> events,
        C141PaymentNoticeDocument? document) : IC141PaymentNoticeService
    {
        public bool ThrowBeforeWrite { get; init; }
        public string? RecordedUserId { get; private set; }
        public string RecordedEntryDate { get; private set; } = string.Empty;
        public C141NoticePreviewPage? LastPage { get; private set; }

        public DateTimeOffset GetOracleNow(CancellationToken cancellationToken = default) =>
            new(2026, 10, 13, 8, 30, 0, TimeSpan.FromHours(8));

        public C141PaymentNoticeDocument? Calculate(C141AdmissionKey key, CancellationToken cancellationToken = default)
        {
            events.Add("calculate");
            return document;
        }

        public void RecordBeforeDocument(C141PaymentNoticeDocument notice, string userId, CancellationToken cancellationToken = default)
        {
            events.Add("before");
            if (ThrowBeforeWrite)
                throw new InvalidOperationException("database failure");
            RecordedUserId = userId;
        }

        public void RecordAfterDocument(C141AdmissionKey key, string entryDateRoc, CancellationToken cancellationToken = default)
        {
            events.Add("after");
            RecordedEntryDate = entryDateRoc;
        }
    }

    private sealed class FakeRenderer(ICollection<string> events) : IC141RazorViewRenderer
    {
        public C141NoticePreviewPage? LastPage { get; private set; }

        public Task<string> RenderToStringAsync(
            ActionContext actionContext,
            string viewPath,
            object model,
            CancellationToken cancellationToken)
        {
            if (model is C141PaymentNoticeDocument)
            {
                events.Add("render-document");
                return Task.FromResult("<article>notice</article>");
            }

            events.Add("render-page");
            var page = (C141NoticePreviewPage)model;
            LastPage = page;
            return Task.FromResult(string.Join('|', page.Items.Select(item => item.Status)));
        }
    }
}
