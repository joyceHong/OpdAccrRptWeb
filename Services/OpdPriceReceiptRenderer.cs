using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
namespace OpdAccrRptWeb.Services;
public sealed class OpdPriceReceiptRenderer(IOpdPriceQueryRepository repository) : IOpdPriceReceiptRenderer
{
    public async Task<OpdReceiptPreview> RenderAsync(OpdPriceReceiptKey key,CancellationToken token)
    {
        OpdReceiptHeader header=await repository.QueryReceiptHeaderAsync(key,token)??throw new KeyNotFoundException();
        IReadOnlyList<OpdReceiptChargeAggregate> source=await repository.QueryReceiptChargesAsync(key,token);
        if(source.Count==0)throw new KeyNotFoundException();
        var included=new List<OpdReceiptChargeAggregate>(); decimal sub169=0,sub1=0,sub3=0,sub5=0,amt1=0,amt2=0;
        foreach(OpdReceiptChargeAggregate row in source.OrderBy(x=>x.ChargeCode,StringComparer.Ordinal))
        { if(row.ChargeCode=="69"){sub169=row.Sub1;break;} amt1+=row.InsuranceAmount;amt2+=row.SelfPayAmount;sub1+=row.Sub1;sub3+=row.Sub3;sub5+=row.Sub5;if(row.InsuranceAmount>0||row.SelfPayAmount>0)included.Add(row); }
        if(included.Count==0&&sub169==0)throw new KeyNotFoundException();
        IReadOnlyDictionary<string,string> names=await repository.QueryChargeNamesAsync(included.Select(x=>x.ChargeCode),token);
        OpdReceiptItem[] items=included.Select(x=>new OpdReceiptItem(x.ChargeCode,names.GetValueOrDefault(x.ChargeCode,string.Empty),x.InsuranceAmount,x.SelfPayAmount)).ToArray();
        IReadOnlyList<IReadOnlyList<OpdReceiptItem>> itemRows=items.Chunk(3).Select(x=>(IReadOnlyList<OpdReceiptItem>)x).ToArray();
        (decimal collected,decimal balance)=CalculateCollected(sub169,amt2,sub1,sub3,sub5);
        return new(header,itemRows,amt1+amt2,amt1,amt2,sub3,sub5,collected,balance,ToChineseDigits(collected),header.ReceiptNo.StartsWith('E')?"( 急診 )":string.Empty);
    }
    public static (decimal Collected,decimal Balance) CalculateCollected(decimal sub169,decimal amt2,decimal sub1,decimal sub3,decimal sub5)
    { if(sub169!=0)return(sub169,amt2-sub3-sub5-sub169);return sub3+sub1+sub5==amt2?(amt2-sub3-sub5,amt2-sub1-sub3-sub5):(0,amt2-sub3-sub5); }
    public static string ToChineseDigits(decimal value)
    { const string digits="零壹貳參肆伍陸柒捌玖";long whole=decimal.ToInt64(decimal.Truncate(value));string converted=string.Concat(Math.Abs(whole).ToString().Select(c=>digits[c-'0']));if(whole<0)converted=" 負 "+converted;return new string('△',Math.Max(0,7-converted.Length))+converted; }
}
