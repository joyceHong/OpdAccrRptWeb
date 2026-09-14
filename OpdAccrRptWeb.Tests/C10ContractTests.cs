using System.Text.Json;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C10ContractTests
{
    [Fact]
    public void ChargeAggregate_SerializesNullableOracleAmountsWithoutConvertingThemToZero()
    {
        var charge = new C10ChargeAggregate(
            new C10VisitKey("1150901", "083000", "A01", 7),
            C10ChargeSource.Order,
            "診察費",
            null,
            1.5m,
            null,
            2m);

        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(charge));

        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("Sub6").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("Sub1").ValueKind);
        Assert.Equal(1.5m, json.RootElement.GetProperty("Sub3").GetDecimal());
    }

    [Fact]
    public void OutputRow_ExposesThreeItemSlotsAndVisitTotals()
    {
        string json = JsonSerializer.Serialize(new C10ReceivableDetailRow
        {
            VisitDate = "1150901",
            AmountDue = 10,
            ItemName1 = "A",
            ItemName2 = "B",
            ItemName3 = "C"
        });

        Assert.Contains("\"AmountDue\":10", json, StringComparison.Ordinal);
        Assert.Contains("\"ItemName1\":\"A\"", json, StringComparison.Ordinal);
        Assert.Contains("\"ItemName3\":\"C\"", json, StringComparison.Ordinal);
    }
}
