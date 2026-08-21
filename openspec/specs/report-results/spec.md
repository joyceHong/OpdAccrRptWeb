# Report Results Specification

## Purpose

定義報表結果的欄位與資料呈現、共用分頁行為，以及下一階段預定補上的 Excel 匯出能力。

## Requirements

### Requirement: Dynamic report columns

系統 SHALL 依報表定義回傳並顯示該報表專屬的欄位與資料列。

#### Scenario: Display successful query results

- **WHEN** 報表查詢成功並回傳欄位及資料
- **THEN** 系統 SHALL 按回傳欄位呈現表格表頭
- **AND** 系統 SHALL 將每筆資料呈現於對應欄位

---
### Requirement: Empty result feedback

系統 SHALL 明確呈現查無資料的狀態。

#### Scenario: Query returns no rows

- **WHEN** 有效查詢未取得任何資料
- **THEN** 系統 SHALL 顯示查無資料訊息
- **AND** 系統 SHALL NOT 將查無資料呈現為系統錯誤

---
### Requirement: Query failure feedback

系統 SHALL 將查詢失敗與查無資料區分，並提供不洩漏敏感資料的錯誤訊息。

#### Scenario: Data source query fails

- **WHEN** 資料來源或查詢流程發生錯誤
- **THEN** 系統 SHALL 顯示查詢失敗訊息
- **AND** 使用者訊息 SHALL NOT 包含連線字串、SQL 或敏感個資

---
### Requirement: Paginated result table

The system SHALL support paginated report results with a default page size of 10 and SHALL allow users to select 10, 30, or 50 rows per page. C171 and C174 SHALL obtain each displayed page from the server and SHALL use server-provided total-count and total-page metadata; reports not designated for server pagination SHALL retain client-side pagination.

#### Scenario: Open a multi-page server-paginated result

- **WHEN** a C171 or C174 query matches 28 rows and the user has not changed the page size
- **THEN** the server SHALL return page 1 with 10 rows, total count 28, and total pages 3
- **AND** the UI SHALL display the first 10 rows and controls for navigating to another page

##### Example: Server page boundaries

| Request | Returned rows | Total count | Total pages |
| ----- | ----- | ----- | ----- |
| page 1, size 10 | rows 1-10 | 28 | 3 |
| page 2, size 10 | rows 11-20 | 28 | 3 |
| page 3, size 10 | rows 21-28 | 28 | 3 |
| page 4, size 10 | empty | 28 | 3 |

#### Scenario: Navigate to another server-provided page

- **WHEN** the user moves from page 1 to page 2 for C171 or C174
- **THEN** the UI SHALL request page 2 from the server with unchanged report filters and page size
- **AND** the UI SHALL replace the displayed rows with the returned page

#### Scenario: Change a server-paginated report page size

- **WHEN** the user changes the C171 or C174 page size from 10 to 30
- **THEN** the UI SHALL reset the current page to 1
- **AND** the UI SHALL request page 1 with page size 30 from the server

#### Scenario: Display an empty server-paginated result

- **WHEN** a C171 or C174 query matches zero rows
- **THEN** the response SHALL contain an empty data collection, total count 0, and total pages 0
- **AND** the UI SHALL display the existing no-data state rather than page navigation

## Planned Requirements

下列能力屬下一階段工作。


<!-- @trace
source: add-error-file-logging-and-c171-server-pagination
updated: 2026-08-19
code:
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/HealthCenterRepositoryTests.cs
  - OpdAccrRptWeb.Tests/GlobalUsings.cs
  - OpdAccrRptWeb.Tests/TestDoubles.cs
  - ViewModels/HealthCenterDetailViewModel.cs
  - wwwroot/js/report-app.js
  - ViewModels/SearchReportCondition.cs
  - Repositories/IHealthCenterRepository.cs
  - ViewModels/ReportDataAndColumns.cs
  - wwwroot/js/reports/report-template.js
  - appsettings.Development.json
  - OpdAccrRptWeb.csproj
  - ViewModels/HelthCenterCountViewModel.cs
  - Views/Report/_TemplateReport.cshtml
  - ViewModels/HealthCenterContractBillingReport.cs
  - Controllers/ReportController.cs
  - Repositories/HealthCenterRepository.cs
  - Services/ReportService.cs
  - ViewModels/HelthCenterDetailViewModel.cs
  - Infrastructure/FileLoggingConfiguration.cs
  - OpdAccrRptWeb.Tests/OpdAccrRptWeb.Tests.csproj
  - ViewModels/HealthCenterCountViewModel.cs
  - Properties/AssemblyInfo.cs
  - ViewModels/HealthCheckupVisits.cs
  - ViewModels/PagedReportResult.cs
  - OpdAccrRptWeb.Tests/FileLoggingTests.cs
  - OpdAccrRptWeb.Tests/ReportServiceTests.cs
  - appsettings.json
  - Program.cs
tests:
  - OpdAccrRptWeb.Tests/report-template.test.js
-->


<!-- @trace
source: c174-server-pagination-total-count-cache
updated: 2026-08-19
code:
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/OpdAccrRptWeb.Tests.csproj
  - ViewModels/HealthCenterCountViewModel.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/FileLoggingTests.cs
  - Infrastructure/FileLoggingConfiguration.cs
  - Properties/AssemblyInfo.cs
  - ViewModels/HealthCenterContractBillingReport.cs
  - ViewModels/HealthCheckupVisits.cs
  - ViewModels/PagedReportResult.cs
  - wwwroot/js/reports/report-template.js
  - ViewModels/HealthCenterDetailViewModel.cs
  - Repositories/IHealthCenterRepository.cs
  - OpdAccrRptWeb.Tests/ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/GlobalUsings.cs
  - Services/ReportService.cs
  - Services/ReportTotalCountCache.cs
  - Services/IReportTotalCountCache.cs
  - Repositories/HealthCenterRepository.cs
  - OpdAccrRptWeb.Tests/HealthCenterRepositoryTests.cs
  - OpdAccrRptWeb.Tests/TestDoubles.cs
  - Program.cs
  - OpdAccrRptWeb.Tests/ReportTotalCountCacheTests.cs
  - .spectra.yaml
tests:
  - OpdAccrRptWeb.Tests/report-template.test.js
-->

### Requirement: Excel export

系統 SHALL 允許使用者將符合目前查詢條件的結果匯出為 Excel 相容檔案。

#### Scenario: Export current query result

- **WHEN** 使用者在查詢成功後執行匯出
- **THEN** 系統 SHALL 產生對應目前報表與查詢條件的 Excel 相容檔案
- **AND** 匯出範圍 SHALL NOT 僅限於目前顯示頁