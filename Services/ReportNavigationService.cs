using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public static class ReportNavigationService
{
    public static string? FirstAllowedPath(
        ReportIndexViewModel source, ReportPermissionSnapshot permissions)
    {
        string? code = source.Categories
            .SelectMany(category => category.Groups)
            .SelectMany(group => group.Reports)
            .Select(report => report.Code)
            .FirstOrDefault(reportCode => permissions.Allows(reportCode, ReportAction.Query));
        return code switch
        {
            null => null,
            "M1" => "/medical-statistics/doctor-daily",
            "M2" => "/medical-statistics/doctor-monthly",
            "M3" => "/medical-statistics/opd-emergency-daily",
            "Q1" => "/data-query/opd-price",
            "Q2" => "/data-query/medical-record",
            "Q3" => "/data-query/registration",
            "SAP" => "/sap-interface",
            _ => "/Report/" + code
        };
    }

    public static ReportIndexViewModel Filter(
        ReportIndexViewModel source, ReportPermissionSnapshot permissions)
    {
        IReadOnlyList<ReportCategoryViewModel> categories = source.Categories
            .Select(category => new ReportCategoryViewModel
            {
                Key = category.Key,
                Name = category.Name,
                Groups = category.Groups
                    .Select(group => new ReportGroupViewModel
                    {
                        Name = group.Name,
                        Reports = group.Reports
                            .Where(report => permissions.Allows(report.Code, ReportAction.Query))
                            .Select(report => new ReportDefinitionViewModel
                            {
                                Code = report.Code,
                                Name = report.Name,
                                CanExcel = permissions.Allows(report.Code, ReportAction.Excel),
                                CanPrint = permissions.Allows(report.Code, ReportAction.Print),
                                CanRebuild = report.CanRebuild && permissions.Allows(report.Code, ReportAction.Rebuild)
                            })
                            .ToArray()
                    })
                    .Where(group => group.Reports.Count > 0)
                    .ToArray()
            })
            .Where(category => category.Groups.Count > 0)
            .ToArray();

        return new ReportIndexViewModel
        {
            Categories = categories,
            DefaultStartDate = source.DefaultStartDate,
            DefaultEndDate = source.DefaultEndDate
        };
    }
}
