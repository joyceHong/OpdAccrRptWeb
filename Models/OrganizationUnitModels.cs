namespace OpdAccrRptWeb.Models;

public enum OrganizationUnitSource
{
    Section,
    Place,
    Fixed
}

public enum OrganizationUnitMappingScope
{
    SectionOnly,
    SectionAndPlace
}

public sealed record OrganizationUnitMapping(
    OrganizationUnitSource Source,
    string LegacyCode,
    string NewCode,
    string DisplayName,
    bool IsActive);

public sealed class OrganizationUnitMappingAmbiguousException(string newCode)
    : ArgumentException($"新部門代碼 {newCode} 對應到多個舊代碼，請聯絡系統管理員確認主檔。")
{
}

public sealed class OrganizationUnitLegacyMappingAmbiguousException(string legacyCode)
    : ArgumentException($"舊科別／部門代碼 {legacyCode} 對應到多個新代碼，請聯絡系統管理員確認主檔。")
{
}
