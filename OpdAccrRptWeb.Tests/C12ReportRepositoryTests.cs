using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;
namespace OpdAccrRptWeb.Tests;
public sealed class C12ReportRepositoryTests
{
    [Fact] public void Visits_UseFourKeyCancellationAndBindParameters()
    { Assert.Contains(":StartDate",C12Sql.OutpatientVisits);Assert.Contains("NOT EXISTS",C12Sql.OutpatientVisits);Assert.Contains("R.intOp0No=B.intOp1No",C12Sql.OutpatientVisits); }
    [Fact] public void Charges_UseUnionDistinctAndLegacyNullExpression()
    { Assert.Contains(" UNION SELECT ",C12Sql.OutpatientCharges);Assert.DoesNotContain("UNION ALL",C12Sql.OutpatientCharges);Assert.Contains("SUM(O.rlOp4Sub3+O.rlOp4Sub5)",C12Sql.OutpatientCharges);Assert.DoesNotContain("NVL",C12Sql.OutpatientCharges); }
    [Fact] public void ChargeSql_HasNoOuterOrderBy()
    { Assert.DoesNotContain("ORDER BY",C12Sql.OutpatientCharges);Assert.DoesNotContain("ORDER BY",C12Sql.InpatientCharges); }
    [Fact] public void MedicalRecordParameter_MatchesLegacyChar10Column()
    { OracleParameter parameter=C12ReportRepository.CreateMedicalRecordParameter("K94913");Assert.Equal(OracleDbType.Char,parameter.OracleDbType);Assert.Equal(10,parameter.Size);Assert.Equal("K94913",parameter.Value); }
}
