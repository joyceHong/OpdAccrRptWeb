using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

internal interface IReportExportDefinition
{
    string ReportCode { get; }
    IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    int GetCount(SearchReportCondition condition);
    IEnumerable<object> ReadRows(SearchReportCondition condition, int batchSize);
}

internal sealed class ReportExportDefinition<T> : IReportExportDefinition where T : class
{
    private readonly Func<IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata>> _getColumns;
    private readonly Func<SearchReportCondition, int> _getCount;
    private readonly Func<SearchReportCondition, int, int, IReadOnlyList<T>> _getBatch;

    public ReportExportDefinition(
        string reportCode,
        Func<IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata>> getColumns,
        Func<SearchReportCondition, int> getCount,
        Func<SearchReportCondition, int, int, IReadOnlyList<T>> getBatch)
    {
        ReportCode = reportCode;
        _getColumns = getColumns;
        _getCount = getCount;
        _getBatch = getBatch;
    }

    public string ReportCode { get; }
    public IReadOnlyList<ModelDescriptionsHelper.PropertyMetadata> GetColumns() => _getColumns();
    public int GetCount(SearchReportCondition condition) => _getCount(condition);

    public IEnumerable<object> ReadRows(SearchReportCondition condition, int batchSize)
    {
        var offset = 0;
        while (true)
        {
            IReadOnlyList<T> batch = _getBatch(condition, offset, batchSize);
            foreach (T row in batch)
            {
                yield return row;
            }
            if (batch.Count < batchSize)
            {
                yield break;
            }
            offset += batch.Count;
        }
    }
}
