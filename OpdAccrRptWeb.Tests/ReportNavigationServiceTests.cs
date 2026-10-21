using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportNavigationServiceTests
{
    [Fact]
    public void QueryGrantsFilterBothMenusAndEmptyGroups()
    {
        ReportIndexViewModel catalog = new ReportCatalogService().GetReportIndex();
        var permissions = new ReportPermissionSnapshot(
            "E100", false, new Dictionary<string, ReportAction>
            {
                ["C21"] = ReportAction.Query,
                ["Q2"] = ReportAction.Query,
                ["M1"] = ReportAction.Excel
            });

        ReportIndexViewModel result = ReportNavigationService.Filter(catalog, permissions);

        Assert.Equal(["outpatient", "query"], result.Categories.Select(category => category.Key));
        Assert.Equal(["C21"], result.Categories[0].Groups.SelectMany(group => group.Reports)
            .Select(report => report.Code));
        Assert.Equal(["Q2"], result.Categories[1].Groups.SelectMany(group => group.Reports)
            .Select(report => report.Code));
        Assert.All(result.Categories, category =>
            Assert.All(category.Groups, group => Assert.NotEmpty(group.Reports)));
    }

    [Fact]
    public void NoQueryGrantsProduceNoCategories()
    {
        ReportIndexViewModel catalog = new ReportCatalogService().GetReportIndex();
        var permissions = new ReportPermissionSnapshot(
            "E100", false, new Dictionary<string, ReportAction>());

        ReportIndexViewModel result = ReportNavigationService.Filter(catalog, permissions);

        Assert.Empty(result.Categories);
    }

    [Fact]
    public void RebuildPermissionAppearsOnlyForReportsWithRebuildOperation()
    {
        ReportIndexViewModel catalog = new ReportCatalogService().GetReportIndex();
        var permissions = new ReportPermissionSnapshot("E100", false,
            new Dictionary<string, ReportAction>
            {
                ["C21"] = ReportAction.Query | ReportAction.Rebuild,
                ["C22"] = ReportAction.Query | ReportAction.Rebuild,
                ["C23"] = ReportAction.Query,
                ["C24"] = ReportAction.Query | ReportAction.Rebuild
            });

        ReportIndexViewModel result = ReportNavigationService.Filter(catalog, permissions);
        var reports = result.Categories.SelectMany(category => category.Groups)
            .SelectMany(group => group.Reports).ToDictionary(report => report.Code);

        Assert.True(reports["C21"].CanRebuild);
        Assert.False(reports["C22"].CanRebuild);
        Assert.False(reports["C23"].CanRebuild);
        Assert.True(reports["C24"].CanRebuild);
    }
}
