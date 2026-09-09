# Report Catalog Specification

## Purpose

定義第一階段報表網站應保留的舊系統報表目錄、分類及目前可用狀態。

## Requirements

### Requirement: Outpatient accounting report catalog

「門診批價統計報表」分類 SHALL 包含下列群組與報表代碼：

- 主群組：`C1`
- 會計報表：`C21`、`C22`、`C23`、`C24`、`C25`、`C27`、`C28`、`C29`、`C211`、`C212`、`C213`、`C214`
- 計價、材料與明細報表：`C3`、`C4`、`C5`、`C6`、`C7`、`C8`、`C9`
- 應收、收據、社服與催款報表：`C10`、`C11`、`C12`、`C13`、`C141`、`C142`、`C143`、`C144`、`C15`、`C16`
- 健康管理中心及其他報表：`C171`、`C172`、`C173`、`C174`、`C18`、`C19`

#### Scenario: Browse the outpatient report catalog

- **WHEN** 使用者選擇「門診批價統計報表」
- **THEN** 系統 SHALL 依上述群組呈現報表代碼與名稱

---
### Requirement: Medical statistics catalog

「醫務統計報表」分類 SHALL 包含下列既有功能，並由產品維護其穩定代碼：

- `M1`：醫師看診人數日表
- `M2`：醫師看診人數月表
- `M3`：門急診日報表

#### Scenario: Browse medical statistics

- **WHEN** 使用者選擇「醫務統計報表」
- **THEN** 系統 SHALL 顯示醫師看診人數日表、醫師看診人數月表及門急診日報表

---
### Requirement: Preserve inactive legacy entry status

舊系統殘留代碼 `C26` SHALL NOT 出現在一般使用者的正常報表目錄，除非業務單位另行決定恢復或取代。

#### Scenario: Load the normal report catalog

- **WHEN** 一般使用者開啟報表目錄
- **THEN** 系統 SHALL NOT 顯示 `C26`

---
### Requirement: Data query category status

「資料查詢」分類 SHALL 保留為第一層入口，但在功能規格完成前 SHALL 標示為規劃中。

#### Scenario: Open data query category

- **WHEN** 使用者選擇「資料查詢」
- **THEN** 系統 SHALL 顯示功能仍在規劃或建置中的訊息

---
### Requirement: Available C23 catalog entry
The outpatient accounting report catalog SHALL expose C23 as an available "合約單位記帳表" entry and SHALL route selection to the C23-specific query form.

#### Scenario: Select C23 from catalog
- **WHEN** a user selects C23 in the outpatient accounting report catalog
- **THEN** the application displays the C23-specific query controls and permits a query submission

<!-- @trace
source: add-c23-contract-accounting-report
updated: 2026-09-09
code:
  - Repositories/ISurgicalAccountingRepository.cs
  - OpdAccrRptWeb.Tests/ReferralMemberRepositoryTests.cs
  - OpdAccrRptWeb.Tests/C23RebuildServiceTests.cs
  - Repositories/IC21AccountingSummaryRepository.cs
  - Services/C21RebuildForbiddenException.cs
  - Sql/C23/C23_I_encounter_date.sql
  - Services/C21AccountingSummaryCalculationService.cs
  - Sql/C23/C23_O_encounter_date.sql
  - ViewModels/ReportExportJobResponse.cs
  - Sql/C23/C23_O_balance42_rebuild.sql
  - OpdAccrRptWeb.Tests/BackgroundReportExportServiceTests.cs
  - OpdAccrRptWeb.Tests/C21AccountingSummaryRepositoryTests.cs
  - Repositories/InpatientReceivableBalanceRepository.cs
  - Services/IReportExportService.cs
  - OpdAccrRptWeb.Tests/ReportExportOptionsTests.cs
  - Services/ReportExportService.cs
  - OpdAccrRptWeb.Tests/CashierCashSummaryReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C21AccountingSummaryCalculationServiceTests.cs
  - OpdAccrRptWeb.Tests/C23AccountingCalculationServiceTests.cs
  - OpdAccrRptWeb.Tests/ReportExportServiceTests.cs
  - Services/IReportTotalCountCache.cs
  - Services/C23RebuildForbiddenException.cs
  - OpdAccrRptWeb.Tests/ReportExportJobStoreTests.cs
  - Properties/AssemblyInfo.cs
  - Services/C23RebuildAuthorizationService.cs
  - Sql/C23/C23_I_general_balance.sql
  - ViewModels/HealthCenterContractBillingReport.cs
  - Repositories/C23ContractAccountingRepository.cs
  - Views/Report/Index.cshtml
  - ViewModels/SearchReportCondition.cs
  - ViewModels/HealthCheckupVisits.cs
  - Repositories/SurgicalAccountingRepository.cs
  - Repositories/C23RebuildSql.cs
  - Views/Report/_TableSkeleton.cshtml
  - ViewModels/InpatientAdvancePaymentBalanceReportViewModel.cs
  - Sql/C23/C23_O_04_noncredit_drg.sql
  - OpdAccrRptWeb.Tests/ReportServiceTests.cs
  - Repositories/IOutpatientReceivableBalanceRepository.cs
  - ViewModels/SafeNeedleReportViewModel.cs
  - OpdAccrRptWeb.Tests/OutpatientReceivableBalanceRepositoryTests.cs
  - document/C22.md
  - Services/C23AccountingCalculationService.cs
  - OpdAccrRptWeb.Tests/InpatientAdvancePaymentBalanceRepositoryTests.cs
  - OpdAccrRptWeb.Tests/OpdAccrRptWeb.Tests.csproj
  - Services/IC23RebuildAuthorizationService.cs
  - Sql/C23/C23_I_03_noncredit_ord.sql
  - OpdAccrRptWeb.csproj
  - Repositories/SafeNeedleRepository.cs
  - OpdAccrRptWeb.Tests/AssistiveDeviceDepositBalanceRepositoryTests.cs
  - Repositories/ContractPaymentDetailRepository.cs
  - Services/IC21RebuildAuthorizationService.cs
  - Services/IC21RebuildService.cs
  - Repositories/CashierCashSummaryRepository.cs
  - ViewModels/OutpatientReceivableBalanceReportViewModel.cs
  - Services/BackgroundReportExportService.cs
  - OpdAccrRptWeb.Tests/ReportTotalCountCacheTests.cs
  - Services/IC23UserIdentityProvider.cs
  - appsettings.json
  - OpdAccrRptWeb.Tests/ConnectionStringProviderTests.cs
  - Services/ReportExportJobStore.cs
  - Repositories/IContractPaymentDetailRepository.cs
  - Services/ConfiguredC23UserIdentityProvider.cs
  - Repositories/ICashierCashRepository.cs
  - Repositories/IInpatientReceivableBalanceRepository.cs
  - Sql/C23/C23_I_01_credit_ord.sql
  - Repositories/IInpatientAdvancePaymentBalanceRepository.cs
  - OpdAccrRptWeb.Tests/InpatientReceivableBalanceRepositoryTests.cs
  - Services/ReportTotalCountCache.cs
  - OpdAccrRptWeb.Tests/FileLoggingTests.cs
  - Repositories/ICashierCashSummaryRepository.cs
  - Repositories/ReferralMemberRepository.cs
  - .tmp/crash-cache-backup-20260828-1608/rjsmrazor.dswa.cache.json
  - OpdAccrRptWeb.Tests/C21RebuildServiceTests.cs
  - OpdAccrRptWeb.Tests/OutpatientReceivableBalanceReportServiceTests.cs
  - Services/C21RebuildAuthorizationService.cs
  - OpdAccrRptWeb.Tests/InpatientAdvancePaymentBalanceReportServiceTests.cs
  - Repositories/OutpatientReceivableBalanceRepository.cs
  - Services/C23RebuildService.cs
  - ViewModels/C21AccountingSummaryReportViewModel.cs
  - ViewModels/CashierCashReportViewModel.cs
  - Controllers/ReportController.cs
  - ViewModels/ContractPaymentDetailReportViewModel.cs
  - ViewModels/InpatientReceivableBalanceReportViewModel.cs
  - ViewModels/ReportIndexViewModel.cs
  - wwwroot/js/reports/report-template.js
  - ViewModels/AssistiveDeviceDepositBalanceReportViewModel.cs
  - OpdAccrRptWeb.Tests/CashierCashSummaryRepositoryTests.cs
  - .tmp/crash-cache-backup-20260828-1608/rpswa.dswa.cache.json
  - Services/IC23RebuildService.cs
  - ViewModels/C23ContractAccountingReportViewModel.cs
  - OpdAccrRptWeb.Tests/GlobalUsings.cs
  - OpdAccrRptWeb.Tests/ContractPaymentDetailRepositoryTests.cs
  - .tmp/crash-cache-backup-20260828-1608/rjsmcshtml.dswa.cache.json
  - OpdAccrRptWeb.Tests/InpatientReceivableBalanceReportServiceTests.cs
  - OpdAccrRptWeb.Tests/CashierCashReportServiceTests.cs
  - OpdAccrRptWeb.Tests/TestDoubles.cs
  - OpdAccrRptWeb.Tests/HealthCenterRepositoryTests.cs
  - Services/C21Options.cs
  - Services/ReportService.cs
  - Sql/C23/C23_O_01_credit_ord.sql
  - Repositories/InpatientAdvancePaymentBalanceRepository.cs
  - Repositories/C23SourceSql.cs
  - OpdAccrRptWeb.Tests/SafeNeedleRepositoryTests.cs
  - ViewModels/HealthCenterDetailViewModel.cs
  - Repositories/CashierCashRepository.cs
  - ViewModels/PagedReportResult.cs
  - Program.cs
  - Services/IC21UserIdentityProvider.cs
  - OpdAccrRptWeb.Tests/ContractPaymentDetailReportServiceTests.cs
  - OpdAccrRptWeb.Tests/CashierCashRepositoryTests.cs
  - Services/C21RebuildService.cs
  - Repositories/IAssistiveDeviceDepositBalanceRepository.cs
  - Repositories/IReferralMemberRepository.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - Repositories/ISafeNeedleRepository.cs
  - Sql/C23/C23_I_02_credit_drg.sql
  - Repositories/C21AccountingSummaryRepository.cs
  - OpdAccrRptWeb.Tests/C21AccountingSummaryReportServiceTests.cs
  - ViewModels/CashierCashSummaryReportViewModel.cs
  - OpdAccrRptWeb.Tests/C23ContractAccountingRepositoryTests.cs
  - Services/ConfiguredC21UserIdentityProvider.cs
  - ViewModels/ReferralMemberReportViewModel.cs
  - OpdAccrRptWeb.Tests/SurgicalAccountingRepositoryTests.cs
  - Services/IC21AccountingSummaryCalculationService.cs
  - Repositories/AssistiveDeviceDepositBalanceRepository.cs
  - Infrastructure/FileLoggingConfiguration.cs
  - Sql/C23/C23_I_04_noncredit_drg.sql
  - Services/C23Options.cs
  - Sql/C23/C23_O_03_noncredit_ord.sql
  - Views/Report/_TemplateReport.cshtml
  - Sql/C23/C23_O_02_credit_drg.sql
  - .tmp/crash-cache-backup-20260828-1608/staticwebassets.upToDateCheck.txt
  - Repositories/C21RebuildSql.cs
  - wwwroot/js/report-app.js
  - Services/ReportExportOptions.cs
  - Repositories/IC23ContractAccountingRepository.cs
  - Services/ReportExportOptionsValidator.cs
  - Sql/C23/C23_O_general_balance.sql
  - Sql/C23/C23_I_05_discharge_snapshot.sql
  - Sql/C23/C23_I_balance42_rebuild.sql
  - ViewModels/HealthCenterCountViewModel.cs
  - OpdAccrRptWeb.Tests/AssistiveDeviceDepositBalanceReportServiceTests.cs
  - ViewModels/SurgicalAccountingReportViewModel.cs
tests:
  - OpdAccrRptWeb.Tests/report-template.test.js
-->