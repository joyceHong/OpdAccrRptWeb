# Report Query Specification

## Purpose

定義報表共用查詢條件、驗證及送出行為；個別報表可在此基礎上增加專屬條件。

## Requirements

### Requirement: Shared simple report component

All standard report query pages SHALL reuse the established page title, query panel, panel title, date row, source fieldset, advanced-condition grid, required-field marker, form actions, shared table skeleton, result heading, and empty-state structures and their shared CSS behavior. Report-specific query controls SHALL express differences through configuration and field composition rather than divergent visual behavior. Print-preview documents SHALL be permitted to use report-specific layout and print styles.

#### Scenario: Open reports with the same interaction pattern

- **WHEN** users open two standard report query pages with different query fields
- **THEN** both pages use the same shared query structure, spacing, input styling, action placement, pending skeleton, result heading, and empty-state behavior
- **AND** each page displays its own report code, name, conditions, and result content

#### Scenario: Open a report-specific print preview

- **WHEN** a report has an approved document-specific print contract
- **THEN** its print preview can use report-specific layout and print styles
- **AND** its normal query page continues to use the global shared UI behavior


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
### Requirement: Required date range

支援查詢的報表 SHALL 提供必填的起始日期與截止日期，初始值 SHALL 為使用者開啟頁面當日。

#### Scenario: Open a queryable report

- **WHEN** 使用者開啟已支援查詢的報表
- **THEN** 起始日期與截止日期 SHALL 預設為當日

#### Scenario: Submit an invalid date range

- **WHEN** 使用者輸入的起始日期晚於截止日期
- **THEN** 系統 SHALL 拒絕送出查詢
- **AND** 系統 SHALL 顯示可理解的日期範圍錯誤訊息

---
### Requirement: ROC date conversion boundary

前端 SHALL 使用可供使用者操作的日期格式，後端 MAY 在資料存取邊界將日期轉換為既有資料來源所需的民國日期格式。

#### Scenario: Submit a valid Gregorian date range

- **WHEN** 使用者以西元日期送出有效日期範圍
- **THEN** 系統 SHALL 以等值日期執行查詢
- **AND** 日期格式轉換 SHALL NOT 改變使用者選取的日期範圍

---
### Requirement: Optional advanced conditions

系統 SHALL 允許各報表定義選填的進階條件，例如科別、診間、醫院代碼或批價碼。

#### Scenario: Query without advanced conditions

- **WHEN** 使用者僅填寫必要日期條件
- **THEN** 系統 SHALL 允許送出查詢

---
### Requirement: Reset query conditions

系統 SHALL 提供重設功能，使查詢條件回復該報表的初始狀態。

#### Scenario: Reset edited conditions

- **WHEN** 使用者修改條件後執行重設
- **THEN** 系統 SHALL 清除選填條件
- **AND** 必填條件 SHALL 回復預設值

---
### Requirement: Report-specific query dispatch

系統 SHALL 依所選報表代碼呼叫該報表獨立的後端查詢流程，不得以其他報表的查詢替代。C25 SHALL 使用既有共用報表元件與分頁 request/response lifecycle，不得為相同互動另建專用元件。

#### Scenario: Submit C172 query

- **WHEN** 使用者在 `C172` 報表送出有效條件
- **THEN** 系統 SHALL 執行健康管理中心金額統計的查詢流程

#### Scenario: Submit C25 query

- **WHEN** 使用者在 `C25` 報表送出有效日期與分頁條件
- **THEN** 系統 SHALL 執行住院預收醫療費餘額的截止日快照查詢流程
- **AND** 系統 SHALL 透過共用報表元件呈現結果及分頁

#### Scenario: Submit an unsupported report

- **WHEN** 使用者對尚未支援的報表送出請求
- **THEN** 系統 SHALL 回傳明確的未支援結果
- **AND** 系統 SHALL NOT 靜默回傳另一報表的資料


<!-- @trace
source: add-c25-inpatient-advance-payment-balance
updated: 2026-08-27
code:
  - Repositories/ISafeNeedleRepository.cs
  - ViewModels/PagedReportResult.cs
  - Properties/AssemblyInfo.cs
  - OpdAccrRptWeb.Tests/ReportTotalCountCacheTests.cs
  - Services/IReportTotalCountCache.cs
  - ViewModels/CashierCashReportViewModel.cs
  - OpdAccrRptWeb.Tests/SafeNeedleRepositoryTests.cs
  - OpdAccrRptWeb.Tests/HealthCenterRepositoryTests.cs
  - Repositories/InpatientAdvancePaymentBalanceRepository.cs
  - Repositories/SafeNeedleRepository.cs
  - Repositories/SurgicalAccountingRepository.cs
  - document/C22.md
  - OpdAccrRptWeb.Tests/InpatientAdvancePaymentBalanceReportServiceTests.cs
  - Services/IReportExportService.cs
  - OpdAccrRptWeb.Tests/CashierCashReportServiceTests.cs
  - wwwroot/js/reports/report-template.js
  - OpdAccrRptWeb.Tests/ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/OpdAccrRptWeb.Tests.csproj
  - OpdAccrRptWeb.Tests/ReportExportOptionsTests.cs
  - Repositories/ReferralMemberRepository.cs
  - ViewModels/HealthCenterContractBillingReport.cs
  - OpdAccrRptWeb.Tests/ReportExportServiceTests.cs
  - Repositories/ICashierCashRepository.cs
  - Repositories/ISurgicalAccountingRepository.cs
  - Program.cs
  - Services/ReportExportService.cs
  - ViewModels/SearchReportCondition.cs
  - Services/ReportExportOptions.cs
  - Repositories/CashierCashRepository.cs
  - Views/Report/_TemplateReport.cshtml
  - Services/ReportExportOptionsValidator.cs
  - OpdAccrRptWeb.Tests/GlobalUsings.cs
  - OpdAccrRptWeb.Tests/ConnectionStringProviderTests.cs
  - ViewModels/ReferralMemberReportViewModel.cs
  - ViewModels/HealthCenterCountViewModel.cs
  - ViewModels/HealthCheckupVisits.cs
  - OpdAccrRptWeb.Tests/ReportControllerTests.cs
  - ViewModels/ReportExportJobResponse.cs
  - Repositories/IReferralMemberRepository.cs
  - OpdAccrRptWeb.Tests/BackgroundReportExportServiceTests.cs
  - OpdAccrRptWeb.Tests/CashierCashRepositoryTests.cs
  - ViewModels/InpatientAdvancePaymentBalanceReportViewModel.cs
  - Views/Report/_TableSkeleton.cshtml
  - OpdAccrRptWeb.Tests/ReferralMemberRepositoryTests.cs
  - Services/BackgroundReportExportService.cs
  - ViewModels/SurgicalAccountingReportViewModel.cs
  - ViewModels/HealthCenterDetailViewModel.cs
  - OpdAccrRptWeb.Tests/TestDoubles.cs
  - Services/ReportExportJobStore.cs
  - ViewModels/SafeNeedleReportViewModel.cs
  - OpdAccrRptWeb.Tests/InpatientAdvancePaymentBalanceRepositoryTests.cs
  - OpdAccrRptWeb.Tests/ReportExportJobStoreTests.cs
  - Services/ReportService.cs
  - OpdAccrRptWeb.Tests/FileLoggingTests.cs
  - Controllers/ReportController.cs
  - Infrastructure/FileLoggingConfiguration.cs
  - Services/ReportTotalCountCache.cs
  - wwwroot/js/report-app.js
  - OpdAccrRptWeb.Tests/SurgicalAccountingRepositoryTests.cs
  - Repositories/IInpatientAdvancePaymentBalanceRepository.cs
tests:
  - OpdAccrRptWeb.Tests/report-template.test.js
-->

---
### Requirement: Report-configured query fields

The shared report component SHALL use report configuration to determine report-specific query fields, their allowed values, their initial values, and their reset values. A report-specific field SHALL remain part of the shared form, validation, request, and pagination lifecycle without requiring a complete report-specific component.

#### Scenario: Display the C18 source field

- **WHEN** a user opens C18
- **THEN** the shared report component SHALL display an encounter-source field with `Emergency` and `Inpatient` choices
- **AND** `Emergency` SHALL be selected

#### Scenario: Open a report without a source field

- **WHEN** a user opens a report whose configuration does not define encounter source
- **THEN** the shared report component SHALL NOT display or submit the encounter-source field for that report

#### Scenario: Submit a configured source

- **WHEN** a user selects `Inpatient` for C18 and submits the form
- **THEN** the request SHALL include `EncounterSource` with value `Inpatient`

#### Scenario: Preserve source during server pagination

- **WHEN** a C18 user navigates to another page or changes page size
- **THEN** each follow-up request SHALL retain the selected encounter source

#### Scenario: Reset a configured source

- **WHEN** a C18 user resets the query form
- **THEN** the encounter source SHALL return to its configured `Emergency` default

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

---
### Requirement: Accessible report query busy state

The shared report result panel SHALL expose a busy state while report data is being requested. The busy state SHALL be driven by the same loading state that disables query and pagination controls, SHALL contain one non-visual status message identifying that report data is loading, and SHALL be cleared after both successful and failed requests.

#### Scenario: Submit a valid report query

- **WHEN** a user submits valid query conditions and the data request is pending
- **THEN** the report result panel SHALL expose that it is busy
- **AND** assistive technology SHALL have access to one status message indicating that report data is loading

#### Scenario: Complete or fail a report query

- **WHEN** a pending report request either returns a response or fails
- **THEN** the report result panel SHALL no longer expose that it is busy
- **AND** the UI SHALL present the applicable result, empty-result, or error state

---
### Requirement: Report-specific rebuild controls
The shared report query screen SHALL render a rebuild control only when the selected report declares rebuild support and that report's server-provided RebuildEnabled setting is true. Hiding the control MUST NOT replace server-side validation of submitted rebuild flags.

#### Scenario: C23 rebuild is disabled by configuration
- **WHEN** C23 is selected and the server reports C23 RebuildEnabled as false
- **THEN** the query screen does not render the rebuild checkbox and the request state keeps ForceRebuild false

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
### Requirement: C4 uses the shared query experience
The canonical C4 query route SHALL be `/Report/C4` and SHALL render inside the existing Report application shell through the shared `ReportTemplate` component. It SHALL retain the global header, sidebar menu bar, breadcrumb, content layout, shared report page structure, Gregorian HTML date controls, pending table skeleton, validation presentation, result table, empty state, pagination styling, and result reset behavior. It MUST NOT redirect normal report navigation to a standalone full-page C4 document. Its optional organization-unit autocomplete SHALL display new codes in a floating candidate panel immediately below and equal in width to its input. It SHALL retain the selected candidate's normalized legacy code as the section prefix. Direct new-code input SHALL be converted to a unique legacy code through the shared organization-unit mapping service before query or preview submission. Editing the input after selection SHALL clear the stale selected code. Missing or ambiguous direct-input mappings MUST stop submission and MUST NOT run an unfiltered query.

#### Scenario: Submit C4 query
- **WHEN** a user chooses dates and an organization-unit candidate then submits C4
- **THEN** the shared skeleton remains visible until the request settles
- **AND** the payload contains Gregorian dates and the candidate legacy code

#### Scenario: Enter a C4 organization code directly
- **WHEN** a user enters new code ` 11910 ` without selecting a candidate and submits C4
- **THEN** the shared organization-unit service resolves legacy code `0201`
- **AND** the payload contains section prefix `0201`
- **AND** the query result is filtered instead of returning all departments

#### Scenario: Display C4 organization candidates
- **WHEN** a C4 organization search returns candidates
- **THEN** they appear in a floating panel immediately below the input
- **AND** the panel width equals the input container width
- **AND** every candidate presents its new code and display name while retaining its legacy code for submission

#### Scenario: Navigate to C4 from the report menu
- **WHEN** a user selects C4 from the existing sidebar menu
- **THEN** the URL is `/Report/C4`
- **AND** the global header, sidebar menu, breadcrumb, and shared report panels remain visible
- **AND** no automatic navigation to `/reports/c4` occurs

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
### Requirement: C3 Gregorian and multi-code query controls
The C3 query page SHALL use the shared report page structure and Gregorian HTML date inputs. It SHALL accept room and charge codes as safe multi-value controls or comma-delimited values that are parsed, trimmed, uppercased, and individually bound. It MUST NOT expose ROC free-text date entry or legacy SQL-list syntax.

#### Scenario: Submit C3 query controls
- **WHEN** a user enters valid Gregorian dates and comma-delimited room and charge codes
- **THEN** the pending state uses the shared table skeleton
- **AND** the submitted condition contains normalized code arrays rather than a SQL fragment

#### Scenario: Change a result-affecting condition
- **WHEN** a user changes any C3 date, source, detail, logistics, department, room, or charge condition
- **THEN** the next query starts at page one

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
### Requirement: C15 uses date-range server pagination queries
The shared report query contract SHALL accept C15 StartDate, EndDate, Page, and PageSize. Changing either C15 date SHALL reset Page to one, and the browser SHALL use Gregorian date controls while the server converts the values to ROC dates before repository access.

#### Scenario: Date changes after viewing a later page
- **WHEN** a user on a page after page one changes the C15 start date or end date and submits
- **THEN** the query requests page one with the new date range

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
### Requirement: Shared report autocomplete presentation
Report query fields that provide autocomplete SHALL use the shared report-autocomplete presentation and interaction contract. The suggestion panel SHALL appear immediately below its input, SHALL match the input width, SHALL use rounded corners, a visible border, elevation, bounded vertical scrolling, and distinct hover and active-option states. The control SHALL support pointer selection, ArrowUp, ArrowDown, Enter, Escape, outside-click dismissal, and combobox/listbox/option accessibility semantics. Direct entry of an allowed free-form code SHALL remain available when the report permits it.

#### Scenario: Open autocomplete suggestions
- **WHEN** a user focuses a report autocomplete input with matching candidates
- **THEN** the suggestion panel opens directly below the input at the same width
- **AND** the panel remains within the field width at desktop and narrow viewport sizes

#### Scenario: Select with keyboard
- **WHEN** the suggestion panel is open and the user moves the active option with ArrowDown or ArrowUp and presses Enter
- **THEN** the active option value is written to the report field and the panel closes

#### Scenario: Dismiss suggestions
- **WHEN** the panel is open and the user presses Escape or clicks outside the autocomplete container
- **THEN** the panel closes without changing the current input value

#### Scenario: Reuse presentation across reports
- **WHEN** C12 new-section autocomplete and C211 contract autocomplete are rendered
- **THEN** both controls use the same panel geometry, rounded styling, option states, and accessibility behavior
- **AND** each control retains its report-specific candidate labels and submitted code value

<!-- @trace
source: fix-c12-new-section-filter
updated: 2026-09-18
code:
  - OpdAccrRptWeb.Tests/C4MaterialReportControllerTests.cs
  - OpdAccrRptWeb.Tests/C15AssistiveDeviceDepositDetailReportServiceTests.cs
  - Views/C4/Preview.cshtml
  - Views/Report/_SearchResults.cshtml
  - OpdAccrRptWeb.Tests/C12ReportServiceTests.cs
  - OpdAccrRptWeb.Tests/C143AccountingBalanceDebtRepositoryTests.cs
  - Services/C16LegacyReducer.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportTests.cs
  - Services/IC144DebtDetailReportService.cs
  - Repositories/C3ReportRepository.cs
  - Views/Report/_C3NursingStationChargePreview.cshtml
  - Services/C144XlsxRenderer.cs
  - ViewModels/C12MedicalReceiptSummaryViewModel.cs
  - OpdAccrRptWeb.Tests/C3CoreTests.cs
  - Repositories/C144Sql.cs
  - Repositories/C4MaterialReportRepository.cs
  - Repositories/IC143AccountingBalanceDebtRepository.cs
  - Views/Report/_C16MedicalSubsidyPreview.cshtml
  - ViewModels/C4MaterialReportViewModel.cs
  - OpdAccrRptWeb.Tests/C144DebtDetailReportServiceTests.cs
  - Services/IC4MaterialReportRenderer.cs
  - Services/C143AccountingBalanceDebtReportService.cs
  - Services/IOrganizationUnitCodeService.cs
  - Models/OrganizationUnitModels.cs
  - OpdAccrRptWeb.Tests/C15GoldenFixtureTests.cs
  - OpdAccrRptWeb.Tests/C12ReportControllerTests.cs
  - Models/C15SourceRow.cs
  - ViewModels/C15AssistiveDeviceDepositDetailReportViewModel.cs
  - Controllers/C4MaterialReportController.cs
  - Models/C4MaterialReportModels.cs
  - ViewModels/C143AccountingBalanceDebtReportViewModel.cs
  - Models/C16ReportModels.cs
  - Services/C144DebtDetailReportService.cs
  - Services/ReportCatalogService.cs
  - OpdAccrRptWeb.Tests/C16ReportControllerTests.cs
  - Repositories/ISectionMappingRepository.cs
  - Views/Report/_C12MedicalReceiptSummary.cshtml
  - Services/C12ReportService.cs
  - Services/IC144XlsxRenderer.cs
  - OpdAccrRptWeb.Tests/C12ReportRepositoryTests.cs
  - Services/OrganizationUnitCodeService.cs
  - OpdAccrRptWeb.Tests/C144ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/C15LegacyReducerTests.cs
  - OpdAccrRptWeb.Tests/C15AssistiveDeviceDepositDetailRepositoryTests.cs
  - Repositories/IC12ReportRepository.cs
  - Services/C3ReportService.cs
  - OpdAccrRptWeb.Tests/C143ReportControllerTests.cs
  - Repositories/C15AssistiveDeviceDepositDetailRepository.cs
  - Services/IC15LegacyReducer.cs
  - Views/Report/_C11ReceivablesCollectionReport.cshtml
  - wwwroot/css/site.css
  - ViewModels/C144DebtDetailReportViewModel.cs
  - ViewModels/C3ReportViewModel.cs
  - OpdAccrRptWeb.Tests/C16LegacyReducerTests.cs
  - Services/C15LegacyReducer.cs
  - Services/ReportService.cs
  - wwwroot/js/reports/report-template.js
  - OpdAccrRptWeb.Tests/C16RequestValidationTests.cs
  - ViewModels/C16MedicalSubsidyReportViewModel.cs
  - Services/C3ServiceCollectionExtensions.cs
  - Repositories/IC4MaterialReportRepository.cs
  - OpdAccrRptWeb.Tests/C144XlsxRendererTests.cs
  - OpdAccrRptWeb.Tests/C3ReportControllerTests.cs
  - Repositories/IC15AssistiveDeviceDepositDetailRepository.cs
  - Repositories/C143Sql.cs
  - OpdAccrRptWeb.Tests/C143AccountingBalanceDebtReportServiceTests.cs
  - Repositories/C143AccountingBalanceDebtRepository.cs
  - Repositories/OrganizationUnitMappingRepository.cs
  - ViewModels/SearchReportCondition.cs
  - Repositories/IOrganizationUnitMappingRepository.cs
  - Services/C16ReportService.cs
  - OpdAccrRptWeb.Tests/C144DebtDetailReportRepositoryTests.cs
  - Repositories/C3Sql.cs
  - Services/ReportExportService.cs
  - Repositories/SectionMappingRepository.cs
  - Controllers/ReportController.cs
  - OpdAccrRptWeb.Tests/C16ReportServiceTests.cs
  - Repositories/C12ReportRepository.cs
  - Repositories/C144DebtDetailReportRepository.cs
  - Services/IC16LegacyReducer.cs
  - Services/C4MaterialReportRenderer.cs
  - Models/C15WorkingRow.cs
  - Services/C4MaterialReportService.cs
  - Models/C3ReportModels.cs
  - Repositories/C12Sql.cs
  - Repositories/IC144DebtDetailReportRepository.cs
  - Program.cs
  - OpdAccrRptWeb.Tests/C16ReportRepositoryTests.cs
  - OpdAccrRptWeb.Tests/C4MaterialReportRendererTests.cs
  - Views/Report/_TemplateReport.cshtml
  - Views/Report/Index.cshtml
  - Services/IC16ReportService.cs
  - Services/IC143AccountingBalanceDebtReportService.cs
  - wwwroot/js/report-app.js
  - Services/DepartmentFilterResolver.cs
  - Services/IC4MaterialReportService.cs
  - Repositories/C16ReportRepository.cs
  - Services/IReportService.cs
  - Repositories/IC16ReportRepository.cs
  - Sql/C4.Report.Daily.sql
  - Services/C4OracleFailurePolicy.cs
  - Repositories/IC3ReportRepository.cs
  - OpdAccrRptWeb.Tests/C15ReportControllerTests.cs
  - OpdAccrRptWeb.Tests/C3RequestValidationTests.cs
  - Services/DepartmentAssignmentService.cs
  - Services/IC3ReportService.cs
  - OpdAccrRptWeb.csproj
tests:
  - OpdAccrRptWeb.Tests/c4-report.test.js
  - OpdAccrRptWeb.Tests/c12-report.test.js
  - OpdAccrRptWeb.Tests/c3-report.test.js
  - OpdAccrRptWeb.Tests/c11-report.test.js
  - OpdAccrRptWeb.Tests/report-template.test.js
-->

---
### Requirement: C13 shared report query lifecycle

The shared report component SHALL route C13 through its configured Gregorian date form, validation, reset, loading, error, empty-result, result-table, and pagination lifecycle. C13 SHALL use the shared table skeleton while a query or page request is pending and SHALL NOT introduce a report-specific spinner, hourglass, funnel, or text-only loader.

#### Scenario: Submit C13 query

- **WHEN** a user submits valid C13 start and end dates
- **THEN** the server SHALL execute the C13-specific query flow
- **AND** the pending result panel SHALL display the shared skeleton and accessible busy status

#### Scenario: Reset C13 query

- **WHEN** a user resets edited C13 query dates
- **THEN** both date fields SHALL return to the report's configured initial Gregorian dates
- **AND** prior rows, errors, and pagination state SHALL be cleared

#### Scenario: Reject invalid C13 dates

- **WHEN** C13 receives a missing date, an invalid Gregorian date, or a start date later than the end date
- **THEN** the system SHALL reject the request before Oracle access
- **AND** the UI SHALL display a comprehensible validation message


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

---
### Requirement: Conditional advanced-filter section

The shared report component SHALL render the `進階條件（選填）` section only when the selected report defines at least one advanced filter. When the advanced-filter collection is absent or empty, the component MUST NOT render the section heading, container, or reserved spacing. This behavior SHALL be derived from report filter configuration and SHALL NOT use a C13-specific report-code check.

#### Scenario: Report has no advanced filters

- **WHEN** C13 or another report supplies no advanced-filter definitions
- **THEN** the query page SHALL omit the complete `進階條件（選填）` section
- **AND** the basic query fields and actions SHALL remain visible and usable

#### Scenario: Report has configured advanced filters

- **WHEN** a report supplies one or more advanced-filter definitions
- **THEN** the query page SHALL render the existing `進階條件（選填）` section
- **AND** its fields, defaults, and submitted values SHALL retain their existing behavior

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

---
### Requirement: C9 uses the shared report query experience
The C9 query page SHALL reuse the standard title, query panel, Gregorian date row, required markers, action controls, pending table skeleton, result heading, empty state, total count, and pagination structures. Controls unrelated to C9 dates and paging MUST NOT be displayed.

#### Scenario: Submit and complete a C9 query
- **WHEN** a user submits valid dates
- **THEN** the shared skeleton is displayed while pending and is replaced by either the C9 result table or the shared empty state when the request completes

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