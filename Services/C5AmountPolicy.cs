using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public static class C5AmountPolicy
{
    public static (decimal UnitPrice, decimal Amount) Calculate(C5SourceRow row, bool deductSub3)
    {
        decimal amount = row.SPay?.Trim() switch
        {
            "1" => row.Amount1,
            "0" or "4" => row.Amount2,
            _ => 0m
        };
        if (deductSub3 && row.SPay?.Trim() is "1" or "0" or "4") amount -= row.Sub3 ?? 0m;
        return (row.SPay?.Trim() == "1" ? row.Pric1 : row.Pric2, amount);
    }
}
