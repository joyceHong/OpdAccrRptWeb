namespace OpdAccrRptWeb.ViewModels;

public sealed class ReportDefinitionViewModel
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public bool CanExcel { get; init; } = true;

    public bool CanPrint { get; init; } = true;
    public bool CanLineBot { get; init; }

    public bool CanRebuild { get; init; } = true;
}
