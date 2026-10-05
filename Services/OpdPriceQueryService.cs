using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class OpdPriceQueryService(IOpdPriceQueryRepository repository,
    IOrganizationUnitCodeService organizationUnits,IReportTotalCountCache totals,
    IOpdPriceTokenService tokens,IOpdPriceReceiptRenderer receipts,
    IConfiguration? configuration = null, ILogger<OpdPriceQueryService>? logger = null) : IOpdPriceQueryService
{
    private bool IsFemh => string.Equals(configuration?["Hospital:Code"],"FEMH",StringComparison.OrdinalIgnoreCase);
    public async Task<OpdPriceVisitPage> QueryVisitsAsync(OpdPriceVisitRequest request,string actor,CancellationToken token)
    {
        string mrNo=(request.MedicalRecordNo??string.Empty).Trim().ToUpperInvariant();
        if(mrNo.Length==0)throw new ArgumentException("請輸入病歷號。");
        if(request.VisitDate is { Year: < 1912 })throw new ArgumentException("請輸入有效的就診日期。");
        if(request.PageNumber<1||request.PageSize is not(10 or 30 or 50))throw new ArgumentException("分頁條件不正確。");
        string roc=request.VisitDate is null?string.Empty:ToRocDate(request.VisitDate.Value); string? legacy=null;
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
        (OpdPricePatient patient,OpdPriceEncounter encounter)=await LoadBasicAsync(key,token);
        Task<IReadOnlyList<OpdPriceChargeSource>> drugTask=repository.QueryDrugsAsync(key,request.ShowDc,token);
        Task<IReadOnlyList<OpdPriceChargeSource>> orderTask=repository.QueryOrdersAsync(key,request.ShowDc,token);
        Task<IReadOnlyList<OpdPriceReceiptSource>> receiptTask=repository.QueryReceiptsAsync(key,request.ShowDc,token);
        await Task.WhenAll(drugTask,orderTask,receiptTask);
        var charges=(await drugTask).Concat(await orderTask).Select(x=>MapCharge(x,request.ShowExtendedCode&&IsFemh)).ToArray();
        var receiptRows=(await receiptTask).Select(x=>new OpdPriceReceiptRow(x.ReceiptNo,x.CheckoutDate,x.CheckoutUser,
            x.Receivable,x.Cash,x.Check,x.Card,x.OnAccount,x.SocialService,x.DcDate,x.DcUser,
            x.Status!="D"&&x.Key.RegistrationNo>0&&x.Key.ReceiptSequence>=0?tokens.ProtectReceipt(x.Key,actor):null,x.Status)).ToArray();
        return new(patient,encounter,charges,receiptRows,configuration?["Hospital:Code"]??"亞東紀念醫院");
    }

    public async Task<OpdPriceBasic> QueryBasicAsync(string visitToken,string actor,CancellationToken token)
    {
        if(!tokens.TryReadVisit(visitToken,actor,out OpdPriceVisitKey key))throw new KeyNotFoundException();
        (OpdPricePatient patient,OpdPriceEncounter encounter)=await LoadBasicAsync(key,token);
        return new(patient,encounter);
    }

    private async Task<(OpdPricePatient Patient,OpdPriceEncounter Encounter)> LoadBasicAsync(OpdPriceVisitKey key,CancellationToken token)
    {
        OpdPriceVisitSource visit=await repository.QueryVisitAsync(key,token)??throw new KeyNotFoundException();
        if(!string.Equals(visit.Key.MedicalRecordNo,key.MedicalRecordNo,StringComparison.Ordinal))throw new KeyNotFoundException();
        var section=await organizationUnits.ResolveNewCodeAsync(visit.SectionCode,key.Room=="0000"?"E":string.Empty,OrganizationUnitMappingScope.SectionOnly,token);
        var patient=new OpdPricePatient(key.MedicalRecordNo,visit.PatientName,visit.Birthday,visit.IdentityNumber);
        var encounter=new OpdPriceEncounter(key.VisitDate,key.VisitTime,key.Room,key.RegistrationNo,section?.NewCode??visit.SectionCode,
            visit.DoctorId+visit.DoctorName,IsFemh&&visit.IdentityType=="01"?"01:自費":visit.IdentityType,
            visit.DiscountType,visit.InsuranceSequence,visit.Copayment,visit.Diagnoses??["","","","","",""]);
        return(patient,encounter);
    }

    public async Task<IReadOnlyList<OpdPriceSectionOption>> SearchSectionsAsync(string query,CancellationToken token)=>(await organizationUnits.SearchAsync(query,true,false,false,20,token))
        .Select(x=>new OpdPriceSectionOption(x.NewCode,x.DisplayName)).ToArray();
    public Task<OpdReceiptPreview> CreateReceiptAsync(string receiptToken,string actor,CancellationToken token)
    { if(!tokens.TryReadReceipt(receiptToken,actor,out OpdPriceReceiptKey key))throw new KeyNotFoundException();return receipts.RenderAsync(key,token); }
    public async Task<OpdPriceReceiptBatch> CreateReceiptBatchAsync(OpdPriceReceiptBatchRequest request,string actor,CancellationToken token)
    {
        if(!tokens.TryReadVisit(request.VisitToken??string.Empty,actor,out OpdPriceVisitKey visit))
            throw new KeyNotFoundException();
        string[] submitted=request.ReceiptTokens?.Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray()??[];
        if(submitted.Length is 0 or > 25 || submitted.Distinct(StringComparer.Ordinal).Count()!=submitted.Length)
            throw new ArgumentException("請選擇 1 至 25 張不同的收據。");
        IReadOnlyList<OpdPriceReceiptSource> current=await repository.QueryReceiptsAsync(visit,true,token);
        var previews=new List<OpdReceiptPreview>();
        var results=new List<OpdPriceReceiptBatchResult>();
        var seenSequences=new HashSet<decimal>();
        for(int i=0;i<submitted.Length;i++)
        {
            token.ThrowIfCancellationRequested();
            if(!tokens.TryReadReceipt(submitted[i],actor,out OpdPriceReceiptKey key) ||
               key.VisitDate!=visit.VisitDate || key.VisitTime!=visit.VisitTime ||
               key.Room!=visit.Room || key.RegistrationNo!=visit.RegistrationNo ||
               key.MedicalRecordNo!=visit.MedicalRecordNo)
            { results.Add(new($"第 {i+1} 張",false,"收據識別已失效或不屬於本次就診。"));continue; }
            OpdPriceReceiptSource? row=current.FirstOrDefault(x=>x.Key.ReceiptSequence==key.ReceiptSequence);
            if(row is null){results.Add(new($"第 {i+1} 張",false,"收據已不存在。"));continue;}
            if(!seenSequences.Add(key.ReceiptSequence)){results.Add(new(row.ReceiptNo,false,"同一張收據不可重複列印。"));continue;}
            if(row.Status=="D"){results.Add(new(row.ReceiptNo,false,"此收據狀態不可列印。"));continue;}
            try
            {
                previews.Add(await receipts.RenderAsync(key,token));
                results.Add(new(row.ReceiptNo,true,"已產生列印預覽。"));
            }
            catch(KeyNotFoundException)
            { results.Add(new(row.ReceiptNo,false,"收據主檔或可列印科目資料不足。")); }
            catch(OperationCanceledException) when(token.IsCancellationRequested) { throw; }
            catch(Exception error)
            {
                logger?.LogError(error,"Q1 receipt batch item failed. ReceiptSequence={ReceiptSequence}",key.ReceiptSequence);
                results.Add(new(row.ReceiptNo,false,"此收據暫時無法產生預覽。"));
            }
        }
        return new(previews,results);
    }
    internal static string ToRocDate(DateOnly date)=>$"{date.Year-1911:000}{date:MMdd}";
    internal static OpdPriceChargeRow MapCharge(OpdPriceChargeSource x,bool showExtended)
    { bool self=x.SelfPayCode is "0" or "4"; string category=x.IsDrug?"1藥":x.Status.Length==2&&x.Status[0]=='9'&&x.Status[1]>='A'&&x.Status[1]<='Z'?"2固":"2醫";
      return new(category,!x.IsDrug&&showExtended?x.ExtendedCode:x.Code,x.Name,x.Quantity,x.IsDrug?100:x.DaysOrPercent,
        self?"自":x.SelfPayCode=="1"?"健":string.Empty,self?x.SelfPayPrice:x.InsurancePrice,self?x.SelfPayAmount:x.InsuranceAmount,
        x.IsDrug?x.Status:x.Status+(x.RequestType.Length>0?" "+x.RequestType:string.Empty),
        x.InputUser,x.PriceUser,x.DeleteUser,x.SubAmounts,x.PrescriptionDate,x.ReceiptSequence,
        x.Dose,x.Frequency,x.Days,ShortDate(x.InputDate),ShortDate(x.PriceDate),
        ShortDate(x.DeleteDate),x.ReportOrPickupNo,x.ExecutionDate,x.ExecutionDoctor); }

    private static string ShortDate(string value)=>value.Length<=3?string.Empty:value.Substring(3,Math.Min(4,value.Length-3));
}
