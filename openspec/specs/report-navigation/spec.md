# Report Navigation Specification

## Purpose

定義門急診報表網站的主要導覽、報表樹與報表路由行為。

## Requirements

### Requirement: Primary report categories

系統 SHALL 在頁面上方提供「門診批價統計報表」、「醫務統計報表」及「資料查詢」三個主要功能分類。

#### Scenario: Switch primary category

- **WHEN** 使用者選擇一個主要功能分類
- **THEN** 系統 SHALL 將該分類設為目前分類
- **AND** 左側選單 SHALL 顯示該分類所屬的報表群組與報表

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