using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using OpdAccrRptWeb.Infrastructure;

namespace OpdAccrRptWeb.Tests;

public sealed class ReferralMemberRepositoryTests
{
    [Fact]
    public void Constructor_DoesNotResolveConnectionBeforeAQueryRuns()
    {
        var provider = new ThrowingConnectionStringProvider();

        _ = new ReferralMemberRepository(provider);

        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public void EmergencyBaseSql_UsesFixedEmergencyTablesAndRules()
    {
        string sql = ReferralMemberRepository.EmergencyBaseSql;

        Assert.Contains("FROM OpdBasicTbl b", sql);
        Assert.Contains("JOIN OpdRegPtnTbl r", sql);
        Assert.Contains("b.chop1room = '0000'", sql);
        Assert.Contains("r.chop0dc = '0'", sql);
        Assert.DoesNotContain("IpdBasicTbl", sql);
        AssertSharedRules(sql);
    }

    [Fact]
    public void InpatientBaseSql_UsesFixedInpatientTableAndClaimRule()
    {
        string sql = ReferralMemberRepository.InpatientBaseSql;

        Assert.Contains("FROM IpdBasicTbl b", sql);
        Assert.Contains("TRIM(b.chop1clmflg) <> 'D'", sql);
        Assert.Contains("b.chop1clmflg IS NULL", sql);
        Assert.DoesNotContain("OpdRegPtnTbl", sql);
        AssertSharedRules(sql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Outpatient")]
    public void GetBaseSql_UnknownSource_Throws(string? source)
    {
        Assert.Throws<ArgumentException>(() => ReferralMemberRepository.GetBaseSql(source));
    }

    [Theory]
    [InlineData(EncounterSources.Emergency)]
    [InlineData(EncounterSources.Inpatient)]
    public void PageSql_ProjectsTwentyColumnsAndUsesStableBoundPaging(string source)
    {
        string sql = ReferralMemberRepository.GetPageSql(source);
        string publicProjection = sql.Split("FROM (", StringSplitOptions.None)[0];

        string[] aliases =
        [
            "ClinicCode", "ClinicName", "PatientIdentifier", "CaseCategory", "PatientName",
            "MedicalRecordNumber", "EncounterDepartment", "BedNumber", "AttendingPhysician",
            "AdmissionDate", "DischargeDate", "DiagnosisCode1", "DiagnosisCode2", "DiagnosisCode3",
            "DiagnosisName1", "DiagnosisName2", "DiagnosisName3", "NetworkConsent",
            "CompleteResponse", "ReferralAnnotation"
        ];
        Assert.All(aliases, alias => Assert.Contains(alias, publicProjection));
        Assert.DoesNotContain("SourceRowId", publicProjection);
        Assert.Contains(
            "SortEncounterDate,\n            SortEncounterTime,\n            SortClinicRoom,\n            SortEncounterSequence,\n            MemberSourceRowId,\n            ReferralSourceRowId,\n            EncounterSourceRowId",
            sql.Replace("\r\n", "\n"));
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", sql);
    }

    [Fact]
    public void CountAndPageSql_ReuseTheSameEmergencyBaseSql()
    {
        string countSql = ReferralMemberRepository.GetCountSql(EncounterSources.Emergency);
        string pageSql = ReferralMemberRepository.GetPageSql(EncounterSources.Emergency);

        Assert.Contains(ReferralMemberRepository.EmergencyBaseSql, countSql);
        Assert.Contains(ReferralMemberRepository.EmergencyBaseSql, pageSql);
    }

    [Fact]
    public void CreateParameters_UsesMemberYearAndOneBasedLongOffset()
    {
        object parameters = ReferralMemberRepository.CreateParameters(new SearchReportCondition
        {
            EncounterSource = EncounterSources.Inpatient,
            StartDate = "1150101",
            EndDate = "1151231",
            PageNumber = 3,
            PageSize = 30
        });

        Assert.Equal("115", ReadProperty<string>(parameters, "memberYear"));
        Assert.Equal("1150101", ReadProperty<string>(parameters, "strSDate"));
        Assert.Equal("1151231", ReadProperty<string>(parameters, "strEDate"));
        Assert.Equal(60L, ReadProperty<long>(parameters, "rowOffset"));
        Assert.Equal(30, ReadProperty<int>(parameters, "pageSize"));
    }

    [Fact]
    public void CreateParameters_MaximumPageNumber_DoesNotOverflowOffset()
    {
        object parameters = ReferralMemberRepository.CreateParameters(new SearchReportCondition
        {
            EncounterSource = EncounterSources.Emergency,
            StartDate = "1150101",
            EndDate = "1151231",
            PageNumber = int.MaxValue,
            PageSize = 50
        });

        Assert.Equal(((long)int.MaxValue - 1) * 50, ReadProperty<long>(parameters, "rowOffset"));
    }

    private static void AssertSharedRules(string sql)
    {
        Assert.Contains("JOIN GenReferralMemberTbl f", sql);
        Assert.Contains("f.chyear = :memberYear", sql);
        Assert.Contains("JOIN GenSectionTbl s", sql);
        Assert.Contains("LEFT JOIN OpdTFHospitalTbl h", sql);
        Assert.Equal(3, CountOccurrences(sql, "LEFT JOIN GenICD9Tbl"));
        Assert.Contains("LEFT JOIN OpdTFTbl t", sql);
        Assert.Contains("REGEXP_LIKE(TRIM(t.intop1no), '^[0-9]+$')", sql);
        Assert.Contains("THEN TO_NUMBER(TRIM(t.intop1no))", sql);
        Assert.Contains("b.chop1tranout = 'N'", sql);
        Assert.Contains("b.chop1tranout = 'Y'", sql);
        Assert.Contains("b.chop1sec <> '0340'", sql);
        Assert.Contains("THEN '轉入'", sql);
        Assert.Contains("THEN '轉出'", sql);
        Assert.Contains("b.chop1date BETWEEN :strSDate AND :strEDate", sql);
        Assert.DoesNotContain("{BasicTbl}", sql);
    }

    private static int CountOccurrences(string value, string search) =>
        value.Split(search, StringSplitOptions.None).Length - 1;

    private static T ReadProperty<T>(object value, string name) =>
        (T)value.GetType().GetProperty(name)!.GetValue(value)!;

    private sealed class ThrowingConnectionStringProvider : IConnectionStringProvider
    {
        public int Calls { get; private set; }

        public string GetConnectionString()
        {
            Calls++;
            throw new InvalidOperationException();
        }
    }
}
