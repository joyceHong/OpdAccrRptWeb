using System.Globalization;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;
public sealed class C7ReportService(IC7ReportRepository repository, IOrganizationUnitCodeService units,
    IC7ReportResultCache cache) : IC7ReportService
{
    public async Task<IReadOnlyList<C7InputUser>> GetEligibleUsersAsync(string endDate,CancellationToken token=default)
    {
        var validated=new C7ReportRequest(endDate,endDate,InputUserId: "LOOKUP").Validate();
        return await repository.GetEligibleUsersAsync(validated.RocEndDate,token);
    }
    public async Task<C7ReportResult> QueryAsync(C7ReportRequest request,CancellationToken token=default)
    {
        C7ValidatedRequest v=request.Validate(); var filters=Filters(v);
        if(cache.TryGet(filters,out var hit)) return Build(hit.Request with { PageNumber=v.PageNumber,PageSize=v.PageSize },hit.Rows,hit.QueryIds);
        var eligible=await repository.GetEligibleUsersAsync(v.RocEndDate,token);
        if(!eligible.Any(x=>x.UserId.Equals(v.InputUserId,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("輸入人員不符合查詢資格。");
        var rows=new List<C7ReportRow>(); var ids=new List<string>();
        await using var session=await repository.OpenSessionAsync(token);
        for(var day=v.Start;day<=v.End;day=day.AddDays(1))
        {
            token.ThrowIfCancellationRequested(); string roc=C7ReportRequest.ToRoc(day); ids.Add(v.ChargeKind==C7ChargeKind.Drug?"C7_OPD_DRUG_DETAIL":"C7_OPD_ORDER_DETAIL");
            foreach(var s in await session.QueryDayAsync(v,roc,token))
            {
                string legacy=s.LegacySectionCode?.TrimEnd()??""; string room=s.RoomType??"";
                string section=Emergency(legacy,room)??(await units.ResolveNewCodeAsync(legacy,room,OrganizationUnitMappingScope.SectionOnly,token))?.NewCode??"";
                var a=C7AmountPolicy.Calculate(s); rows.Add(new(s.VisitDate?.Trim()??"",s.VisitTime?.Trim()??"",section,room=="E"?"急診":"門診",s.Status?.Trim()=="DC"?"DC":null,s.ChargeCode?.Trim()??"",s.ChargeName?.Trim()??"",a.UnitPrice,a.Quantity,a.Amount,s.MedicalRecordNo?.Trim()??"",s.IdentityCode?.Trim()??"",s.InputUserId?.Trim()??"",s.InputUserName?.Trim()??""));
            }
        }
        cache.Set(filters,new(v,rows,ids)); return Build(v,rows,ids);
    }
    internal static string? Emergency(string code,string room)=>room=="E"?code switch{"0201"=>"11910","0281"=>"11920","0220" or "0221"=>"11930","0230"=>"11309",_=>null}:null;
    private static Dictionary<string,string?> Filters(C7ValidatedRequest v)=>new(){["StartDate"]=v.RocStartDate,["EndDate"]=v.RocEndDate,["StartTime"]=v.StartTime,["EndTime"]=v.EndTime,["InputUserId"]=v.InputUserId,["ChargeKind"]=((int)v.ChargeKind).ToString(CultureInfo.InvariantCulture)};
    private static C7ReportResult Build(C7ValidatedRequest v,IReadOnlyList<C7ReportRow> all,IReadOnlyList<string> ids)
    {int total=all.Count;var data=all.Skip((v.PageNumber-1)*v.PageSize).Take(v.PageSize).Select(C7DailyChargeDetailViewModel.From).ToList();return new(v,all,new(){Columns=ModelDescriptionsHelper.GetPropertyDescriptions<C7DailyChargeDetailViewModel>().ToList(),Data=data,TotalCount=total,PageNumber=v.PageNumber,PageSize=v.PageSize,TotalPages=total==0?0:(int)Math.Ceiling(total/(double)v.PageSize)},ids);}
}
