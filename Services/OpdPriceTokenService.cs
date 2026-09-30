using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public sealed class OpdPriceTokenService(IMemoryCache cache) : IOpdPriceTokenService
{
    private static readonly TimeSpan Lifetime=TimeSpan.FromMinutes(15);
    public string ProtectVisit(OpdPriceVisitKey key,string actor)=>Store("visit",actor,key);
    public string ProtectReceipt(OpdPriceReceiptKey key,string actor)=>Store("receipt",actor,key);
    public bool TryReadVisit(string token,string actor,out OpdPriceVisitKey key)=>TryRead("visit",token,actor,out key);
    public bool TryReadReceipt(string token,string actor,out OpdPriceReceiptKey key)=>TryRead("receipt",token,actor,out key);
    private string Store<T>(string kind,string actor,T value)
    { string token=Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+','-').Replace('/','_').TrimEnd('=');
      cache.Set(Key(kind,token),new Entry<T>(actor,value),Lifetime);return token; }
    private bool TryRead<T>(string kind,string token,string actor,out T value)
    { value=default!; if(string.IsNullOrWhiteSpace(token)||!cache.TryGetValue(Key(kind,token),out Entry<T>? entry)
        ||entry is null||!CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(entry.Actor),System.Text.Encoding.UTF8.GetBytes(actor)))return false;
      value=entry.Value;return true; }
    private static string Key(string kind,string token)=>$"OpdPrice:{kind}:{token}";
    private sealed record Entry<T>(string Actor,T Value);
}
