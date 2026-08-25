using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = FileLoggingConfiguration.CreateLogger(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    builder.Environment.IsDevelopment());
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services
    .AddOptions<ReportExportOptions>()
    .Bind(builder.Configuration.GetSection(ReportExportOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<ReportExportOptions>, ReportExportOptionsValidator>();
builder.Services
    .AddOptions<DatabaseConnectionOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseConnectionOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DatabaseConnectionOptions>, DatabaseConnectionOptionsValidator>();
builder.Services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
builder.Services.AddSingleton<IHealthCenterRepository, HealthCenterRepository>();
builder.Services.AddSingleton<IReferralMemberRepository, ReferralMemberRepository>();
builder.Services.AddSingleton<ISafeNeedleRepository, SafeNeedleRepository>();
builder.Services.AddSingleton<IReportTotalCountCache, ReportTotalCountCache>();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddSingleton<IReportCatalogService, ReportCatalogService>();
builder.Services.AddSingleton<IReportExportJobStore, ReportExportJobStore>();
builder.Services.AddSingleton<ReportExportWorkQueue>();
builder.Services.AddSingleton<IReportExportWorkQueue>(provider => provider.GetRequiredService<ReportExportWorkQueue>());
builder.Services.AddSingleton<IReportExportQueue>(provider => provider.GetRequiredService<ReportExportWorkQueue>());
builder.Services.AddSingleton<ReportExportService>();
builder.Services.AddSingleton<IReportExportService>(provider => provider.GetRequiredService<ReportExportService>());
builder.Services.AddSingleton<IReportWorkbookGenerator>(provider => provider.GetRequiredService<ReportExportService>());
builder.Services.AddHostedService<BackgroundReportExportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Report}/{action=Index}/{id?}");


app.Run();

public partial class Program;
