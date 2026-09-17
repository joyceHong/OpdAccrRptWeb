using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public static class C3ServiceCollectionExtensions
{
    public static IServiceCollection AddC3ReportServices(this IServiceCollection services)
    {
        services.AddScoped<IC3ReportRepository, C3ReportRepository>();
        services.AddScoped<ISectionMappingRepository, SectionMappingRepository>();
        services.AddScoped<IOrganizationUnitMappingRepository, OrganizationUnitMappingRepository>();
        services.AddScoped<DepartmentFilterResolver>();
        services.AddScoped<IOrganizationUnitCodeService, OrganizationUnitCodeService>();
        services.AddScoped<DepartmentAssignmentService>();
        services.AddScoped<IC3ReportService, C3ReportService>();
        return services;
    }
}
