# Report Results Specification

## Purpose

定義報表結果的欄位與資料呈現、共用分頁行為，以及下一階段預定補上的 Excel 匯出能力。

## Requirements

### Requirement: Shared C144 result and export experience
The shared report page SHALL render C144 through the existing pending skeleton, dynamic result table, empty state, total count, page-size selector, and first, previous, next, and last pagination controls. It MUST provide an explicit Excel export action after a valid query, and changing a result-affecting C144 condition MUST reset navigation to page one.

#### Scenario: Query pending and then succeeds
- **WHEN** a user submits a valid C144 query
- **THEN** the shared table skeleton is displayed while the request is pending
- **AND** the 31-column table, total count, and pagination are displayed after success

#### Scenario: Export from a paged result
- **WHEN** a user invokes Excel export while a valid C144 query is active
- **THEN** the export request uses the active dates and source without limiting the export to the current page

---
### Requirement: Dynamic report columns

Every enabled query report SHALL return and display its report-specific detail rows in a normal browsable TABLE result area. The TABLE SHALL display a total row count and SHALL use the established server-side or client-side pagination contract for that report. Report-specific summary, preview, print, chart, or document layouts SHALL be additional views and SHALL NOT replace the normal TABLE result.

#### Scenario: Display successful query results

- **WHEN** any enabled report query succeeds with detail rows
- **THEN** the normal result area displays a TABLE header and every returned detail row in its corresponding columns
- **AND** the result area displays total row count and pagination controls when multiple pages exist

#### Scenario: Display a report with a specialized preview

- **WHEN** an enabled report also provides a report-specific preview, print document, summary, or chart
- **THEN** users can still browse the normal TABLE detail result
- **AND** opening and closing the specialized view does not discard or replace the TABLE result state


<!-- @trace
source: add-c12-medical-receipt-summary
updated: 2026-09-15
code:
  - wwwroot/css/site.css
  - OpdAccrRptWeb.Tests/C12ReportControllerTests.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/C12LegacyAmountConverterTests.cs
  - OpdAccrRptWeb.Tests/C12ReportServiceTests.cs
  - Services/C12ReportService.cs
  - Services/IC12PatientAccessAuditWriter.cs
  - Services/IC12PatientAccessAuthorizer.cs
  - Services/IC12ReportService.cs
  - wwwroot/js/report-app.js
  - Program.cs
  - Repositories/C12Sql.cs
  - Services/IC12LegacyAmountConverter.cs
  - OpdAccrRptWeb.Tests/C12ReportRepositoryTests.cs
  - ViewModels/SearchReportCondition.cs
  - Repositories/C12ReportRepository.cs
  - Repositories/IC12ReportRepository.cs
  - ViewModels/C12MedicalReceiptSummaryViewModel.cs
  - Views/Report/_C12MedicalReceiptSummary.cshtml
  - Views/Report/Index.cshtml
tests:
  - OpdAccrRptWeb.Tests/c12-report.test.js
-->

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

The system SHALL support paginated report results with a default page size of 10 and SHALL allow users to select 10, 30, or 50 rows per page. Every report result pagination control SHALL present, in order, a first-page button labeled `|<`, a previous-page button, the current page indicator, a next-page button, and a last-page button labeled `>|`. The first-page button SHALL navigate directly to page 1, and the last-page button SHALL navigate directly to the current result's `totalPages`. First/previous controls SHALL be disabled on page 1; next/last controls SHALL be disabled on the final page; navigation controls SHALL remain disabled while a server-paginated request is loading. New controls MUST reuse the existing pagination button markup classes and stylesheet behavior; the implementation MUST NOT add or modify pagination CSS rules. C171 and C174 SHALL obtain each displayed page from the server and SHALL use server-provided total-count and total-page metadata; reports not designated for server pagination SHALL retain client-side pagination. Page navigation SHALL preserve the active report filters and page size.

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

#### Scenario: Jump directly to the first or last page

- **WHEN** a paginated result is on an intermediate page and the user selects `|<` or `>|`
- **THEN** `|<` SHALL navigate directly to page 1 and `>|` SHALL navigate directly to `totalPages`
- **AND** the active report filters and page size SHALL remain unchanged
- **AND** a server-paginated report SHALL request the selected target page while a client-paginated report SHALL switch its local page

#### Scenario: Disable navigation at page boundaries

- **WHEN** the current page is page 1
- **THEN** the first-page and previous-page buttons SHALL be disabled
- **WHEN** the current page equals `totalPages`
- **THEN** the next-page and last-page buttons SHALL be disabled

#### Scenario: Preserve existing pagination styling

- **WHEN** first-page and last-page controls are added
- **THEN** they SHALL use the existing pagination button styling without any stylesheet modification

#### Scenario: Change a server-paginated report page size

- **WHEN** the user changes the C171 or C174 page size from 10 to 30
- **THEN** the UI SHALL reset the current page to 1
- **AND** the UI SHALL request page 1 with page size 30 from the server

#### Scenario: Display an empty server-paginated result

- **WHEN** a C171 or C174 query matches zero rows
- **THEN** the response SHALL contain an empty data collection, total count 0, and total pages 0
- **AND** the UI SHALL display the existing no-data state rather than page navigation


<!-- @trace
source: add-first-last-pagination-controls
updated: 2026-09-18
code:
  - OpdAccrRptWeb.Tests/C3ReportControllerTests.cs
  - Services/IC16ReportService.cs
  - Repositories/IC3ReportRepository.cs
  - Repositories/C143Sql.cs
  - OpdAccrRptWeb.Tests/C16RequestValidationTests.cs
  - Repositories/IC143AccountingBalanceDebtRepository.cs
  - Repositories/IC12ReportRepository.cs
  - ViewModels/C143AccountingBalanceDebtReportViewModel.cs
  - OpdAccrRptWeb.Tests/C12ReportControllerTests.cs
  - Services/C15LegacyReducer.cs
  - OpdAccrRptWeb.csproj
  - ViewModels/C144DebtDetailReportViewModel.cs
  - Services/C4OracleFailurePolicy.cs
  - Services/C4MaterialReportRenderer.cs
  - OpdAccrRptWeb.Tests/C16LegacyReducerTests.cs
  - Repositories/C12ReportRepository.cs
  - Repositories/C12Sql.cs
  - Services/OrganizationUnitCodeService.cs
  - Services/C3ServiceCollectionExtensions.cs
  - Models/OrganizationUnitModels.cs
  - ViewModels/C3ReportViewModel.cs
  - Repositories/IC15AssistiveDeviceDepositDetailRepository.cs
  - Services/C16ReportService.cs
  - OpdAccrRptWeb.Tests/C12ReportRepositoryTests.cs
  - OpdAccrRptWeb.Tests/C16ReportRepositoryTests.cs
  - Services/C16LegacyReducer.cs
  - Models/C3ReportModels.cs
  - Services/ReportService.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportRendererTests.cs
  - OpdAccrRptWeb.Tests/C15GoldenFixtureTests.cs
  - Views/Report/_C12MedicalReceiptSummary.cshtml
  - OpdAccrRptWeb.Tests/C143AccountingBalanceDebtReportServiceTests.cs
  - Repositories/IC4MaterialReportRepository.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - Repositories/C144DebtDetailReportRepository.cs
  - OpdAccrRptWeb.Tests/C144DebtDetailReportServiceTests.cs
  - Repositories/OrganizationUnitMappingRepository.cs
  - Sql/C4.Report.Daily.sql
  - Services/IC4MaterialReportService.cs
  - Services/C12ReportService.cs
  - Controllers/ReportController.cs
  - Repositories/ISectionMappingRepository.cs
  - Services/IReportService.cs
  - ViewModels/C12MedicalReceiptSummaryViewModel.cs
  - Services/C144DebtDetailReportService.cs
  - Controllers/C4MaterialReportController.cs
  - OpdAccrRptWeb.Tests/C3RequestValidationTests.cs
  - Repositories/IC144DebtDetailReportRepository.cs
  - Services/C3ReportService.cs
  - Services/IC144DebtDetailReportService.cs
  - Program.cs
  - Views/Report/_C3NursingStationChargePreview.cshtml
  - Services/IC3ReportService.cs
  - Models/C15WorkingRow.cs
  - Repositories/SectionMappingRepository.cs
  - Models/C4MaterialReportModels.cs
  - OpdAccrRptWeb.Tests/C3CoreTests.cs
  - Services/IC16LegacyReducer.cs
  - wwwroot/js/report-app.js
  - Services/IC144XlsxRenderer.cs
  - OpdAccrRptWeb.Tests/C144ReportControllerTests.cs
  - Repositories/C3Sql.cs
  - Services/ReportExportService.cs
  - ViewModels/C16MedicalSubsidyReportViewModel.cs
  - Views/Report/_TemplateReport.cshtml
  - Repositories/IOrganizationUnitMappingRepository.cs
  - Models/C15SourceRow.cs
  - OpdAccrRptWeb.Tests/C144DebtDetailReportRepositoryTests.cs
  - Services/IOrganizationUnitCodeService.cs
  - Repositories/C15AssistiveDeviceDepositDetailRepository.cs
  - wwwroot/css/site.css
  - Services/DepartmentAssignmentService.cs
  - Repositories/C143AccountingBalanceDebtRepository.cs
  - Models/C16ReportModels.cs
  - OpdAccrRptWeb.Tests/C143AccountingBalanceDebtRepositoryTests.cs
  - Services/C144XlsxRenderer.cs
  - ViewModels/SearchReportCondition.cs
  - Services/IC15LegacyReducer.cs
  - wwwroot/js/reports/report-template.js
  - Views/Report/Index.cshtml
  - Services/IC4MaterialReportRenderer.cs
  - Services/C143AccountingBalanceDebtReportService.cs
  - OpdAccrRptWeb.Tests/C15LegacyReducerTests.cs
  - OpdAccrRptWeb.Tests/C143ReportControllerTests.cs
  - Services/C4MaterialReportService.cs
  - OpdAccrRptWeb.Tests/C12ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C15ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportTests.cs
  - Views/Report/_SearchResults.cshtml
  - Repositories/C4MaterialReportRepository.cs
  - Repositories/C16ReportRepository.cs
  - Services/ReportCatalogService.cs
  - Repositories/IC16ReportRepository.cs
  - Views/Report/_C11ReceivablesCollectionReport.cshtml
  - Repositories/C144Sql.cs
  - OpdAccrRptWeb.Tests/C15AssistiveDeviceDepositDetailRepositoryTests.cs
  - Views/Report/_C16MedicalSubsidyPreview.cshtml
  - OpdAccrRptWeb.Tests/C144XlsxRendererTests.cs
  - Services/IC143AccountingBalanceDebtReportService.cs
  - ViewModels/C15AssistiveDeviceDepositDetailReportViewModel.cs
  - Services/DepartmentFilterResolver.cs
  - Views/C4/Preview.cshtml
  - Repositories/C3ReportRepository.cs
  - OpdAccrRptWeb.Tests/C16ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C15AssistiveDeviceDepositDetailReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C16ReportControllerTests.cs
  - ViewModels/C4MaterialReportViewModel.cs
tests:
  - OpdAccrRptWeb.Tests/c11-report.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
  - OpdAccrRptWeb.Tests/c4-report.test.js
  - OpdAccrRptWeb.Tests/c12-report.test.js
  - OpdAccrRptWeb.Tests/c3-report.test.js
-->

---
### Requirement: C18 server-paginated results

C18 SHALL use server-side pagination with a default page size of 10 and allowed page sizes of 10, 30, and 50. Every successful response SHALL include the requested page data, total count, page number, page size, and total pages for the selected date range and encounter source.

#### Scenario: Open a multi-page C18 result

- **WHEN** a C18 query matches 28 rows and the user has not changed the page size
- **THEN** the server SHALL return page 1 with 10 rows, total count 28, and total pages 3
- **AND** the UI SHALL display server page navigation

##### Example: C18 page boundaries

| Request | Returned rows | Total count | Total pages |
| ----- | ----- | ----- | ----- |
| page 1, size 10 | rows 1-10 | 28 | 3 |
| page 2, size 10 | rows 11-20 | 28 | 3 |
| page 3, size 10 | rows 21-28 | 28 | 3 |
| page 4, size 10 | empty | 28 | 3 |

#### Scenario: Navigate to another C18 page

- **WHEN** a user moves from C18 page 1 to page 2
- **THEN** the UI SHALL request page 2 with the unchanged date range, encounter source, and page size
- **AND** the UI SHALL replace the displayed rows with the returned page

#### Scenario: Display an empty C18 result

- **WHEN** a valid C18 query matches zero rows
- **THEN** the response SHALL contain an empty data collection, total count 0, and total pages 0
- **AND** the UI SHALL display the existing no-data state

---
### Requirement: Report result skeleton during data loading

The shared report result area SHALL display a table-shaped Skeleton Loader while report data is being requested. The Skeleton Loader SHALL use a stable four-column and three-row placeholder structure, SHALL replace rather than accompany stale result content, and SHALL NOT require report-column metadata from the pending response.

#### Scenario: Load the first query result

- **WHEN** a valid report query is pending
- **THEN** the result area SHALL display the table-shaped Skeleton Loader
- **AND** the result area SHALL NOT display the no-query, no-data, or result-table state at the same time

#### Scenario: Load another server-provided page

- **WHEN** a server-paginated report is requesting another page
- **THEN** the result area SHALL replace the previous page with the Skeleton Loader until the request completes
- **AND** pagination controls SHALL NOT be presented as an active result state during loading

#### Scenario: Reload after changing page size

- **WHEN** a user changes page size after completing a server-paginated query and the replacement request is pending
- **THEN** the result area SHALL display the same Skeleton Loader used for the initial query

#### Scenario: Finish loading with no rows

- **WHEN** a pending query completes successfully with zero rows
- **THEN** the Skeleton Loader SHALL be removed
- **AND** the result area SHALL display the no-data state

#### Scenario: Finish loading with an error

- **WHEN** a pending query fails
- **THEN** the Skeleton Loader SHALL be removed
- **AND** the existing query failure feedback SHALL be displayed

---
### Requirement: Motion preference for report skeleton

The Skeleton Loader SHALL use the site's native CSS without a third-party styling dependency. It SHALL display a shimmer animation under normal motion preferences and SHALL render a static, recognizable placeholder when the user requests reduced motion.

#### Scenario: Display with normal motion preference

- **WHEN** report data is loading and the user has not requested reduced motion
- **THEN** the Skeleton Loader SHALL display a shimmer animation

#### Scenario: Display with reduced motion preference

- **WHEN** report data is loading and the user has requested reduced motion
- **THEN** the Skeleton Loader SHALL remain visible without positional animation

---
### Requirement: C23 detail and summary result sets
Report results SHALL present C23 single-day detail rows and multi-day contract/item summaries with their corresponding detail rows. Both multi-day result sets SHALL use the same submitted filters, and paging or export SHALL preserve those filters.

#### Scenario: Display multi-day C23 results
- **WHEN** a valid multi-day C23 query succeeds
- **THEN** the user can view contract/item totals and corresponding detail data produced with the same date, source, mode, inpatient-type, and contract filters

---
### Requirement: C4 uses paged results and explicit document actions
The C4 result SHALL use the shared table, empty state, total count, page-size selector, and first, previous, next, and last controls. A successful condition SHALL expose one page-title preview/print action in the same placement as other reports, SHALL NOT expose a PDF export action, and any result-affecting condition change SHALL reset navigation to page one. The preview SHALL render inside the existing Report shell as a modal overlay with close and print actions, and SHALL NOT require a new browser tab.

#### Scenario: Navigate C4 results
- **WHEN** a C4 query returns more rows than the selected page size
- **THEN** the page displays the current rows, total count, and enabled applicable navigation controls
- **AND** the preview action retains the validated query conditions rather than a client-side subset

#### Scenario: Open C4 print preview
- **WHEN** a successful C4 result user selects preview
- **THEN** a modal overlay displays the C4 metadata and nine report columns inside the current Report shell
- **AND** the user can close the modal or print its content
- **AND** no new browser tab is required

---
### Requirement: Shared C3 paginated result experience
The shared report page SHALL render C3 through the existing pending skeleton, dynamic result table, empty state, total count, page-size selector, and first, previous, next, and last pagination controls. Summary and detail queries SHALL expose their applicable columns, and preview MUST be entered through an explicit action without replacing the normal result list.

#### Scenario: Query C3 successfully
- **WHEN** a user submits a valid C3 query
- **THEN** the shared skeleton is displayed while pending
- **AND** the applicable C3 columns, total count, and pagination are displayed after success

#### Scenario: Query C3 with no rows
- **WHEN** a valid C3 query returns zero rows
- **THEN** the established no-data state is displayed
- **AND** an empty preview document is not opened

---
### Requirement: C16 server-paginated detail results
C16 SHALL render its reduced detail rows in the shared table result area with the existing pending skeleton, empty state, total count, page-size selector, and first, previous, next, and last controls. Responses SHALL contain page data, total count, page number, page size, and total pages. Changing dates, source, report type, or effective date basis MUST reset the current page to one.

#### Scenario: Query a multi-page C16 result
- **WHEN** a valid C16 query reduces to 28 rows with default page size 10
- **THEN** the response contains rows 1 through 10, total count 28, page 1, page size 10, and total pages 3

#### Scenario: Change a C16 result filter
- **WHEN** a user changes source, report type, date range, or effective date basis after viewing another page
- **THEN** the next request loads page one and shows the shared skeleton while pending

---
### Requirement: C15 uses the shared detailed-result experience
The shared report page SHALL render C15 with the existing pending table skeleton, empty state, total count, page-size selector, and first, previous, next, and last pagination controls. It SHALL preserve C15 group headings, group totals, fixed blank columns, and legacy amount-column order.

#### Scenario: Pending C15 query
- **WHEN** a C15 query is awaiting a response
- **THEN** the shared table skeleton is visible and no report-specific spinner or text-only loading indicator is introduced

#### Scenario: C15 query returns no rows
- **WHEN** a valid C15 query returns zero canonical rows
- **THEN** the shared empty state is displayed and no blank preview is opened

#### Scenario: C15 query has multiple pages
- **WHEN** C15 TotalCount exceeds PageSize
- **THEN** the total count and first, previous, next, and last controls reflect the server pagination metadata


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
### Requirement: Shared pagination uses unambiguous double-angle boundary controls
Shared report pagination SHALL render the first-page control as `‹‹`, the previous-page control as `‹`, the next-page control as `›`, and the last-page control as `››`. The first and last controls MUST retain accessible labels that identify their actions.

#### Scenario: Paginated result controls
- **WHEN** any shared report result renders pagination
- **THEN** the four navigation controls appear in the order `‹‹`, `‹`, `›`, `››`, with accessible first-page and last-page labels

---
### Requirement: C13 server-paginated results

C13 SHALL obtain each normal result page from the server with a default page size of 10 and allowed page sizes of 10, 30, or 50. The response SHALL include server-provided total count, total pages, page number, and page size. A page navigation or page-size request SHALL preserve the submitted date range and SHALL never substitute client-side slicing of an all-row response.

#### Scenario: Display 28 C13 rows

- **WHEN** a C13 query matches 28 rows and requests page 1 with size 10
- **THEN** the server SHALL return rows 1 through 10, total count 28, and total pages 3
- **AND** the UI SHALL display the shared total-count and pagination controls

##### Example: C13 page boundaries

| Request | Returned rows | Total count | Total pages |
| ------- | ------------- | ----------- | ----------- |
| page 1, size 10 | rows 1-10 | 28 | 3 |
| page 2, size 10 | rows 11-20 | 28 | 3 |
| page 3, size 10 | rows 21-28 | 28 | 3 |
| page 4, size 10 | empty | 28 | 3 |

#### Scenario: Change C13 page size

- **WHEN** a user changes C13 page size from 10 to 30
- **THEN** the UI SHALL reset to page 1
- **AND** the server SHALL return at most 30 rows with unchanged date filters and updated total pages

#### Scenario: Display empty C13 result

- **WHEN** a valid C13 request has total count zero
- **THEN** the shared empty state SHALL be displayed
- **AND** total pages SHALL be zero and preview SHALL remain unavailable

---
### Requirement: C9 results use shared pagination and explicit preview
C9 SHALL render its mapped columns in the shared result table, display the server-provided total count, accept page sizes 10, 30, and 50, reset to page one when a result-affecting date changes, and provide an explicit preview action only after a valid query snapshot exists.

#### Scenario: Change the C9 page size
- **WHEN** a user changes page size from 10 to 30 after a valid query
- **THEN** C9 requests page one for the same normalized date range and displays the unchanged total count with up to 30 rows

#### Scenario: Preview remains in the current page
- **WHEN** a user activates C9 preview after a successful query
- **THEN** both printable documents appear in an accessible in-page modal with close and print actions and no new window, tab, or target-blank form

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


<!-- @trace
source: add-c18-referral-member-report
updated: 2026-08-21
code:
  - ViewModels/PagedReportResult.cs
  - ViewModels/HealthCenterCountViewModel.cs
  - ViewModels/ReferralMemberReportViewModel.cs
  - Repositories/ReferralMemberRepository.cs
  - Repositories/IReferralMemberRepository.cs
  - wwwroot/js/reports/report-template.js
  - ViewModels/HealthCheckupVisits.cs
  - ViewModels/SearchReportCondition.cs
  - Views/Report/_TemplateReport.cshtml
  - ViewModels/HealthCenterContractBillingReport.cs
  - ViewModels/HealthCenterDetailViewModel.cs
  - Program.cs
  - wwwroot/js/report-app.js
  - OpdAccrRptWeb.Tests/ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/ReferralMemberRepositoryTests.cs
  - OpdAccrRptWeb.Tests/GlobalUsings.cs
  - Properties/AssemblyInfo.cs
  - Services/ReportService.cs
  - OpdAccrRptWeb.Tests/OpdAccrRptWeb.Tests.csproj
  - OpdAccrRptWeb.Tests/FileLoggingTests.cs
  - Controllers/ReportController.cs
  - Services/IReportTotalCountCache.cs
  - OpdAccrRptWeb.Tests/ReportTotalCountCacheTests.cs
  - Infrastructure/FileLoggingConfiguration.cs
  - OpdAccrRptWeb.Tests/HealthCenterRepositoryTests.cs
  - OpdAccrRptWeb.Tests/TestDoubles.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - Services/ReportTotalCountCache.cs
  - wwwroot/css/site.css
tests:
  - OpdAccrRptWeb.Tests/report-template.test.js
-->


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


<!-- @trace
source: add-c13-high-risk-emergency-detail
updated: 2026-09-18
code:
  - Services/DepartmentFilterResolver.cs
  - OpdAccrRptWeb.Tests/C16ReportControllerTests.cs
  - Services/IC16LegacyReducer.cs
  - OpdAccrRptWeb.csproj
  - OpdAccrRptWeb.Tests/C144DebtDetailReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C13ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/C13HighRiskEmergencyRepositoryTests.cs
  - Repositories/IC15AssistiveDeviceDepositDetailRepository.cs
  - Services/ReportExportService.cs
  - OpdAccrRptWeb.Tests/C16ReportRepositoryTests.cs
  - Services/C144XlsxRenderer.cs
  - Views/Report/_C11ReceivablesCollectionReport.cshtml
  - Repositories/SectionMappingRepository.cs
  - Services/C13HighRiskEmergencyReportService.cs
  - Models/C4MaterialReportModels.cs
  - Services/IC3ReportService.cs
  - ViewModels/C4MaterialReportViewModel.cs
  - Views/Report/_C13HighRiskEmergencyPreview.cshtml
  - Sql/C4.Report.Daily.sql
  - wwwroot/css/site.css
  - ViewModels/C12MedicalReceiptSummaryViewModel.cs
  - Services/IC15LegacyReducer.cs
  - OpdAccrRptWeb.Tests/C144ReportControllerTests.cs
  - Services/IOrganizationUnitCodeService.cs
  - Models/C15WorkingRow.cs
  - Repositories/IC13HighRiskEmergencyRepository.cs
  - OpdAccrRptWeb.Tests/C16RequestValidationTests.cs
  - OpdAccrRptWeb.Tests/C144DebtDetailReportRepositoryTests.cs
  - OpdAccrRptWeb.Tests/C16LegacyReducerTests.cs
  - OpdAccrRptWeb.Tests/C143AccountingBalanceDebtReportServiceTests.cs
  - Repositories/C143AccountingBalanceDebtRepository.cs
  - Services/ReportCatalogService.cs
  - Services/DepartmentAssignmentService.cs
  - Services/IC144DebtDetailReportService.cs
  - Services/IC143AccountingBalanceDebtReportService.cs
  - Repositories/C3ReportRepository.cs
  - OpdAccrRptWeb.Tests/C3RequestValidationTests.cs
  - Services/IReportService.cs
  - ViewModels/C144DebtDetailReportViewModel.cs
  - Repositories/C16ReportRepository.cs
  - ViewModels/C3ReportViewModel.cs
  - Models/C16ReportModels.cs
  - OpdAccrRptWeb.Tests/C13ReportServiceIntegrationTests.cs
  - Services/C3ServiceCollectionExtensions.cs
  - Services/C16LegacyReducer.cs
  - wwwroot/js/reports/report-template.js
  - Models/C15SourceRow.cs
  - Repositories/C144DebtDetailReportRepository.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - Services/C4OracleFailurePolicy.cs
  - ViewModels/C15AssistiveDeviceDepositDetailReportViewModel.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportTests.cs
  - Repositories/IC16ReportRepository.cs
  - OpdAccrRptWeb.Tests/C15AssistiveDeviceDepositDetailReportServiceTests.cs
  - Services/C15LegacyReducer.cs
  - Controllers/ReportController.cs
  - Services/IC4MaterialReportRenderer.cs
  - OpdAccrRptWeb.Tests/C15LegacyReducerTests.cs
  - Views/Report/_C12MedicalReceiptSummary.cshtml
  - OpdAccrRptWeb.Tests/C4MaterialReportRendererTests.cs
  - Views/Report/Index.cshtml
  - Repositories/IC12ReportRepository.cs
  - ViewModels/C143AccountingBalanceDebtReportViewModel.cs
  - Services/C144DebtDetailReportService.cs
  - Repositories/C15AssistiveDeviceDepositDetailRepository.cs
  - Views/Report/_SearchResults.cshtml
  - Program.cs
  - Services/IC13HighRiskEmergencyReportService.cs
  - Repositories/IOrganizationUnitMappingRepository.cs
  - OpdAccrRptWeb.Tests/C12ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/C15ReportControllerTests.cs
  - Repositories/ISectionMappingRepository.cs
  - Views/Report/_C16MedicalSubsidyPreview.cshtml
  - Models/C3ReportModels.cs
  - OpdAccrRptWeb.Tests/C3ReportControllerTests.cs
  - Repositories/IC4MaterialReportRepository.cs
  - Services/C4MaterialReportRenderer.cs
  - Repositories/C4MaterialReportRepository.cs
  - ViewModels/C16MedicalSubsidyReportViewModel.cs
  - OpdAccrRptWeb.Tests/C3CoreTests.cs
  - Services/IC4MaterialReportService.cs
  - Repositories/IC144DebtDetailReportRepository.cs
  - Views/Report/_C3NursingStationChargePreview.cshtml
  - Repositories/IC3ReportRepository.cs
  - Repositories/C12Sql.cs
  - wwwroot/js/report-app.js
  - OpdAccrRptWeb.Tests/C15GoldenFixtureTests.cs
  - OpdAccrRptWeb.Tests/C15AssistiveDeviceDepositDetailRepositoryTests.cs
  - Services/C16ReportService.cs
  - OpdAccrRptWeb.Tests/C143AccountingBalanceDebtRepositoryTests.cs
  - OpdAccrRptWeb.Tests/C13HighRiskEmergencyReportServiceTests.cs
  - Repositories/C13HighRiskEmergencyRepository.cs
  - Services/C143AccountingBalanceDebtReportService.cs
  - OpdAccrRptWeb.Tests/C144XlsxRendererTests.cs
  - OpdAccrRptWeb.Tests/C143ReportControllerTests.cs
  - Services/IC144XlsxRenderer.cs
  - Repositories/C144Sql.cs
  - Repositories/C143Sql.cs
  - OpdAccrRptWeb.Tests/C12ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C16ReportServiceTests.cs
  - Services/C4MaterialReportService.cs
  - Services/OrganizationUnitCodeService.cs
  - Repositories/IC143AccountingBalanceDebtRepository.cs
  - Services/C3ReportService.cs
  - Repositories/C3Sql.cs
  - Repositories/C12ReportRepository.cs
  - Repositories/OrganizationUnitMappingRepository.cs
  - Services/ReportService.cs
  - ViewModels/C13HighRiskEmergencyReportViewModel.cs
  - Views/Report/_TemplateReport.cshtml
  - Controllers/C4MaterialReportController.cs
  - ViewModels/SearchReportCondition.cs
  - Services/IC16ReportService.cs
  - Models/OrganizationUnitModels.cs
  - Views/C4/Preview.cshtml
  - OpdAccrRptWeb.Tests/C12ReportRepositoryTests.cs
  - Services/C12ReportService.cs
tests:
  - OpdAccrRptWeb.Tests/c12-report.test.js
  - OpdAccrRptWeb.Tests/c11-report.test.js
  - OpdAccrRptWeb.Tests/c3-report.test.js
  - OpdAccrRptWeb.Tests/c4-report.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
-->


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

### Requirement: Excel export

系統 SHALL 允許使用者將符合目前查詢條件的結果匯出為 Excel 相容檔案。

#### Scenario: Export current query result

- **WHEN** 使用者在查詢成功後執行匯出
- **THEN** 系統 SHALL 產生對應目前報表與查詢條件的 Excel 相容檔案
- **AND** 匯出範圍 SHALL NOT 僅限於目前顯示頁

<!-- @trace
source: add-c174-excel-export
updated: 2026-08-26
code:
  - ViewModels/ReferralMemberReportViewModel.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/ReportServiceTests.cs
  - Repositories/ISafeNeedleRepository.cs
  - OpdAccrRptWeb.Tests/SafeNeedleRepositoryTests.cs
  - Services/IReportExportService.cs
  - Services/ReportExportOptionsValidator.cs
  - Services/ReportExportService.cs
  - wwwroot/js/report-app.js
  - ViewModels/SafeNeedleReportViewModel.cs
  - ViewModels/HealthCenterCountViewModel.cs
  - Repositories/SafeNeedleRepository.cs
  - ViewModels/SurgicalAccountingReportViewModel.cs
  - OpdAccrRptWeb.Tests/TestDoubles.cs
  - OpdAccrRptWeb.Tests/GlobalUsings.cs
  - document/C22.md
  - Repositories/ReferralMemberRepository.cs
  - OpdAccrRptWeb.Tests/CashierCashRepositoryTests.cs
  - Repositories/IHealthCenterRepository.cs
  - Views/Report/_TemplateReport.cshtml
  - OpdAccrRptWeb.Tests/FileLoggingTests.cs
  - Repositories/CashierCashRepository.cs
  - ViewModels/HealthCenterContractBillingReport.cs
  - OpdAccrRptWeb.Tests/ReportExportServiceTests.cs
  - Repositories/ISurgicalAccountingRepository.cs
  - ViewModels/HealthCheckupVisits.cs
  - Repositories/IReferralMemberRepository.cs
  - appsettings.json
  - Services/IReportTotalCountCache.cs
  - OpdAccrRptWeb.Tests/ReportExportOptionsTests.cs
  - ViewModels/CashierCashReportViewModel.cs
  - OpdAccrRptWeb.Tests/ConnectionStringProviderTests.cs
  - ViewModels/HealthCenterDetailViewModel.cs
  - Views/Report/_TableSkeleton.cshtml
  - Services/ReportExportOptions.cs
  - OpdAccrRptWeb.Tests/SurgicalAccountingRepositoryTests.cs
  - ViewModels/ReportExportJobResponse.cs
  - Services/ReportTotalCountCache.cs
  - OpdAccrRptWeb.Tests/ReportTotalCountCacheTests.cs
  - OpdAccrRptWeb.Tests/OpdAccrRptWeb.Tests.csproj
  - OpdAccrRptWeb.Tests/HealthCenterRepositoryTests.cs
  - Repositories/ICashierCashRepository.cs
  - ViewModels/PagedReportResult.cs
  - OpdAccrRptWeb.Tests/BackgroundReportExportServiceTests.cs
  - Program.cs
  - Properties/AssemblyInfo.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/ReferralMemberRepositoryTests.cs
  - Services/ReportExportJobStore.cs
  - ViewModels/SearchReportCondition.cs
  - OpdAccrRptWeb.csproj
  - Repositories/HealthCenterRepository.cs
  - wwwroot/css/site.css
  - Repositories/SurgicalAccountingRepository.cs
  - wwwroot/js/reports/report-template.js
  - Services/ReportService.cs
  - Infrastructure/FileLoggingConfiguration.cs
  - Services/BackgroundReportExportService.cs
  - OpdAccrRptWeb.Tests/ReportExportJobStoreTests.cs
  - OpdAccrRptWeb.Tests/CashierCashReportServiceTests.cs
tests:
  - OpdAccrRptWeb.Tests/report-template.test.js
-->