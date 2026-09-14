namespace OpdAccrRptWeb.Services;

public static class C24OracleFailurePolicy
{
    private static readonly HashSet<int> TransientCodes = [3113, 3114, 12170, 12541, 12543];
    public const int MaximumAttempts = 2;
    public static bool IsTransient(int oracleCode) => TransientCodes.Contains(Math.Abs(oracleCode));
}
