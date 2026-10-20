namespace OpdAccrRptWeb.Models;

public sealed class PermissionCopyRequest
{
    public string SourceUserId { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public List<string> ReportCodes { get; set; } = [];
    public Dictionary<string, int>? PreviewSourceMasks { get; set; }
    public Dictionary<string, int?>? ExpectedTargetMasks { get; set; }
}

public sealed record PermissionCopyPreviewRow(
    string ReportCode, string Operation, int ActionMask,
    int? OldMask, int AddedMask, int RemovedMask);
