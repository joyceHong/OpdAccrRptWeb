# Report Query Specification

## Purpose

定義報表共用查詢條件、驗證及送出行為；個別報表可在此基礎上增加專屬條件。

## Requirements

### Requirement: Shared simple report component

格式單純的報表 SHALL 優先共用查詢條件、查詢狀態、結果表格、分頁及錯誤訊息元件；個別報表 SHALL 以設定與欄位定義表達差異。

#### Scenario: Open reports with the same interaction pattern

- **WHEN** 兩張報表都只需要日期條件及表格結果
- **THEN** 系統 SHALL 使用相同的共用報表互動流程
- **AND** 畫面 SHALL 顯示各自的報表代碼、名稱及結果欄位

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