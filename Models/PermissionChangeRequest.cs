namespace OpdAccrRptWeb.Models;

public sealed class PermissionChangeRequest
{
    public int TargetType { get; set; }
    public string Target { get; set; } = string.Empty;
    public List<PermissionChangeItem> Grants { get; set; } = [];
}

public sealed class PermissionChangeItem
{
    public string ReportCode { get; set; } = string.Empty;
    public int ActionMask { get; set; }
    public int? ExpectedMask { get; set; }
    public bool Revoke { get; set; }
}

public sealed record PermissionChangePreviewRow(
    string ReportCode, int? OldMask, int NewMask,
    int AddedMask, int RemovedMask, string Operation);

public sealed class PermissionConflictException : Exception
{
    public PermissionConflictException() : base("權限已由其他人變更，請重新預覽。") { }
}
