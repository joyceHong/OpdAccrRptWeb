using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class M1DoctorDailyReportServiceTests
{
    [Theory]
    [InlineData("0212A", "0212*")]
    [InlineData("1234A", "1234")]
    [InlineData("1234B", "1234")]
    [InlineData("1234C", "1234")]
    [InlineData("1234D", "1234")]
    [InlineData("1234E", "1234")]
    [InlineData("1234F", "1234")]
    [InlineData("1234M", "1234")]
    [InlineData("1234*", "1234")]
    public void NormalizeSection_PreservesLegacyRules(string input, string expected) =>
        Assert.Equal(expected, M1DoctorDailyReportService.NormalizeSection(input));

    [Fact]
    public async Task Transform_SplitsRowsAndUsesEmergencyExceptions()
    {
        ServiceFixture fixture = CreateFixture([Aggregate("0201", s1: 2, s2: 3, s7: 4, s8: 5)]);
        IReadOnlyList<M1DoctorDailyReportRow> rows = await fixture.Service.TransformAsync(
            await fixture.Repository.QueryAsync("1150923"), default);

        Assert.Collection(rows.OrderBy(row => row.VisitType),
            emergency =>
            {
                Assert.Equal("E", emergency.VisitType);
                Assert.Equal("11910", emergency.SectionNo);
                Assert.Equal(0, emergency.AppointmentCount);
                Assert.Equal(9, emergency.TotalCount);
            },
            outpatient =>
            {
                Assert.Equal("R", outpatient.VisitType);
                Assert.Equal("12001", outpatient.SectionNo);
                Assert.Equal(5, outpatient.TotalCount);
            });
    }

    [Fact]
    public void Merge0420_MergesOnlyExistingDoctorVisitTargetAndDropsMissingTarget()
    {
        var target = Row("0450", "D1", "R", 1);
        var source = Row("0420", "D1", "R", 2);
        var missing = Row("0420", "D2", "R", 8);

        IReadOnlyList<M1DoctorDailyReportRow> rows =
            M1DoctorDailyReportService.Merge0420Into0450([target, source, missing]);

        M1DoctorDailyReportRow merged = Assert.Single(rows);
        Assert.Equal(3, merged.SelfPayCount);
    }

    [Fact]
    public async Task Query_ValidatesDateConfirmationAndReusesRunForPaging()
    {
        ServiceFixture fixture = CreateFixture(Enumerable.Range(1, 12)
            .Select(index => Aggregate("0201", doctor: $"D{index:00}", s1: 1)).ToArray());

        await Assert.ThrowsAsync<M1FutureDateConfirmationRequiredException>(() => fixture.Service.QueryAsync(
            new(new(2026, 9, 24)), "alice"));
        Assert.Equal(0, fixture.Repository.Calls);

        var first = await fixture.Service.QueryAsync(new(new(2026, 9, 23), PageSize: 10), "alice");
        var second = await fixture.Service.QueryAsync(new(null, PageNumber: 2, PageSize: 10,
            RunId: first.RunId), "alice");

        Assert.Equal("1150923", Assert.Single(fixture.Repository.Dates));
        Assert.Equal(12, first.TotalCount);
        Assert.Equal(2, second.Data.Count);
        await Assert.ThrowsAsync<M1ReportRunNotFoundException>(() => fixture.Service.QueryAsync(
            new(null, RunId: first.RunId), "bob"));
    }

    [Fact]
    public async Task Query_EmptySourceReturnsEmptyPageWithoutRun()
    {
        ServiceFixture fixture = CreateFixture([]);
        var response = await fixture.Service.QueryAsync(new(new(2026, 9, 23)), "alice");
        Assert.Null(response.RunId);
        Assert.Empty(response.Data);
        Assert.Equal(0, response.TotalCount);
    }

    private static ServiceFixture CreateFixture(IReadOnlyList<M1DoctorDailyAggregateRow> rows)
    {
        var repository = new Repository(rows);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new M1ReportRunStore(cache, new FixedTimeProvider());
        var service = new M1DoctorDailyReportService(repository, new Sections(), store,
            new FixedTimeProvider());
        return new(service, repository, cache);
    }

    private static M1DoctorDailyAggregateRow Aggregate(string section, string doctor = "D1",
        int s1 = 0, int s2 = 0, int s7 = 0, int s8 = 0) =>
        new(section, "原科別", doctor, "王醫師", s1, s2, 1, 2, 3, 4, s7, s8, 5, 6, 7);

    private static M1DoctorDailyReportRow Row(string section, string doctor, string type, int count) =>
        M1DoctorDailyReportRow.Create(section, "科別", doctor, "醫師", type,
            count, 0, 0, 0, 0, 0);

    private sealed class Repository(IReadOnlyList<M1DoctorDailyAggregateRow> rows)
        : IM1DoctorDailyReportRepository
    {
        public int Calls { get; private set; }
        public List<string> Dates { get; } = [];
        public Task<IReadOnlyList<M1DoctorDailyAggregateRow>> QueryAsync(
            string rocDate, CancellationToken cancellationToken = default)
        {
            Calls++;
            Dates.Add(rocDate);
            return Task.FromResult(rows);
        }
    }

    private sealed class Sections : ISectionMappingRepository
    {
        public Task<SectionMapping> TranslateAsync(string oldCode, string roomType = "",
            CancellationToken cancellationToken = default) => Task.FromResult(
                roomType == "E" && oldCode == "0201"
                    ? new SectionMapping("11910", "急診內科")
                    : new SectionMapping(oldCode == "0201" ? "12001" : oldCode, "新科別"));
        public Task<bool> LocationExistsAsync(string location, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SectionMapping?> FindPlaceAsync(string code, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SectionMapping?> FindSectionAsync(string code, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SectionMapping?> FindLocationAsync(string sectionCode, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed record ServiceFixture(
        M1DoctorDailyReportService Service,
        Repository Repository,
        IDisposable Cache) : IDisposable
    {
        public void Dispose() => Cache.Dispose();
    }
}
