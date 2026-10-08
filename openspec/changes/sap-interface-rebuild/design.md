## Context

VB6 `frmSAP` 依序將四類會計資料寫入 Oracle SAP 中介表。依據文件為 `D:/joyce/coding/c#_OpdAccRpt/ISAP/01_ASPNET_Core_MVC_SPEC.md`，完整 SQL 在第 8 節。現有 MVC 專案已使用 Oracle Managed Data Access、報表目錄、Vue 3 Options API、西元日期欄位和共用報表樣式。目前設定資料庫為 DbTest3；正式物件定義與 SAP 下游讀取契約尚待確認。作業中各事件可能有多筆中介表 INSERT；只有 INSERT 合計大於零才代表本次建立了資料。

## Goals / Non-Goals

**Goals:** 保留規格的 15 條 SQL、提供單日與逐事件重跑選擇，並使每事件的目標表變動與 `logday` 紀錄具有原子性。INSERT 合計為零時回復該事件交易、不建立完成紀錄，且重跑時保留原資料與紀錄。

**Non-Goals:** 新增個別使用者授權規則、重建上游來源、Crystal Reports 輸出、改動 SAP 下游交接程序。

## Decisions

### Preserve Oracle expressions in embedded C# SQL

以 C# 原始字串常數保留第 8 節 SQL，使用 OracleCommand 綁定日期與事件參數，不建立獨立 `.sql` 檔。Oracle 的 CHAR、NVL、DECODE、ROUND 與聚合語意留在資料庫端，避免改寫公式後產生差異。

### Use one transaction per selected event and date

依 SAPCASH、SAPCONS、SAPACC、SAPREV2 的順序執行。每事件先鎖定 `logday`、查完成狀態、刪目標，再執行中介表 INSERT。只加總中介表 INSERT 的影響筆數，不計入 DELETE 或 `logday` 寫入；合計大於零時寫入 `logday(ok='Y')` 並提交。所有 INSERT 成功但合計為零時，回復整個事件交易並回傳 `noData`，不新增完成紀錄，且保留重跑前的目標資料與 `logday`。SQL 例外則回復該事件並回傳失敗。整表鎖會序列化其他日期的作業，但不需新增鎖定資料表或 DBMS_LOCK 授權，且能保護尚無紀錄的日期。鎖定權限與等待時間須在 DbTest3 驗證。

### Require explicit confirmation for completed events

執行前先查已完成的選取項目，要求操作員逐項選擇重跑；拒絕重跑的項目從本次執行清單移除，後續項目仍繼續。取得鎖後再次查詢，處理兩人同時操作的情況。

### Use Gregorian date controls at the request boundary

瀏覽器送出 `yyyy-MM-dd`。服務驗證日期後產生民國七碼與西元八碼，分別綁定來源／紀錄及目標刪除條件。預設日期沿用 VB6 的 SQL；查無可用日期時取昨天。

## Implementation Contract

目錄提供 SAP 項目，導向 `/sap-interface`。GET `/sap-interface/default-date` 回傳 `{ businessDate }`；GET `/sap-interface/status?businessDate=yyyy-MM-dd` 回傳四項依序排列的狀態；POST `/sap-interface/run` 接收 `businessDate`、四個執行布林值與 `confirmRerun[]`，並驗證防偽權杖。回應逐項標示完成、無資料、失敗或需確認。

每個事件和日期各自使用一筆交易。成功執行的中介表 INSERT 影響筆數合計大於零時，提交資料並寫入 `logday.ok='Y'`；合計為零時回復該交易、不寫完成紀錄，並回傳 `status: "noData"`。首次零筆時事件維持未完成；已完成事件經確認重跑後零筆時，回復先前刪除，保留原中介表資料及完成紀錄。任一 SQL 例外時回復該事件的目標表與 `logday`，並顯示失敗結果。未確認重跑仍回傳需確認狀態。零項選取不寫入，單項 noData 或失敗不阻止後續已選事件執行。

SQL 內容與執行順序分別依參考規格第 8 節與第 2 節驗收。需在同一份非正式 Oracle 固定快照上，將四張目標表與 VB6 結果做多重集合比較，涵蓋重跑、零金額與 X1 費率分界。程式可先編譯；正式等值與上線條件須待資料字典、樣本輸出及下游交接契約確認。

## Risks / Trade-offs

- [正式目標欄序與同義詞尚未核對] → 上線前核對五張寫入表的欄位、精度、鍵、授權與實際物件。
- [SAPREV2 只刪除部分科目] → 以多重集合比對重跑結果，確認保留舊列的意圖。
- [`logday` 整表鎖可能影響其他寫入] → 觀察 DbTest3 鎖等待；若有競爭，再協調更細的資料庫鎖定機制。
- [上游資料可能尚未完成] → 零筆結果回復該事件，保留原有中介表資料及完成紀錄，並在結果欄呈現「查無資料」。
- [上游資料可能同時重建] → 與維運確認來源完成訊號及切換時序。

## Migration Plan

先在 DbTest3 查資料字典並比對固定快照，再與 SAP 維運確認讀取時點，驗收後於目標環境開放入口。若輸出不符，回退應用程式版本；已供下游讀取的資料需依另行核准的資料庫復原流程處理。

## Open Questions

- 哪個 SAP 程序讀取四張中介表，讀取時點為何？
- 正式欄序、鍵、授權與同義詞是否符合文件？
- 哪份固定 Oracle 快照及 VB6 輸出可供等值驗收？
