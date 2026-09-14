using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ContractPaymentDetailReportServiceTests
{
    [Fact]
    public void DependencyInjection_ResolvesReportServiceWithC29Repository()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHealthCenterRepository, FakeHealthCenterRepository>();
        services.AddSingleton<IReferralMemberRepository, FakeReferralMemberRepository>();
        services.AddSingleton<ISafeNeedleRepository, FakeSafeNeedleRepository>();
        services.AddSingleton<IReportTotalCountCache, PassthroughReportTotalCountCache>();
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<ReportService>>(
            NullLogger<ReportService>.Instance);
        services.AddSingleton<IContractPaymentDetailRepository, FakeContractPaymentDetailRepository>();
        services.AddSingleton<IReportService, ReportService>();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<ReportService>(provider.GetRequiredService<IReportService>());
        Assert.Contains(
            "AddSingleton<IContractPaymentDetailRepository, ContractPaymentDetailRepository>()",
            File.ReadAllText(Path.Combine(ProjectRoot(), "Program.cs")));
    }

    [Fact]
    public void ReportDataAndColumns_C29_ReturnsNineColumnsAndPageMetadata()
    {
        var repository = new FakeContractPaymentDetailRepository
        {
            TotalCount = 21,
            Data = [new ContractPaymentDetailReportViewModel { ContractCode = "A01" }]
        };

        var result = CreateService(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<ContractPaymentDetailReportViewModel>(Condition(
                EncounterSources.Emergency, "  A01  ", 2, 10));

        Assert.Equal(9, result.Columns!.Count);
        Assert.Single(result.Data!);
        Assert.Equal(21, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal("1150801", repository.CountConditions.Single().StartDate);
        Assert.Equal("1150831", repository.CountConditions.Single().EndDate);
        Assert.Equal("A01", repository.CountConditions.Single().BillingCode);
        Assert.Equal("A01", repository.PageConditions.Single().BillingCode);
    }

    [Fact]
    public void ReportDataAndColumns_C29_NormalizesBlankContractCodeToNull()
    {
        var repository = new FakeContractPaymentDetailRepository();

        CreateService(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<ContractPaymentDetailReportViewModel>(Condition(
                EncounterSources.Emergency, "   ", 1, 10));

        Assert.Null(repository.CountConditions.Single().BillingCode);
        Assert.Null(repository.PageConditions.Single().BillingCode);
    }

    [Fact]
    public void ReportDataAndColumns_C29_CachesByMembershipFiltersButAlwaysQueriesPage()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeContractPaymentDetailRepository { TotalCount = 21 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<ContractPaymentDetailReportViewModel>(
            Condition(EncounterSources.Emergency, "A01", 1, 10));
        service.ReportDataAndColumns<ContractPaymentDetailReportViewModel>(
            Condition(EncounterSources.Emergency, " A01 ", 2, 30));
        service.ReportDataAndColumns<ContractPaymentDetailReportViewModel>(
            Condition(EncounterSources.Inpatient, "A01", 1, 10));
        service.ReportDataAndColumns<ContractPaymentDetailReportViewModel>(
            Condition(EncounterSources.Emergency, "B02", 1, 10));
        var changedDate = Condition(EncounterSources.Emergency, "A01", 1, 10);
        changedDate.StartDate = "2026-08-02";
        service.ReportDataAndColumns<ContractPaymentDetailReportViewModel>(changedDate);

        Assert.Equal(4, repository.CountCalls);
        Assert.Equal(5, repository.PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C29_CountFailureIsRetriedAndNotCached()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var expected = new InvalidOperationException("count failed");
        var repository = new FakeContractPaymentDetailRepository { CountException = expected };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<ContractPaymentDetailReportViewModel>(
                Condition(EncounterSources.Emergency, null, 1, 10))));

        repository.CountException = null;
        service.ReportDataAndColumns<ContractPaymentDetailReportViewModel>(
            Condition(EncounterSources.Emergency, null, 1, 10));

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(1, repository.PageCalls);
    }

    private static ReportService CreateService(
        IContractPaymentDetailRepository repository,
        IReportTotalCountCache cache) => new(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            cache,
            NullLogger<ReportService>.Instance,
            contractPaymentDetailRepository: repository);

    private static SearchReportCondition Condition(
        string source,
        string? billingCode,
        int pageNumber,
        int pageSize) => new()
    {
        ReportCode = "C29",
        StartDate = "2026-08-01",
        EndDate = "2026-08-31",
        EncounterSource = source,
        BillingCode = billingCode,
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    private static string ProjectRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
