using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C16ReportServiceTests
{
    [Fact]
    public async Task QueryAsync_ReducesBeforePagingAndReturnsPageMetadata()
    {
        var repository = new FakeRepository { Rows = Enumerable.Range(1, 28).Select(i => Row(i)).ToList() };
        C16ReportResult result = await Create(repository).QueryAsync(Request(page: 3));
        Assert.Equal((28, 3, 10, 3, 8), (result.Page.TotalCount, result.Page.PageNumber,
            result.Page.PageSize, result.Page.TotalPages, result.Page.Data!.Count));
    }

    [Fact]
    public async Task QueryAsync_SelectsExactlyOneModeAndNormalizesInpatientBasis()
    {
        var repository = new FakeRepository();
        await Create(repository).QueryAsync(Request(basis: C16DateBasis.VisitDate));
        Assert.Equal((1,0,0),(repository.VisitCalls,repository.AccountingCalls,repository.InpatientCalls));
        await Create(repository).QueryAsync(Request(source:C16Source.Inpatient,basis:C16DateBasis.VisitDate));
        Assert.Equal(1, repository.InpatientCalls);
    }

    [Fact]
    public async Task QueryAsync_CacheIdentityExcludesPageAndSeparatesFilters()
    {
        var repository = new FakeRepository { Rows = [Row(1), Row(2)] };
        var cache = new ReportTotalCountCache(new MemoryCache(new MemoryCacheOptions()));
        var service = new C16ReportService(repository,new C16LegacyReducer(),cache);
        await service.QueryAsync(Request(page:1));
        repository.Rows = [Row(1),Row(2),Row(3)];
        Assert.Equal(2,(await service.QueryAsync(Request(page:2))).Page.TotalCount);
        Assert.Equal(3,(await service.QueryAsync(Request(page:1,type:C16ReportType.NewHope))).Page.TotalCount);
    }

    [Fact]
    public async Task ConcurrentRequests_DoNotShareMutableRows()
    {
        var repository=new FakeRepository{RowFactory=request=>[Row(request.ReportType==C16ReportType.Child?1:2) with {PatientName=request.ReportType.ToString()}]};
        var service=Create(repository);
        C16ReportResult[] results=await Task.WhenAll(service.QueryAsync(Request(type:C16ReportType.Child)),service.QueryAsync(Request(type:C16ReportType.NewHope)));
        Assert.Equal("Child",Assert.Single(results[0].AllRows).PatientName);
        Assert.Equal("NewHope",Assert.Single(results[1].AllRows).PatientName);
    }

    [Fact]
    public async Task CancelledRequest_DoesNotReachRepositoryOrCache()
    {
        var repository=new FakeRepository(); using var source=new CancellationTokenSource();source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Create(repository).QueryAsync(Request(),source.Token));
        Assert.Equal(0,repository.AccountingCalls+repository.VisitCalls+repository.InpatientCalls);
    }

    private static C16ReportService Create(FakeRepository repository) => new(repository,new C16LegacyReducer(),
        new ReportTotalCountCache(new MemoryCache(new MemoryCacheOptions())));
    private static C16PreviewRequest Request(int page=1,C16Source source=C16Source.OutpatientEmergency,
        C16ReportType type=C16ReportType.All,C16DateBasis basis=C16DateBasis.AccountingDate) =>
        new("2026-09-01","2026-09-16",source,type,basis,page,10);
    private static C16SourceRow Row(int i) => new(i-1,"N","P","0900101","1150901","1150902","S","Z",null,
        "30","104","1-25-99","1",i.ToString("000000"),i,1,null);

    private sealed class FakeRepository : IC16ReportRepository
    {
        public IReadOnlyList<C16SourceRow> Rows { get; set; }=[];
        public Func<C16PreviewRequest,IReadOnlyList<C16SourceRow>>? RowFactory {get;init;}
        public int VisitCalls,AccountingCalls,InpatientCalls;
        private IReadOnlyList<C16SourceRow> Get(C16PreviewRequest r)=>RowFactory?.Invoke(r)??Rows;
        public Task<IReadOnlyList<C16SourceRow>> QueryOutpatientByVisitDateAsync(C16PreviewRequest r,C16QueryPeriod p,CancellationToken c=default){VisitCalls++;return Task.FromResult(Get(r));}
        public Task<IReadOnlyList<C16SourceRow>> QueryOutpatientByAccountingDateAsync(C16PreviewRequest r,C16QueryPeriod p,CancellationToken c=default){AccountingCalls++;return Task.FromResult(Get(r));}
        public Task<IReadOnlyList<C16SourceRow>> QueryInpatientByAccountingDateAsync(C16PreviewRequest r,C16QueryPeriod p,CancellationToken c=default){InpatientCalls++;return Task.FromResult(Get(r));}
    }
}
