namespace OpdAccrRptWeb.Models;

public sealed class PermissionBatchRequest
{
    public int TargetType { get; set; }
    public string Target { get; set; } = string.Empty;
    public List<string> ReportCodes { get; set; } = [];
    public bool Query { get; set; } = true;
    public bool Excel { get; set; } = true;
    public bool Print { get; set; } = true;
    public bool Rebuild { get; set; }
}

public sealed record PermissionBatchPreviewRow(
    string ReportCode, string Operation, int ActionMask);
