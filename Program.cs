using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services
    .AddOptions<DatabaseConnectionOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseConnectionOptions.SectionName))
    .Validate(
        options => IsEndpointConfigured(options.GuidAp01),
        "DatabaseConnections:GuidAp01 必須設定 DatabaseName 與 ApplicationName。")
    .Validate(
        options => IsEndpointConfigured(options.DbTest3),
        "DatabaseConnections:DbTest3 必須設定 DatabaseName 與 ApplicationName。")
    .ValidateOnStart();
builder.Services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();
builder.Services.AddSingleton<IHealthCenterRepository, HealthCenterRepository>();
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

static bool IsEndpointConfigured(DatabaseEndpointOptions endpoint)
{
    return !string.IsNullOrWhiteSpace(endpoint.DatabaseName)
        && !string.IsNullOrWhiteSpace(endpoint.ApplicationName);
}
