using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportPermissionRepositoryTests
{
    [Fact]
    public void LoginLookupBindsTheFullAuthenticatedName()
    {
        using var connection = new OracleConnection();
        using OracleCommand command =
            ReportPermissionRepository.CreateFindUsersCommand(connection, @"DOMAIN\E100");

        Assert.True(command.BindByName);
        Assert.Contains("RTRIM(chUserID) = :LoginName", command.CommandText);
        Assert.Equal(@"DOMAIN\E100", command.Parameters["LoginName"].Value);
    }

    [Fact]
    public void GrantLookupBindsUserAndSectorAndExcludesDeletedRows()
    {
        using var connection = new OracleConnection();
        using OracleCommand command =
            ReportPermissionRepository.CreateFindGrantsCommand(connection, "E100", "D10");

        Assert.True(command.BindByName);
        Assert.Contains("FROM OpdAccrReportPermissionTbl", command.CommandText);
        Assert.Contains("VCHSTAT IS NULL", command.CommandText);
        Assert.Equal("E100", command.Parameters["UserId"].Value);
        Assert.Equal("D10", command.Parameters["Sector"].Value);
    }
}
