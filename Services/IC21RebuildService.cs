using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC21RebuildService
{
    bool EnsureData(SearchReportCondition condition);
}
