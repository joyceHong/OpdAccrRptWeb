using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportPermissionRepositoryTests
{
    [Fact]
    public void GrantListSourceUsesActiveRowsAndJoinedTargetNames()
    {
        Assert.Contains("grantRow.VCHSTAT IS NULL", ReportPermissionRepository.ActiveGrantSourceSql);
        Assert.Contains("RTRIM(chUserName)", ReportPermissionRepository.ActiveGrantSourceSql);
        Assert.Contains("NVL(p.CHPLANAME, s.CHSECNAME)",
            ReportPermissionRepository.ActiveGrantSourceSql);
    }

    [Fact]
    public void TargetListGroupsGrantsByTypeAndCodeBeforePagination()
    {
        string sql = ReportPermissionRepository.BuildTargetGroupSql(
            ReportPermissionRepository.ActiveGrantSourceSql);

        Assert.Contains("grantRow.VCHSTAT IS NULL", sql);
        Assert.Contains("COUNT(DISTINCT grantRow.NMENUCODE) AS ReportCount", sql);
        Assert.Contains("GROUP BY grantRow.NPERMISSIONTYPE, RTRIM(grantRow.VCHPERMISSIONTARGET)", sql);
        Assert.DoesNotContain("ROW_NUMBER()", sql);
    }

    [Fact]
    public void TargetQueriesUseActivePeopleAndCombinedDepartmentCodes()
    {
        using var connection = new OracleConnection();
        using OracleCommand people = ReportPermissionRepository.CreateSearchTargetsCommand(
            connection, 2, "E100");
        using OracleCommand departments = ReportPermissionRepository.CreateSearchTargetsCommand(
            connection, 1, "0691");

        Assert.Contains("vchStatus = 1", people.CommandText);
        Assert.Contains("vchStatus = 1", ReportPermissionRepository.ValidatePersonSql);
        Assert.Contains("FULL OUTER JOIN", departments.CommandText);
        Assert.Contains("NVL(p.CHPLANO, s.CHSECNO)", departments.CommandText);
        Assert.Contains("CHPLAWORK = '1'", departments.CommandText);
        Assert.Contains("FULL OUTER JOIN", ReportPermissionRepository.ValidateDepartmentSql);
        Assert.Equal("%0691%", departments.Parameters["Search"].Value);
    }

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
