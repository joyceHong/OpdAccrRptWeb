using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;
namespace OpdAccrRptWeb.Tests;
public sealed class C12ReportRepositoryTests
{
    [Fact] public void SectionOptions_AreDistinctNonblankAndStablyOrdered()
    { Assert.Contains("SELECT DISTINCT RTRIM(S.chNewSecNo) Code",C12Sql.SectionOptions);Assert.Contains("RTRIM(S.chSecName) Name",C12Sql.SectionOptions);Assert.Contains("RTRIM(S.chNewSecNo) IS NOT NULL",C12Sql.SectionOptions);Assert.Contains("ORDER BY Code, Name",C12Sql.SectionOptions); }
    [Theory][InlineData(nameof(C12Sql.OutpatientVisits))][InlineData(nameof(C12Sql.InpatientVisits))]
    public void Visits_FilterNewSectionThroughParameterizedMapping(string source)
    { string sql=source==nameof(C12Sql.OutpatientVisits)?C12Sql.OutpatientVisits:C12Sql.InpatientVisits;Assert.Contains(":ApplySection=0 OR EXISTS",sql);Assert.Contains("RTRIM(S.chSecNo)=RTRIM(B.chOp1Sec)",sql);Assert.Contains("RTRIM(S.chNewSecNo)=:NewSectionCode",sql);Assert.DoesNotContain("SectionPrefix",sql); }
    [Fact] public void Visits_UseFourKeyCancellationAndBindParameters()
    { Assert.Contains(":StartDate",C12Sql.OutpatientVisits);Assert.Contains("NOT EXISTS",C12Sql.OutpatientVisits);Assert.Contains("R.intOp0No=B.intOp1No",C12Sql.OutpatientVisits); }
    [Fact] public void Charges_UseUnionDistinctAndLegacyNullExpression()
    { Assert.Contains(" UNION SELECT ",C12Sql.OutpatientCharges);Assert.DoesNotContain("UNION ALL",C12Sql.OutpatientCharges);Assert.Contains("SUM(O.rlOp4Sub3+O.rlOp4Sub5)",C12Sql.OutpatientCharges);Assert.DoesNotContain("NVL",C12Sql.OutpatientCharges); }
    [Fact] public void ChargeSql_HasNoOuterOrderBy()
    { Assert.DoesNotContain("ORDER BY",C12Sql.OutpatientCharges);Assert.DoesNotContain("ORDER BY",C12Sql.InpatientCharges); }
    [Fact] public void MedicalRecordParameter_MatchesLegacyChar10Column()
    { OracleParameter parameter=C12ReportRepository.CreateMedicalRecordParameter("K94913");Assert.Equal(OracleDbType.Char,parameter.OracleDbType);Assert.Equal(10,parameter.Size);Assert.Equal("K94913",parameter.Value); }
}
