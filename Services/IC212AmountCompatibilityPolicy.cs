namespace OpdAccrRptWeb.Services;

public interface IC212AmountCompatibilityPolicy
{
    decimal Convert(decimal rawOracleAmount);
}

public sealed class C212PreserveDecimalAmountPolicy : IC212AmountCompatibilityPolicy
{
    public decimal Convert(decimal rawOracleAmount) => rawOracleAmount;
}
