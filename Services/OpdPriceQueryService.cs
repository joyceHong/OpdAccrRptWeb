using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class OpdPriceQueryService(IOpdPriceQueryRepository repository,
    IOrganizationUnitCodeService organizationUnits,IReportTotalCountCache totals,
    IOpdPriceTokenService tokens,IOpdPriceReceiptRenderer receipts) : IOpdPriceQueryService
{
    public async Task<OpdPriceVisitPage> QueryVisitsAsync(OpdPriceVisitRequest request,string actor,CancellationToken token)
    {
        string mrNo=(request.MedicalRecordNo??string.Empty).Trim().ToUpperInvariant();
        if(mrNo.Length==0||request.VisitDate is null||request.VisitDate.Value.Year<1912)
            throw new ArgumentException("請輸入有效的病歷號與就診日期。");
        if(request.PageNumber<1||request.PageSize is not(10 or 30 or 50))throw new ArgumentException("分頁條件不正確。");
        string roc=ToRocDate(request.VisitDate.Value); string? legacy=null;
        if(!string.IsNullOrWhiteSpace(request.SectionCode))
        { var mapping=await organizationUnits.ResolveLegacyCodeAsync(request.SectionCode,false,token);
          legacy=mapping?.LegacyCode??throw new ArgumentException("查無可使用的科別代碼。"); }
        var filters=new Dictionary<string,string?>{{"mr",mrNo},{"date",roc},{"section",legacy}};
        int count=totals.GetOrCreate("Q1",filters,()=>repository.CountVisits(mrNo,roc,legacy));
        int pages=(int)Math.Ceiling(count/(double)request.PageSize); int page=Math.Min(request.PageNumber,Math.Max(1,pages));
        IReadOnlyList<OpdPriceVisitSource> source=count==0?[]:await repository.QueryVisitsAsync(mrNo,roc,legacy,(page-1)*request.PageSize,request.PageSize,token);
        var rows=new List<OpdPriceVisitRow>(source.Count);
        foreach(var value in source)
        { var map=await organizationUnits.ResolveNewCodeAsync(value.SectionCode,value.Key.Room=="0000"?"E":string.Empty,OrganizationUnitMappingScope.SectionOnly,token);
          rows.Add(new(value.Key.VisitDate,value.Key.VisitTime,value.Key.Room,value.Key.RegistrationNo,map?.NewCode??value.SectionCode,
            value.InsuranceSequence,value.IsCancelled?"退掛":string.Empty,tokens.ProtectVisit(value.Key,actor))); }
        return new(rows,count,page,request.PageSize,pages);
    }

    public async Task<OpdPriceDetail> QueryDetailAsync(OpdPriceDetailRequest request,string actor,CancellationToken token)
    {
        if(!tokens.TryReadVisit(request.VisitToken??string.Empty,actor,out OpdPriceVisitKey key))throw new KeyNotFoundException();
        OpdPriceVisitSource visit=await repository.QueryVisitAsync(key,token)??throw new KeyNotFoundException();
        if(!string.Equals(visit.Key.MedicalRecordNo,key.MedicalRecordNo,StringComparison.Ordinal))throw new KeyNotFoundException();
        Task<OpdPricePatient?> patientTask=repository.QueryPatientAsync(key.MedicalRecordNo,token);
        Task<IReadOnlyList<OpdPriceChargeSource>> drugTask=repository.QueryDrugsAsync(key,request.ShowDc,token);
        Task<IReadOnlyList<OpdPriceChargeSource>> orderTask=repository.QueryOrdersAsync(key,request.ShowDc,token);
        Task<IReadOnlyList<OpdPriceReceiptSource>> receiptTask=repository.QueryReceiptsAsync(key,request.ShowDc,token);
        await Task.WhenAll(patientTask,drugTask,orderTask,receiptTask);
        OpdPricePatient patient=await patientTask??new(key.MedicalRecordNo,visit.PatientName,string.Empty,string.Empty);
        var charges=(await drugTask).Concat(await orderTask).Select(x=>MapCharge(x,request.ShowExtendedCode)).ToArray();
        var receiptRows=(await receiptTask).Select(x=>new OpdPriceReceiptRow(x.ReceiptNo,x.CheckoutDate,x.CheckoutUser,
            x.Receivable,x.Cash,x.Check,x.Card,x.OnAccount,x.SocialService,x.DcDate,x.DcUser,
            x.Key.RegistrationNo>0&&x.Key.ReceiptSequence>=0?tokens.ProtectReceipt(x.Key,actor):null)).ToArray();
        var section=await organizationUnits.ResolveNewCodeAsync(visit.SectionCode,key.Room=="0000"?"E":string.Empty,OrganizationUnitMappingScope.SectionOnly,token);
        var encounter=new OpdPriceEncounter(key.VisitDate,key.VisitTime,key.Room,key.RegistrationNo,section?.NewCode??visit.SectionCode,
            visit.DoctorName,string.Empty,string.Empty,visit.InsuranceSequence,string.Empty,[]);
        return new(patient,encounter,charges,receiptRows);
    }

    public async Task<IReadOnlyList<OpdPriceSectionOption>> SearchSectionsAsync(string query,CancellationToken token)=>(await organizationUnits.SearchAsync(query,true,false,false,20,token))
        .Select(x=>new OpdPriceSectionOption(x.NewCode,x.DisplayName)).ToArray();
    public Task<OpdReceiptPreview> CreateReceiptAsync(string receiptToken,string actor,CancellationToken token)
    { if(!tokens.TryReadReceipt(receiptToken,actor,out OpdPriceReceiptKey key))throw new KeyNotFoundException();return receipts.RenderAsync(key,token); }
    internal static string ToRocDate(DateOnly date)=>$"{date.Year-1911:000}{date:MMdd}";
    internal static OpdPriceChargeRow MapCharge(OpdPriceChargeSource x,bool showExtended)
    { bool self=x.SelfPayCode is "0" or "4"; string category=x.IsDrug?"1藥":x.Status.Length==2&&x.Status[0]=='9'&&x.Status[1]>='A'&&x.Status[1]<='Z'?"2固":"2醫";
      return new(category,!x.IsDrug&&showExtended&&!string.IsNullOrWhiteSpace(x.ExtendedCode)?x.ExtendedCode:x.Code,x.Name,x.Quantity,x.DaysOrPercent,
        self?"自":x.SelfPayCode=="1"?"健":string.Empty,self?x.SelfPayPrice:x.InsurancePrice,self?x.SelfPayAmount:x.InsuranceAmount,
        x.Status,x.InputUser,x.PriceUser,x.DeleteUser,x.SubAmounts,x.PrescriptionDate,x.ReceiptSequence); }
}
