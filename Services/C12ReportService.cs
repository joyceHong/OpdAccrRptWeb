using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C12ReportService(IC12ReportRepository repository, IC12LegacyAmountConverter converter,
    IC12PatientAccessAuthorizer authorizer, IC12PatientAccessAuditWriter auditWriter) : IC12ReportService
{
    public async Task<C12MedicalReceiptSummaryViewModel> CreateAsync(C12ReportRequest request,string userId,CancellationToken ct=default)
    {
        if (!await authorizer.AuthorizeAsync(userId,ct)) { await auditWriter.WriteAsync(userId,C12AuditOutcome.Denied,ct); throw new C12AccessDeniedException(); }
        string? oldSection=request.OldSectionCode;
        if (!string.IsNullOrWhiteSpace(request.NewSectionCode)) oldSection=await repository.ResolveOldSectionCodeAsync(request.NewSectionCode,ct);
        string input=request.PatientIdentity.Trim().ToUpperInvariant();
        string mrNo=await repository.ResolveMedicalRecordNoAsync(input,ct) ?? input;
        C12ReportRequest normalized=request with { PatientIdentity=input,OldSectionCode=oldSection };
        IReadOnlyList<C12VisitRow> visits=await repository.QueryVisitsAsync(normalized,mrNo,ct);
        if(visits.Count==0)
        {
            await auditWriter.WriteAsync(userId,C12AuditOutcome.NoData,ct);
            return Empty(normalized,mrNo);
        }
        C12PatientRow patient=await repository.QueryPatientAsync(mrNo,ct) ?? new(mrNo,string.Empty,string.Empty);
        IReadOnlyDictionary<string,string?> sectionNames=await repository.QuerySectionNamesAsync(visits.Select(x=>x.SectionCode),ct);
        var visitModels=new List<C12VisitModel>(visits.Count); var warnings=new List<string>();
        foreach(C12VisitRow visit in visits)
        {
            IReadOnlyList<C12ChargeRow> charges=await repository.QueryVisitChargesAsync(request.Source,visit.Key,ct);
            var items=charges.Select((row,index)=>new C12ReportItem(index+1,row.Name.Trim(),converter.Convert(row.InsuranceAmount),converter.Convert(row.RawSelfPayAmount),converter.Convert(row.DiscountOrOnAccountAmount))).ToList();
            sectionNames.TryGetValue(visit.SectionCode.Trim(),out string? sectionName);
            visitModels.Add(new(visit.Key,visit.SectionCode,sectionName??string.Empty,visit.DoctorName,visit.IsCurrentlyInpatient,items));
            if(visit.IsCurrentlyInpatient) warnings.Add($"病歷號：{mrNo}\n住院日：{visit.Key.Date}\n此病患仍住院中，\n報表呈現之數據為「應收」醫療費用!!");
        }
        var model=new C12ReportModel(patient,Criteria(normalized),warnings,visitModels);
        C12MedicalReceiptSummaryViewModel output=Project(model);
        if(output.DetailTotals!=output.SummaryTotals) throw new C12CompatibilityException("C12 明細與彙總總額不一致。");
        await auditWriter.WriteAsync(userId,C12AuditOutcome.Success,ct); return output;
    }

    private static C12MedicalReceiptSummaryViewModel Empty(C12ReportRequest r,string mrNo)=>new(new(mrNo,"",""),Criteria(r),[],[],[],[],new(0,0,0,0),new(0,0,0,0),false);
    private static C12ReportCriteria Criteria(C12ReportRequest r)=>new(r.StartDate,r.EndDate,r.Source,r.Source==C12Source.Inpatient?"住院":r.RoomType switch{1=>"急診",2=>"門診",_=>"門急診"},r.OldSectionCode);
    private static C12MedicalReceiptSummaryViewModel Project(C12ReportModel model)
    {
        List<C12VisitDetail> detail=model.Visits.Select(v=>
        {
            var rows=v.Items.Chunk(3).Select(a=>new C12PackedItemRow(a.ElementAtOrDefault(0),a.ElementAtOrDefault(1),a.ElementAtOrDefault(2))).ToList();
            int insurance=Sum(v.Items,x=>x.InsuranceAmount), raw=Sum(v.Items,x=>x.RawSelfPayAmount), discount=Sum(v.Items,x=>x.DiscountOrOnAccountAmount);
            return new C12VisitDetail(v,rows,insurance,raw,discount,checked(raw-discount),checked(insurance+raw));
        }).ToList();
        List<C12SummaryRow> summary=model.Visits.SelectMany(x=>x.Items).GroupBy(x=>x.Name,StringComparer.Ordinal).OrderBy(x=>x.Key,StringComparer.Ordinal).Select(g=>new C12SummaryRow(g.Key,Sum(g,x=>x.InsuranceAmount),Sum(g,x=>x.RawSelfPayAmount),Sum(g,x=>x.DiscountOrOnAccountAmount))).ToList();
        List<C12DetailRow> detailRows=model.Visits.SelectMany(visit=>visit.Items.Select(item=>new C12DetailRow(
            visit.Key.Date,visit.Key.Time,visit.Key.Room,visit.Key.Number,visit.SectionName,
            visit.DoctorName,item.Sequence,item.Name,item.InsuranceAmount,item.RawSelfPayAmount,
            item.DiscountOrOnAccountAmount,item.ReceivedAmount))).ToList();
        C12ReportTotals dt=new(Sum(detail,x=>x.Total),Sum(detail,x=>x.InsuranceTotal),Sum(detail,x=>x.DiscountTotal),Sum(detail,x=>x.ReceivedTotal));
        C12ReportTotals st=new(Sum(summary,x=>checked(x.InsuranceAmount+x.RawSelfPayAmount)),Sum(summary,x=>x.InsuranceAmount),Sum(summary,x=>x.DiscountOrOnAccountAmount),Sum(summary,x=>checked(x.RawSelfPayAmount-x.DiscountOrOnAccountAmount)));
        return new(model.Patient,model.Criteria,model.Warnings,detail,detailRows,summary,dt,st,true);
    }
    private static int Sum<T>(IEnumerable<T> values,Func<T,int> selector){int total=0;foreach(T value in values) total=checked(total+selector(value));return total;}
}
