using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OpdAccrRptWeb.Tests;

public sealed class C23ContractAccountingRepositoryTests
{
    [Fact]
    public void CreateParameters_ConvertsDatesAndNormalizesContract()
    {
        var parameters = C23ContractAccountingRepository.CreateParameters(new SearchReportCondition
        {
            StartDate = "2026-09-08", EndDate = "2026-09-09", ContractCode = " 42 ",
            DateMode = C23DateModes.General, PageNumber = 2, PageSize = 10
        });

        Assert.Equal("1150908", Property(parameters, "startDate"));
        Assert.Equal("1150909", Property(parameters, "endDate"));
        Assert.Equal("42", Property(parameters, "contract"));
        Assert.Equal(10L, Property(parameters, "rowOffset"));
    }

    [Fact]
    public void QuerySql_UsesWhitelistedSourceAndBindParameters()
    {
        var sql = C23ContractAccountingRepository.GetBaseSql(new SearchReportCondition
        {
            StartDate = "2026-09-08", EndDate = "2026-09-08",
            EncounterSource = C23EncounterSources.Outpatient, DateMode = C23DateModes.General
        });

        Assert.Contains("FROM OpdRecRpt_PFin2SumDM1", sql);
        Assert.Contains(":dateFlagStart", sql);
        Assert.Contains(":dateFlagEnd", sql);
        Assert.Contains(":contract", sql);
        Assert.Contains("chOp1PSecNm", sql);
        Assert.DoesNotContain("chOp1SecNm", sql);
        Assert.Contains("rlOp1SubAMT", sql);
        Assert.DoesNotContain("rlOp1Sub5", sql);
        Assert.DoesNotContain("2026-09-08", sql);
    }

    [Fact]
    public void MultiDaySql_ContainsSummaryAndDetailButNoLegacyReportThree()
    {
        var sql = C23ContractAccountingRepository.GetBaseSql(new SearchReportCondition
        {
            StartDate = "2026-09-01", EndDate = "2026-09-08",
            EncounterSource = C23EncounterSources.Inpatient, DateMode = C23DateModes.General
        });

        Assert.Contains("'Summary' AS ResultType", sql);
        Assert.Contains("'Detail' AS ResultType", sql);
        Assert.DoesNotContain("PFin2SumDM2", sql);
    }

    [Fact]
    public void MultiDaySql_ConvertsVarcharContractAmountToNumberOnBothUnionBranches()
    {
        var sql = C23ContractAccountingRepository.GetBaseSql(new SearchReportCondition
        {
            StartDate = "2026-09-30", EndDate = "2026-10-01",
            EncounterSource = C23EncounterSources.Outpatient, DateMode = C23DateModes.General
        });

        Assert.Contains("TO_NUMBER(NVL(NULLIF(TRIM(rlOp1Sub2), ''), '0')) AS ContractAmount", sql);
        Assert.Contains("SUM(TO_NUMBER(NVL(NULLIF(TRIM(rlOp1Sub2), ''), '0'))) AS ContractAmount", sql);
        Assert.DoesNotContain("SUM(NVL(rlOp1Sub2, 0))", sql);
    }

    [Fact]
    public void ExecuteAtomically_FailureRollsBackWithoutCommit()
    {
        var commits = 0;
        var rollbacks = 0;
        Assert.Throws<InvalidOperationException>(() => C23ContractAccountingRepository.ExecuteAtomically(
            ["delete", "insert-ord", "insert-drg"],
            command => { if (command == "insert-ord") throw new InvalidOperationException(); },
            () => { },
            () => commits++, () => rollbacks++));
        Assert.Equal(0, commits);
        Assert.Equal(1, rollbacks);
    }

    [Fact]
    public void ExecuteAtomically_SuccessCommitsExactlyOnce()
    {
        var commits = 0;
        var rollbacks = 0;
        C23ContractAccountingRepository.ExecuteAtomically(["delete", "insert"], _ => { }, () => { },
            () => commits++, () => rollbacks++);
        Assert.Equal(1, commits);
        Assert.Equal(0, rollbacks);
    }

    [Fact]
    public void SourceSql_ContainsFourOutpatientAndFiveInpatientParameterizedBranches()
    {
        var queries = C23SourceSql.QueriesFor(C23EncounterSources.Outpatient)
            .Concat(C23SourceSql.QueriesFor(C23EncounterSources.Inpatient));
        Assert.Equal(4, C23SourceSql.QueriesFor(C23EncounterSources.Outpatient).Count);
        Assert.Equal(5, C23SourceSql.QueriesFor(C23EncounterSources.Inpatient).Count);
        foreach (var query in queries)
        {
            Assert.Contains(":SDate", query.Sql);
            Assert.DoesNotContain("COMMIT", query.Sql, StringComparison.OrdinalIgnoreCase);
            Assert.False(query.Sql.TrimEnd().EndsWith(';'));
        }
    }

    [Fact]
    public void FinalizedSql_InventoryHashesAndBindPlaceholdersMatchApprovedBaseline()
    {
        var expectedHashes = new Dictionary<string, string>
        {
            ["C23_I_01_credit_ord.sql"] = "9D4BE2075D460A6DCB528A1B164A722AA26EC8EDAF3CFC3389DC0466F14AFFD7",
            ["C23_I_02_credit_drg.sql"] = "FCA82ACFDECE41D31D621EDB7512DA77F74B0939BC2A3BD4B0D3D7CB9D74FFFA",
            ["C23_I_03_noncredit_ord.sql"] = "E075FE7ECD4A5B2F5DB0202630CE8E947C904E57BD36E2E41A81E372499D4E92",
            ["C23_I_04_noncredit_drg.sql"] = "07E64EA90C2DC3E27FF8DE6E267F4B79F5BBC8C557312911D43E16A3270A7B24",
            ["C23_I_05_discharge_snapshot.sql"] = "E7732547534E825994CCC059F1E413DEC1CD974977606050E59A353CC16059B3",
            ["C23_I_balance42_rebuild.sql"] = "B45967E4DF5D1895FF9E8D894699CA7441CA3D17EAF5F5A8AABEE6CF2BC40599",
            ["C23_I_encounter_date.sql"] = "C961E6920EC5ED4366F7E77921DDA2C810FFD91CECC07242E11594B78D43DAC0",
            ["C23_I_general_balance.sql"] = "AB4B2F4DFC934B480132857183F4A66EA556C9A7BF9E2F464F8DCD7D3FC8E288",
            ["C23_O_01_credit_ord.sql"] = "678BE167C91E1537F06E104EB002839E6EF73704C9269C865302566EABEDB183",
            ["C23_O_02_credit_drg.sql"] = "72DE883355AA663878C931806AA08C984FA427182A71F07A2BD13B94C82DA32B",
            ["C23_O_03_noncredit_ord.sql"] = "FAC2123076ED2380EADE6827BF17E19DB856142A8E03C54E15C69BED6872411B",
            ["C23_O_04_noncredit_drg.sql"] = "B63E8D7A9F9ABDF07A843591E06C261C962EAB825750D78473CA1EC46A0C5D7E",
            ["C23_O_balance42_rebuild.sql"] = "FEB5BA6F294C5165D3633DFC667CE1E21F927D2DA028994303320BF58FDAE080",
            ["C23_O_encounter_date.sql"] = "E5478EAA3368134DB0609522D528E80BE7F0EA0AFEE51A1C92DCD4DD74CF9F96",
            ["C23_O_general_balance.sql"] = "CCD1CBBF9FCD7380EA82F9647CBF8F1D79340EFBCD29ECF246FD614B5F86AD23"
        };

        Assert.Equal(15, C23SourceSql.FinalizedSql.Count);
        Assert.Equal(expectedHashes.Keys.Order(), C23SourceSql.FinalizedSql.Keys.Order());
        foreach (var (file, hash) in expectedHashes)
        {
            var sql = NormalizeExecutableSql(C23SourceSql.FinalizedSql[file]);
            Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql))));
            Assert.NotEmpty(Regex.Matches(sql, @":([A-Za-z][A-Za-z0-9_]*)"));
        }

        Assert.Equal(["PFin2", "SDate"], BindNames(C23SourceSql.FinalizedSql["C23_O_01_credit_ord.sql"]));
        Assert.Equal(["Contract", "EndDate", "StartDate"], BindNames(C23SourceSql.FinalizedSql["C23_I_encounter_date.sql"]));
        Assert.Equal(["AccountingDate", "AccountingDayEnd", "AccountingDayStart"],
            BindNames(C23SourceSql.FinalizedSql["C23_O_general_balance.sql"]));
    }

    [Fact]
    public void SqlSelection_UnsupportedSourceFailsClosed()
    {
        Assert.Throws<ArgumentException>(() => C23SourceSql.QueriesFor("unknown"));
        Assert.Throws<ArgumentException>(() => C23SourceSql.EncounterDateFor("unknown"));
        Assert.Throws<ArgumentException>(() => C23SourceSql.GeneralBalanceCommandsFor("unknown"));
        Assert.Throws<ArgumentException>(() => C23SourceSql.Balance42CommandsFor("unknown"));
    }

    [Fact]
    public void EncounterDateSql_UsesFinalizedOrderAndDrugBranches()
    {
        var sql = C23ContractAccountingRepository.GetBaseSql(new SearchReportCondition
        {
            EncounterSource = C23EncounterSources.Outpatient, DateMode = C23DateModes.EncounterDate
        });

        Assert.Contains("from opdordtbl A", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("from opddrgtbl A", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(":StartDate", sql);
        Assert.Contains(":EndDate", sql);
    }

    [Fact]
    public void SpecialContracts_AreMergedDeduplicatedAndDoNotInvent42()
    {
        var result = C23ContractAccountingRepository.MergeContracts(
            [new("AA", "主檔"), new("TT", "主檔研究"), new("42", "主檔42")]);

        Assert.Equal(result.Count, result.Select(option => option.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(["42", "AA", "TT", "UU", "VV", "WW", "XX", "YY", "ZZ"], result.Select(option => option.Code));
        Assert.Equal("主檔研究", result.Single(option => option.Code == "TT").Name);
        Assert.Single(result, option => option.Code == "42");
    }

    [Theory]
    [InlineData("Outpatient", "0980831", 1)]
    [InlineData("Outpatient", "0980901", 3)]
    [InlineData("Inpatient", "0980731", 1)]
    [InlineData("Inpatient", "0980801", 3)]
    [InlineData("Inpatient", "0991101", 7)]
    public void RebuildCommands_ApplyLegacyDateThresholds(string source, string date, int expectedCount)
    {
        var commands = C23RebuildSql.GetCommands(source, date);
        Assert.Equal(expectedCount, commands.Count);
        Assert.DoesNotContain(commands, sql => sql.Contains("COMMIT", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(C23EncounterSources.Outpatient, "OPDCONTRACTMRNOTBL", "GENCONTRACT42MRNOTBL")]
    [InlineData(C23EncounterSources.Inpatient, "IPDCONTRACTMRNOTBL", "GENCONTRACT42MRNOTBL")]
    public void RebuildCommands_PreserveDeterministicGeneralAndBalance42Order(
        string source, string generalTable, string balanceTable)
    {
        var commands = C23RebuildSql.GetCommands(source, "1150908");

        Assert.Equal(7, commands.Count);
        Assert.StartsWith("DELETE FROM", commands[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(generalTable, commands[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(generalTable, commands[2], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(balanceTable, commands[3], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(balanceTable, commands[4], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(balanceTable, commands[5], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(balanceTable, commands[6], StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("DELETE FROM", commands[3], StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("INSERT INTO", commands[4], StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("DELETE FROM", commands[5], StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("INSERT INTO", commands[6], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OutpatientBalanceRebuild_UsesOrderAndDrugEventsWithoutInventedCaseDayTable()
    {
        var commands = C23RebuildSql.GetCommands(C23EncounterSources.Outpatient, "1150908");
        var balanceSql = commands.Single(sql => sql.Contains("INSERT INTO OPDCONTRACTMRNOTBL", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain("OpdAccCaseDayTbl", balanceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OpdDrgTbl", balanceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OpdOrdTbl", balanceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, Count(balanceSql.ToUpperInvariant(), "UNION ALL"));
        Assert.Contains("chOp3DCDate", balanceSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("chOp4DCDate", balanceSql, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string value, string text) =>
        (value.Length - value.Replace(text, string.Empty, StringComparison.Ordinal).Length) / text.Length;

    private static string NormalizeExecutableSql(string sql) => string.Join(";\n",
        Regex.Replace(sql, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(command => command.Replace("\r\n", "\n", StringComparison.Ordinal)));

    private static string[] BindNames(string sql) => Regex.Matches(sql, @":([A-Za-z][A-Za-z0-9_]*)")
        .Select(match => match.Groups[1].Value)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    private static object? Property(object value, string name) => value.GetType().GetProperty(name)!.GetValue(value);
}
