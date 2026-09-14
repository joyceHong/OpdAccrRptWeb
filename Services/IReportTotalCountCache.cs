namespace OpdAccrRptWeb.Services;

public interface IReportTotalCountCache
{
    int GetOrCreate(
        string reportCode,
        IReadOnlyDictionary<string, string?> normalizedFilters,
        Func<int> countFactory);

    void Invalidate(string reportCode);
}
