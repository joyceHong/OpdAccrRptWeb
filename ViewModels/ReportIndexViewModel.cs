namespace OpdAccrRptWeb.ViewModels;

public sealed class ReportIndexViewModel
{
    public required IReadOnlyList<ReportCategoryViewModel> Categories { get; init; }

    public required string DefaultStartDate { get; init; }

    public required string DefaultEndDate { get; init; }
}
