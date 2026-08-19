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

### Requirement: Empty result feedback

系統 SHALL 明確呈現查無資料的狀態。

#### Scenario: Query returns no rows

- **WHEN** 有效查詢未取得任何資料
- **THEN** 系統 SHALL 顯示查無資料訊息
- **AND** 系統 SHALL NOT 將查無資料呈現為系統錯誤

### Requirement: Query failure feedback

系統 SHALL 將查詢失敗與查無資料區分，並提供不洩漏敏感資料的錯誤訊息。

#### Scenario: Data source query fails

- **WHEN** 資料來源或查詢流程發生錯誤
- **THEN** 系統 SHALL 顯示查詢失敗訊息
- **AND** 使用者訊息 SHALL NOT 包含連線字串、SQL 或敏感個資

### Requirement: Paginated result table

系統 SHALL 支援報表結果分頁，預設每頁 10 筆，並允許使用者從下拉選單調整每頁筆數。

#### Scenario: Open a multi-page result

- **WHEN** 查詢結果超過 10 筆且使用者未變更每頁筆數
- **THEN** 系統 SHALL 顯示前 10 筆
- **AND** 系統 SHALL 提供前往其他頁面的控制項

## Planned Requirements

下列能力屬下一階段工作。

### Requirement: Excel export

系統 SHALL 允許使用者將符合目前查詢條件的結果匯出為 Excel 相容檔案。

#### Scenario: Export current query result

- **WHEN** 使用者在查詢成功後執行匯出
- **THEN** 系統 SHALL 產生對應目前報表與查詢條件的 Excel 相容檔案
- **AND** 匯出範圍 SHALL NOT 僅限於目前顯示頁
