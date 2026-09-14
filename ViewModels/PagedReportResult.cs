namespace OpdAccrRptWeb.ViewModels;

public sealed class PagedReportResult<T>
{
    public required List<T> Data { get; init; }

    public required int TotalCount { get; init; }
}
