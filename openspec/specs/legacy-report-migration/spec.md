# Legacy Report Migration Specification

## Purpose

定義 VB6 門急診報表移植至 ASP.NET Core MVC 與 Vue 3 網站時的範圍、證據及驗證原則。

## Requirements

### Requirement: First-phase migration scope

第一階段 SHALL 僅規格化報表網站、報表查詢及結果呈現；SAP 介接、庫存結帳、掛號／退掛、病歷維護及收據列印 SHALL 留待後續階段另行規格化。

#### Scenario: Evaluate a legacy feature for first phase

- **WHEN** 舊系統功能會異動正式資料或不屬於報表查詢流程
- **THEN** 該功能 SHALL NOT 因本規格而被視為第一階段交付項目

### Requirement: Legacy behavior as migration evidence

移植工作 SHALL 將 `OpdAccRpt.md` 的 VB6 程序、SQL 資料來源、Access 暫存表、Crystal Reports 與外部元件清冊作為調查及驗證依據，但 SHALL NOT 強制新版沿用 VB6、Access 暫存表或 Crystal Reports 的技術實作。

#### Scenario: Replace a legacy implementation

- **WHEN** 新版以不同技術產生與舊系統等值的結果
- **THEN** 該實作 MAY 被接受
- **AND** 驗證 SHALL 以業務結果與核准的規則為準

### Requirement: Report-by-report parity validation

每一報表在標示為完成前 SHALL 使用相同查詢條件比對舊版與新版的關鍵結果。

#### Scenario: Validate a migrated report

- **WHEN** 團隊準備將一張報表標示為完成
- **THEN** 團隊 SHALL 比對資料筆數、金額合計、日期與時間邊界、診別及身分等適用欄位
- **AND** 團隊 SHALL 記錄已知差異及其核准依據

### Requirement: Unknown legacy dependencies

外部 DLL、缺少的報表檔、Crystal 公式及未取得的部署資產 SHALL 被視為未確認依賴，不得由靜態清冊推定其完整行為。

#### Scenario: Encounter an opaque dependency

- **WHEN** 報表行為依賴無法檢視的外部元件或缺少的資產
- **THEN** 規格與實作狀態 SHALL 標記該依賴為待確認
- **AND** 報表 SHALL NOT 僅依推測宣告完成

### Requirement: Licensed web dependencies

網站採用的第三方元件 SHALL 使用經專案確認可合法使用及散布的開源授權，並 SHALL 保留必要的授權聲明。

#### Scenario: Add a third-party component

- **WHEN** 團隊準備加入新的前端或後端第三方元件
- **THEN** 團隊 SHALL 在採用前確認其授權相容性
- **AND** 未確認授權的元件 SHALL NOT 納入正式交付

