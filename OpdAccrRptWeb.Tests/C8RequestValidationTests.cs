using System.ComponentModel.DataAnnotations;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Tests;

public sealed class C8RequestValidationTests
{
    [Fact] public void PositionalRecord_DoesNotPutValidationMetadataOnGeneratedProperties()
    {
        Assert.Empty(typeof(C8ReportRequest).GetProperty(nameof(C8ReportRequest.StartDate))!
            .GetCustomAttributes(inherit: true).OfType<ValidationAttribute>());
        Assert.Empty(typeof(C8ReportRequest).GetProperty(nameof(C8ReportRequest.EndDate))!
            .GetCustomAttributes(inherit: true).OfType<ValidationAttribute>());
    }
    [Fact] public void Validate_ConvertsGregorianBoundaryToRoc()
    { var v=new C8ReportRequest(new(2026,9,20),new(2026,9,22)).Validate(); Assert.Equal("1150920",v.RocStartDate); Assert.Equal("1150922",v.RocEndDate); }
    [Fact] public void Validate_AcceptsLeapDayAndCrossYear()
    { var v=new C8ReportRequest(new(2024,2,29),new(2025,1,1)).Validate(); Assert.Equal("1130229",v.RocStartDate); Assert.Equal("1140101",v.RocEndDate); }
    [Fact] public void Validate_RejectsMissingDate()=>Assert.Throws<ArgumentException>(()=>new C8ReportRequest(null,new(2026,1,1)).Validate());
    [Fact] public void Validate_RejectsReverseRange()=>Assert.Throws<ArgumentException>(()=>new C8ReportRequest(new(2026,1,2),new(2026,1,1)).Validate());
    [Fact] public void Validate_RejectsPreRocDate()=>Assert.Throws<ArgumentException>(()=>new C8ReportRequest(new(1911,12,31),new(1912,1,1)).Validate());
}
