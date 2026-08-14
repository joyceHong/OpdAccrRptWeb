namespace OpdAccrRptWeb.ViewModels;

public sealed class ReportCategoryViewModel
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<ReportGroupViewModel> Groups { get; init; }
}
