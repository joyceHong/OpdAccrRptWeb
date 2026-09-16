using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C143AccountingBalanceDebtReportServiceTests
{
    [Theory]
    [InlineData("OpdEr", "Difference", "1150901")]
    [InlineData("Inpatient", "Difference", "1150901")]
    [InlineData("OpdEr", "All", "1040101")]
    [InlineData("Inpatient", "All", "0970331")]
    public async Task QueryAsync_UsesExpectedEffectiveRocDates(
        string source, string reportType, string expectedStart)
    {
        var repository = new FakeRepository();
        var service = new C143AccountingBalanceDebtReportService(
            repository, new PassthroughReportTotalCountCache());

        await service.QueryAsync(Condition(source, reportType));

        Assert.Equal(expectedStart, repository.LastQuery!.StartDate);
        Assert.Equal("1150914", repository.LastQuery.EndDate);
    }

    [Theory]
    [InlineData(null, "Difference")]
    [InlineData("Other", "Difference")]
    [InlineData("OpdEr", null)]
    [InlineData("OpdEr", "Other")]
    public async Task QueryAsync_RejectsInvalidEnumsWithoutRepositoryCall(
        string? source, string? reportType)
    {
        var repository = new FakeRepository();
        var service = new C143AccountingBalanceDebtReportService(
            repository, new PassthroughReportTotalCountCache());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.QueryAsync(Condition(source, reportType)));

        Assert.Null(repository.LastQuery);
    }

    [Fact]
    public async Task QueryAsync_ReturnsExactSourceSpecificColumnsAndNullableDecimalRows()
    {
        var repository = new FakeRepository
        {
            OutpatientRows = [new() { BillingDebt = null, Difference = 10.25m,
                ResultGroup = C143ResultGroups.OutpatientEmergency }]
        };
        var service = new C143AccountingBalanceDebtReportService(
            repository, new PassthroughReportTotalCountCache());

        ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel> result =
            await service.QueryAsync(Condition("OpdEr", "Difference"));

        Assert.Equal(new[] { "診別", "就診日", "病歷號", "離院日", "一般身分自費金額_會計",
            "健保身分自費金額_會計", "尚欠_會計", "欠款_批價", "尚欠_批價",
            "差額(尚欠_會計-尚欠_批價)" }, result.Columns!.Select(x => x.Label));
        Assert.Null(result.Data![0].BillingDebt);
        Assert.Equal(10.25m, result.Data[0].Difference);
    }

    [Fact]
    public async Task QueryAsync_CachesCountsAcrossPageInputsAndSplitsInpatientBoundary()
    {
        var repository = new FakeRepository
        {
            DischargedRows = Enumerable.Range(1, 12).Select(i => new C143AccountingBalanceDebtReportViewModel
                { MedicalRecordNumber = $"D{i}", ResultGroup = C143ResultGroups.Discharged }).ToList(),
            InHospitalRows = Enumerable.Range(1, 25).Select(i => new C143AccountingBalanceDebtReportViewModel
                { MedicalRecordNumber = $"I{i}", ResultGroup = C143ResultGroups.InHospital }).ToList()
        };
        var cache = new TrackingCache();
        var service = new C143AccountingBalanceDebtReportService(repository, cache);

        SearchReportCondition first = Condition("Inpatient", "All");
        first.PageSize = 10;
        first.PageNumber = 2;
        var firstResult = await service.QueryAsync(first);
        SearchReportCondition second = Condition("Inpatient", "All");
        second.PageSize = 30;
        second.PageNumber = 1;
        var secondResult = await service.QueryAsync(second);

        Assert.Equal(37, firstResult.TotalCount);
        Assert.Equal(new[] { "D11", "D12", "I1", "I2", "I3", "I4", "I5", "I6", "I7", "I8" },
            firstResult.Data!.Select(x => x.MedicalRecordNumber));
        Assert.Equal(30, secondResult.Data!.Count);
        Assert.Equal(1, repository.Group1CountCalls);
        Assert.Equal(1, repository.Group2CountCalls);
        Assert.DoesNotContain(cache.Keys, key => key.Contains("PageNumber", StringComparison.Ordinal));
        Assert.DoesNotContain(cache.Keys, key => key.Contains("PageSize", StringComparison.Ordinal));
    }

    [Fact]
    public async Task QueryAsync_InpatientDifference_SkipsImpossibleInHospitalQueries()
    {
        var repository = new FakeRepository
        {
            DischargedRows = Enumerable.Range(1, 12).Select(i => new C143AccountingBalanceDebtReportViewModel
                { MedicalRecordNumber = $"D{i}", ResultGroup = C143ResultGroups.Discharged }).ToList(),
            InHospitalRows = [new()
                { MedicalRecordNumber = "must-not-be-returned", ResultGroup = C143ResultGroups.InHospital }]
        };
        var service = new C143AccountingBalanceDebtReportService(
            repository, new PassthroughReportTotalCountCache());

        ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel> result =
            await service.QueryAsync(Condition("Inpatient", "Difference"));

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(10, result.Data!.Count);
        Assert.All(result.Data, row => Assert.Equal(C143ResultGroups.Discharged, row.ResultGroup));
        Assert.Equal(1, repository.Group1CountCalls);
        Assert.Equal(0, repository.Group2CountCalls);
        Assert.Equal(1, repository.Group1PageCalls);
        Assert.Equal(0, repository.Group2PageCalls);
    }

    private static SearchReportCondition Condition(string? source, string? reportType) => new()
    {
        ReportCode = "C143", StartDate = "2026-09-01", EndDate = "2026-09-14",
        Source = source, ReportType = reportType, PageNumber = 1, PageSize = 10
    };

    private sealed class FakeRepository : IC143AccountingBalanceDebtRepository
    {
        public C143Query? LastQuery { get; private set; }
        public List<C143AccountingBalanceDebtReportViewModel> OutpatientRows { get; init; } = [];
        public List<C143AccountingBalanceDebtReportViewModel> DischargedRows { get; init; } = [];
        public List<C143AccountingBalanceDebtReportViewModel> InHospitalRows { get; init; } = [];
        public int Group1CountCalls { get; private set; }
        public int Group2CountCalls { get; private set; }
        public int Group1PageCalls { get; private set; }
        public int Group2PageCalls { get; private set; }
        public int GetOutpatientEmergencyCount(C143Query query, CancellationToken cancellationToken = default)
        { LastQuery = query; return OutpatientRows.Count; }
        public List<C143AccountingBalanceDebtReportViewModel> GetOutpatientEmergencyPage(
            C143Query query, int offset, int pageSize, CancellationToken cancellationToken = default)
        { LastQuery = query; return OutpatientRows.Skip(offset).Take(pageSize).ToList(); }
        public int GetInpatientCount(C143Query query, int dischargeGroup, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            if (dischargeGroup == 1) Group1CountCalls++; else Group2CountCalls++;
            return dischargeGroup == 1 ? DischargedRows.Count : InHospitalRows.Count;
        }
        public List<C143AccountingBalanceDebtReportViewModel> GetInpatientPage(
            C143Query query, int dischargeGroup, int offset, int pageSize,
            CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            if (dischargeGroup == 1) Group1PageCalls++; else Group2PageCalls++;
            return (dischargeGroup == 1 ? DischargedRows : InHospitalRows)
                .Skip(offset).Take(pageSize).ToList();
        }
    }

    private sealed class TrackingCache : IReportTotalCountCache
    {
        private readonly Dictionary<string, int> _values = [];
        public List<string> Keys { get; } = [];
        public int GetOrCreate(string reportCode, IReadOnlyDictionary<string, string?> filters, Func<int> factory)
        {
            string key = reportCode + "|" + string.Join("|", filters.OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}"));
            Keys.Add(key);
            if (_values.TryGetValue(key, out int value)) return value;
            value = factory();
            _values.Add(key, value);
            return value;
        }
        public void Invalidate(string reportCode) { }
    }
}
