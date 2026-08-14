namespace OpdAccrRptWeb.ViewModels;

public sealed class ReportGroupViewModel
{
    public required string Name { get; init; }

    public required IReadOnlyList<ReportDefinitionViewModel> Reports { get; init; }
}
