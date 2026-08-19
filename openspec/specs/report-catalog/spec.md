# Report Catalog Specification

## Purpose

定義第一階段報表網站應保留的舊系統報表目錄、分類及目前可用狀態。

## Requirements

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

### Requirement: Medical statistics catalog

「醫務統計報表」分類 SHALL 包含下列既有功能，並由產品維護其穩定代碼：

- `M1`：醫師看診人數日表
- `M2`：醫師看診人數月表
- `M3`：門急診日報表

#### Scenario: Browse medical statistics

- **WHEN** 使用者選擇「醫務統計報表」
- **THEN** 系統 SHALL 顯示醫師看診人數日表、醫師看診人數月表及門急診日報表

### Requirement: Preserve inactive legacy entry status

舊系統殘留代碼 `C26` SHALL NOT 出現在一般使用者的正常報表目錄，除非業務單位另行決定恢復或取代。

#### Scenario: Load the normal report catalog

- **WHEN** 一般使用者開啟報表目錄
- **THEN** 系統 SHALL NOT 顯示 `C26`

### Requirement: Data query category status

「資料查詢」分類 SHALL 保留為第一層入口，但在功能規格完成前 SHALL 標示為規劃中。

#### Scenario: Open data query category

- **WHEN** 使用者選擇「資料查詢」
- **THEN** 系統 SHALL 顯示功能仍在規劃或建置中的訊息
