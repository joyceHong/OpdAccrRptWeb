using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class ReportPermissionAdministrationService(
    IReportPermissionRepository repository,
    IReportCatalogService catalog)
{
    public async Task<PermissionTargetListPage> GetTargetListAsync(
        string? search, string? categoryKey, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            throw new ArgumentException("分頁參數無效。");
        string term = search?.Trim() ?? string.Empty;
        if (term.Length > 100) throw new ArgumentException("搜尋文字過長。", nameof(search));
        var categories = catalog.GetReportIndex().Categories;
        if (!string.IsNullOrWhiteSpace(categoryKey) &&
            !categories.Any(category => category.Key == categoryKey))
            throw new ArgumentException("模組不存在。", nameof(categoryKey));
        var reports = categories
            .Where(category => string.IsNullOrWhiteSpace(categoryKey) || category.Key == categoryKey)
            .SelectMany(category => category.Groups.SelectMany(group => group.Reports))
            .ToArray();
        string[] nameCodes = term.Length == 0 ? [] : reports
            .Where(report => report.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(report => report.Code).ToArray();
        return await repository.FindActiveTargetPageAsync(
            reports.Select(report => report.Code).ToArray(), nameCodes,
            term, page, pageSize, cancellationToken);
    }

    public async Task<PermissionGrantListPage> GetGrantListAsync(
        string? search, string? categoryKey, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            throw new ArgumentException("分頁參數無效。");
        string searchText = search?.Trim() ?? string.Empty;
        if (searchText.Length > 100)
            throw new ArgumentException("搜尋文字過長。", nameof(search));

        var categories = catalog.GetReportIndex().Categories;
        if (!string.IsNullOrWhiteSpace(categoryKey) &&
            !categories.Any(category => category.Key == categoryKey))
            throw new ArgumentException("模組不存在。", nameof(categoryKey));
        var reports = categories
            .Where(category => string.IsNullOrWhiteSpace(categoryKey) || category.Key == categoryKey)
            .SelectMany(category => category.Groups.SelectMany(group => group.Reports)
                .Select(report => new { report.Code, report.Name,
                    CategoryKey = category.Key, CategoryName = category.Name }))
            .ToArray();
        var byCode = reports.ToDictionary(report => report.Code, StringComparer.Ordinal);
        var matchingNames = searchText.Length == 0 ? [] : reports
            .Where(report => report.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Select(report => report.Code).ToArray();
        PermissionGrantRecordPage records = await repository.FindActiveGrantPageAsync(
            byCode.Keys.ToArray(), matchingNames, searchText, page, pageSize, cancellationToken);
        PermissionGrantListItem[] items = records.Items.Select(record =>
        {
            var report = byCode[record.ReportCode];
            return new PermissionGrantListItem(record.ReportCode, report.Name,
                report.CategoryKey, report.CategoryName, record.TargetType,
                record.Target, record.TargetName, record.ActionMask,
                record.ModifiedBy, record.ModifiedAt);
        }).ToArray();
        return new(items, page, pageSize, records.TotalCount);
    }

    public Task<IReadOnlyList<PermissionTargetOption>> SearchTargetsAsync(
        int targetType, string? search, CancellationToken cancellationToken = default)
    {
        if (targetType is < 0 or > 2)
            throw new ArgumentException("授權類型無效。", nameof(targetType));
        string term = search?.Trim() ?? string.Empty;
        if (term.Length is < 2 or > 100)
            throw new ArgumentException("請輸入 2 至 100 字的搜尋文字。", nameof(search));
        return repository.SearchTargetsAsync(targetType, term, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionChangePreviewRow>> PreviewChangesAsync(
        PermissionChangeRequest request, CancellationToken cancellationToken = default)
    {
        string target = await ValidateChangeRequestAsync(request, cancellationToken);
        IReadOnlyList<ReportPermissionGrant> existing = await repository.FindTargetGrantsAsync(
            request.TargetType, target, cancellationToken);
        Dictionary<string, int> masks = existing.ToDictionary(grant => grant.ReportCode,
            grant => grant.ActionMask, StringComparer.Ordinal);
        return request.Grants.Select(grant =>
        {
            int? oldMask = masks.TryGetValue(grant.ReportCode, out int value) ? value : null;
            if (grant.Revoke && oldMask is null) throw new PermissionConflictException();
            int oldBits = oldMask ?? 0;
            string operation = grant.Revoke ? "撤銷" : oldMask is null ? "新增" :
                oldMask == grant.ActionMask ? "無變更" : "變更";
            return new PermissionChangePreviewRow(grant.ReportCode, oldMask,
                grant.Revoke ? 0 : grant.ActionMask,
                grant.Revoke ? 0 : grant.ActionMask & ~oldBits,
                grant.Revoke ? oldBits : oldBits & ~grant.ActionMask,
                operation);
        }).ToArray();
    }

    public async Task ApplyChangesAsync(
        PermissionChangeRequest request, string actor,
        CancellationToken cancellationToken = default)
    {
        string target = await ValidateChangeRequestAsync(request, cancellationToken);
        if (request.Grants.Any(grant => grant.Revoke && grant.ExpectedMask is null))
            throw new ArgumentException("撤銷需要先預覽既有授權。", nameof(request));
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 10)
            throw new ArgumentException("操作者帳號無效。", nameof(actor));
        await repository.SaveGrantsIfUnchangedAsync(
            request.TargetType, target, request.Grants, actor, cancellationToken);
    }

    public async Task RevokeGrantAsync(
        PermissionRevokeRequest request, string actor,
        CancellationToken cancellationToken = default)
    {
        if (request.TargetType is < 0 or > 2
            || string.IsNullOrWhiteSpace(request.Target) || request.Target.Length > 10
            || !CatalogCodes().Contains(request.ReportCode)
            || request.ExpectedMask is < 0 or > 15)
            throw new ArgumentException("撤銷的授權資料無效。", nameof(request));
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 10)
            throw new ArgumentException("操作者帳號無效。", nameof(actor));
        await repository.RevokeGrantIfUnchangedAsync(
            request, actor, cancellationToken);
    }

    public async Task DeleteTargetAsync(
        PermissionDeleteTargetRequest request, string actor,
        CancellationToken cancellationToken = default)
    {
        string target = ValidateTargetKey(request.TargetType, request.Target);
        if (request.ExpectedMasks is null || request.ExpectedMasks.Count == 0
            || request.ExpectedMasks.Any(pair => string.IsNullOrWhiteSpace(pair.Key)
                || pair.Value is < 0 or > 15))
            throw new ArgumentException("請重新讀取此對象的權限。", nameof(request));
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 10)
            throw new ArgumentException("操作者帳號無效。", nameof(actor));
        request.Target = target;
        await repository.RevokeTargetIfUnchangedAsync(request, actor, cancellationToken);
    }

    private async Task<string> ValidateChangeRequestAsync(
        PermissionChangeRequest request, CancellationToken cancellationToken)
    {
        if (request.Grants is null || request.Grants.Count == 0)
            throw new ArgumentException("請選擇報表。", nameof(request));
        string target = request.Grants.All(grant => grant.Revoke)
            ? ValidateTargetKey(request.TargetType, request.Target)
            : await ValidateTargetAsync(request.TargetType, request.Target, cancellationToken);
        HashSet<string> known = CatalogCodes();
        if (request.Grants.Any(grant => !known.Contains(grant.ReportCode)
            || grant.ActionMask is < 0 or > 15
            || grant.ExpectedMask is < 0 or > 15))
            throw new ArgumentException("報表或權限值無效。", nameof(request));
        if (request.Grants.Select(grant => grant.ReportCode).Distinct(StringComparer.Ordinal).Count()
            != request.Grants.Count)
            throw new ArgumentException("報表不可重複選取。", nameof(request));
        return target;
    }

    public async Task<IReadOnlyList<ReportPermissionGrant>> GetGrantsAsync(
        int targetType, string target, CancellationToken cancellationToken = default)
    {
        string canonicalTarget = ValidateTargetKey(targetType, target);
        return await repository.FindTargetGrantsAsync(targetType, canonicalTarget, cancellationToken);
    }

    public async Task SaveGrantAsync(
        PermissionGrantEditRequest request, string actor,
        CancellationToken cancellationToken = default)
    {
        string target = await ValidateTargetAsync(
            request.TargetType, request.Target, cancellationToken);
        if (!CatalogCodes().Contains(request.ReportCode))
            throw new ArgumentException("報表代碼不存在。", nameof(request));
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 10)
            throw new ArgumentException("操作者帳號無效。", nameof(actor));

        int mask = ActionMask(request.Query, request.Excel, request.Print, request.Rebuild);
        await repository.SaveGrantsAsync(
            request.TargetType, target,
            new Dictionary<string, int> { [request.ReportCode] = mask },
            actor, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionBatchPreviewRow>> PreviewBatchAsync(
        PermissionBatchRequest request, CancellationToken cancellationToken = default)
    {
        string target = await ValidateTargetAsync(
            request.TargetType, request.Target, cancellationToken);
        IReadOnlyList<string> codes = ValidateReportCodes(request.ReportCodes);
        IReadOnlyList<ReportPermissionGrant> existing = await repository.FindTargetGrantsAsync(
            request.TargetType, target, cancellationToken);
        HashSet<string> existingCodes = existing
            .Select(grant => grant.ReportCode).ToHashSet(StringComparer.Ordinal);
        int mask = ActionMask(request.Query, request.Excel, request.Print, request.Rebuild);
        return codes.Select(code => new PermissionBatchPreviewRow(
            code, existingCodes.Contains(code) ? "更新" : "新增", mask)).ToArray();
    }

    public async Task ApplyBatchAsync(
        PermissionBatchRequest request, string actor, CancellationToken cancellationToken = default)
    {
        string target = await ValidateTargetAsync(
            request.TargetType, request.Target, cancellationToken);
        IReadOnlyList<string> codes = ValidateReportCodes(request.ReportCodes);
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 10)
            throw new ArgumentException("操作者帳號無效。", nameof(actor));
        int mask = ActionMask(request.Query, request.Excel, request.Print, request.Rebuild);
        await repository.SaveGrantsAsync(
            request.TargetType, target,
            codes.ToDictionary(code => code, _ => mask, StringComparer.Ordinal),
            actor, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionCopyPreviewRow>> PreviewCopyAsync(
        PermissionCopyRequest request, CancellationToken cancellationToken = default)
    {
        (IReadOnlyDictionary<string, int> masks, string target) =
            await PrepareCopyAsync(request, cancellationToken);
        IReadOnlyList<ReportPermissionGrant> targetGrants =
            await repository.FindTargetGrantsAsync(0, target, cancellationToken);
        Dictionary<string, int> existing = targetGrants.ToDictionary(
            grant => grant.ReportCode, grant => grant.ActionMask, StringComparer.Ordinal);
        return masks.Select(pair =>
        {
            int? oldMask = existing.TryGetValue(pair.Key, out int mask) ? mask : null;
            int oldBits = oldMask ?? 0;
            string operation = oldMask is null ? "新增" : oldMask == pair.Value ? "無變更" : "覆寫";
            return new PermissionCopyPreviewRow(pair.Key, operation, pair.Value,
                oldMask, pair.Value & ~oldBits, oldBits & ~pair.Value);
        }).ToArray();
    }

    public async Task ApplyCopyAsync(
        PermissionCopyRequest request, string actor, CancellationToken cancellationToken = default)
    {
        (IReadOnlyDictionary<string, int> masks, string target) =
            await PrepareCopyAsync(request, cancellationToken);
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 10)
            throw new ArgumentException("操作者帳號無效。", nameof(actor));
        if (request.PreviewSourceMasks is null || request.ExpectedTargetMasks is null
            || masks.Any(pair => !request.PreviewSourceMasks.TryGetValue(pair.Key, out int expected)
                || expected != pair.Value || !request.ExpectedTargetMasks.ContainsKey(pair.Key)))
            throw new PermissionConflictException();
        await repository.SaveGrantsIfUnchangedAsync(0, target,
            masks.Select(pair => new PermissionChangeItem
            {
                ReportCode = pair.Key,
                ActionMask = pair.Value,
                ExpectedMask = request.ExpectedTargetMasks[pair.Key]
            }).ToArray(), actor, cancellationToken, request.SourceUserId.TrimEnd());
    }

    private async Task<(IReadOnlyDictionary<string, int> Masks, string Target)> PrepareCopyAsync(
        PermissionCopyRequest request, CancellationToken cancellationToken)
    {
        string source = await ValidateTargetAsync(0, request.SourceUserId, cancellationToken);
        string target = await ValidateTargetAsync(0, request.TargetUserId, cancellationToken);
        if (source == target)
            throw new ArgumentException("來源與目標管理者不可相同。", nameof(request));
        IReadOnlyList<string> codes = ValidateReportCodes(request.ReportCodes);
        IReadOnlyList<ReportPermissionGrant> sourceGrants =
            await repository.FindTargetGrantsAsync(0, source, cancellationToken);
        HashSet<string> catalogCodes = CatalogCodes();
        Dictionary<string, int> available = sourceGrants
            .Where(grant => grant.ActionMask is >= 0 and <= 15 && catalogCodes.Contains(grant.ReportCode))
            .ToDictionary(grant => grant.ReportCode, grant => grant.ActionMask, StringComparer.Ordinal);
        if (codes.Any(code => !available.ContainsKey(code)))
            throw new ArgumentException("來源管理者沒有選取報表的有效授權。", nameof(request));
        return (codes.ToDictionary(code => code, code => available[code], StringComparer.Ordinal), target);
    }

    private IReadOnlyList<string> ValidateReportCodes(IReadOnlyList<string>? reportCodes)
    {
        if (reportCodes is null || reportCodes.Count == 0)
            throw new ArgumentException("請選擇報表。", nameof(reportCodes));
        HashSet<string> known = CatalogCodes();
        if (reportCodes.Any(code => !known.Contains(code)))
            throw new ArgumentException("報表代碼不存在。", nameof(reportCodes));
        return reportCodes.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static int ActionMask(bool query, bool excel, bool print, bool rebuild) =>
        (query ? 1 : 0) | (excel ? 2 : 0) | (print ? 4 : 0) | (rebuild ? 8 : 0);

    private async Task<string> ValidateTargetAsync(
        int targetType, string target, CancellationToken cancellationToken)
    {
        string canonicalTarget = ValidateTargetKey(targetType, target);
        if (!await repository.TargetExistsAsync(targetType, canonicalTarget, cancellationToken))
            throw new ArgumentException("授權對象不存在或不唯一。", nameof(target));
        return canonicalTarget;
    }

    private static string ValidateTargetKey(int targetType, string target)
    {
        if (targetType is < 0 or > 2)
            throw new ArgumentException("授權類型無效。", nameof(targetType));
        string canonicalTarget = target?.TrimEnd() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(canonicalTarget) || canonicalTarget.Length > 10)
            throw new ArgumentException("授權對象無效。", nameof(target));
        return canonicalTarget;
    }

    private HashSet<string> CatalogCodes() => catalog.GetReportIndex().Categories
        .SelectMany(category => category.Groups)
        .SelectMany(group => group.Reports)
        .Select(report => report.Code)
        .ToHashSet(StringComparer.Ordinal);
}
