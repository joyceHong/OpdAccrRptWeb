using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class OutpatientReceivableBalanceReportServiceTests
{
    [Fact]
    public void ReportDataAndColumns_C214_NormalizesCutoffAndReturnsPagedMetadata()
    {
        var repository = new FakeOutpatientReceivableBalanceRepository
        {
            TotalCount = 21,
            Data = Enumerable.Range(0, 10)
                .Select(_ => new OutpatientReceivableBalanceReportViewModel())
                .ToList()
        };

        var result = CreateService(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(
                Condition("2026-08-31", ReceivableBalanceTypes.SelfPay, 1, 10));

        Assert.Equal(3, result.Columns!.Count);
        Assert.Equal(10, result.Data!.Count);
        Assert.Equal(21, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal("1150831", repository.CountConditions.Single().EndDate);
        Assert.Equal(ReceivableBalanceTypes.SelfPay,
            repository.CountConditions.Single().ReceivableBalanceType);
    }

    [Fact]
    public void ReportDataAndColumns_C214_CachesByCutoffAndTypeButAlwaysQueriesPage()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeOutpatientReceivableBalanceRepository { TotalCount = 21 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-31", ReceivableBalanceTypes.SelfPay, 1, 10));
        service.ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-31", ReceivableBalanceTypes.SelfPay, 2, 30));
        service.ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-31", ReceivableBalanceTypes.Insurance, 1, 10));
        service.ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-30", ReceivableBalanceTypes.SelfPay, 1, 10));

        Assert.Equal(3, repository.CountCalls);
        Assert.Equal(4, repository.PageCalls);
        Assert.Equal(2, repository.PageConditions[1].PageNumber);
        Assert.Equal(30, repository.PageConditions[1].PageSize);
    }

    [Fact]
    public void ReportDataAndColumns_C214_CountFailureIsNotCached()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var failure = new InvalidOperationException("count failed");
        var repository = new FakeOutpatientReceivableBalanceRepository
        {
            TotalCount = 0,
            CountException = failure
        };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(
                Condition("2026-08-31", ReceivableBalanceTypes.SelfPay, 1, 10))));
        repository.CountException = null;

        var result = service.ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-31", ReceivableBalanceTypes.SelfPay, 1, 10));

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(1, repository.PageCalls);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void ReportDataAndColumns_C214_PageFailurePropagates()
    {
        var failure = new InvalidOperationException("page failed");
        var repository = new FakeOutpatientReceivableBalanceRepository
        {
            TotalCount = 1,
            PageException = failure
        };

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
            CreateService(repository, new PassthroughReportTotalCountCache())
                .ReportDataAndColumns<OutpatientReceivableBalanceReportViewModel>(
                    Condition("2026-08-31", ReceivableBalanceTypes.Insurance, 1, 10))));
        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(1, repository.PageCalls);
    }

    private static ReportService CreateService(
        FakeOutpatientReceivableBalanceRepository repository,
        IReportTotalCountCache cache) => new(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            cache,
            NullLogger<ReportService>.Instance,
            outpatientReceivableBalanceRepository: repository);

    private static SearchReportCondition Condition(
        string endDate,
        string balanceType,
        int pageNumber,
        int pageSize) => new()
    {
        ReportCode = "C214",
        EndDate = endDate,
        ReceivableBalanceType = balanceType,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
