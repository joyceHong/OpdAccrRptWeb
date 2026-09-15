using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
namespace OpdAccrRptWeb.Tests;
public sealed class C12ReportServiceTests
{
    [Theory][InlineData(0,0)][InlineData(1,1)][InlineData(2,1)][InlineData(3,1)][InlineData(4,2)][InlineData(6,2)][InlineData(7,3)]
    public async Task CreateAsync_PacksThreeItemsAndKeepsTotals(int count,int rows)
    {
        var repository=new FakeRepository(count); var audit=new FakeAudit();
        var service=new C12ReportService(repository,new C12LegacyAmountConverter(),new AllowConfiguredC12PatientAccessAuthorizer(),audit);
        C12MedicalReceiptSummaryViewModel result=await service.CreateAsync(Request(),"user");
        Assert.Equal(rows,result.DetailVisits.Single().Rows.Count); Assert.Equal(result.DetailTotals,result.SummaryTotals);
        Assert.Equal(count*3,result.DetailTotals.Insurance); Assert.Equal(count*10,result.DetailTotals.Total);
        Assert.Equal(count,result.DetailRows.Count);
        Assert.Equal(count>0,result.SummaryRows.Count>0);
        Assert.True(result.HasVisits);
        Assert.Equal(C12AuditOutcome.Success,audit.Outcome);
    }
    [Fact] public async Task CreateAsync_NoVisits_AuditsNoData()
    { var audit=new FakeAudit();var result=await new C12ReportService(new FakeRepository(-1),new C12LegacyAmountConverter(),new AllowConfiguredC12PatientAccessAuthorizer(),audit).CreateAsync(Request(),"user");Assert.False(result.HasVisits);Assert.Equal(C12AuditOutcome.NoData,audit.Outcome); }
    [Fact] public async Task CreateAsync_Denied_AuditsAndDoesNotQuery()
    { var repository=new FakeRepository(1);var audit=new FakeAudit();var service=new C12ReportService(repository,new C12LegacyAmountConverter(),new Deny(),audit);await Assert.ThrowsAsync<C12AccessDeniedException>(()=>service.CreateAsync(Request(),"user"));Assert.Equal(C12AuditOutcome.Denied,audit.Outcome);Assert.Equal(0,repository.VisitCalls); }
    [Fact]
    public async Task CreateAsync_ProjectsOneBrowsableRowPerChargeInProviderSequence()
    {
        var service=new C12ReportService(new FakeRepository(28),new C12LegacyAmountConverter(),new AllowConfiguredC12PatientAccessAuthorizer(),new FakeAudit());

        C12MedicalReceiptSummaryViewModel result=await service.CreateAsync(Request(),"user");

        Assert.Equal(28,result.DetailRows.Count);
        Assert.Equal(Enumerable.Range(1,28),result.DetailRows.Select(row=>row.Sequence));
        C12DetailRow first=result.DetailRows[0];
        Assert.Equal("1150901",first.VisitDate);
        Assert.Equal("1",first.VisitTime);
        Assert.Equal("0101",first.Room);
        Assert.Equal(40000m,first.VisitNumber);
        Assert.Equal("內科",first.SectionName);
        Assert.Equal("醫師",first.DoctorName);
        Assert.Equal("項目1",first.ItemName);
        Assert.Equal(3,first.InsuranceAmount);
        Assert.Equal(7,first.RawSelfPayAmount);
        Assert.Equal(2,first.DiscountOrOnAccountAmount);
        Assert.Equal(5,first.ReceivedAmount);
    }
    private static C12ReportRequest Request()=>new("1150901","1150914",C12Source.OutpatientAndEmergency,0,"A123456789",null,null);
    private sealed class FakeAudit:IC12PatientAccessAuditWriter { public C12AuditOutcome? Outcome{get;private set;} public Task WriteAsync(string u,C12AuditOutcome o,CancellationToken c){Outcome=o;return Task.CompletedTask;} }
    private sealed class Deny:IC12PatientAccessAuthorizer { public Task<bool> AuthorizeAsync(string user,CancellationToken ct)=>Task.FromResult(false); }
    private sealed class FakeRepository(int count):IC12ReportRepository
    {
        public int VisitCalls{get;private set;}
        public Task<string?> ResolveOldSectionCodeAsync(string value,CancellationToken ct)=>Task.FromResult<string?>(value);
        public Task<string?> ResolveMedicalRecordNoAsync(string value,CancellationToken ct)=>Task.FromResult<string?>("MR1");
        public Task<IReadOnlyList<C12VisitRow>> QueryVisitsAsync(C12ReportRequest r,string m,CancellationToken ct){VisitCalls++;return Task.FromResult<IReadOnlyList<C12VisitRow>>(count<0?[]:[new(new("1150901","1","0101",40000m),"01","醫師","MR1",false)]);}
        public Task<IReadOnlyList<C12ChargeRow>> QueryVisitChargesAsync(C12Source s,C12VisitKey k,CancellationToken ct)=>Task.FromResult<IReadOnlyList<C12ChargeRow>>(Enumerable.Range(1,count).Select(i=>new C12ChargeRow($"項目{i}",3m,7m,2m)).ToList());
        public Task<IReadOnlyDictionary<string,string?>> QuerySectionNamesAsync(IEnumerable<string> values,CancellationToken ct)=>Task.FromResult<IReadOnlyDictionary<string,string?>>(new Dictionary<string,string?>{{"01","內科"}});
        public Task<C12PatientRow?> QueryPatientAsync(string m,CancellationToken ct)=>Task.FromResult<C12PatientRow?>(new(m,"病患","A123456789"));
    }
}
