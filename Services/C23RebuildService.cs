using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C23RebuildService(
    IC23ContractAccountingRepository repository,
    IC23RebuildAuthorizationService authorizationService,
    IC23UserIdentityProvider identityProvider,
    ILogger<C23RebuildService> logger) : IC23RebuildService
{
    public bool EnsureData(SearchReportCondition condition)
    {
        if (condition.DateMode != C23DateModes.General)
        {
            return false;
        }

        var start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd");
        if (start != end)
        {
            return false;
        }

        var rocDate = C23ContractAccountingRepository.ToRocDate(start);
        var requiresRebuild = condition.ForceRebuild || !repository.HasIntermediateData(condition.EncounterSource!, rocDate);
        if (!requiresRebuild)
        {
            return false;
        }

        if (!authorizationService.CanRebuild(condition.ForceRebuild, out var userId))
        {
            throw new C23RebuildForbiddenException(condition.ForceRebuild
                ? "C23 手動重建功能未開放或未設定有效的固定使用者帳號。"
                : "C23 自動重建需要設定有效的固定使用者帳號。");
        }

        logger.LogInformation("C23 rebuild started for {AccountingDate}; UserId={UserId}; IdentitySource={IdentitySource}; Forced={Forced}",
            rocDate, userId, identityProvider.IdentitySource, condition.ForceRebuild);
        repository.RebuildSingleDay(condition.EncounterSource!, rocDate, condition.ContractCode);
        logger.LogInformation("C23 rebuild completed for {AccountingDate}; UserId={UserId}", rocDate, userId);
        return true;
    }
}
