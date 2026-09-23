# Report Catalog Specification

## Purpose

定義第一階段報表網站應保留的舊系統報表目錄、分類及目前可用狀態。

## Requirements

### Requirement: Available C144 catalog entry
The outpatient accounting report catalog SHALL expose C144 as an available `欠款明細報表` entry and SHALL route selection to the C144-specific query controls.

#### Scenario: Select C144 from the catalog
- **WHEN** a user selects C144 in the outpatient accounting report catalog
- **THEN** the application displays Gregorian start and end dates plus outpatient/emergency and inpatient source controls
- **AND** the application permits a validated C144 query submission

---
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

---
### Requirement: Available C4 catalog entry
The outpatient accounting report catalog SHALL expose C4 as an available `門急診材料寄售表` entry and SHALL route selection to the C4-specific Gregorian date and organization-unit query controls.

#### Scenario: Select C4 from the catalog
- **WHEN** a user selects C4 in the report catalog
- **THEN** the application opens the C4 query page and permits an authorized validated query

<!-- @trace
source: add-c4-material-consignment-report
updated: 2026-09-18
code:
  - Services/OrganizationUnitCodeService.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportRendererTests.cs
  - wwwroot/css/site.css
  - wwwroot/js/reports/report-template.js
  - ViewModels/C4MaterialReportViewModel.cs
  - Controllers/ReportController.cs
  - Sql/C4.Report.Daily.sql
  - Repositories/C4MaterialReportRepository.cs
  - OpdAccrRptWeb.csproj
  - Repositories/IC4MaterialReportRepository.cs
  - Services/ReportCatalogService.cs
  - Views/C4/Preview.cshtml
  - Views/Report/Index.cshtml
  - Services/C4MaterialReportService.cs
  - Services/IC4MaterialReportRenderer.cs
  - Models/C4MaterialReportModels.cs
  - OpdAccrRptWeb.Tests/C3CoreTests.cs
  - Views/Report/_TemplateReport.cshtml
  - Models/OrganizationUnitModels.cs
  - Services/C4MaterialReportRenderer.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportTests.cs
  - wwwroot/js/report-app.js
  - Repositories/OrganizationUnitMappingRepository.cs
  - Program.cs
  - Services/C4OracleFailurePolicy.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - Repositories/IOrganizationUnitMappingRepository.cs
  - Services/IC4MaterialReportService.cs
  - Controllers/C4MaterialReportController.cs
  - Services/IOrganizationUnitCodeService.cs
tests:
  - OpdAccrRptWeb.Tests/c4-report.test.js
-->

---
### Requirement: Available C3 catalog entry
The outpatient accounting report catalog SHALL expose C3 as an available `各護理站計價品彙總明細表` entry and SHALL route selection to C3-specific query controls.

#### Scenario: Select C3 from the catalog
- **WHEN** a user selects C3 in the report catalog
- **THEN** the application displays Gregorian start and end dates, source, detail type, logistics type, department, room-code, and charge-code controls
- **AND** the application permits a validated C3 query submission

<!-- @trace
source: add-c3-nursing-station-charge-report
updated: 2026-09-18
code:
  - Program.cs
  - Controllers/C4MaterialReportController.cs
  - Services/ReportCatalogService.cs
  - ViewModels/C4MaterialReportViewModel.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportRendererTests.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - Services/IOrganizationUnitCodeService.cs
  - Models/C4MaterialReportModels.cs
  - Views/Report/Index.cshtml
  - wwwroot/js/report-app.js
  - Sql/C4.Report.Daily.sql
  - OpdAccrRptWeb.csproj
  - OpdAccrRptWeb.Tests/C4MaterialReportTests.cs
  - Services/IC4MaterialReportRenderer.cs
  - Views/Report/_TemplateReport.cshtml
  - Models/OrganizationUnitModels.cs
  - Repositories/IOrganizationUnitMappingRepository.cs
  - Services/IC4MaterialReportService.cs
  - Repositories/OrganizationUnitMappingRepository.cs
  - Services/C4MaterialReportRenderer.cs
  - Services/C4OracleFailurePolicy.cs
  - OpdAccrRptWeb.Tests/C3CoreTests.cs
  - Views/C4/Preview.cshtml
  - wwwroot/css/site.css
  - wwwroot/js/reports/report-template.js
  - Repositories/IC4MaterialReportRepository.cs
  - Services/C4MaterialReportService.cs
  - Services/OrganizationUnitCodeService.cs
  - Repositories/C4MaterialReportRepository.cs
tests:
  - OpdAccrRptWeb.Tests/c4-report.test.js
-->

---
### Requirement: Available C16 catalog entry
The outpatient accounting report catalog SHALL expose C16 as an available `新北市醫療補助費用申請總表` entry and route selection to C16-specific controls for Gregorian dates, source, report type, and date basis. Selecting inpatient SHALL make accounting date the effective basis and indicate that visit date does not apply.

#### Scenario: Select C16 from the catalog
- **WHEN** a user selects C16
- **THEN** the application displays the C16 controls and permits a validated query submission

#### Scenario: Select inpatient source
- **WHEN** a user selects inpatient on the C16 form
- **THEN** the form displays accounting date as the effective date basis

<!-- @trace
source: add-c16-new-taipei-medical-subsidy-summary
updated: 2026-09-18
code:
  - ViewModels/C4MaterialReportViewModel.cs
  - Repositories/IC4MaterialReportRepository.cs
  - Program.cs
  - Services/C4MaterialReportService.cs
  - Sql/C4.Report.Daily.sql
  - Repositories/IOrganizationUnitMappingRepository.cs
  - OpdAccrRptWeb.csproj
  - Services/OrganizationUnitCodeService.cs
  - Services/IC4MaterialReportService.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - Views/C4/Preview.cshtml
  - Models/OrganizationUnitModels.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportRendererTests.cs
  - Repositories/OrganizationUnitMappingRepository.cs
  - wwwroot/js/report-app.js
  - Controllers/ReportController.cs
  - Services/C4MaterialReportRenderer.cs
  - Controllers/C4MaterialReportController.cs
  - Services/IC4MaterialReportRenderer.cs
  - Views/Report/_TemplateReport.cshtml
  - wwwroot/css/site.css
  - wwwroot/js/reports/report-template.js
  - OpdAccrRptWeb.Tests/C3CoreTests.cs
  - Services/ReportCatalogService.cs
  - Repositories/C4MaterialReportRepository.cs
  - Services/IOrganizationUnitCodeService.cs
  - Services/C4OracleFailurePolicy.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportTests.cs
  - Views/Report/Index.cshtml
  - Models/C4MaterialReportModels.cs
tests:
  - OpdAccrRptWeb.Tests/c4-report.test.js
-->

---
### Requirement: Catalog exposes C15 assistive-device deposit detail
The report catalog SHALL list C15 with the Traditional Chinese name 社工輔助器具保證金明細表 in the established report category and SHALL route selection to the C15 query experience.

#### Scenario: User selects C15
- **WHEN** the user selects C15 from the report catalog
- **THEN** the page displays the C15 start-date and end-date query controls

<!-- @trace
source: add-c15-assistive-device-deposit-detail
updated: 2026-09-18
code:
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - Models/OrganizationUnitModels.cs
  - Services/IC4MaterialReportService.cs
  - Services/OrganizationUnitCodeService.cs
  - Views/Report/_TemplateReport.cshtml
  - Repositories/C4MaterialReportRepository.cs
  - ViewModels/C4MaterialReportViewModel.cs
  - Services/C4MaterialReportService.cs
  - Services/IOrganizationUnitCodeService.cs
  - Repositories/OrganizationUnitMappingRepository.cs
  - Services/C4MaterialReportRenderer.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportRendererTests.cs
  - Services/C4OracleFailurePolicy.cs
  - OpdAccrRptWeb.Tests/C3CoreTests.cs
  - Views/C4/Preview.cshtml
  - Services/ReportCatalogService.cs
  - wwwroot/css/site.css
  - OpdAccrRptWeb.Tests/C4MaterialReportTests.cs
  - Views/Report/Index.cshtml
  - Sql/C4.Report.Daily.sql
  - Controllers/ReportController.cs
  - wwwroot/js/reports/report-template.js
  - Program.cs
  - Models/C4MaterialReportModels.cs
  - Repositories/IC4MaterialReportRepository.cs
  - OpdAccrRptWeb.csproj
  - Controllers/C4MaterialReportController.cs
  - Repositories/IOrganizationUnitMappingRepository.cs
  - Services/IC4MaterialReportRenderer.cs
  - wwwroot/js/report-app.js
tests:
  - OpdAccrRptWeb.Tests/c4-report.test.js
-->

---
### Requirement: Available C5 catalog entry
The outpatient accounting report catalog SHALL expose C5 as an available `批價數量查詢表` entry and SHALL route selection to the C5-specific query controls.

#### Scenario: Select C5 from the catalog
- **WHEN** a user selects C5 in the report catalog
- **THEN** the application displays Gregorian date, source, detail type, encounter type, charge kind, a C4-style new organization-unit selector, room, charge-code, and the constrained `健保身份` selector
- **AND** the application permits a validated C5 query submission

<!-- @trace
source: add-c5-charge-quantity-report
updated: 2026-09-22
code:
  - Services/C7ReportService.cs
  - ViewModels/C7DailyChargeDetailViewModel.cs
  - Program.cs
  - Views/Report/_C7DailyChargeDetailReport.cshtml
  - wwwroot/js/reports/c7-report.js
  - Repositories/C7ReportRepository.cs
  - Controllers/C7ReportController.cs
  - Services/IC7ReportService.cs
  - OpdAccrRptWeb.Tests/C7RequestValidationTests.cs
  - Services/C7PatientAccessAudit.cs
  - OpdAccrRptWeb.Tests/C7ReportServiceTests.cs
  - Repositories/C7Sql.cs
  - Views/C7/Preview.cshtml
  - Views/Report/Index.cshtml
  - wwwroot/js/report-app.js
  - Models/C7ReportModels.cs
  - OpdAccrRptWeb.Tests/C7ReportControllerTests.cs
  - Services/C7AmountPolicy.cs
  - Services/C7ReportResultCache.cs
  - Repositories/IC7ReportRepository.cs
  - OpdAccrRptWeb.Tests/C7ReportRepositoryTests.cs
  - package.json
tests:
  - OpdAccrRptWeb.Tests/c7-report.test.js
-->

---
### Requirement: C8 catalog entry is available
The outpatient accounting report catalog SHALL expose C8 as an available `批價補帳明細表` entry and SHALL route selection to the independent C8 report controller.

#### Scenario: Select C8 from the catalog
- **WHEN** a user selects C8
- **THEN** the browser navigates to the C8 query page

<!-- @trace
source: add-c8-patch-bill-detail-report
updated: 2026-09-23
code:
  - OpdAccrRptWeb.Tests/C7ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/C9ReportServiceTests.cs
  - Services/C7ReportResultCache.cs
  - Views/C7/Preview.cshtml
  - Services/IC7ReportService.cs
  - Repositories/C9ReportRepository.cs
  - Models/C8ReportModels.cs
  - Views/Report/_C7DailyChargeDetailReport.cshtml
  - Repositories/C9Sql.cs
  - OpdAccrRptWeb.Tests/C8ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C8RequestValidationTests.cs
  - Repositories/C8Sql.cs
  - Services/C9PatientAccessAudit.cs
  - Views/Report/_TemplateReport.cshtml
  - Services/C9OracleFailurePolicy.cs
  - Controllers/C8ReportController.cs
  - Services/C7PatientAccessAudit.cs
  - Services/C8ReportResultCache.cs
  - wwwroot/js/reports/c8-report.js
  - OpdAccrRptWeb.Tests/C8ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/C9ReportRepositoryTests.cs
  - OpdAccrRptWeb.Tests/C8ReportRepositoryTests.cs
  - Views/Report/_C8PatchBillDetailReport.cshtml
  - wwwroot/js/reports/c7-report.js
  - OpdAccrRptWeb.Tests/C7ReportServiceTests.cs
  - Services/C7ReportService.cs
  - ViewModels/C7DailyChargeDetailViewModel.cs
  - Models/C9ReportModels.cs
  - OpdAccrRptWeb.Tests/C7ReportRepositoryTests.cs
  - Services/C7AmountPolicy.cs
  - wwwroot/js/reports/c9-report.js
  - Repositories/IC8ReportRepository.cs
  - ViewModels/C9MaterialAccountingMonthlyViewModel.cs
  - wwwroot/js/report-app.js
  - OpdAccrRptWeb.Tests/C9RequestValidationTests.cs
  - Services/C8PatientAccessAudit.cs
  - OpdAccrRptWeb.Tests/C7RequestValidationTests.cs
  - Views/Report/Index.cshtml
  - Services/IC9ReportService.cs
  - Views/Report/_C9MaterialAccountingMonthlyReport.cshtml
  - Services/C8ReportService.cs
  - Controllers/C9ReportController.cs
  - Services/C9ReportService.cs
  - Services/IC8ReportService.cs
  - Views/Report/_C5ChargeQuantityReport.cshtml
  - wwwroot/css/site.css
  - Views/C9/Preview.cshtml
  - Repositories/C7ReportRepository.cs
  - wwwroot/js/reports/c5-report.js
  - Models/C7ReportModels.cs
  - OpdAccrRptWeb.Tests/C9ReportControllerTests.cs
  - Views/C8/Preview.cshtml
  - Program.cs
  - Repositories/C7Sql.cs
  - Repositories/IC9ReportRepository.cs
  - ViewModels/C8PatchBillDetailViewModel.cs
  - package.json
  - Controllers/C7ReportController.cs
  - Repositories/IC7ReportRepository.cs
  - Repositories/C8ReportRepository.cs
  - Services/C9ReportResultCache.cs
  - wwwroot/js/reports/report-template.js
tests:
  - OpdAccrRptWeb.Tests/c3-report.test.js
  - OpdAccrRptWeb.Tests/c9-report.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
  - OpdAccrRptWeb.Tests/c7-report.test.js
  - OpdAccrRptWeb.Tests/c5-report.test.js
  - OpdAccrRptWeb.Tests/c8-report.test.js
-->

---
### Requirement: C9 catalog entry is available
The report catalog SHALL expose report C9 as `維康耗材記帳月報表`, mark it available, and associate it with the independent C9 report route.

#### Scenario: User selects C9 from the catalog
- **WHEN** a user views the report catalog and selects C9
- **THEN** the application navigates to the available C9 query page with the specified display name

<!-- @trace
source: add-c9-material-accounting-monthly-report
updated: 2026-09-23
code:
  - package.json
  - Models/C7ReportModels.cs
  - Services/C7ReportResultCache.cs
  - Views/C8/Preview.cshtml
  - Controllers/C9ReportController.cs
  - Views/Report/_C7DailyChargeDetailReport.cshtml
  - Services/C8PatientAccessAudit.cs
  - Views/Report/_C5ChargeQuantityReport.cshtml
  - wwwroot/css/site.css
  - ViewModels/C8PatchBillDetailViewModel.cs
  - OpdAccrRptWeb.Tests/C8ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C8ReportRepositoryTests.cs
  - Program.cs
  - Views/Report/_C9MaterialAccountingMonthlyReport.cshtml
  - Services/C7PatientAccessAudit.cs
  - Services/C8ReportResultCache.cs
  - Services/C9ReportService.cs
  - Views/Report/_TemplateReport.cshtml
  - wwwroot/js/reports/c7-report.js
  - wwwroot/js/reports/c9-report.js
  - Controllers/C8ReportController.cs
  - OpdAccrRptWeb.Tests/C7RequestValidationTests.cs
  - wwwroot/js/report-app.js
  - OpdAccrRptWeb.Tests/C7ReportControllerTests.cs
  - Models/C9ReportModels.cs
  - OpdAccrRptWeb.Tests/C9ReportControllerTests.cs
  - Services/C9OracleFailurePolicy.cs
  - Repositories/C9Sql.cs
  - Models/C8ReportModels.cs
  - OpdAccrRptWeb.Tests/C9RequestValidationTests.cs
  - Services/C9PatientAccessAudit.cs
  - OpdAccrRptWeb.Tests/C7ReportServiceTests.cs
  - wwwroot/js/reports/report-template.js
  - OpdAccrRptWeb.Tests/C9ReportRepositoryTests.cs
  - Repositories/C7ReportRepository.cs
  - Repositories/IC7ReportRepository.cs
  - Services/C7ReportService.cs
  - OpdAccrRptWeb.Tests/C8ReportControllerTests.cs
  - Services/IC7ReportService.cs
  - OpdAccrRptWeb.Tests/C7ReportRepositoryTests.cs
  - Repositories/C8ReportRepository.cs
  - Controllers/C7ReportController.cs
  - Views/C9/Preview.cshtml
  - wwwroot/js/reports/c5-report.js
  - Services/C7AmountPolicy.cs
  - Repositories/C9ReportRepository.cs
  - Views/C7/Preview.cshtml
  - Views/Report/_C8PatchBillDetailReport.cshtml
  - OpdAccrRptWeb.Tests/C8RequestValidationTests.cs
  - Repositories/C7Sql.cs
  - Services/IC9ReportService.cs
  - ViewModels/C7DailyChargeDetailViewModel.cs
  - wwwroot/js/reports/c8-report.js
  - Repositories/IC9ReportRepository.cs
  - OpdAccrRptWeb.Tests/C9ReportServiceTests.cs
  - Repositories/C8Sql.cs
  - Repositories/IC8ReportRepository.cs
  - ViewModels/C9MaterialAccountingMonthlyViewModel.cs
  - Views/Report/Index.cshtml
  - Services/C9ReportResultCache.cs
  - Services/C8ReportService.cs
  - Services/IC8ReportService.cs
tests:
  - OpdAccrRptWeb.Tests/c9-report.test.js
  - OpdAccrRptWeb.Tests/c7-report.test.js
  - OpdAccrRptWeb.Tests/c8-report.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
  - OpdAccrRptWeb.Tests/c5-report.test.js
  - OpdAccrRptWeb.Tests/c3-report.test.js
-->