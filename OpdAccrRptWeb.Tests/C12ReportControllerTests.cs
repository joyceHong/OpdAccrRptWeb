using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
namespace OpdAccrRptWeb.Tests;
public sealed class C12ReportControllerTests
{
    [Theory]
    [InlineData("2026-09-01","2026-09-14","OpdEr","Outpatient"," ab12 ",true)]
    [InlineData("bad","2026-09-14","OpdEr","All","AB12",false)]
    [InlineData("2026-09-15","2026-09-14","OpdEr","All","AB12",false)]
    [InlineData("2026-09-01","2026-09-14","Inpatient","Emergency","AB12",false)]
    [InlineData("2026-09-01","2026-09-14","OpdEr","All"," ",false)]
    public void Validation_NormalizesAndDispatches(string start,string end,string source,string scope,string identity,bool accepted)
    {
        var c12=new CapturingService(); IActionResult result=Create(c12).GetReportData(new(){ReportCode="C12",StartDate=start,EndDate=end,Source=source,RoomScope=scope,MedicalRecordNo=identity});
        Assert.Equal(accepted,c12.Request is not null); if(accepted){Assert.IsType<OkObjectResult>(result);Assert.Equal("1150901",c12.Request!.StartDate);Assert.Equal("1150914",c12.Request.EndDate);Assert.Equal(2,c12.Request.RoomType);Assert.Equal("AB12",c12.Request.PatientIdentity);}else Assert.IsType<BadRequestObjectResult>(result);
    }
    [Fact] public void Validation_NormalizesNewSectionWithoutOldSection()
    { var c12=new CapturingService();IActionResult result=Create(c12).GetReportData(new(){ReportCode="C12",StartDate="2026-09-01",EndDate="2026-09-14",Source="OpdEr",RoomScope="All",MedicalRecordNo="AB12",Chop1sec="OLD",NewSectionCode=" 119a "});Assert.IsType<OkObjectResult>(result);Assert.Equal("119A",c12.Request?.NewSectionCode); }
    [Fact] public async Task SectionOptions_ReturnCodeAndName()
    { IActionResult result=await Create(new CapturingService(),new FakeC12Repository()).GetC12SectionOptions(default);var ok=Assert.IsType<OkObjectResult>(result);var options=Assert.IsAssignableFrom<IReadOnlyList<C12SectionOption>>(ok.Value);Assert.Equal(new C12SectionOption("11910","心臟內科"),options.Single()); }
    [Fact] public async Task SectionOptions_FailureReturnsSafeMessage()
    { IActionResult result=await Create(new CapturingService(),new FakeC12Repository(true)).GetC12SectionOptions(default);var unavailable=Assert.IsType<ObjectResult>(result);Assert.Equal(503,unavailable.StatusCode);var problem=Assert.IsType<ProblemDetails>(unavailable.Value);Assert.Equal("無法載入科別清單，仍可直接輸入科別代碼。",problem.Title);Assert.DoesNotContain("test failure",problem.Title); }
    private static ReportController Create(IC12ReportService c12,IC12ReportRepository? repository=null){var controller=new ReportController(new FakeReportCatalogService(),new DummyReportService(),new FakeReportExportService(),new CapturingLogger<ReportController>(),c12ReportService:c12,c12Repository:repository);controller.ControllerContext=new(){HttpContext=new DefaultHttpContext()};return controller;}
    private sealed class DummyReportService:IReportService{public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition c)=>new();}
    private sealed class CapturingService:IC12ReportService
    { public C12ReportRequest? Request{get;private set;} public Task<C12MedicalReceiptSummaryViewModel>CreateAsync(C12ReportRequest r,string u,CancellationToken c=default){Request=r;return Task.FromResult(new C12MedicalReceiptSummaryViewModel(new("MR","N","ID"),new(r.StartDate,r.EndDate,r.Source,"門診",null),[],[],[],[],new(0,0,0,0),new(0,0,0,0),true));} }
    private sealed class FakeC12Repository(bool fail=false):IC12ReportRepository
    { public Task<IReadOnlyList<C12SectionOption>> QuerySectionOptionsAsync(CancellationToken ct)=>fail?Task.FromException<IReadOnlyList<C12SectionOption>>(new InvalidOperationException("test failure")):Task.FromResult<IReadOnlyList<C12SectionOption>>([new("11910","心臟內科")]);public Task<string?> ResolveMedicalRecordNoAsync(string input,CancellationToken ct)=>throw new NotSupportedException();public Task<IReadOnlyList<C12VisitRow>> QueryVisitsAsync(C12ReportRequest request,string mrNo,CancellationToken ct)=>throw new NotSupportedException();public Task<IReadOnlyList<C12ChargeRow>> QueryVisitChargesAsync(C12Source source,C12VisitKey key,CancellationToken ct)=>throw new NotSupportedException();public Task<IReadOnlyDictionary<string,string?>> QuerySectionNamesAsync(IEnumerable<string> sectionNos,CancellationToken ct)=>throw new NotSupportedException();public Task<C12PatientRow?> QueryPatientAsync(string mrNo,CancellationToken ct)=>throw new NotSupportedException(); }
}
