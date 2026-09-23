using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = FileLoggingConfiguration.CreateLogger(
    builder.Configuration,
    builder.Environment.ContentRootPath,
    builder.Environment.IsDevelopment());
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
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
builder.Services.AddSingleton<ISurgicalAccountingRepository, SurgicalAccountingRepository>();
builder.Services.AddSingleton<ICashierCashRepository, CashierCashRepository>();
builder.Services.AddSingleton<ICashierCashSummaryRepository, CashierCashSummaryRepository>();
builder.Services.AddSingleton<IC21AccountingSummaryRepository, C21AccountingSummaryRepository>();
builder.Services.AddSingleton<IC23ContractAccountingRepository, C23ContractAccountingRepository>();
builder.Services.AddSingleton<IC24DebtPaymentRepository, C24DebtPaymentRepository>();
builder.Services.AddScoped<IC10ReceivableDetailRepository, C10ReceivableDetailRepository>();
builder.Services.AddScoped<IC11ReceivablesCollectionRepository, C11ReceivablesCollectionRepository>();
builder.Services.AddScoped<IC12ReportRepository, C12ReportRepository>();
builder.Services.AddScoped<IC13HighRiskEmergencyRepository, C13HighRiskEmergencyRepository>();
builder.Services.AddScoped<IC143AccountingBalanceDebtRepository, C143AccountingBalanceDebtRepository>();
builder.Services.AddScoped<IC144DebtDetailReportRepository, C144DebtDetailReportRepository>();
builder.Services.AddSingleton<IOutpatientReceivableBalanceRepository, OutpatientReceivableBalanceRepository>();
builder.Services.AddSingleton<IInpatientAdvancePaymentBalanceRepository, InpatientAdvancePaymentBalanceRepository>();
builder.Services.AddSingleton<IAssistiveDeviceDepositBalanceRepository, AssistiveDeviceDepositBalanceRepository>();
builder.Services.AddScoped<IC15AssistiveDeviceDepositDetailRepository, C15AssistiveDeviceDepositDetailRepository>();
builder.Services.AddScoped<IC16ReportRepository, C16ReportRepository>();
builder.Services.AddC3ReportServices();
builder.Services.AddScoped<IC4MaterialReportRepository, C4MaterialReportRepository>();
builder.Services.AddScoped<IC4MaterialReportService, C4MaterialReportService>();
builder.Services.AddScoped<IC5ReportRepository, C5ReportRepository>();
builder.Services.AddScoped<IC5ReportService, C5ReportService>();
builder.Services.AddSingleton<IC5ReportResultCache, C5ReportResultCache>();
builder.Services.AddSingleton<IC5PatientAccessAuditWriter, C5SerilogPatientAccessAuditWriter>();
builder.Services.AddScoped<IC7ReportRepository, C7ReportRepository>();
builder.Services.AddScoped<IC7ReportService, C7ReportService>();
builder.Services.AddSingleton<IC7ReportResultCache, C7ReportResultCache>();
builder.Services.AddSingleton<IC7PatientAccessAuditWriter, C7SerilogPatientAccessAuditWriter>();
builder.Services.AddScoped<IC8ReportRepository, C8ReportRepository>();
builder.Services.AddScoped<IC8ReportService, C8ReportService>();
builder.Services.AddSingleton<IC8ReportResultCache, C8ReportResultCache>();
builder.Services.AddSingleton<IC8PatientAccessAuditWriter, C8SerilogPatientAccessAuditWriter>();
builder.Services.AddScoped<IC9ReportRepository, C9ReportRepository>();
builder.Services.AddScoped<IC9ReportService, C9ReportService>();
builder.Services.AddSingleton<IC9ReportResultCache, C9ReportResultCache>();
builder.Services.AddSingleton<IC9PatientAccessAuditWriter, C9SerilogPatientAccessAuditWriter>();
builder.Services.AddSingleton<IC9TransientFailurePolicy, C9OracleFailurePolicy>();
builder.Services.AddSingleton<IC4TransientFailurePolicy, C4OracleFailurePolicy>();
builder.Services.AddSingleton<IC4MaterialReportRenderer, C4MaterialReportRenderer>();
builder.Services.AddSingleton<IInpatientReceivableBalanceRepository, InpatientReceivableBalanceRepository>();
builder.Services.AddSingleton<IContractPaymentDetailRepository, ContractPaymentDetailRepository>();
builder.Services.AddScoped<IC211ContractBalanceRepository, C211ContractBalanceRepository>();
builder.Services.AddScoped<IC212BoneBankBalanceRepository, C212BoneBankBalanceRepository>();
builder.Services.AddSingleton<IReportTotalCountCache, ReportTotalCountCache>();
builder.Services.AddSingleton<IC21AccountingSummaryCalculationService, C21AccountingSummaryCalculationService>();
builder.Services.AddSingleton<IC24DebtPaymentCalculationService, C24DebtPaymentCalculationService>();
builder.Services.AddSingleton<IC10AmountCalculationService, C10AmountCalculationService>();
builder.Services.AddSingleton<IC10PatientAccessAuditWriter, C10SerilogPatientAccessAuditWriter>();
builder.Services.AddScoped<IC11ReceivablesCollectionReportService, C11ReceivablesCollectionReportService>();
builder.Services.AddSingleton<IC12LegacyAmountConverter, C12LegacyAmountConverter>();
builder.Services.AddSingleton<IC12PatientAccessAuthorizer, AllowConfiguredC12PatientAccessAuthorizer>();
builder.Services.AddSingleton<IC12PatientAccessAuditWriter, C12SerilogPatientAccessAuditWriter>();
builder.Services.AddScoped<IC12ReportService, C12ReportService>();
builder.Services.AddSingleton<IC13LegacyPhoneMasker, C13LegacyPhoneMasker>();
builder.Services.AddSingleton<IC15LegacyReducer, C15LegacyReducer>();
builder.Services.AddSingleton<IC16LegacyReducer, C16LegacyReducer>();
builder.Services.AddScoped<IC16ReportService, C16ReportService>();
builder.Services.AddScoped<IC13HighRiskEmergencyReportService, C13HighRiskEmergencyReportService>();
builder.Services.AddScoped<IC143AccountingBalanceDebtReportService, C143AccountingBalanceDebtReportService>();
builder.Services.AddScoped<IC144DebtDetailReportService, C144DebtDetailReportService>();
builder.Services.AddSingleton<IC144XlsxRenderer, C144XlsxRenderer>();
builder.Services.AddScoped<IC211ContractBalanceReportService, C211ContractBalanceReportService>();
builder.Services.AddSingleton<IC212AmountCompatibilityPolicy, C212PreserveDecimalAmountPolicy>();
builder.Services.AddScoped<IC212BoneBankBalanceReportService, C212BoneBankBalanceReportService>();
builder.Services.AddSingleton<IC24CanonicalResultRenderer, C24HtmlRenderer>();
builder.Services.AddSingleton<IC24CanonicalResultRenderer, C24JsonRenderer>();
builder.Services.AddSingleton<IC24CanonicalResultRenderer, C24PdfRenderer>();
builder.Services.AddSingleton<IC24CanonicalResultRenderer, C24XlsxRenderer>();
builder.Services.AddSingleton<IC21UserIdentityProvider, ConfiguredC21UserIdentityProvider>();
builder.Services.AddSingleton<IC21RebuildAuthorizationService, C21RebuildAuthorizationService>();
builder.Services.AddSingleton<IC21RebuildService, C21RebuildService>();
builder.Services.AddOptions<C21Options>()
    .Bind(builder.Configuration.GetSection(C21Options.SectionName));
builder.Services.AddSingleton<IC23UserIdentityProvider, ConfiguredC23UserIdentityProvider>();
builder.Services.AddSingleton<IC23RebuildAuthorizationService, C23RebuildAuthorizationService>();
builder.Services.AddSingleton<IC23RebuildService, C23RebuildService>();
builder.Services.AddOptions<C23Options>()
    .Bind(builder.Configuration.GetSection(C23Options.SectionName));
builder.Services.AddScoped<IReportService, ReportService>();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Report}/{action=Index}/{id?}");


app.Run();

public partial class Program;
