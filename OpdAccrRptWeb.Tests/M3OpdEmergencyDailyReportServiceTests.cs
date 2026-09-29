using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class M3OpdEmergencyDailyReportServiceTests
{
    [Fact]
    public async Task Query_AppliesLegacyOrderCutoffMappingAndKpis()
    {
        var repo = new Repository
        {
            Result = new(
                [Aggregate("0420",1), Aggregate("0450",2), Aggregate("0201",3)],
                [Aggregate("0999",4)], [Aggregate("0999",5)],
                new(20,15,10), new(120,90,60,100,12))
        };
        var mappings = new Mappings { Values = { ["0201"] = new("ignored", "new") } };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new M3OpdEmergencyDailyReportService(repo, mappings,
            new M3ReportRunStore(memory, TimeProvider.System));
        M3OpdEmergencyDailyPagedResponse result = await service.QueryAsync(
            new(new(2026,9,24)), "actor");
        Assert.Equal(3, result.TotalCount);
        Assert.DoesNotContain(result.Data, row => row.DepartmentId == "0420");
        Assert.Contains(result.Data, row => row.DepartmentId == "0450" && row.Op1SQty == 6 && row.Op1DaySum == 9);
        Assert.Contains(result.Data, row => row.DepartmentId == "11910" && row.DepartmentName == "N0201");
        Assert.Contains(result.Data, row => row.DepartmentId == "0999" && row.Op1MonQty == 12 && row.Op1YearQty == 15);
        Assert.Equal(new M3NineKpis(100,75,50,20,15,10,100,12,88), result.NineKpis);
        Assert.All(mappings.RoomTypes, Assert.Empty);
    }

    [Fact]
    public void PreCutoffFormula_UsesLegacyIdentitySplit()
    {
        M3OpdEmergencyDailyReportService.WorkRow row = Assert.Single(
            M3OpdEmergencyDailyReportService.Map([new("1",1,2,3,4,5,6,7,8,9,10,11,12)], "1000831"));
        Assert.Equal(1, row.Op1S); Assert.Equal(5, row.Op1H);
        Assert.Equal(4, row.Op2S); Assert.Equal(11, row.Op2H);
        Assert.Equal(7, row.Em1S); Assert.Equal(17, row.Em1H);
        Assert.Equal(10, row.Em2S); Assert.Equal(23, row.Em2H);
    }

    [Fact]
    public void Merge0420Without0450_DropsSource()
    {
        List<M3OpdEmergencyDailyReportService.WorkRow> rows =
            M3OpdEmergencyDailyReportService.Map([Aggregate("0420",1)], "1150924");
        M3OpdEmergencyDailyReportService.Merge0420Into0450(rows);
        Assert.Empty(rows);
    }

    [Fact]
    public async Task ExistingRun_PagesWithoutQueryingRepository()
    {
        var repo = new Repository { Result = new(Enumerable.Range(1,12).Select(i=>Aggregate(i.ToString("0000"),1)).ToArray(),[],[],new(0,0,0),new(null,null,null,null,null)) };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new M3OpdEmergencyDailyReportService(repo, new Mappings(), new M3ReportRunStore(memory, TimeProvider.System));
        var first = await service.QueryAsync(new(new(2026,9,24),PageSize:10),"actor");
        var second = await service.QueryAsync(new(null,2,10,first.RunId),"actor");
        Assert.Equal(1, repo.QueryCount); Assert.Equal(2, second.Data.Count); Assert.Equal(2, second.PageNumber);
    }

    private static M3DepartmentAggregate Aggregate(string id,long n) => new(id,n,n,n,n,n,n,n,n,n,n,n,n);

    private sealed class Repository : IM3OpdEmergencyDailyReportRepository
    {
        public required M3RepositoryResult Result { get; init; }
        public int QueryCount { get; private set; }
        public Task<M3RepositoryResult> QueryAsync(string reportDate,string month,string year,CancellationToken token=default)
        { QueryCount++; Assert.Equal("1150924",reportDate); Assert.Equal("1150901",month); Assert.Equal("1150101",year); return Task.FromResult(Result); }
        public Task<string> ResolveDepartmentNameAsync(string code,CancellationToken token=default) => Task.FromResult("N"+code);
    }
    private sealed class Mappings : ISectionMappingRepository
    {
        public Dictionary<string,SectionMapping> Values { get; }=[]; public List<string> RoomTypes { get; }=[];
        public Task<SectionMapping> TranslateAsync(string code,string roomType="",CancellationToken token=default){RoomTypes.Add(roomType);return Task.FromResult(Values.TryGetValue(code,out var v)?v:new SectionMapping("", ""));}
        public Task<bool> LocationExistsAsync(string x,CancellationToken t=default)=>Task.FromResult(false);
        public Task<SectionMapping?> FindPlaceAsync(string x,CancellationToken t=default)=>Task.FromResult<SectionMapping?>(null);
        public Task<SectionMapping?> FindSectionAsync(string x,CancellationToken t=default)=>Task.FromResult<SectionMapping?>(null);
        public Task<SectionMapping?> FindLocationAsync(string x,CancellationToken t=default)=>Task.FromResult<SectionMapping?>(null);
    }
}
