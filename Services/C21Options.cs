namespace OpdAccrRptWeb.Services;

public sealed class C21Options
{
    public const string SectionName = "C21";

    public string? CurrentUserId { get; set; }

    public bool RebuildEnabled { get; set; }
}
