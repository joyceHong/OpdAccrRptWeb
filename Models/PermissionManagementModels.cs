namespace OpdAccrRptWeb.Models;

public sealed record PermissionTargetOption(string Code, string Name);

public sealed record PermissionGrantListRecord(
    string ReportCode, int TargetType, string Target, int ActionMask,
    string TargetName, string ModifiedBy, DateTime ModifiedAt);

public sealed record PermissionGrantListItem(
    string ReportCode, string ReportName, string CategoryKey, string CategoryName,
    int TargetType, string Target, string TargetName, int ActionMask,
    string ModifiedBy, DateTime ModifiedAt);

public sealed record PermissionGrantListPage(
    IReadOnlyList<PermissionGrantListItem> Items, int Page, int PageSize, int TotalCount);

public sealed record PermissionGrantRecordPage(
    IReadOnlyList<PermissionGrantListRecord> Items, int TotalCount);

public sealed record PermissionTargetListRecord(
    int TargetType, string Target, string TargetName, int ReportCount,
    string ModifiedBy, DateTime ModifiedAt);

public sealed record PermissionTargetListPage(
    IReadOnlyList<PermissionTargetListRecord> Items, int Page, int PageSize, int TotalCount);
