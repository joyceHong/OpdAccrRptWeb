using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
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
    private static ReportController Create(IC12ReportService c12){var controller=new ReportController(new FakeReportCatalogService(),new DummyReportService(),new FakeReportExportService(),new CapturingLogger<ReportController>(),c12ReportService:c12);controller.ControllerContext=new(){HttpContext=new DefaultHttpContext()};return controller;}
    private sealed class DummyReportService:IReportService{public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition c)=>new();}
    private sealed class CapturingService:IC12ReportService
    { public C12ReportRequest? Request{get;private set;} public Task<C12MedicalReceiptSummaryViewModel>CreateAsync(C12ReportRequest r,string u,CancellationToken c=default){Request=r;return Task.FromResult(new C12MedicalReceiptSummaryViewModel(new("MR","N","ID"),new(r.StartDate,r.EndDate,r.Source,"門診",null),[],[],[],[],new(0,0,0,0),new(0,0,0,0),true));} }
}
