# Report Navigation Specification

## Purpose

定義門急診報表網站的主要導覽、報表樹與報表路由行為。

## Requirements

### Requirement: Primary report categories

The system SHALL present four primary categories in the top navigation, in this order: `門診批價統計報表`, `醫務統計報表`, `資料查詢`, and `SAP介接作業`.

#### Scenario: Switch primary category

- **WHEN** a user selects a primary category
- **THEN** the system SHALL activate that category
- **AND** the sidebar SHALL display only the groups and entries belonging to that category

#### Scenario: Open SAP interface category

- **WHEN** a user selects `SAP介接作業` in the top navigation
- **THEN** the system SHALL activate `SAP介接作業` and select the SAP intermediate-table operation
- **AND** the system SHALL display the SAP operation workspace and a breadcrumb identifying `SAP介接作業` and `SAP 中介表作業`

#### Scenario: Open the existing SAP route directly

- **WHEN** a user opens or refreshes `/sap-interface`
- **THEN** the system SHALL activate `SAP介接作業` and select the SAP intermediate-table operation in the sidebar
- **AND** the system SHALL display the same SAP operation workspace


<!-- @trace
source: move-sap-interface-to-primary-menu
updated: 2026-10-19
code:
  - scripts/sql/create-opd-accrreport-permission.sql
-->

---
### Requirement: Hierarchical report menu

系統 SHALL 在左側以可展開及收合的階層選單呈現報表群組與報表。

#### Scenario: Expand a report group

- **WHEN** 使用者展開報表群組
- **THEN** 系統 SHALL 顯示該群組內各報表的舊系統報表代碼及名稱

#### Scenario: Collapse all report groups

- **WHEN** 使用者執行全部收合
- **THEN** 系統 SHALL 收合目前分類的所有報表群組

---
### Requirement: Search report catalog

系統 SHALL 允許使用者依報表代碼或報表名稱篩選左側報表清單。

#### Scenario: Filter reports by keyword

- **WHEN** 使用者輸入報表代碼或名稱關鍵字
- **THEN** 系統 SHALL 僅顯示符合關鍵字的報表及其所屬群組

---
### Requirement: Stable legacy report codes

系統 SHALL 保留舊系統報表代碼作為使用者可見的穩定識別碼。

#### Scenario: Open a report

- **WHEN** 使用者選擇代碼為 `C171` 的報表
- **THEN** 系統 SHALL 導向該代碼對應的報表頁面
- **AND** 頁面 SHALL 顯示「健康管理中心明細資料」

---
### Requirement: Unavailable report handling

尚未完成的報表 SHALL 保留於報表目錄，但系統不得將其呈現為已可查詢。

#### Scenario: Select an unavailable report

- **WHEN** 使用者選擇尚未建置的報表
- **THEN** 系統 SHALL 顯示該報表尚未建置的訊息
- **AND** 系統 SHALL NOT 執行其他報表的查詢

---
### Requirement: C8 uses an independent report route
The report tree SHALL link C8 to the independent C8 report controller within the existing MVC application and MUST NOT require an MVC Area route.

#### Scenario: Open C8 route
- **WHEN** a user follows the C8 report-tree link
- **THEN** the independent C8 controller renders its query view using the shared site layout

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
### Requirement: C9 uses the standard report navigation flow
C9 SHALL open through its independent MVC controller within the existing site layout and SHALL provide the standard path back to the report catalog without using an Area or a popup window.

#### Scenario: Navigate into and out of C9
- **WHEN** a user opens C9 and activates the return action
- **THEN** the query page uses the site layout and returns the user to the report catalog in the same browsing context

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

---
### Requirement: M1 medical statistics navigation
The primary report navigation SHALL list `M1 醫師看診人數日表` under `醫務統計報表` and SHALL route its selection to `/medical-statistics/doctor-daily`.

#### Scenario: Select M1 from medical statistics
- **WHEN** an authenticated user opens the medical statistics category and selects M1
- **THEN** the application displays the M1 doctor daily report query experience at `/medical-statistics/doctor-daily`

---
### Requirement: M2 medical statistics navigation
The medical statistics navigation SHALL expose an available M2 doctor monthly report entry and SHALL route it to the dedicated doctor-monthly report experience.

#### Scenario: Select M2 from medical statistics
- **WHEN** a user selects M2 doctor monthly report
- **THEN** the application displays the M2 Gregorian month and report option controls

---
### Requirement: M3 uses the medical-statistics report route
The report tree SHALL present M3 as an available medical-statistics report and SHALL open its query experience at /medical-statistics/opd-emergency-daily in the existing site layout. The selected report state SHALL remain M3 when the query route is opened or shared.

#### Scenario: Open M3 from the report tree
- **WHEN** a user selects M3 from the medical-statistics report group
- **THEN** the application opens /medical-statistics/opd-emergency-daily in the same browsing context
- **AND** the query page identifies M3 as the selected report

#### Scenario: Open a shared M3 query URL
- **WHEN** a user opens /medical-statistics/opd-emergency-daily directly
- **THEN** the application renders the M3 query experience with the standard site navigation

---
### Requirement: Data query routes
The application SHALL route Q1 selection to the outpatient price query component and SHALL route Q2 and Q3 selection to query-specific unavailable states. Direct navigation to the Q1 route MUST restore the `資料查詢` category and Q1 selection.

#### Scenario: Select outpatient price query
- **WHEN** a user selects Q1 or directly opens `/data-query/opd-price`
- **THEN** the application activates `資料查詢`, selects Q1, and displays the outpatient price query component

#### Scenario: Select an unavailable data query
- **WHEN** a user selects Q2 or Q3
- **THEN** the application displays that query's name and a `尚未建置` message without displaying the old category-level coming-soon panel

<!-- @trace
source: add-opd-price-query
updated: 2026-10-01
code:
  - Repositories/IHealthCenterRepository.cs
  - Program.cs
  - Services/ReportExportService.cs
  - Views/OpdEmergencyDailyReport/Preview.cshtml
  - Services/C144XlsxRenderer.cs
  - OpdAccrRptWeb.Tests/M3OpdEmergencyDailyReportServiceTests.cs
  - OpdAccrRptWeb.Tests/BackgroundReportExportServiceTests.cs
  - Services/IOpdPriceQueryService.cs
  - Repositories/OpdPriceQuerySql.cs
  - Repositories/ReferralMemberRepository.cs
  - Views/Shared/_TableSkeleton.cshtml
  - Services/ReportExportDefinitions.cs
  - Repositories/IC144DebtDetailReportRepository.cs
  - OpdAccrRptWeb.Tests/ReportExportServiceTests.cs
  - Services/IC144XlsxRenderer.cs
  - Repositories/C144DebtDetailReportRepository.cs
  - Views/Report/_M2DoctorMonthlyReport.cshtml
  - Views/Report/_M3OpdEmergencyDailyReport.cshtml
  - Views/Report/_TemplateReport.cshtml
  - Repositories/IOpdPriceQueryRepository.cs
  - OpdAccrRptWeb.Tests/TestDoubles.cs
  - Repositories/SafeNeedleRepository.cs
  - wwwroot/js/reports/report-template.js
  - wwwroot/css/site.css
  - Services/ReportCatalogService.cs
  - Views/OpdPriceQuery/Receipt.cshtml
  - Views/Report/_M1DoctorDailyReport.cshtml
  - OpdAccrRptWeb.Tests/ReferralMemberRepositoryTests.cs
  - wwwroot/js/report-app.js
  - Views/Report/_OpdPriceQuery.cshtml
  - wwwroot/js/reports/opd-price-query.js
  - Repositories/HealthCenterRepository.cs
  - Services/BackgroundReportExportService.cs
  - OpdAccrRptWeb.Tests/SafeNeedleRepositoryTests.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/OpdPriceQueryControllerTests.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - Views/DoctorMonthlyReport/Preview.cshtml
  - OpdAccrRptWeb.Tests/HealthCenterRepositoryTests.cs
  - OpdAccrRptWeb.Tests/OpdAccrRptWeb.Tests.csproj
  - Models/OpdPriceQueryModels.cs
  - OpdAccrRptWeb.Tests/C144DebtDetailReportRepositoryTests.cs
  - Services/OpdPriceQueryService.cs
  - Services/IOpdPricePatientAccessAuditWriter.cs
  - Views/Report/Index.cshtml
  - Services/OpdPriceReceiptRenderer.cs
  - Repositories/ISafeNeedleRepository.cs
  - Services/OpdPriceTokenService.cs
  - wwwroot/js/components/report-autocomplete.js
  - package.json
  - Controllers/OpdPriceQueryController.cs
  - Repositories/OpdPriceQueryRepository.cs
  - OpdAccrRptWeb.Tests/C144XlsxRendererTests.cs
  - OpdAccrRptWeb.csproj
  - OpdAccrRptWeb.Tests/OpdPriceQueryTests.cs
  - Services/IOpdPriceReceiptRenderer.cs
  - Repositories/IReferralMemberRepository.cs
tests:
  - OpdAccrRptWeb.Tests/report-layout.test.js
  - OpdAccrRptWeb.Tests/c4-report.test.js
  - OpdAccrRptWeb.Tests/opd-price-query.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
  - OpdAccrRptWeb.Tests/report-autocomplete.test.js
  - OpdAccrRptWeb.Tests/m3-opd-emergency-daily-report.test.js
  - OpdAccrRptWeb.Tests/report-app-bootstrap.test.js
-->

---
### Requirement: Adaptive sliding report sidebar

The report shell SHALL provide an expanded sidebar and a fully off-canvas collapsed state. The collapsed state MUST retain no category icon rail and MUST release the complete sidebar width to report content. A visible `>>` control SHALL open the menu with a horizontal slide-in transition, and a visible `<<` control SHALL close it with a horizontal slide-out transition. The application MUST persist only `expanded` or `collapsed` using a versioned browser-storage key and MUST fall back to expanded when storage is unavailable or invalid.

#### Scenario: Collapse and restore the sidebar

- **WHEN** a desktop user collapses the pinned sidebar
- **THEN** the sidebar slides fully off canvas, no icon rail remains, and report content expands into the complete released width
- **AND** refreshing or navigating to another report preserves the collapsed preference
- **WHEN** the user activates the visible `>>` control
- **THEN** the full sidebar slides in and exposes a `<<` close control

#### Scenario: Explicitly open and close the menu

- **WHEN** the user activates `>>` by pointer, keyboard, or touch
- **THEN** the menu opens without reconstructing or clearing the active report
- **WHEN** the user activates `<<`, the backdrop, or Escape
- **THEN** the menu slides fully out and focus returns to the opening control

#### Scenario: Browser storage is unavailable

- **WHEN** reading or writing the sidebar preference throws an error or returns a value other than `expanded` or `collapsed`
- **THEN** the report shell remains operable and uses the expanded desktop default


<!-- @trace
source: unify-report-query-layout
updated: 2026-10-07
code:
  - wwwroot/js/reports/c5-report.js
  - .agents/skills/spectra-review/SKILL.md
  - Views/Report/Index.cshtml
  - wwwroot/js/reports/registration-query.js
  - Services/RegistrationQueryService.cs
  - OpdAccrRptWeb.Tests/RegistrationQueryRepositoryTests.cs
  - Services/C143AccountingBalanceDebtReportService.cs
  - Views/Report/_M2DoctorMonthlyReport.cshtml
  - appsettings.json
  - Views/Report/_MedicalRecordQuery.cshtml
  - OpdAccrRptWeb.Tests/ReportExportServiceTests.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/OpdPriceQueryTests.cs
  - wwwroot/js/reports/opd-price-query.js
  - Repositories/RegistrationQueryRepository.cs
  - Services/IMedicalRecordQueryService.cs
  - Services/MedicalRecordQueryService.cs
  - Repositories/IMedicalRecordQueryRepository.cs
  - Views/Report/_RegistrationQuery.cshtml
  - OpdAccrRptWeb.Tests/RegistrationQueryServiceTests.cs
  - OpdAccrRptWeb.Tests/MedicalRecordQueryControllerTests.cs
  - Repositories/OpdPriceQuerySql.cs
  - Models/RegistrationQueryModels.cs
  - .agents/skills/spectra-ingest/SKILL.md
  - wwwroot/js/report-app.js
  - Controllers/RegistrationQueryController.cs
  - OpdAccrRptWeb.Tests/MedicalRecordQueryRepositoryTests.cs
  - Repositories/RegistrationQuerySql.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - Views/Report/_OpdPriceQuery.cshtml
  - Services/IOpdPriceQueryService.cs
  - Repositories/IOpdPriceQueryRepository.cs
  - Views/Report/_TemplateReport.cshtml
  - wwwroot/js/reports/report-template.js
  - .agents/skills/spectra-debug/SKILL.md
  - .agents/skills/spectra-discuss/SKILL.md
  - Controllers/OpdPriceQueryController.cs
  - OpdAccrRptWeb.Tests/MedicalRecordQueryServiceTests.cs
  - Repositories/IRegistrationQueryRepository.cs
  - Services/ReportExportService.cs
  - Repositories/MedicalRecordQueryRepository.cs
  - Views/Report/_C5ChargeQuantityReport.cshtml
  - .agents/skills/spectra-propose/SKILL.md
  - Repositories/OpdPriceQueryRepository.cs
  - REPORT_ACCESS_CONTROL_PLAN.md
  - Controllers/MedicalRecordQueryController.cs
  - Program.cs
  - wwwroot/js/reports/medical-record-query.js
  - Services/OpdPriceReceiptRenderer.cs
  - OpdAccrRptWeb.Tests/ReportExportJobStoreTests.cs
  - package.json
  - OpdAccrRptWeb.Tests/OpdPriceQueryControllerTests.cs
  - .agents/skills/spectra-apply/SKILL.md
  - .agents/skills/spectra-audit/SKILL.md
  - OpdAccrRptWeb.Tests/RegistrationQueryControllerTests.cs
  - Models/MedicalRecordQueryModels.cs
  - Services/ReportExportJobStore.cs
  - Models/OpdPriceQueryModels.cs
  - wwwroot/css/site.css
  - wwwroot/js/reports/m2-doctor-monthly-report.js
  - .agents/skills/spectra-ask/SKILL.md
  - OpdAccrRptWeb.Tests/MedicalRecordQueryClientContractTests.cs
  - Services/IRegistrationQueryService.cs
  - .agents/skills/spectra-commit/SKILL.md
  - Services/OpdPriceQueryService.cs
  - Views/OpdPriceQuery/BatchReceipt.cshtml
  - .agents/skills/spectra-analyze/SKILL.md
  - .agents/skills/spectra-drift/SKILL.md
  - .agents/skills/spectra-archive/SKILL.md
  - OpdAccrRptWeb.Tests/C143AccountingBalanceDebtReportServiceTests.cs
  - Repositories/MedicalRecordQuerySql.cs
  - Services/ReportExportDefinitions.cs
  - AGENTS.md
  - .agents/skills/spectra-verify/SKILL.md
tests:
  - OpdAccrRptWeb.Tests/opd-price-query.test.js
  - OpdAccrRptWeb.Tests/c5-report.test.js
  - OpdAccrRptWeb.Tests/report-layout.test.js
  - OpdAccrRptWeb.Tests/m2-doctor-monthly-report.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
  - OpdAccrRptWeb.Tests/report-app-bootstrap.test.js
  - OpdAccrRptWeb.Tests/registration-query.test.js
-->

---
### Requirement: Accessible sliding drawer controls

The collapsed report navigation SHALL use text controls `>>` and `<<` with accessible names and correct expanded semantics. Keyboard and touch users MUST be able to open, traverse, and close the menu. The menu SHALL provide a backdrop on narrow screens and MUST NOT depend on hover.

#### Scenario: Navigate with keyboard

- **WHEN** a keyboard user focuses and activates the `>>` control
- **THEN** the full menu becomes available with its existing category, search, group, and report controls
- **AND** the selected report exposes active semantics and Escape returns focus to the triggering control

#### Scenario: Navigate on a touch screen

- **WHEN** a user activates the menu control on a device without hover
- **THEN** the report menu opens as a drawer with correct expanded semantics
- **AND** selecting a report, activating the backdrop, or pressing Escape closes the drawer without changing the desktop pinned preference

<!-- @trace
source: unify-report-query-layout
updated: 2026-10-07
code:
  - wwwroot/js/reports/c5-report.js
  - .agents/skills/spectra-review/SKILL.md
  - Views/Report/Index.cshtml
  - wwwroot/js/reports/registration-query.js
  - Services/RegistrationQueryService.cs
  - OpdAccrRptWeb.Tests/RegistrationQueryRepositoryTests.cs
  - Services/C143AccountingBalanceDebtReportService.cs
  - Views/Report/_M2DoctorMonthlyReport.cshtml
  - appsettings.json
  - Views/Report/_MedicalRecordQuery.cshtml
  - OpdAccrRptWeb.Tests/ReportExportServiceTests.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/OpdPriceQueryTests.cs
  - wwwroot/js/reports/opd-price-query.js
  - Repositories/RegistrationQueryRepository.cs
  - Services/IMedicalRecordQueryService.cs
  - Services/MedicalRecordQueryService.cs
  - Repositories/IMedicalRecordQueryRepository.cs
  - Views/Report/_RegistrationQuery.cshtml
  - OpdAccrRptWeb.Tests/RegistrationQueryServiceTests.cs
  - OpdAccrRptWeb.Tests/MedicalRecordQueryControllerTests.cs
  - Repositories/OpdPriceQuerySql.cs
  - Models/RegistrationQueryModels.cs
  - .agents/skills/spectra-ingest/SKILL.md
  - wwwroot/js/report-app.js
  - Controllers/RegistrationQueryController.cs
  - OpdAccrRptWeb.Tests/MedicalRecordQueryRepositoryTests.cs
  - Repositories/RegistrationQuerySql.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - Views/Report/_OpdPriceQuery.cshtml
  - Services/IOpdPriceQueryService.cs
  - Repositories/IOpdPriceQueryRepository.cs
  - Views/Report/_TemplateReport.cshtml
  - wwwroot/js/reports/report-template.js
  - .agents/skills/spectra-debug/SKILL.md
  - .agents/skills/spectra-discuss/SKILL.md
  - Controllers/OpdPriceQueryController.cs
  - OpdAccrRptWeb.Tests/MedicalRecordQueryServiceTests.cs
  - Repositories/IRegistrationQueryRepository.cs
  - Services/ReportExportService.cs
  - Repositories/MedicalRecordQueryRepository.cs
  - Views/Report/_C5ChargeQuantityReport.cshtml
  - .agents/skills/spectra-propose/SKILL.md
  - Repositories/OpdPriceQueryRepository.cs
  - REPORT_ACCESS_CONTROL_PLAN.md
  - Controllers/MedicalRecordQueryController.cs
  - Program.cs
  - wwwroot/js/reports/medical-record-query.js
  - Services/OpdPriceReceiptRenderer.cs
  - OpdAccrRptWeb.Tests/ReportExportJobStoreTests.cs
  - package.json
  - OpdAccrRptWeb.Tests/OpdPriceQueryControllerTests.cs
  - .agents/skills/spectra-apply/SKILL.md
  - .agents/skills/spectra-audit/SKILL.md
  - OpdAccrRptWeb.Tests/RegistrationQueryControllerTests.cs
  - Models/MedicalRecordQueryModels.cs
  - Services/ReportExportJobStore.cs
  - Models/OpdPriceQueryModels.cs
  - wwwroot/css/site.css
  - wwwroot/js/reports/m2-doctor-monthly-report.js
  - .agents/skills/spectra-ask/SKILL.md
  - OpdAccrRptWeb.Tests/MedicalRecordQueryClientContractTests.cs
  - Services/IRegistrationQueryService.cs
  - .agents/skills/spectra-commit/SKILL.md
  - Services/OpdPriceQueryService.cs
  - Views/OpdPriceQuery/BatchReceipt.cshtml
  - .agents/skills/spectra-analyze/SKILL.md
  - .agents/skills/spectra-drift/SKILL.md
  - .agents/skills/spectra-archive/SKILL.md
  - OpdAccrRptWeb.Tests/C143AccountingBalanceDebtReportServiceTests.cs
  - Repositories/MedicalRecordQuerySql.cs
  - Services/ReportExportDefinitions.cs
  - AGENTS.md
  - .agents/skills/spectra-verify/SKILL.md
tests:
  - OpdAccrRptWeb.Tests/opd-price-query.test.js
  - OpdAccrRptWeb.Tests/c5-report.test.js
  - OpdAccrRptWeb.Tests/report-layout.test.js
  - OpdAccrRptWeb.Tests/m2-doctor-monthly-report.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
  - OpdAccrRptWeb.Tests/report-app-bootstrap.test.js
  - OpdAccrRptWeb.Tests/registration-query.test.js
-->

---
### Requirement: Route Q2 medical record query

The report workspace SHALL route the catalog entry with code Q2 and name 「病歷查詢」 to /data-query/medical-record.

#### Scenario: Select Q2 from the data query category

- **WHEN** an authenticated user selects Q2「病歷查詢」from the data query category
- **THEN** the browser route becomes /data-query/medical-record
- **AND** the report workspace keeps the selected Q2 entry active in the sidebar and breadcrumb

#### Scenario: Open the Q2 route directly

- **WHEN** an authenticated user opens /data-query/medical-record directly
- **THEN** the existing report workspace loads
- **AND** the Q2 query component is rendered without an unavailable-feature placeholder

<!-- @trace
source: add-medical-record-query
updated: 2026-10-07
code:
  - .agents/skills/spectra-debug/SKILL.md
  - .agents/skills/spectra-archive/SKILL.md
  - Views/Report/_RegistrationQuery.cshtml
  - .agents/skills/spectra-propose/SKILL.md
  - wwwroot/js/reports/registration-query.js
  - .agents/skills/spectra-apply/SKILL.md
  - Services/RegistrationQueryService.cs
  - .agents/skills/spectra-discuss/SKILL.md
  - .agents/skills/spectra-drift/SKILL.md
  - .agents/skills/spectra-analyze/SKILL.md
  - AGENTS.md
  - .agents/skills/spectra-ask/SKILL.md
  - REPORT_ACCESS_CONTROL_PLAN.md
  - .agents/skills/spectra-commit/SKILL.md
  - .agents/skills/spectra-review/SKILL.md
  - .agents/skills/spectra-audit/SKILL.md
  - Repositories/RegistrationQueryRepository.cs
  - .agents/skills/spectra-ingest/SKILL.md
  - .agents/skills/spectra-verify/SKILL.md
-->

---
### Requirement: Authorized default landing
The system SHALL choose the first implemented catalog entry with effective query permission in catalog order when an authenticated user opens / or /Report without a code. It SHALL use that entry's existing route. If no such entry exists, /Report SHALL render a no-permissions empty state without loading report data. Failure to read permissions SHALL be reported as a service failure rather than a no-permissions state. An unauthenticated request SHALL require authentication.

#### Scenario: C1 denied but C21 allowed
- **WHEN** a signed-in user lacks C1 query permission but has C21 query permission and opens /
- **THEN** the system SHALL redirect to /Report/C21
- **AND** C1 SHALL not be selected in the client

#### Scenario: No accessible entries
- **WHEN** a signed-in user has no query-authorized implemented catalog entry and opens /Report
- **THEN** the page SHALL show "尚未取得報表權限"
- **AND** it SHALL not request report data or initialize the first catalog entry

#### Scenario: Permission source fails
- **WHEN** the permission store cannot be read while resolving /
- **THEN** the server SHALL return a service failure instead of a no-permissions page


<!-- @trace
source: add-report-action-permissions
updated: 2026-11-02
code:
  - Services/C21RebuildAuthorizationService.cs
  - wwwroot/js/components/report-date-range.js
  - Repositories/C23ContractAccountingRepository.cs
  - wwwroot/js/reports/sap-interface.js
  - OpdAccrRptWeb.Tests/ReportPermissionManagerFilterTests.cs
  - scripts/sql/create-opd-accrreport-permission.sql
  - OpdAccrRptWeb.Tests/ReportQueryPermissionFilterTests.cs
  - wwwroot/js/reports/c9-report.js
  - OpdAccrRptWeb.Tests/C21RebuildServiceTests.cs
  - Models/ReportAction.cs
  - Models/PermissionBatchRequest.cs
  - Services/C23RebuildAuthorizationService.cs
  - package.json
  - Views/Report/_C24DebtPaymentPreview.cshtml
  - Controllers/DoctorMonthlyReportController.cs
  - OpdAccrRptWeb.Tests/CashierCashReportServiceTests.cs
  - wwwroot/js/reports/report-template.js
  - appsettings.json
  - wwwroot/js/reports/c8-report.js
  - Models/PermissionRevokeRequest.cs
  - Services/IReportService.cs
  - Controllers/HomeController.cs
  - Models/PermissionCopyRequest.cs
  - Repositories/C24DebtPaymentRepository.cs
  - Properties/PublishProfiles/FolderProfile.pubxml
  - ViewModels/C29ContractPaymentPreviewViewModel.cs
  - Views/Report/_C5ChargeQuantityReport.cshtml
  - Views/ReportPermissionManagement/_PermissionPanel.cshtml
  - OpdAccrRptWeb.Tests/CashierCashRepositoryTests.cs
  - Services/ReportPermissionAdministrationService.cs
  - Views/Report/_C9MaterialAccountingMonthlyReport.cshtml
  - wwwroot/js/reports/c7-report.js
  - Services/C21AccountingSummaryPreviewBuilder.cs
  - Services/C21Options.cs
  - Services/C23ContractAccountingPreviewBuilder.cs
  - Views/Report/_M3OpdEmergencyDailyReport.cshtml
  - scripts/seed-c24-inpatient-dbtest3.ps1
  - wwwroot/css/report-permissions.css
  - OpdAccrRptWeb.Tests/C24ReportServiceTests.cs
  - Services/DefaultLoginRebuildUserIdentityProvider.cs
  - wwwroot/css/c23-preview.css
  - Services/ReportCatalogService.cs
  - Services/ReportQueryPermissionFilter.cs
  - Models/PermissionManagementModels.cs
  - scripts/sql/add-c141-linebot-permission.sql
  - OpdAccrRptWeb.Tests/DefaultUserAuthenticationHandlerTests.cs
  - Views/Report/_M2DoctorMonthlyReport.cshtml
  - Views/Report/_M1DoctorDailyReport.cshtml
  - wwwroot/js/reports/c5-report.js
  - Repositories/C24Sql.cs
  - ViewModels/ReportDefinitionViewModel.cs
  - Views/Report/_C23ContractAccountingPreview.cshtml
  - Repositories/ICashierCashRepository.cs
  - ViewModels/C24PreviewViewModel.cs
  - Controllers/OpdEmergencyDailyReportController.cs
  - Repositories/ReportPermissionRepository.cs
  - Services/ReportExportService.cs
  - ViewModels/C23ContractAccountingReportViewModel.cs
  - OpdAccrRptWeb.Tests/C29ContractPaymentPreviewViewModelTests.cs
  - OpdAccrRptWeb.Tests/C23ContractAccountingRepositoryTests.cs
  - Views/Report/_C1SurgicalPrintPreview.cshtml
  - Views/Report/_C29ContractPaymentPreview.cshtml
  - Views/Report/_C8PatchBillDetailReport.cshtml
  - Services/ReportService.cs
  - OpdAccrRptWeb.Tests/C24DebtPaymentViewModelTests.cs
  - Models/PermissionChangeRequest.cs
  - Models/ReportPermissionGrant.cs
  - Infrastructure/DefaultUserAuthenticationHandler.cs
  - OpdAccrRptWeb.Tests/C23RebuildServiceTests.cs
  - Repositories/IUserProfileRepository.cs
  - Services/C23Options.cs
  - Services/IUserDisplayNameService.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - OpdAccrRptWeb.Tests/ReportPermissionAdministrationServiceTests.cs
  - Views/Report/_C11ReceivablesCollectionReport.cshtml
  - OpdAccrRptWeb.Tests/ReportExportServiceTests.cs
  - Repositories/IReportPermissionRepository.cs
  - Repositories/UserProfileRepository.cs
  - Models/PermissionDeleteTargetRequest.cs
  - Repositories/C21RebuildSql.cs
  - OpdAccrRptWeb.Tests/C23ContractAccountingPreviewBuilderTests.cs
  - Views/Report/_OpdPriceQuery.cshtml
  - ViewModels/SearchReportCondition.cs
  - Views/Shared/_Layout.cshtml
  - OpdAccrRptWeb.Tests/RegistrationQueryRepositoryTests.cs
  - Program.cs
  - ViewModels/C22CashierCashPreviewViewModel.cs
  - ViewModels/C23ContractAccountingPreviewViewModel.cs
  - Services/ConfiguredC23UserIdentityProvider.cs
  - Services/ReportPermissionService.cs
  - OpdAccrRptWeb.Tests/UserDisplayNameServiceTests.cs
  - Controllers/DoctorDailyReportController.cs
  - ViewModels/C21AccountingSummaryPreviewViewModel.cs
  - Views/Report/_SapInterface.cshtml
  - Views/Report/_C22CashierCashPreview.cshtml
  - OpdAccrRptWeb.Tests/C21AccountingSummaryRepositoryTests.cs
  - OpdAccrRptWeb.Tests/InpatientAdvancePaymentBalanceReportServiceTests.cs
  - Views/Report/_C21AccountingSummaryPreview.cshtml
  - ViewModels/CashierCashReportViewModel.cs
  - OpdAccrRptWeb.Tests/C21AccountingSummaryPreviewBuilderTests.cs
  - Views/Report/_TemplateReport.cshtml
  - Controllers/ReportPermissionManagementController.cs
  - Services/C21RebuildService.cs
  - wwwroot/css/site.css
  - Repositories/CashierCashRepository.cs
  - Views/Report/Index.cshtml
  - scripts/seed-c24-dbtest3.ps1
  - Services/ReportNavigationService.cs
  - wwwroot/js/report-permissions.js
  - Services/ConfiguredC21UserIdentityProvider.cs
  - Views/Report/_C7DailyChargeDetailReport.cshtml
  - ViewModels/ReportIndexViewModel.cs
  - Views/Report/_C12MedicalReceiptSummary.cshtml
  - scripts/publish-c24-inpatient-dbtest3.ps1
  - Services/C24DebtPaymentCalculationService.cs
  - Controllers/ReportPermissionStatusController.cs
  - OpdAccrRptWeb.Tests/ReportPermissionServiceTests.cs
  - Services/C23RebuildService.cs
  - Services/ReportPermissionManagerFilter.cs
  - Services/UserDisplayNameService.cs
  - Services/IReportPermissionService.cs
  - OpdAccrRptWeb.Tests/ReportPermissionManagementControllerTests.cs
  - OpdAccrRptWeb.csproj
  - OpdAccrRptWeb.Tests/ReportNavigationServiceTests.cs
  - dotnet-tools.json
  - wwwroot/js/report-app.js
  - OpdAccrRptWeb.Tests/C24DebtPaymentRepositoryTests.cs
  - Models/PermissionGrantEditRequest.cs
  - OpdAccrRptWeb.Tests/ReportPermissionRepositoryTests.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
tests:
  - OpdAccrRptWeb.Tests/report-permissions.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
  - OpdAccrRptWeb.Tests/report-date-range.test.js
  - OpdAccrRptWeb.Tests/report-app-bootstrap.test.js
  - OpdAccrRptWeb.Tests/c11-report.test.js
-->

---
### Requirement: Query-authorized report navigation
The system SHALL display and open an available report only when the signed-in user has its effective query permission. The left report list SHALL contain only query-authorized reports and SHALL omit groups with no visible reports. The top category menu SHALL contain only categories with at least one visible report. Both menus SHALL use the same effective grants and preserve catalog order. It SHALL preserve the existing unavailable-report treatment for reports without implemented query capability. A user without query permission SHALL not gain access through a direct report URL.

#### Scenario: Report without query grant
- **WHEN** a signed-in user lacks query permission for C21
- **THEN** C21 SHALL be absent from that user's report navigation
- **AND** a direct C21 report URL SHALL return 403

#### Scenario: Report with query grant
- **WHEN** a signed-in user has query permission for an available report
- **THEN** its menu entry SHALL appear and its report page SHALL open

#### Scenario: Filter both navigation menus
- **WHEN** a signed-in user has query permission only for C21 in one category and Q2 in another category
- **THEN** the left report list SHALL show only C21 or Q2 within the selected category, with no empty groups
- **AND** the top category menu SHALL show only the two categories containing C21 and Q2, in catalog order

<!-- @trace
source: add-report-action-permissions
updated: 2026-11-02
code:
  - Services/C21RebuildAuthorizationService.cs
  - wwwroot/js/components/report-date-range.js
  - Repositories/C23ContractAccountingRepository.cs
  - wwwroot/js/reports/sap-interface.js
  - OpdAccrRptWeb.Tests/ReportPermissionManagerFilterTests.cs
  - scripts/sql/create-opd-accrreport-permission.sql
  - OpdAccrRptWeb.Tests/ReportQueryPermissionFilterTests.cs
  - wwwroot/js/reports/c9-report.js
  - OpdAccrRptWeb.Tests/C21RebuildServiceTests.cs
  - Models/ReportAction.cs
  - Models/PermissionBatchRequest.cs
  - Services/C23RebuildAuthorizationService.cs
  - package.json
  - Views/Report/_C24DebtPaymentPreview.cshtml
  - Controllers/DoctorMonthlyReportController.cs
  - OpdAccrRptWeb.Tests/CashierCashReportServiceTests.cs
  - wwwroot/js/reports/report-template.js
  - appsettings.json
  - wwwroot/js/reports/c8-report.js
  - Models/PermissionRevokeRequest.cs
  - Services/IReportService.cs
  - Controllers/HomeController.cs
  - Models/PermissionCopyRequest.cs
  - Repositories/C24DebtPaymentRepository.cs
  - Properties/PublishProfiles/FolderProfile.pubxml
  - ViewModels/C29ContractPaymentPreviewViewModel.cs
  - Views/Report/_C5ChargeQuantityReport.cshtml
  - Views/ReportPermissionManagement/_PermissionPanel.cshtml
  - OpdAccrRptWeb.Tests/CashierCashRepositoryTests.cs
  - Services/ReportPermissionAdministrationService.cs
  - Views/Report/_C9MaterialAccountingMonthlyReport.cshtml
  - wwwroot/js/reports/c7-report.js
  - Services/C21AccountingSummaryPreviewBuilder.cs
  - Services/C21Options.cs
  - Services/C23ContractAccountingPreviewBuilder.cs
  - Views/Report/_M3OpdEmergencyDailyReport.cshtml
  - scripts/seed-c24-inpatient-dbtest3.ps1
  - wwwroot/css/report-permissions.css
  - OpdAccrRptWeb.Tests/C24ReportServiceTests.cs
  - Services/DefaultLoginRebuildUserIdentityProvider.cs
  - wwwroot/css/c23-preview.css
  - Services/ReportCatalogService.cs
  - Services/ReportQueryPermissionFilter.cs
  - Models/PermissionManagementModels.cs
  - scripts/sql/add-c141-linebot-permission.sql
  - OpdAccrRptWeb.Tests/DefaultUserAuthenticationHandlerTests.cs
  - Views/Report/_M2DoctorMonthlyReport.cshtml
  - Views/Report/_M1DoctorDailyReport.cshtml
  - wwwroot/js/reports/c5-report.js
  - Repositories/C24Sql.cs
  - ViewModels/ReportDefinitionViewModel.cs
  - Views/Report/_C23ContractAccountingPreview.cshtml
  - Repositories/ICashierCashRepository.cs
  - ViewModels/C24PreviewViewModel.cs
  - Controllers/OpdEmergencyDailyReportController.cs
  - Repositories/ReportPermissionRepository.cs
  - Services/ReportExportService.cs
  - ViewModels/C23ContractAccountingReportViewModel.cs
  - OpdAccrRptWeb.Tests/C29ContractPaymentPreviewViewModelTests.cs
  - OpdAccrRptWeb.Tests/C23ContractAccountingRepositoryTests.cs
  - Views/Report/_C1SurgicalPrintPreview.cshtml
  - Views/Report/_C29ContractPaymentPreview.cshtml
  - Views/Report/_C8PatchBillDetailReport.cshtml
  - Services/ReportService.cs
  - OpdAccrRptWeb.Tests/C24DebtPaymentViewModelTests.cs
  - Models/PermissionChangeRequest.cs
  - Models/ReportPermissionGrant.cs
  - Infrastructure/DefaultUserAuthenticationHandler.cs
  - OpdAccrRptWeb.Tests/C23RebuildServiceTests.cs
  - Repositories/IUserProfileRepository.cs
  - Services/C23Options.cs
  - Services/IUserDisplayNameService.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - OpdAccrRptWeb.Tests/ReportPermissionAdministrationServiceTests.cs
  - Views/Report/_C11ReceivablesCollectionReport.cshtml
  - OpdAccrRptWeb.Tests/ReportExportServiceTests.cs
  - Repositories/IReportPermissionRepository.cs
  - Repositories/UserProfileRepository.cs
  - Models/PermissionDeleteTargetRequest.cs
  - Repositories/C21RebuildSql.cs
  - OpdAccrRptWeb.Tests/C23ContractAccountingPreviewBuilderTests.cs
  - Views/Report/_OpdPriceQuery.cshtml
  - ViewModels/SearchReportCondition.cs
  - Views/Shared/_Layout.cshtml
  - OpdAccrRptWeb.Tests/RegistrationQueryRepositoryTests.cs
  - Program.cs
  - ViewModels/C22CashierCashPreviewViewModel.cs
  - ViewModels/C23ContractAccountingPreviewViewModel.cs
  - Services/ConfiguredC23UserIdentityProvider.cs
  - Services/ReportPermissionService.cs
  - OpdAccrRptWeb.Tests/UserDisplayNameServiceTests.cs
  - Controllers/DoctorDailyReportController.cs
  - ViewModels/C21AccountingSummaryPreviewViewModel.cs
  - Views/Report/_SapInterface.cshtml
  - Views/Report/_C22CashierCashPreview.cshtml
  - OpdAccrRptWeb.Tests/C21AccountingSummaryRepositoryTests.cs
  - OpdAccrRptWeb.Tests/InpatientAdvancePaymentBalanceReportServiceTests.cs
  - Views/Report/_C21AccountingSummaryPreview.cshtml
  - ViewModels/CashierCashReportViewModel.cs
  - OpdAccrRptWeb.Tests/C21AccountingSummaryPreviewBuilderTests.cs
  - Views/Report/_TemplateReport.cshtml
  - Controllers/ReportPermissionManagementController.cs
  - Services/C21RebuildService.cs
  - wwwroot/css/site.css
  - Repositories/CashierCashRepository.cs
  - Views/Report/Index.cshtml
  - scripts/seed-c24-dbtest3.ps1
  - Services/ReportNavigationService.cs
  - wwwroot/js/report-permissions.js
  - Services/ConfiguredC21UserIdentityProvider.cs
  - Views/Report/_C7DailyChargeDetailReport.cshtml
  - ViewModels/ReportIndexViewModel.cs
  - Views/Report/_C12MedicalReceiptSummary.cshtml
  - scripts/publish-c24-inpatient-dbtest3.ps1
  - Services/C24DebtPaymentCalculationService.cs
  - Controllers/ReportPermissionStatusController.cs
  - OpdAccrRptWeb.Tests/ReportPermissionServiceTests.cs
  - Services/C23RebuildService.cs
  - Services/ReportPermissionManagerFilter.cs
  - Services/UserDisplayNameService.cs
  - Services/IReportPermissionService.cs
  - OpdAccrRptWeb.Tests/ReportPermissionManagementControllerTests.cs
  - OpdAccrRptWeb.csproj
  - OpdAccrRptWeb.Tests/ReportNavigationServiceTests.cs
  - dotnet-tools.json
  - wwwroot/js/report-app.js
  - OpdAccrRptWeb.Tests/C24DebtPaymentRepositoryTests.cs
  - Models/PermissionGrantEditRequest.cs
  - OpdAccrRptWeb.Tests/ReportPermissionRepositoryTests.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
tests:
  - OpdAccrRptWeb.Tests/report-permissions.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
  - OpdAccrRptWeb.Tests/report-date-range.test.js
  - OpdAccrRptWeb.Tests/report-app-bootstrap.test.js
  - OpdAccrRptWeb.Tests/c11-report.test.js
-->