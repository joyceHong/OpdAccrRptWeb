using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC23RebuildService
{
    bool EnsureData(SearchReportCondition condition);
}
