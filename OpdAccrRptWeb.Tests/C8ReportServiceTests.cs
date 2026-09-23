using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C8ReportServiceTests
{
    [Fact] public async Task Query_UsesOneRangeCallAndMapsLegacyValues()
    {
        var repo=new Repo(); var service=new C8ReportService(repo,new NoCache());
        var result=await service.QueryAsync(new(new(2026,9,20),new(2026,9,22)),"user-a");
        Assert.Equal(1,repo.QueryCalls); Assert.Equal(("1150920","1150922"),repo.Range);
        Assert.Equal(4,result.AllRows.Count); Assert.Equal("11910",result.AllRows[0].SectionCode);
        Assert.Equal("11930",result.AllRows[1].SectionCode); Assert.Equal("12000",result.AllRows[2].SectionCode);
        Assert.Equal(string.Empty,result.AllRows[3].SectionCode); Assert.Equal(string.Empty,result.AllRows[0].MedicalRecordNo);
        Assert.Equal(0m,result.AllRows[0].SelfPayAmount); Assert.Equal(-2.5m,result.AllRows[0].InsuranceAmount);
    }
    [Fact] public async Task Cache_IsolatesActorsAndReusesSameActorQuery()
    {
        var repo=new Repo(); using var cache=new C8ReportResultCache(); var service=new C8ReportService(repo,cache); var request=new C8ReportRequest(new(2026,1,1),new(2026,1,1));
        await service.QueryAsync(request,"A"); await service.QueryAsync(request with{PageSize=30},"A"); await service.QueryAsync(request,"B");
        Assert.Equal(2,repo.QueryCalls);
    }
    private sealed class Repo:IC8ReportRepository
    {
        public int QueryCalls; public (string,string) Range;
        public Task<IReadOnlyList<C8SourceRow>> QueryAsync(string s,string e,CancellationToken t=default){QueryCalls++;Range=(s,e);return Task.FromResult<IReadOnlyList<C8SourceRow>>([
            new("E"," 1150920 ","0201",null," 01 "," A1 "," Name ",1.25m,null,2m,-2.5m,null," U1 "),
            new("E","1150920","0221","M2","02","A2","N2",1,2,3,4,5,"U2"),
            new("R","1150921","0201","M3","03","A3","N3",1,2,3,4,5,"U3"),
            new("R","1150922","9999","M4","04","A4","N4",1,2,3,4,5,"U4")]);}
        public Task<IReadOnlyDictionary<string,string>> GetSectionMappingsAsync(IReadOnlyCollection<string> c,CancellationToken t=default)=>Task.FromResult<IReadOnlyDictionary<string,string>>(new Dictionary<string,string>{{"0201","12000"}});
    }
    private sealed class NoCache:IC8ReportResultCache{public bool TryGet(string a,C8ValidatedRequest q,out C8CachedResult r){r=null!;return false;}public void Set(string a,C8ValidatedRequest q,C8CachedResult r){}}
}
