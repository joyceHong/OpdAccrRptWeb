using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Services;

public interface IC9TransientFailurePolicy
{
    int MaxAttempts { get; }
    TimeSpan RetryDelay { get; }
    bool IsTransient(Exception exception);
}

public sealed class C9OracleFailurePolicy : IC9TransientFailurePolicy
{
    private static readonly HashSet<int> TransientNumbers =
        [3113, 3114, 12170, 12535, 12537, 12541, 12543, 12545, 12547, 12560];
    public int MaxAttempts => 3;
    public TimeSpan RetryDelay => TimeSpan.FromMilliseconds(100);
    public bool IsTransient(Exception exception) => exception is TimeoutException
        || exception is OracleException oracle && TransientNumbers.Contains(Math.Abs(oracle.Number));
}
