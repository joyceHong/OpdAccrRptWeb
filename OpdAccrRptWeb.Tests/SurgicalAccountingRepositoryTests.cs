using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using Dapper;

namespace OpdAccrRptWeb.Tests;

public sealed class SurgicalAccountingRepositoryTests
{
    [Fact]
    public void DependencyInjection_ResolvesC1RepositoryAndReportService()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddLogging()
            .AddMemoryCache()
            .AddSingleton<IHealthCenterRepository, FakeHealthCenterRepository>()
            .AddSingleton<IReferralMemberRepository, FakeReferralMemberRepository>()
            .AddSingleton<ISafeNeedleRepository, FakeSafeNeedleRepository>()
            .AddSingleton<ISurgicalAccountingRepository, FakeSurgicalAccountingRepository>()
            .AddSingleton<IReportTotalCountCache, ReportTotalCountCache>()
            .AddSingleton<IReportService, ReportService>()
            .BuildServiceProvider();

        Assert.IsType<FakeSurgicalAccountingRepository>(provider.GetRequiredService<ISurgicalAccountingRepository>());
        Assert.IsType<ReportService>(provider.GetRequiredService<IReportService>());
    }

    [Fact]
    public void Columns_ExposeEighteenLegacyFieldsInDisplayOrder()
    {
        var columns = new SurgicalAccountingRepository(new FakeConnectionStringProvider()).GetColumns();

        Assert.Equal(18, columns.Count);
        Assert.Equal(
            [
                "encounterType", "encounterDate", "encounterTime", "medicalRecordNumber",
                "patientName", "patientIdentity", "paymentMethod", "doctorName",
                "departmentCode", "surgicalOrderCode", "quantity", "amount", "chargeItem",
                "surgicalClass", "ratioDepartmentCode", "ratioDoctorNumber", "ratioQuantity",
                "ratioAmount"
            ],
            columns.Select(column => column.Key));
        Assert.Equal(
            [
                "診別", "看診日期", "時間", "病歷號", "病患姓名", "身分", "付費方式", "醫師姓名",
                "科別", "手術碼", "數量", "金額", "科目", "刀別", "手術比例科別",
                "手術比例醫師", "手術比例", "手術比例金額"
            ],
            columns.Select(column => column.Label));
    }

    [Fact]
    public void BaseSql_PreservesLegacyJoinsFiltersAndUsesBoundDates()
    {
        string sql = SurgicalAccountingRepository.BaseSql;

        Assert.Contains("JOIN OpdBasicTbl b", sql);
        Assert.Contains("LEFT JOIN GenDoctorTbl d", sql);
        Assert.Contains("LEFT JOIN IPDPriceDrRatioTbl r", sql);
        Assert.Contains("LEFT JOIN GenSectionTbl orderingSection", sql);
        Assert.Contains("b.chOp1Date BETWEEN :startDate AND :endDate", sql);
        Assert.Contains("o.chOp4Stat <> 'DC'", sql);
        Assert.Contains("o.chOp4HinCls NOT IN ('34', '38', '39', '52')", sql);
        Assert.Contains("b.chOp1MrNo NOT IN ('C36979', '1000000')", sql);
        Assert.Contains("o.chOp4Proj NOT IN ('I', 'S')", sql);
        Assert.Contains("o.chStation IN ('0532', '0296', '0330')", sql);
        Assert.Contains("o.chStation <> '1OR4F'", sql);
        Assert.DoesNotContain("SurgicalPrintTbl", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT ", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("0201", "11910")]
    [InlineData("0281", "11920")]
    [InlineData("0220", "11930")]
    [InlineData("0221", "11930")]
    [InlineData("0230", "11309")]
    public void BaseSql_ContainsEmergencySectionOverride(string oldSection, string newSection)
    {
        if (oldSection is "0220" or "0221")
        {
            Assert.Contains("resolvedSection.OldSection IN ('0220', '0221') THEN '11930'", SurgicalAccountingRepository.BaseSql);
            Assert.Contains("RTRIM(r.chSecNo) IN ('0220', '0221') THEN '11930'", SurgicalAccountingRepository.BaseSql);
        }
        else
        {
            Assert.Contains($"resolvedSection.OldSection = '{oldSection}' THEN '{newSection}'", SurgicalAccountingRepository.BaseSql);
            Assert.Contains($"RTRIM(r.chSecNo) = '{oldSection}' THEN '{newSection}'", SurgicalAccountingRepository.BaseSql);
        }
    }

    [Theory]
    [InlineData("0000", "0296", "e急診(4F8)")]
    [InlineData("0000", "0532", "e急診(3F1)")]
    [InlineData("OP_01", "0296", "o門診(4F8)")]
    [InlineData("OP_01", "0330", "o門診(3F1)")]
    [InlineData("4F7", null, "o門診(4F7)")]
    [InlineData("4F85", null, "o門診(4F8)")]
    [InlineData("3F1", null, "o門診(3F1)")]
    public void EncounterTypeExample_IsRepresentedInSql(string room, string? station, string expected)
    {
        string sql = SurgicalAccountingRepository.BaseSql;
        Assert.Contains($"THEN '{expected}'", sql);
        Assert.Contains(room.StartsWith("OP_", StringComparison.Ordinal) ? "LIKE 'OP_%'" : $"'{room}'", sql);
        if (station is not null)
        {
            Assert.Contains($"'{station}'", sql);
        }
    }

    [Fact]
    public void BaseSql_ContainsOrderLabelAndAmountRules()
    {
        string sql = SurgicalAccountingRepository.BaseSql;

        Assert.Contains("COALESCE(RTRIM(o.chOp4ExtNo), RTRIM(o.chOp4OrdNo))", sql);
        Assert.Contains("'64202B','64164B','64201B','64162B','86007C','86008C','86011C','86012C','86013C'", sql);
        Assert.Contains("WHEN RTRIM(o.chOp4Sys) = '7' THEN '(麻醉)'", sql);
        Assert.Contains("WHEN RTRIM(o.chOp4Sys) = '9' THEN '(補批)'", sql);
        Assert.Contains("WHEN RTRIM(o.chStation) = '0330' THEN '(麻醉)'", sql);
        Assert.Contains("WHEN o.rlOp4Sub1 > 0 THEN o.rlOp4Sub1", sql);
        Assert.Contains("WHEN o.rlOp4Sub2 > 0 THEN o.rlOp4Sub2", sql);
        Assert.Contains("ROUND(amounts.Amount * NVL(r.intRatioQty, 0) / 100, 2)", sql);
    }

    [Fact]
    public void CountAndPage_ReuseBaseSqlAndPageUsesRowIdTieBreaker()
    {
        Assert.Contains(SurgicalAccountingRepository.BaseSql, SurgicalAccountingRepository.CountSql);
        Assert.Contains(SurgicalAccountingRepository.BaseSql, SurgicalAccountingRepository.PageSql);
        Assert.Equal("EncounterDate, EncounterTime, MedicalRecordNumber, OrderRowId", SurgicalAccountingRepository.StableOrder);
        Assert.Contains("ORDER BY EncounterDate, EncounterTime, MedicalRecordNumber, OrderRowId", SurgicalAccountingRepository.PageSql);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", SurgicalAccountingRepository.PageSql);
        Assert.Contains("o.ROWID AS OrderRowId", SurgicalAccountingRepository.BaseSql);
    }

    [Fact]
    public void DapperMapping_PreservesTwoDoctorRatioRowsForOneOrder()
    {
        var table = new DataTable();
        table.Columns.Add("EncounterDate", typeof(string));
        table.Columns.Add("MedicalRecordNumber", typeof(string));
        table.Columns.Add("SurgicalOrderCode", typeof(string));
        table.Columns.Add("RatioDoctorNumber", typeof(string));
        table.Columns.Add("RatioQuantity", typeof(decimal));
        table.Columns.Add("RatioAmount", typeof(decimal));
        table.Rows.Add("1150801", "M001", "64202B", "D001", 60m, 75.30m);
        table.Rows.Add("1150801", "M001", "64202B", "D002", 40m, 50.20m);

        using DataTableReader reader = table.CreateDataReader();
        var parser = reader.GetRowParser<SurgicalAccountingReportViewModel>();
        var rows = new List<SurgicalAccountingReportViewModel>();
        while (reader.Read())
        {
            rows.Add(parser(reader));
        }

        Assert.Collection(
            rows,
            row => Assert.Equal(("D001", 60m, 75.30m),
                (row.RatioDoctorNumber, row.RatioQuantity, row.RatioAmount)),
            row => Assert.Equal(("D002", 40m, 50.20m),
                (row.RatioDoctorNumber, row.RatioQuantity, row.RatioAmount)));
    }

    [Fact]
    public void CreateParameters_UsesInclusiveRocRangeAndComputesOffset()
    {
        object parameters = SurgicalAccountingRepository.CreateParameters(new SearchReportCondition
        {
            StartDate = "1150801",
            EndDate = "1150803",
            PageNumber = 2,
            PageSize = 30
        });
        var values = parameters.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(parameters));

        Assert.Equal("1150801", values["startDate"]);
        Assert.Equal("1150803", values["endDate"]);
        Assert.Equal(30L, values["rowOffset"]);
        Assert.Equal(30, values["pageSize"]);
    }

    private sealed class FakeConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString() => "Data Source=unused";
    }
}
