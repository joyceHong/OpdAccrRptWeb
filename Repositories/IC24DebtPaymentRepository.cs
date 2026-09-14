using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC24DebtPaymentRepository
{
    C24RepositoryResult Load(SearchReportCondition condition, CancellationToken cancellationToken = default);
    bool HasLegacyResult(string source, DateOnly accountingDate,
        CancellationToken cancellationToken = default);
    C24LegacyResult LoadLegacy(string source, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default);
    void PublishLegacy(string source, DateOnly accountingDate,
        IReadOnlyList<C24LegacyDetailRow> details, IReadOnlyList<C24LegacySummaryRow> summaries,
        CancellationToken cancellationToken = default);
}
