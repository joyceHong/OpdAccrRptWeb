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

### Requirement: Hierarchical report menu

系統 SHALL 在左側以可展開及收合的階層選單呈現報表群組與報表。

#### Scenario: Expand a report group

- **WHEN** 使用者展開報表群組
- **THEN** 系統 SHALL 顯示該群組內各報表的舊系統報表代碼及名稱

#### Scenario: Collapse all report groups

- **WHEN** 使用者執行全部收合
- **THEN** 系統 SHALL 收合目前分類的所有報表群組

### Requirement: Search report catalog

系統 SHALL 允許使用者依報表代碼或報表名稱篩選左側報表清單。

#### Scenario: Filter reports by keyword

- **WHEN** 使用者輸入報表代碼或名稱關鍵字
- **THEN** 系統 SHALL 僅顯示符合關鍵字的報表及其所屬群組

### Requirement: Stable legacy report codes

系統 SHALL 保留舊系統報表代碼作為使用者可見的穩定識別碼。

#### Scenario: Open a report

- **WHEN** 使用者選擇代碼為 `C171` 的報表
- **THEN** 系統 SHALL 導向該代碼對應的報表頁面
- **AND** 頁面 SHALL 顯示「健康管理中心明細資料」

### Requirement: Unavailable report handling

尚未完成的報表 SHALL 保留於報表目錄，但系統不得將其呈現為已可查詢。

#### Scenario: Select an unavailable report

- **WHEN** 使用者選擇尚未建置的報表
- **THEN** 系統 SHALL 顯示該報表尚未建置的訊息
- **AND** 系統 SHALL NOT 執行其他報表的查詢

