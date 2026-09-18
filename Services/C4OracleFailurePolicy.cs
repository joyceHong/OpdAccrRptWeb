using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Services;

public sealed class C4OracleFailurePolicy : IC4TransientFailurePolicy
{
    private static readonly HashSet<int> TransientNumbers =
        [12170, 12535, 12537, 12541, 12543, 12545, 12547, 12560, 3113, 3114];
    public int MaxAttempts => 3;
    public bool IsTransient(Exception exception) => exception is TimeoutException
        || exception is OracleException oracleException && TransientNumbers.Contains(oracleException.Number);
}
