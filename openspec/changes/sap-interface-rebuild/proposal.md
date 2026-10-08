## Why

舊 VB6 `frmSAP` 負責把四類會計資料寫入 Oracle 的 SAP 中介表，現有 MVC 網站尚無對應作業。遷移時需保留原始 SQL 的金額與日期語意，並讓重跑與完成紀錄在失敗時保持一致。

## What Changes

- 在報表目錄加入 SAP 中介表作業入口，提供西元日期選擇、四項預設全選、完成狀態與逐項重跑確認。
- 依櫃員現金、合約記帳、批價收入、轉撥收入的順序執行，按事件與日期分別交易，顯示逐項結果。
- 將規格第 8 節 15 條 Oracle SQL 收入 C# 原始字串類別並以參數執行。
- 以非正式 Oracle 快照核對目標欄序、筆數、分組鍵及金額後，才能確認等值與部署條件。

## Capabilities

### New Capabilities

- `sap-interface-rebuild`: SAP 中介表作業的日期、選項、重跑、原始 SQL 與交易結果契約。

### Modified Capabilities

- `report-catalog`: 門診批價分類新增 SAP 中介表作業入口。

## Impact

- MVC/API：`Controllers/SapInterfaceController.cs`、`Models/SapInterfaceModels.cs`、`Services/SapInterfaceService.cs`、`Repositories/SapInterfaceRepository.cs` 與 SQL 類別。
- UI：`Views/Report/Index.cshtml`、`Views/Report/_SapInterface.cshtml`、`wwwroot/js/report-app.js`、`wwwroot/js/reports/sap-interface.js`、共用樣式。
- 外部依賴：Oracle 17 張來源、紀錄及目標表；正式資料字典與下游 SAP 交接時序仍需維運確認。
