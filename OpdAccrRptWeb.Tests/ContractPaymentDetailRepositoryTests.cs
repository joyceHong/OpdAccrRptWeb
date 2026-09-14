using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ContractPaymentDetailRepositoryTests
{
    [Fact]
    public void Columns_ExposeNineApprovedFieldsAndDecimalAmount()
    {
        var repository = new ContractPaymentDetailRepository(new FakeConnectionStringProvider());

        var columns = repository.GetColumns();

        Assert.Equal(9, columns.Count);
        Assert.Equal("contractCode", columns[0].Key);
        Assert.Equal("合約代碼", columns[0].Label);
        Assert.Equal("cashierUserId", columns[^1].Key);
        Assert.Equal(typeof(decimal), typeof(ContractPaymentDetailReportViewModel)
            .GetProperty(nameof(ContractPaymentDetailReportViewModel.PaymentAmount))!.PropertyType);
        Assert.IsAssignableFrom<IContractPaymentDetailRepository>(repository);
    }

    [Fact]
    public void SourceSql_SelectsFixedTableAndEncounterFilter()
    {
        string outpatientSql = ContractPaymentDetailRepository.GetBaseSql(EncounterSources.Emergency);
        string inpatientSql = ContractPaymentDetailRepository.GetBaseSql(EncounterSources.Inpatient);

        Assert.Contains("JOIN OpdBasicTbl", outpatientSql);
        Assert.DoesNotContain("JOIN IpdBasicTbl", outpatientSql);
        Assert.Contains("c.chOp1Time <> '0'", outpatientSql);
        Assert.Contains("JOIN IpdBasicTbl", inpatientSql);
        Assert.DoesNotContain("JOIN OpdBasicTbl", inpatientSql);
        Assert.Contains("c.chOp1Time = '0'", inpatientSql);
        Assert.Throws<ArgumentException>(() =>
            ContractPaymentDetailRepository.GetBaseSql("Unknown"));
    }

    [Fact]
    public void CountAndPageSql_ShareParameterizedGroupedNonZeroSet()
    {
        string baseSql = ContractPaymentDetailRepository.OutpatientBaseSql;
        string countSql = ContractPaymentDetailRepository.GetCountSql(EncounterSources.Emergency);
        string pageSql = ContractPaymentDetailRepository.GetPageSql(EncounterSources.Emergency);

        Assert.Contains(baseSql, countSql);
        Assert.Contains(baseSql, pageSql);
        Assert.Contains(":startDate", baseSql);
        Assert.Contains(":endDate", baseSql);
        Assert.Contains(":billingCode", baseSql);
        Assert.Contains("SUM(c.intAccCashAll) AS PaymentAmount", baseSql);
        Assert.Contains("HAVING SUM(c.intAccCashAll) <> 0", baseSql);
        Assert.Contains("GROUP BY c.vchAccPFin2", baseSql);
        Assert.Contains($"ORDER BY {ContractPaymentDetailRepository.StableOrder}", pageSql);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", pageSql);
    }

    [Fact]
    public void Parameters_TrimContractCodeAndComputeSecondPageOffset()
    {
        object parameters = ContractPaymentDetailRepository.CreateParameters(new SearchReportCondition
        {
            StartDate = "1150801",
            EndDate = "1150831",
            BillingCode = "  A01  ",
            PageNumber = 2,
            PageSize = 10
        });
        var values = parameters.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(parameters));

        Assert.Equal("1150801", values["startDate"]);
        Assert.Equal("1150831", values["endDate"]);
        Assert.Equal("A01", values["billingCode"]);
        Assert.Equal(10L, values["rowOffset"]);
        Assert.Equal(10, values["pageSize"]);
    }

    [Fact]
    public void Parameters_NormalizeWhitespaceContractCodeToNull()
    {
        object parameters = ContractPaymentDetailRepository.CreateParameters(new SearchReportCondition
        {
            StartDate = "1150801",
            EndDate = "1150831",
            BillingCode = "   "
        });
        var billingCode = parameters.GetType().GetProperty("billingCode")!.GetValue(parameters);

        Assert.Null(billingCode);
    }

    [Fact]
    public void Sql_IsStrictlyReadOnlyAndDoesNotReferenceContractControlData()
    {
        string sql = string.Join(" ",
            ContractPaymentDetailRepository.OutpatientBaseSql,
            ContractPaymentDetailRepository.InpatientBaseSql,
            ContractPaymentDetailRepository.GetCountSql(EncounterSources.Emergency),
            ContractPaymentDetailRepository.GetPageSql(EncounterSources.Inpatient));

        Assert.DoesNotContain("INSERT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MERGE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IpdContractTbl", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Crystal", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Access", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AggregationExample_SumsOneHundredFiftyAndMinusTwenty()
    {
        Assert.Equal(130m, new[] { 100m, 50m, -20m }.Sum());
    }

    private sealed class FakeConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString() => "Data Source=unused";
    }
}
