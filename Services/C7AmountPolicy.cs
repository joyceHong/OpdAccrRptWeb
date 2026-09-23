using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public static class C7AmountPolicy
{
    public static (decimal UnitPrice, decimal Quantity, decimal Amount) Calculate(C7SourceRow row) =>
        row.SelfPayCode?.Trim() switch
        {
            "1" => (row.InsuranceUnitPrice ?? 0, row.Quantity ?? 0, row.InsuranceAmount ?? 0),
            "0" or "4" => (row.SelfPayUnitPrice ?? 0, row.Quantity ?? 0, row.SelfPayAmount ?? 0),
            _ => (row.InsuranceUnitPrice ?? 0, row.Quantity ?? 0, 0)
        };
}
