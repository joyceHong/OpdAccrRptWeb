using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportCatalogServiceTests
{
    [Fact]
    public void GetReportIndex_ListsPrimaryCategoriesInRequiredOrder()
    {
        ReportIndexViewModel catalog = new ReportCatalogService().GetReportIndex();

        Assert.Equal(
            ["outpatient", "medical", "query", "sap"],
            catalog.Categories.Select(category => category.Key));
        Assert.Equal(
            ["門診批價統計報表", "醫務統計報表", "資料查詢", "SAP介接作業"],
            catalog.Categories.Select(category => category.Name));
    }

    [Fact]
    public void GetReportIndex_AssignsSapEntryOnlyToSapCategory()
    {
        ReportIndexViewModel catalog = new ReportCatalogService().GetReportIndex();
        ReportCategoryViewModel outpatient = catalog.Categories.Single(category => category.Key == "outpatient");
        ReportCategoryViewModel sap = catalog.Categories.Single(category => category.Key == "sap");

        Assert.DoesNotContain(outpatient.Groups, group => group.Name == "介接作業");
        Assert.DoesNotContain(
            outpatient.Groups.SelectMany(group => group.Reports),
            report => report.Code == "SAP");

        ReportGroupViewModel interfaceOperations = Assert.Single(sap.Groups);
        Assert.Equal("介接作業", interfaceOperations.Name);
        ReportDefinitionViewModel sapReport = Assert.Single(interfaceOperations.Reports);
        Assert.Equal("SAP", sapReport.Code);
        Assert.Equal("SAP 中介表作業", sapReport.Name);

        Assert.Single(
            catalog.Categories
                .SelectMany(category => category.Groups)
                .SelectMany(group => group.Reports),
            report => report.Code == "SAP");
    }
}
