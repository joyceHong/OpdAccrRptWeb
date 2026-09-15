namespace OpdAccrRptWeb.Services;
public interface IC12LegacyAmountConverter { int Convert(decimal? value); }
public sealed class C12CompatibilityException(string message) : InvalidOperationException(message);
public sealed class C12LegacyAmountConverter : IC12LegacyAmountConverter
{
    public int Convert(decimal? value)
    {
        decimal amount=value??0m;
        if (decimal.Truncate(amount)!=amount || amount<int.MinValue || amount>int.MaxValue)
            throw new C12CompatibilityException("C12 金額超出已驗證的 Jet Integer 相容範圍。");
        return decimal.ToInt32(amount);
    }
}
