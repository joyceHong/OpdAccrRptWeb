using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class ReportPermissionAdministrationService(
    IReportPermissionRepository repository,
    IReportCatalogService catalog)
{
    public async Task<IReadOnlyList<ReportPermissionGrant>> GetGrantsAsync(
        int targetType, string target, CancellationToken cancellationToken = default)
    {
        string canonicalTarget = await ValidateTargetAsync(targetType, target, cancellationToken);
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
        HashSet<string> existing = targetGrants.Select(grant => grant.ReportCode)
            .ToHashSet(StringComparer.Ordinal);
        return masks.Select(pair => new PermissionCopyPreviewRow(
            pair.Key, existing.Contains(pair.Key) ? "覆寫" : "新增", pair.Value)).ToArray();
    }

    public async Task ApplyCopyAsync(
        PermissionCopyRequest request, string actor, CancellationToken cancellationToken = default)
    {
        (IReadOnlyDictionary<string, int> masks, string target) =
            await PrepareCopyAsync(request, cancellationToken);
        if (string.IsNullOrWhiteSpace(actor) || actor.Length > 10)
            throw new ArgumentException("操作者帳號無效。", nameof(actor));
        await repository.SaveGrantsAsync(0, target, masks, actor, cancellationToken);
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
        if (targetType is < 0 or > 2)
            throw new ArgumentException("授權類型無效。", nameof(targetType));
        string canonicalTarget = target?.TrimEnd() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(canonicalTarget) || canonicalTarget.Length > 10)
            throw new ArgumentException("授權對象無效。", nameof(target));
        if (!await repository.TargetExistsAsync(targetType, canonicalTarget, cancellationToken))
            throw new ArgumentException("授權對象不存在或不唯一。", nameof(target));
        return canonicalTarget;
    }

    private HashSet<string> CatalogCodes() => catalog.GetReportIndex().Categories
        .SelectMany(category => category.Groups)
        .SelectMany(group => group.Reports)
        .Select(report => report.Code)
        .ToHashSet(StringComparer.Ordinal);
}
