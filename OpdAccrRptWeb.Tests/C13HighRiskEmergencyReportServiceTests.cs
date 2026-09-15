using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C13HighRiskEmergencyReportServiceTests
{
    [Theory]
    [InlineData("0912345678", "0912******")]
    [InlineData(" 1234 ", "1234")]
    [InlineData(null, "")]
    public void PhoneMasker_PreservesFourBig5BytesAndMasksTheRest(string? input, string expected)
    {
        Assert.Equal(expected, new C13LegacyPhoneMasker().Mask(input));
    }

    [Fact]
    public void PhoneMasker_DoesNotSplitBig5Character()
    {
        string result = new C13LegacyPhoneMasker().Mask("12中文9");

        Assert.Equal("12中***", result);
    }

    [Fact]
    public void CreatePreview_UsesOneTimestampAndAllRepositoryRows()
    {
        var repository = new FakeC13Repository
        {
            PreviewRows = [new() { PatientName = "測試病患" }, new() { PatientName = "第二位" }]
        };
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 9, 15, 8, 35, 22, TimeSpan.FromHours(8)));
        var service = new C13HighRiskEmergencyReportService(repository, time);

        C13PreviewViewModel result = service.CreatePreview(new()
        {
            StartDate = "2026-09-14",
            EndDate = "2026-09-15"
        }, "TEST001");

        Assert.Equal("115/09/14", result.StartDate);
        Assert.Equal("115/09/15", result.EndDate);
        Assert.Equal("115/09/15  08:35:22", result.GeneratedAt);
        Assert.Equal("TEST001", result.GeneratedBy);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(1, repository.PreviewCalls);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone(
            "Asia/Taipei-Test", TimeSpan.FromHours(8), "Taipei", "Taipei");
    }

    private sealed class FakeC13Repository : IC13HighRiskEmergencyRepository
    {
        public List<C13HighRiskEmergencyReportViewModel> PreviewRows { get; init; } = [];
        public int PreviewCalls { get; private set; }
        public int GetCount(SearchReportCondition condition, CancellationToken cancellationToken = default) => 0;
        public List<C13HighRiskEmergencyReportViewModel> GetPage(SearchReportCondition condition, CancellationToken cancellationToken = default) => [];
        public List<C13HighRiskEmergencyReportViewModel> GetAllForPreview(SearchReportCondition condition, CancellationToken cancellationToken = default)
        {
            PreviewCalls++;
            return PreviewRows;
        }
        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() => [];
    }
}
