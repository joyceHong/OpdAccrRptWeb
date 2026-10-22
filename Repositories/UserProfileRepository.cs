using OpdAccrRptWeb.Infrastructure;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class UserProfileRepository(IConnectionStringProvider connectionStringProvider)
    : IUserProfileRepository
{
    private const string GetUserNameSql = """
        SELECT MAX(RTRIM(chUserName))
        FROM GenUserProfile1
        WHERE RTRIM(chUserID) = :UserId
        """;

    public async Task<string?> GetUserNameAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(GetUserNameSql, connection) { BindByName = true };
        command.Parameters.Add("UserId", OracleDbType.Varchar2, userId, System.Data.ParameterDirection.Input);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value)?.Trim();
    }
}
