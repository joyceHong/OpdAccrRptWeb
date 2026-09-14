using System.ComponentModel;
using System.Reflection;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C24DebtPaymentViewModelTests
{
    [Fact]
    public void Detail_ExposesFixedDescriptionsAndNullableItemName()
    {
        var property = typeof(C24DebtPaymentDetail).GetProperty(nameof(C24DebtPaymentDetail.ChargeItemName))!;
        Assert.Equal("科目名稱", property.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>().Single().Description);
        Assert.True(new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable);
    }

    [Fact]
    public void Summary_UsesDecimalAmountsAndIndependentCollection()
    {
        Assert.Equal(typeof(decimal), typeof(C24Summary).GetProperty(nameof(C24Summary.DebtAmount))!.PropertyType);
        var result = new C24CanonicalResult
        {
            Details = [], Summaries = [new(C24RoomCategory.Inpatient, 0m, 0, 0m, 0, 0m, 0)],
            RunId = "run", CorrelationId = "trace"
        };
        Assert.Empty(result.Details);
        Assert.Single(result.Summaries);
    }
}
