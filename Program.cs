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
builder.Services
    .AddOptions<DatabaseConnectionOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseConnectionOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<DatabaseConnectionOptions>, DatabaseConnectionOptionsValidator>();
builder.Services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
builder.Services.AddSingleton<IHealthCenterRepository, HealthCenterRepository>();
builder.Services.AddSingleton<IReferralMemberRepository, ReferralMemberRepository>();
builder.Services.AddSingleton<IReportTotalCountCache, ReportTotalCountCache>();
builder.Services.AddSingleton<IReportService, ReportService>();
builder.Services.AddSingleton<IReportCatalogService, ReportCatalogService>();

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
