using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class OpdPriceQueryTests
{
    [Fact]
    public void MedicalRecordParameter_MatchesLegacyCharColumn()
    {
        using var command = new Oracle.ManagedDataAccess.Client.OracleCommand();
        OpdPriceQueryRepository.AddMedicalRecordNumber(command, "1908897");
        var parameter = command.Parameters["MrNo"];
        Assert.Equal(Oracle.ManagedDataAccess.Client.OracleDbType.Char, parameter.OracleDbType);
        Assert.Equal(10, parameter.Size);
        Assert.Equal("1908897   ", parameter.Value);
    }

    [Fact]
    public void VisitPaging_DoesNotUseOracleOffsetKeywordAsBindName()
    {
        Assert.Contains(":RowOffset", OpdPriceQuerySql.Visits);
        Assert.DoesNotContain(":Offset", OpdPriceQuerySql.Visits);
    }

    [Fact]
    public void VisitPaging_SeparatesSelectListFromFromClause()
    {
        Assert.DoesNotContain("RowNoFROM", OpdPriceQuerySql.Visits);
    }

    [Fact]
    public void VisitQueries_UseDbtest3InsuranceSequenceColumn()
    {
        string sql = OpdPriceQuerySql.Visits + OpdPriceQuerySql.Visit + OpdPriceQuerySql.ReceiptHeader;
        Assert.Contains("chOp1InSeq", sql);
        Assert.DoesNotContain("chOp1HinSeq", sql);
    }
    [Fact] public void Catalog_ContainsThreeDataQueries()
    { var query=new ReportCatalogService().GetReportIndex().Categories.Single(x=>x.Key=="query");Assert.Equal(["Q1","Q2","Q3"],query.Groups.Single().Reports.Select(x=>x.Code)); }

    [Fact] public void Models_SerializeWithWebContractNames()
    { string json=JsonSerializer.Serialize(new OpdPriceVisitPage([],0,1,10,0),new JsonSerializerOptions(JsonSerializerDefaults.Web));Assert.Contains("\"rows\"",json);Assert.Contains("\"totalCount\":0",json);Assert.Contains("\"pageSize\":10",json); }

    [Fact] public void Sql_PreservesParameterizedLegacyBranches()
    { Assert.Contains(":ApplyDate = 0 OR B.chOp1Date LIKE :VisitDate",OpdPriceQuerySql.Visits);Assert.Contains(":LegacySection",OpdPriceQuerySql.Visits);Assert.Contains("ROW_NUMBER() OVER (ORDER BY B.chOp1Date DESC, B.chOp1Time",OpdPriceQuerySql.Visits);Assert.Contains(":ShowDc=1",OpdPriceQuerySql.Drugs);Assert.Contains("chOp3Stat='DC'",OpdPriceQuerySql.Drugs);Assert.Contains("chOp4Stat='PR'",OpdPriceQuerySql.Orders);Assert.Contains("chop4proj NOT IN ('I','S')",OpdPriceQuerySql.Orders);Assert.Contains("LNNVL(R.chOp2Stat='D')",OpdPriceQuerySql.Receipts);Assert.Contains("R.chSeqNo,R.chOp2CDate,R.chOp2CUser",OpdPriceQuerySql.Receipts);Assert.Contains("GROUP BY A.chOp3Dct",OpdPriceQuerySql.ReceiptCharges);Assert.Contains("GROUP BY B.chOp4Dct",OpdPriceQuerySql.ReceiptCharges);Assert.DoesNotContain("Receipt.mdb",OpdPriceQuerySql.ReceiptHeader); }

    [Fact] public void Token_IsOpaqueActorBoundAndTypeBound()
    { using var memory=new MemoryCache(new MemoryCacheOptions());var service=new OpdPriceTokenService(memory);var key=new OpdPriceVisitKey("1150930","1","0101",2,"MR1");string token=service.ProtectVisit(key,"A");Assert.DoesNotContain("MR1",token);Assert.True(service.TryReadVisit(token,"A",out var actual));Assert.Equal(key,actual);Assert.False(service.TryReadVisit(token,"B",out _));Assert.False(service.TryReadReceipt(token,"A",out _)); }

    [Theory]
    [InlineData(0,1000,100,200,700,100,0)]
    [InlineData(0,1000,100,200,300,0,500)]
    [InlineData(250,1000,100,200,300,250,250)]
    public void ReceiptFormula_MatchesExamples(decimal sub169,decimal amt2,decimal sub1,decimal sub3,decimal sub5,decimal collected,decimal balance)
    { Assert.Equal((collected,balance),OpdPriceReceiptRenderer.CalculateCollected(sub169,amt2,sub1,sub3,sub5)); }

    [Fact] public void ChineseDigits_PadsAndSupportsNegative()
    { Assert.Equal("△△△壹貳參肆",OpdPriceReceiptRenderer.ToChineseDigits(1234));Assert.Contains("負",OpdPriceReceiptRenderer.ToChineseDigits(-12)); }

    [Fact] public async Task Service_NormalizesDateSectionAndCachesCount()
    {
        var repo=new FakeRepository();using var memory=new MemoryCache(new MemoryCacheOptions());
        var service=new OpdPriceQueryService(repo,new FakeUnits(),new ReportTotalCountCache(memory),
            new OpdPriceTokenService(memory),new FakeRenderer());
        var request=new OpdPriceVisitRequest(" ab123 ",new DateOnly(2026,9,30),"11910",PageSize:10);
        OpdPriceVisitPage first=await service.QueryVisitsAsync(request,"actor",default);
        await service.QueryVisitsAsync(request,"actor",default);
        Assert.Equal("AB123",repo.MedicalRecordNo);Assert.Equal("1150930",repo.RocDate);
        Assert.Equal("0201",repo.LegacySection);Assert.Equal(1,repo.CountCalls);Assert.Single(first.Rows);
        Assert.Equal("11910",first.Rows[0].SectionCode);
    }

    [Fact] public async Task Service_RejectsInvalidPagingBeforeRepository()
    { using var memory=new MemoryCache(new MemoryCacheOptions());var repo=new FakeRepository();var service=new OpdPriceQueryService(repo,new FakeUnits(),new ReportTotalCountCache(memory),new OpdPriceTokenService(memory),new FakeRenderer());await Assert.ThrowsAsync<ArgumentException>(()=>service.QueryVisitsAsync(new("MR",new DateOnly(2026,9,30),null,PageNumber:0),"actor",default));Assert.Equal(0,repo.CountCalls); }

    [Fact] public async Task Service_AllowsEmptyDateAndQueriesAllVisits()
    {
        var repo=new FakeRepository();using var memory=new MemoryCache(new MemoryCacheOptions());
        var service=new OpdPriceQueryService(repo,new FakeUnits(),new ReportTotalCountCache(memory),new OpdPriceTokenService(memory),new FakeRenderer());
        OpdPriceVisitPage result=await service.QueryVisitsAsync(new(" ab123 ",null,null),"actor",default);
        Assert.Equal("AB123",repo.MedicalRecordNo);Assert.Equal(string.Empty,repo.RocDate);Assert.Single(result.Rows);
    }

    [Fact] public async Task Service_RejectsEmptyMedicalRecordNumber()
    {
        var repo=new FakeRepository();using var memory=new MemoryCache(new MemoryCacheOptions());
        var service=new OpdPriceQueryService(repo,new FakeUnits(),new ReportTotalCountCache(memory),new OpdPriceTokenService(memory),new FakeRenderer());
        await Assert.ThrowsAsync<ArgumentException>(()=>service.QueryVisitsAsync(new(" ",null,null),"actor",default));
        Assert.Equal(0,repo.CountCalls);
    }

    [Fact] public async Task Detail_MapsDrugThenOrderAndProtectsReceipt()
    {
        using var memory=new MemoryCache(new MemoryCacheOptions());var tokenService=new OpdPriceTokenService(memory);
        var key=new OpdPriceVisitKey("1150930","1","0101",1,"MR1");var repo=new FakeRepository{VisitResult=new(key,"0201","H1",false,"醫師","病患","0800101","A1","D1","01","02","P",["I1","I2","I3","I4","I5","I6"])};
        repo.Drugs=[Charge(true,"D1","", "1","藥品")];repo.Orders=[Charge(false,"O1","EXT1","9A","醫令")];
        repo.Receipts=[new(new(key.VisitDate,key.VisitTime,key.Room,key.RegistrationNo,0,key.MedicalRecordNo),"R1","1150930","U",100,100,0,0,0,0,"","","")];
        IConfiguration femh=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Hospital:Code","FEMH"}}).Build();
        var service=new OpdPriceQueryService(repo,new FakeUnits(),new ReportTotalCountCache(memory),tokenService,new FakeRenderer(),femh);
        string token=tokenService.ProtectVisit(key,"actor");OpdPriceDetail detail=await service.QueryDetailAsync(new(token,false,true),"actor",default);
        Assert.Equal(["1藥","2固"],detail.Charges.Select(x=>x.Category));Assert.Equal("EXT1",detail.Charges[1].Code);Assert.NotNull(detail.Receipts[0].ReceiptToken);
        Assert.Equal("0800101",detail.Patient.Birthday);Assert.Equal("A1",detail.Patient.IdentityNumber);
        Assert.Equal("D1醫師",detail.Encounter.DoctorName);Assert.Equal("01:自費",detail.Encounter.IdentityType);
        Assert.Equal("I6",detail.Encounter.Diagnoses[5]);Assert.Equal(100,detail.Charges[0].DaysOrPercent);
    }

    [Fact] public async Task Basic_UsesSelectedVisitWithoutLoadingCharges()
    {
        using var memory=new MemoryCache(new MemoryCacheOptions());var tokens=new OpdPriceTokenService(memory);
        var key=new OpdPriceVisitKey("1150930","1","0101",1,"MR1");
        var repo=new FakeRepository{VisitResult=new(key,"0201","H1",false,"甲醫師","甲病患","0800101","A1")};
        var service=new OpdPriceQueryService(repo,new FakeUnits(),new ReportTotalCountCache(memory),tokens,new FakeRenderer());
        OpdPriceBasic basic=await service.QueryBasicAsync(tokens.ProtectVisit(key,"actor"),"actor",default);
        Assert.Equal("甲病患",basic.Patient.Name);Assert.Equal("0800101",basic.Patient.Birthday);
        Assert.Equal(0,repo.ChargeQueries);
    }

    [Fact] public async Task ReceiptRenderer_StopsAt69AndChunksThreeItems()
    {
        var repo=new FakeRepository{HeaderResult=new("E1","MR","病患","A","1150930","內科","健保","無優待","H","醫師","D","部分負擔")};
        repo.ReceiptAggregates=[new("01",1,2,3,10,20),new("02",1,2,3,10,20),new("03",1,2,3,10,20),new("04",1,2,3,10,20),new("69",250,0,0,0,0),new("70",0,0,0,999,999)];
        repo.Names=new Dictionary<string,string>{{"01","A"},{"02","B"},{"03","C"},{"04","D"}};
        OpdReceiptPreview result=await new OpdPriceReceiptRenderer(repo).RenderAsync(new("1150930","1","0101",1,0,"MR"),default);
        Assert.Equal([3,1],result.ItemRows.Select(x=>x.Count));Assert.Equal(80,result.SelfPay);Assert.Equal(250,result.Collected);Assert.Equal("( 急診 )",result.RoomType);Assert.DoesNotContain(result.ItemRows.SelectMany(x=>x),x=>x.Code=="70");
    }

    [Fact] public async Task ReceiptRenderer_RejectsMissingHeaderOrPrintableRows()
    { await Assert.ThrowsAsync<KeyNotFoundException>(()=>new OpdPriceReceiptRenderer(new FakeRepository()).RenderAsync(new("d","t","r",1,0,"m"),default));var repo=new FakeRepository{HeaderResult=new("R","M","P","I","D","S","F","T","H","N","NO","C"),ReceiptAggregates=[new("01",0,0,0,0,0)]};await Assert.ThrowsAsync<KeyNotFoundException>(()=>new OpdPriceReceiptRenderer(repo).RenderAsync(new("d","t","r",1,0,"m"),default)); }

    [Fact] public async Task ReceiptBatch_KeepsOrderAndReportsPartialAndTotalFailure()
    {
        using var memory=new MemoryCache(new MemoryCacheOptions());var tokenService=new OpdPriceTokenService(memory);
        var visit=new OpdPriceVisitKey("1150930","1","0101",7,"MR1");
        var first=new OpdPriceReceiptKey(visit.VisitDate,visit.VisitTime,visit.Room,visit.RegistrationNo,1,visit.MedicalRecordNo);
        var second=first with { ReceiptSequence=2 };
        var repo=new FakeRepository{Receipts=[Receipt(first,"A001",""),Receipt(second,"A002","")]};
        var renderer=new FakeRenderer{Render=key=>new(new(key.ReceiptSequence==1?"A001":"A002","MR1","P","I","D","S","F","T","H","N","NO","C"),[],0,0,0,0,0,key.ReceiptSequence==1?100:250,0,"","")};
        var service=new OpdPriceQueryService(repo,new FakeUnits(),new ReportTotalCountCache(memory),tokenService,renderer);
        var request=new OpdPriceReceiptBatchRequest(tokenService.ProtectVisit(visit,"actor"),
            [tokenService.ProtectReceipt(second,"actor"),tokenService.ProtectReceipt(first,"actor")]);
        var success=await service.CreateReceiptBatchAsync(request,"actor",default);
        Assert.Equal(["A002","A001"],success.Receipts.Select(x=>x.Header.ReceiptNo));
        Assert.Equal([250,100],success.Receipts.Select(x=>x.Collected));Assert.Equal(2,success.ReadyCount);
        repo.Receipts=[Receipt(first,"A001","D"),Receipt(second,"A002","")];
        var partial=await service.CreateReceiptBatchAsync(request,"actor",default);
        Assert.Single(partial.Receipts);Assert.Equal("A002",partial.Receipts[0].Header.ReceiptNo);
        Assert.Equal(1,partial.ReadyCount);Assert.Equal(1,partial.FailedCount);Assert.Equal("A001",partial.Results[1].ReceiptNo);
        repo.Receipts=[Receipt(first,"A001","D"),Receipt(second,"A002","D")];
        var failed=await service.CreateReceiptBatchAsync(request,"actor",default);
        Assert.Empty(failed.Receipts);Assert.Equal(2,failed.FailedCount);
        repo.Receipts=[Receipt(first,"A001",""),Receipt(second,"A002","")];
        var duplicate=await service.CreateReceiptBatchAsync(new(request.VisitToken,
            [tokenService.ProtectReceipt(first,"actor"),tokenService.ProtectReceipt(first,"actor")]),"actor",default);
        Assert.Single(duplicate.Receipts);Assert.Equal(1,duplicate.FailedCount);
        var otherVisit=first with { Room="9999" };
        var wrongVisit=await service.CreateReceiptBatchAsync(new(request.VisitToken,[tokenService.ProtectReceipt(otherVisit,"actor")]),"actor",default);
        Assert.Empty(wrongVisit.Receipts);Assert.Equal(1,wrongVisit.FailedCount);
        await Assert.ThrowsAsync<KeyNotFoundException>(()=>service.CreateReceiptBatchAsync(request,"another-actor",default));
    }

    [Fact] public void DependencyGraph_ResolvesScopedQ1Services()
    { var services=new ServiceCollection();services.AddMemoryCache();services.AddSingleton<IReportTotalCountCache,ReportTotalCountCache>();services.AddSingleton<IOpdPriceTokenService,OpdPriceTokenService>();services.AddSingleton<IOrganizationUnitCodeService,FakeUnits>();services.AddScoped<IOpdPriceQueryRepository,FakeRepository>();services.AddScoped<IOpdPriceReceiptRenderer,OpdPriceReceiptRenderer>();services.AddScoped<IOpdPriceQueryService,OpdPriceQueryService>();using ServiceProvider provider=services.BuildServiceProvider(new ServiceProviderOptions{ValidateOnBuild=true,ValidateScopes=true});using IServiceScope scope=provider.CreateScope();Assert.IsType<OpdPriceQueryService>(scope.ServiceProvider.GetRequiredService<IOpdPriceQueryService>()); }

    private static OpdPriceChargeSource Charge(bool drug,string code,string ext,string status,string name)=>new(drug,code,ext,name,2,drug?3:100,"1",5,7,10,14,status,"","I","P","D",[1,2,3,4,5,6],"1150930","0");
    private static OpdPriceReceiptSource Receipt(OpdPriceReceiptKey key,string no,string status)=>new(key,no,"1150930","U",100,100,0,0,0,0,"","",status);

    private sealed class FakeUnits : IOrganizationUnitCodeService
    {
        public Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(string code,bool active,CancellationToken ct=default)=>Task.FromResult<OrganizationUnitMapping?>(new(OrganizationUnitSource.Section,"0201",code,"內科",true));
        public Task<OrganizationUnitMapping?> ResolveNewCodeAsync(string code,string room,OrganizationUnitMappingScope scope,CancellationToken ct=default)=>Task.FromResult<OrganizationUnitMapping?>(new(OrganizationUnitSource.Section,code,"11910","內科",true));
        public Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(string query,bool sections,bool places,bool active,int limit=20,CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<OrganizationUnitMapping>>([]);
    }
    private sealed class FakeRenderer : IOpdPriceReceiptRenderer
    { public Func<OpdPriceReceiptKey,OpdReceiptPreview>? Render;public Task<OpdReceiptPreview> RenderAsync(OpdPriceReceiptKey key,CancellationToken token)=>Render is null?throw new NotSupportedException():Task.FromResult(Render(key)); }
    private sealed class FakeRepository : IOpdPriceQueryRepository
    {
        public int CountCalls;public int ChargeQueries;public string? MedicalRecordNo;public string? RocDate;public string? LegacySection;
        public OpdPriceVisitSource? VisitResult;public IReadOnlyList<OpdPriceChargeSource> Drugs=[];public IReadOnlyList<OpdPriceChargeSource> Orders=[];public IReadOnlyList<OpdPriceReceiptSource> Receipts=[];public OpdReceiptHeader? HeaderResult;public IReadOnlyList<OpdReceiptChargeAggregate> ReceiptAggregates=[];public IReadOnlyDictionary<string,string> Names=new Dictionary<string,string>();
        public int CountVisits(string mr,string date,string? section){CountCalls++;MedicalRecordNo=mr;RocDate=date;LegacySection=section;return 1;}
        public Task<IReadOnlyList<OpdPriceVisitSource>> QueryVisitsAsync(string mr,string date,string? section,int offset,int size,CancellationToken ct)=>Task.FromResult<IReadOnlyList<OpdPriceVisitSource>>([new(new(date,"1","0101",1,mr),"0201","H1",false,"醫師","病患")]);
        public Task<OpdPriceVisitSource?> QueryVisitAsync(OpdPriceVisitKey key,CancellationToken ct)=>Task.FromResult(VisitResult);
        public Task<IReadOnlyList<OpdPriceChargeSource>> QueryDrugsAsync(OpdPriceVisitKey key,bool show,CancellationToken ct){ChargeQueries++;return Task.FromResult(Drugs);}
        public Task<IReadOnlyList<OpdPriceChargeSource>> QueryOrdersAsync(OpdPriceVisitKey key,bool show,CancellationToken ct){ChargeQueries++;return Task.FromResult(Orders);}
        public Task<IReadOnlyList<OpdPriceReceiptSource>> QueryReceiptsAsync(OpdPriceVisitKey key,bool show,CancellationToken ct)=>Task.FromResult(Receipts);
        public Task<OpdReceiptHeader?> QueryReceiptHeaderAsync(OpdPriceReceiptKey key,CancellationToken ct)=>Task.FromResult(HeaderResult);
        public Task<IReadOnlyList<OpdReceiptChargeAggregate>> QueryReceiptChargesAsync(OpdPriceReceiptKey key,CancellationToken ct)=>Task.FromResult(ReceiptAggregates);
        public Task<IReadOnlyDictionary<string,string>> QueryChargeNamesAsync(IEnumerable<string> codes,CancellationToken ct)=>Task.FromResult(Names);
    }
}
