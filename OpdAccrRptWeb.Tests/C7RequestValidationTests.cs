using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;
public sealed class C7RequestValidationTests
{
    [Theory][InlineData("2360","2359")][InlineData("0800","0759")][InlineData("800","2359")]
    public void Validate_RejectsInvalidTimes(string start,string end)=>Assert.Throws<ArgumentException>(()=>new C7ReportRequest("2026-01-01","2026-01-01",start,end,"u1").Validate());
    [Fact] public void Validate_NormalizesUserAndRocDates(){var v=new C7ReportRequest("2026-01-01","2026-01-02",InputUserId:" ab ").Validate();Assert.Equal("AB",v.InputUserId);Assert.Equal("1150101",v.RocStartDate);Assert.Equal("1150102",v.RocEndDate);}
    [Fact] public void Validate_RejectsBlankUser()=>Assert.Throws<ArgumentException>(()=>new C7ReportRequest("2026-01-01","2026-01-01",InputUserId:"  ").Validate());
    [Theory][InlineData("1",10,11)][InlineData("0",20,22)][InlineData("4",20,22)][InlineData("",10,0)][InlineData("9",10,0)]
    public void AmountPolicy_UsesStoredFields(string spay,decimal price,decimal amount){var row=new C7SourceRow(null,null,null,null,spay,null,2,null,10,20,11,22,null,null,null,null,null);var a=C7AmountPolicy.Calculate(row);Assert.Equal(price,a.UnitPrice);Assert.Equal(2,a.Quantity);Assert.Equal(amount,a.Amount);}
}
