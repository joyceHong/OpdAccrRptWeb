using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class ReportQueryPermissionFilter(
    IReportPermissionService permissions,
    IReportCatalogService catalog,
    IReportExportService exports,
    IConfiguration configuration,
    ILogger<ReportQueryPermissionFilter> logger) : IAsyncActionFilter
{
    private static readonly IReadOnlyDictionary<string, string> ControllerReports =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["C4MaterialReport"] = "C4",
            ["C5Report"] = "C5",
            ["C7Report"] = "C7",
            ["C8Report"] = "C8",
            ["C9Report"] = "C9",
            ["C141Ranking"] = "C141",
            ["C141PaymentNotice"] = "C141",
            ["C141LineBot"] = "C141",
            ["C142DebtReport"] = "C142",
            ["DoctorDailyReport"] = "M1",
            ["DoctorMonthlyReport"] = "M2",
            ["OpdEmergencyDailyReport"] = "M3",
            ["OpdPriceQuery"] = "Q1",
            ["MedicalRecordQuery"] = "Q2",
            ["RegistrationQuery"] = "Q3",
            ["SapInterface"] = "SAP"
        };

    private static readonly IReadOnlyDictionary<string, string> ReportActions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GetC12SectionOptions"] = "C12",
            ["GetC21BillingItems"] = "C21",
            ["GetC23Contracts"] = "C23",
            ["GetC211Contracts"] = "C211"
        };

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!configuration.GetValue<bool>("ReportPermissions:Enforce"))
        {
            await next();
            return;
        }

        string? reportCode = ResolveReportCode(context);
        bool isReportPage = IsReportPage(context);
        bool isExportJob = IsExportJobAction(context);
        if (reportCode is null && !isReportPage && !isExportJob)
        {
            await next();
            return;
        }

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        ReportPermissionSnapshot snapshot;
        try
        {
            snapshot = await permissions.ResolveAsync(
                context.HttpContext.User.Identity.Name, context.HttpContext.RequestAborted);
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            context.Result = new StatusCodeResult(499);
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cannot check report permission");
            context.Result = new StatusCodeResult(503);
            return;
        }

        if (isExportJob)
        {
            try
            {
                if (!context.ActionArguments.TryGetValue("jobId", out object? value) ||
                    value is not string jobId || !Guid.TryParseExact(jobId, "D", out Guid parsedJobId))
                {
                    context.Result = new BadRequestResult();
                    return;
                }
                ReportExportJob? job = exports.GetJob(parsedJobId, context.HttpContext.User.Identity.Name);
                if (job is null ||
                    !string.Equals(job.CreatedBy, context.HttpContext.User.Identity.Name,
                        StringComparison.Ordinal))
                {
                    context.Result = new NotFoundResult();
                    return;
                }
                if (!snapshot.Allows(job.SearchCondition.ReportCode ?? string.Empty, ReportAction.Excel))
                {
                    context.Result = new StatusCodeResult(403);
                    return;
                }
            }
            catch (Exception exception) when (context.Result is null)
            {
                logger.LogError(exception, "Cannot load report export job permission");
                context.Result = new StatusCodeResult(503);
                return;
            }
            await next();
            return;
        }
        try
        {
            if (IsDefaultLanding(context))
            {
                string? firstPath = ReportNavigationService.FirstAllowedPath(
                    catalog.GetReportIndex(), snapshot);
                if (firstPath is not null)
                {
                    context.Result = new RedirectResult(firstPath);
                    return;
                }
                if (string.Equals(RouteValue(context, "action"), "Root",
                        StringComparison.OrdinalIgnoreCase))
                {
                    context.Result = new RedirectResult("/Report");
                    return;
                }
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Report/Index.cshtml",
                    ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                        new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                        context.ModelState)
                    {
                        Model = ReportNavigationService.Filter(catalog.GetReportIndex(), snapshot)
                    }
                };
                return;
            }
            ReportAction requiredAction = ResolveRequiredAction(context);
            if (reportCode is not null && !snapshot.Allows(reportCode, requiredAction))
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            context.Result = new StatusCodeResult(499);
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cannot check report navigation permission");
            context.Result = new StatusCodeResult(503);
            return;
        }
        ActionExecutedContext executed = await next();
        if (executed.Result is ViewResult view && view.Model is ReportIndexViewModel model)
            view.ViewData.Model = ReportNavigationService.Filter(model, snapshot);
    }

    private static bool IsExportJobAction(ActionExecutingContext context) =>
        string.Equals(RouteValue(context, "controller"), "Report", StringComparison.OrdinalIgnoreCase) &&
        (string.Equals(RouteValue(context, "action"), "GetExportStatus", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(RouteValue(context, "action"), "DownloadExport", StringComparison.OrdinalIgnoreCase));

    private static bool IsReportPage(ActionExecutingContext context)
    {
        string? controller = RouteValue(context, "controller");
        string? action = RouteValue(context, "action");
        return string.Equals(controller, "Report", StringComparison.OrdinalIgnoreCase) &&
               (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(action, "Root", StringComparison.OrdinalIgnoreCase)) ||
               controller is not null && ControllerReports.ContainsKey(controller) &&
               string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDefaultLanding(ActionExecutingContext context)
    {
        if (!string.Equals(RouteValue(context, "controller"), "Report",
                StringComparison.OrdinalIgnoreCase))
            return false;
        string? action = RouteValue(context, "action");
        return string.Equals(action, "Root", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase) &&
               (!context.ActionArguments.TryGetValue("reportCode", out object? code) ||
                string.IsNullOrWhiteSpace(code as string));
    }

    private static string? ResolveReportCode(ActionExecutingContext context)
    {
        string? controller = RouteValue(context, "controller");
        string? action = RouteValue(context, "action");
        if (controller is null) return null;
        if (ControllerReports.TryGetValue(controller, out string? code)) return code;
        if (!string.Equals(controller, "Report", StringComparison.OrdinalIgnoreCase)) return null;
        if (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase))
            return context.ActionArguments.TryGetValue("reportCode", out object? reportCode)
                ? reportCode as string : null;
        if (string.Equals(action, "GetReportData", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(action, "Export", StringComparison.OrdinalIgnoreCase))
            return context.ActionArguments.TryGetValue("searchCondition", out object? condition)
                ? (condition as SearchReportCondition)?.ReportCode : null;
        if (action is not null && ReportActions.TryGetValue(action, out code)) return code;
        string[] segments = context.HttpContext.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return segments.Length >= 3 &&
               string.Equals(segments[0], "Report", StringComparison.OrdinalIgnoreCase) &&
               segments[1].StartsWith('C') &&
               !string.Equals(segments[1], "Export", StringComparison.OrdinalIgnoreCase)
            ? segments[1] : null;
    }

    private static ReportAction ResolveRequiredAction(ActionExecutingContext context)
    {
        string? controller = RouteValue(context, "controller");
        string? action = RouteValue(context, "action");
        if (string.Equals(controller, "Report", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action, "GetReportData", StringComparison.OrdinalIgnoreCase) &&
            context.ActionArguments.TryGetValue("searchCondition", out object? condition) &&
            (condition as SearchReportCondition)?.ForceRebuild == true)
            return ReportAction.Rebuild;
        if (string.Equals(controller, "Report", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action, "Export", StringComparison.OrdinalIgnoreCase) &&
            context.ActionArguments.TryGetValue("searchCondition", out object? exportCondition) &&
            (exportCondition as SearchReportCondition)?.ForceRebuild == true)
            return ReportAction.Excel | ReportAction.Rebuild;
        if (string.Equals(controller, "SapInterface", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action, "Run", StringComparison.OrdinalIgnoreCase))
            return ReportAction.Rebuild;
        if (string.Equals(action, "ExportPdf", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(action, "Receipt", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(action, "Receipts", StringComparison.OrdinalIgnoreCase) ||
            action?.StartsWith("Preview", StringComparison.OrdinalIgnoreCase) == true)
            return ReportAction.Print;
        if (string.Equals(action, "Export", StringComparison.OrdinalIgnoreCase))
            return ReportAction.Excel;
        return ReportAction.Query;
    }

    private static string? RouteValue(ActionExecutingContext context, string key) =>
        context.ActionDescriptor.RouteValues.TryGetValue(key, out string? value) ? value : null;
}
