using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C21RebuildService(
    IC21AccountingSummaryRepository repository,
    IC21RebuildAuthorizationService authorizationService,
    IC21UserIdentityProvider identityProvider,
    ILogger<C21RebuildService> logger) : IC21RebuildService
{
    public bool EnsureData(SearchReportCondition condition)
    {
        if (condition.EncounterSource != C21EncounterSources.Inpatient)
        {
            return false;
        }

        var start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd");
        if (start != end)
        {
            return false;
        }

        var rocDate = C21AccountingSummaryRepository.ToRocDate(start);
        var requiresRebuild = condition.ForceRebuild || !repository.HasInpatientRoom23Data(rocDate);
        if (!requiresRebuild)
        {
            return false;
        }

        if (!authorizationService.CanRebuild(condition.ForceRebuild, out var userId))
        {
            var message = condition.ForceRebuild
                ? "C21 手動重新計算功能未開放或未設定有效的固定使用者帳號。"
                : "C21 自動重新計算需要設定有效的固定使用者帳號。";
            throw new C21RebuildForbiddenException(message);
        }

        logger.LogInformation(
            "C21 inpatient rebuild started for {AccountingDate}; UserId={UserId}; IdentitySource={IdentitySource}; Forced={Forced}",
            rocDate,
            userId,
            identityProvider.IdentitySource,
            condition.ForceRebuild);
        repository.RebuildSingleInpatientDay(rocDate);
        logger.LogInformation(
            "C21 inpatient rebuild completed for {AccountingDate}; UserId={UserId}; IdentitySource={IdentitySource}",
            rocDate,
            userId,
            identityProvider.IdentitySource);
        return true;
    }
}
