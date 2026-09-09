namespace OpdAccrRptWeb.ViewModels;

public sealed class ReportIndexViewModel
{
    public required IReadOnlyList<ReportCategoryViewModel> Categories { get; init; }

    public required string DefaultStartDate { get; init; }

    public required string DefaultEndDate { get; init; }

    public bool C21RebuildEnabled { get; set; }

    public bool C23RebuildEnabled { get; set; }
}
