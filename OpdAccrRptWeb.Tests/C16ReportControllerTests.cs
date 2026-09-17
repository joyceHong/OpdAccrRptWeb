using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C16ReportControllerTests
{
    [Fact]
    public void C16_IsRoutedConfiguredAndRegistered()
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..",".."));
        Assert.Contains("C16: window.ReportComponents.ReportTemplate",File.ReadAllText(Path.Combine(root,"wwwroot","js","report-app.js")));
        string template=File.ReadAllText(Path.Combine(root,"wwwroot","js","reports","report-template.js"));
        Assert.Contains("c16: true",template); Assert.Contains("/Report/C16/Preview",template);
        string program=File.ReadAllText(Path.Combine(root,"Program.cs"));
        Assert.Contains("AddScoped<IC16ReportRepository, C16ReportRepository>()",program);
        Assert.Contains("AddScoped<IC16ReportService, C16ReportService>()",program);
    }

    [Fact]
    public void GetReportData_ValidC16ReturnsPagedDataAndNoStore()
    {
        var service=new FakeService(); var controller=Create(service);
        IActionResult action=controller.GetReportData(Condition());
        Assert.IsType<OkObjectResult>(action); Assert.Equal(1,service.Calls);
        Assert.Equal("no-store, private",controller.Response.Headers.CacheControl);
    }

    [Theory]
    [InlineData(null,"All","AccountingDate")]
    [InlineData("Bad","All","AccountingDate")]
    [InlineData("OutpatientEmergency","Bad","AccountingDate")]
    [InlineData("OutpatientEmergency","All","Bad")]
    public void GetReportData_InvalidClosedValueDoesNotDispatch(string? source,string type,string basis)
    {
        var service=new FakeService(); var condition=Condition(); condition.Source=source;condition.ReportType=type;condition.DateMode=basis;
        Assert.IsType<BadRequestObjectResult>(Create(service).GetReportData(condition)); Assert.Equal(0,service.Calls);
    }

    [Fact]
    public void Preview_ReturnsPartialAndAllSixTitlesAreExact()
    {
        var service=new FakeService{Rows=[new C16ReportRow{PatientName="P"}]}; var controller=Create(service);
        var action=Assert.IsType<PartialViewResult>(controller.PreviewC16(Condition()));
        Assert.Equal("_C16MedicalSubsidyPreview",action.ViewName);
        Assert.Equal("no-store, private",controller.Response.Headers.CacheControl);
        Assert.Contains("表1.新北市兒童",ReportController.C16Title(C16Source.OutpatientEmergency,C16ReportType.Child));
        Assert.Contains("新希望",ReportController.C16Title(C16Source.OutpatientEmergency,C16ReportType.NewHope));
        Assert.Equal("表1.新北市醫療補助費用申請總表(門、急診)",ReportController.C16Title(C16Source.OutpatientEmergency,C16ReportType.All));
        Assert.Contains("表2.新北市兒童",ReportController.C16Title(C16Source.Inpatient,C16ReportType.Child));
        Assert.Contains("新希望",ReportController.C16Title(C16Source.Inpatient,C16ReportType.NewHope));
        Assert.Equal("表2.新北市醫療補助費用申請總表(住院)",ReportController.C16Title(C16Source.Inpatient,C16ReportType.All));
    }

    [Fact]
    public void UnexpectedFailure_DoesNotReturnOrLogPatientSentinel()
    {
        const string sentinel="PATIENT-NAME PID SQL :StartDate secret";
        var logger=new CapturingLogger<ReportController>();
        var controller=new ReportController(new FakeReportCatalogService(),new EmptyReportService(),new FakeReportExportService(),logger,
            c16ReportService:new FakeService{Failure=new InvalidOperationException(sentinel)})
            {ControllerContext=new(){HttpContext=new DefaultHttpContext{TraceIdentifier="safe-trace"}}};
        var action=Assert.IsType<ObjectResult>(controller.GetReportData(Condition()));
        var problem=Assert.IsType<ProblemDetails>(action.Value);
        Assert.Equal(500,action.StatusCode);
        Assert.DoesNotContain(sentinel,problem.Title);
        Assert.DoesNotContain(sentinel,string.Join(" ",logger.Entries.Select(entry=>entry.Message)));
    }

    private static SearchReportCondition Condition()=>new(){ReportCode="C16",StartDate="2026-09-01",EndDate="2026-09-16",Source="OutpatientEmergency",ReportType="All",DateMode="AccountingDate",PageNumber=1,PageSize=10};
    private static ReportController Create(IC16ReportService c16)=>new(new FakeReportCatalogService(),new EmptyReportService(),new FakeReportExportService(),new CapturingLogger<ReportController>(),c16ReportService:c16){ControllerContext=new(){HttpContext=new DefaultHttpContext{TraceIdentifier="c16-trace"}}};
    private sealed class EmptyReportService:IReportService{public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition c)=>new();}
    private sealed class FakeService:IC16ReportService
    {
        public int Calls; public IReadOnlyList<C16ReportRow> Rows {get;init;}=[]; public Exception? Failure {get;init;}
        public Task<C16ReportResult> QueryAsync(C16PreviewRequest request,CancellationToken token=default){Calls++;if(Failure is not null)throw Failure;return Task.FromResult(new C16ReportResult(request,Rows,new(){Columns=[],Data=[],TotalCount=Rows.Count,PageNumber=1,PageSize=10,TotalPages=Rows.Count==0?0:1}));}
    }
}
