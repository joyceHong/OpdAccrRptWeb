using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C141RankingControllerTests
{
    [Fact]
    public void Today_ReturnsOracleDateInGregorianFormat()
    {
        var repository = new FakeRepository(new DateOnly(2026, 10, 13), []);
        C141RankingController controller = CreateController(new C141RankingService(repository));

        var response = Assert.IsType<OkObjectResult>(controller.Today(default));

        Assert.Equal("2026-10-13", Read<string>(response.Value!, "accountingDate"));
    }

    [Fact]
    public void Today_IssuesEntryDateTicketBoundToAuthenticatedServerIdentity()
    {
        var repository = new FakeRepository(new DateOnly(2026, 10, 13), []);
        C141RankingController controller = CreateController(new C141RankingService(repository));
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, "reporter")], "test"));

        var response = Assert.IsType<OkObjectResult>(controller.Today(default));

        Assert.Equal("ticket:reporter:20261013", Read<string>(response.Value!, "entryDateTicket"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1911-12-31")]
    [InlineData("2026/10/13")]
    public void Query_InvalidDate_ReturnsBadRequestWithoutRankingQuery(string? date)
    {
        var repository = new FakeRepository(new DateOnly(2026, 10, 13), []);
        C141RankingController controller = CreateController(new C141RankingService(repository));

        IActionResult result = controller.Query(new(date, null, null, null), default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, repository.QueryCount);
    }

    [Fact]
    public void Query_FutureDate_ReturnsBadRequestWithoutRankingQuery()
    {
        var repository = new FakeRepository(new DateOnly(2026, 10, 13), []);
        C141RankingController controller = CreateController(new C141RankingService(repository));

        IActionResult result = controller.Query(new("2026-10-14", null, null, null), default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, repository.QueryCount);
    }

    [Fact]
    public void Query_OverlongMedicalRecord_ReturnsBadRequestBeforeRankingQuery()
    {
        var repository = new FakeRepository(new DateOnly(2026, 10, 13), []);
        C141RankingController controller = CreateController(new C141RankingService(repository));

        IActionResult result = controller.Query(new("2026-10-13", "12345678901", null, null), default);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, repository.QueryCount);
    }

    [Fact]
    public void Query_ConvertsGregorianDateAndReturnsSharedServerPagedShape()
    {
        var rows = Enumerable.Range(1, 11)
            .Select(index => Source($"115010{index:00}", "1", "A12", index))
            .ToArray();
        var repository = new FakeRepository(new DateOnly(2026, 10, 13), rows);
        C141RankingController controller = CreateController(new C141RankingService(repository));

        IActionResult result = controller.Query(new("2026-01-10", null, null, null, 1, 10), default);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(12, C141RankingColumns.Visible.Count);
        Assert.Equal("1150110", repository.LastSearch?.AccountingDateRoc);
        Assert.Equal(11, Read<int>(response.Value!, "totalCount"));
        Assert.Equal(2, Read<int>(response.Value!, "totalPages"));
        Assert.Equal(1, Read<int>(response.Value!, "pageNumber"));
        Assert.Equal(10, Read<IReadOnlyList<C141RankingRow>>(response.Value!, "data").Count);
    }

    private static C141RankingController CreateController(IC141RankingService service)
    {
        var controller = new C141RankingController(service, new FakeEntryDateTicketService(), NullLogger<C141RankingController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private static T Read<T>(object target, string propertyName) =>
        (T)(target.GetType().GetProperty(propertyName)?.GetValue(target)
            ?? throw new InvalidOperationException($"Response property {propertyName} was missing."));

    private static C141RankingSourceRow Source(string date, string time, string room, decimal number) =>
        new(new(date, time, room, number), "C36979", "測試", room, null, "30", null, null,
            "1", "1", null, 200m, null, null, null, 65m, false);

    private sealed class FakeEntryDateTicketService : IC141EntryDateTicketService
    {
        public string Create(DateOnly entryDate, string userId) => $"ticket:{userId}:{entryDate:yyyyMMdd}";
        public bool TryRead(string? ticket, string userId, out DateOnly entryDate)
        {
            entryDate = default;
            return false;
        }
    }

    private sealed class FakeRepository(DateOnly today, IReadOnlyList<C141RankingSourceRow> rows)
        : IC141RankingRepository
    {
        public int QueryCount { get; private set; }
        public C141RankingSearch? LastSearch { get; private set; }

        public DateOnly GetOracleToday(CancellationToken cancellationToken) => today;

        public IReadOnlyList<C141RankingSourceRow> Query(C141RankingSearch search, CancellationToken cancellationToken)
        {
            QueryCount++;
            LastSearch = search;
            return rows;
        }

        public int GetCount(C141RankingSearch search, CancellationToken cancellationToken)
        {
            QueryCount++;
            LastSearch = search;
            return rows.Count;
        }

        public IReadOnlyList<C141RankingSourceRow> GetPage(
            C141RankingSearch search,
            int offset,
            int pageSize,
            CancellationToken cancellationToken)
        {
            QueryCount++;
            LastSearch = search;
            return rows
                .OrderByDescending(row => (row.ReceivableAmount ?? 0m) - (row.PayTotal ?? 0m))
                .ThenBy(row => row.Key.AdmissionDate, StringComparer.Ordinal)
                .ThenBy(row => row.Key.AdmissionTime, StringComparer.Ordinal)
                .ThenBy(row => row.Key.AdmissionRoom, StringComparer.Ordinal)
                .ThenBy(row => row.Key.AdmissionNumber)
                .Skip(offset)
                .Take(pageSize)
                .ToArray();
        }
    }
}
