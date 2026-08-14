# Report Query Specification

## Purpose

定義報表共用查詢條件、驗證及送出行為；個別報表可在此基礎上增加專屬條件。

## Requirements

### Requirement: Required date range

支援查詢的報表 SHALL 提供必填的起始日期與截止日期，初始值 SHALL 為使用者開啟頁面當日。

#### Scenario: Open a queryable report

- **WHEN** 使用者開啟已支援查詢的報表
- **THEN** 起始日期與截止日期 SHALL 預設為當日

#### Scenario: Submit an invalid date range

- **WHEN** 使用者輸入的起始日期晚於截止日期
- **THEN** 系統 SHALL 拒絕送出查詢
- **AND** 系統 SHALL 顯示可理解的日期範圍錯誤訊息

### Requirement: ROC date conversion boundary

前端 SHALL 使用可供使用者操作的日期格式，後端 MAY 在資料存取邊界將日期轉換為既有資料來源所需的民國日期格式。

#### Scenario: Submit a valid Gregorian date range

- **WHEN** 使用者以西元日期送出有效日期範圍
- **THEN** 系統 SHALL 以等值日期執行查詢
- **AND** 日期格式轉換 SHALL NOT 改變使用者選取的日期範圍

### Requirement: Optional advanced conditions

系統 SHALL 允許各報表定義選填的進階條件，例如科別、診間、醫院代碼或批價碼。

#### Scenario: Query without advanced conditions

- **WHEN** 使用者僅填寫必要日期條件
- **THEN** 系統 SHALL 允許送出查詢

### Requirement: Reset query conditions

系統 SHALL 提供重設功能，使查詢條件回復該報表的初始狀態。

#### Scenario: Reset edited conditions

- **WHEN** 使用者修改條件後執行重設
- **THEN** 系統 SHALL 清除選填條件
- **AND** 必填條件 SHALL 回復預設值

### Requirement: Report-specific query dispatch

系統 SHALL 依所選報表代碼執行對應的查詢，不得以其他報表的查詢替代。

#### Scenario: Submit C172 query

- **WHEN** 使用者在 `C172` 報表送出有效條件
- **THEN** 系統 SHALL 執行健康管理中心金額統計的查詢流程

#### Scenario: Submit an unsupported report

- **WHEN** 使用者對尚未支援的報表送出請求
- **THEN** 系統 SHALL 回傳明確的未支援結果
- **AND** 系統 SHALL NOT 靜默回傳另一報表的資料

