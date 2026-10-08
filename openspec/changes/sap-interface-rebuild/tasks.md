## 1. Oracle 作業契約

- [x] 1.1 Preserve Oracle expressions in embedded C# SQL：將規格第 8 節 15 條 SQL 以 raw string 收入專屬類別，維持原公式與參數；以逐條文字比對確認全部一致。
- [ ] 1.2 Use Gregorian date controls at the request boundary：API 驗證西元日期並轉民國 7 碼及西元 8 碼，預設日期沿用 logday 查詢；以 2026-01-01 對照 1150101 與 20260101，並檢查無效日期回應。
- [ ] 1.3 Use one transaction per selected event and date：依 Atomic event result 契約，只加總中介表 INSERT 筆數；總數大於零才提交並寫入 `logday.ok='Y'`，總數為零時回復交易、不建立完成紀錄並保留重跑前資料；以固定非正式快照驗證 0+0 回復、0+3 提交及 SQL 例外回復。

## 2. 操作畫面與結果

- [ ] 2.1 Explicit rerun choices / Require explicit confirmation for completed events：已完成項目逐項確認，拒絕時略過且後續項目繼續；以已完成日和只選單項的手動操作確認。
- [ ] 2.2 Daily SAP operation controls 與 SAP interface entry：報表目錄進入 SAP 畫面，四項預設全選，使用西元日期；以畫面操作檢查導覽、預設值、載入骨架、狀態與窄螢幕配置。
- [ ] 2.3 Atomic event result：API 逐項區分 completed、noData、failed 與 confirmationRequired；noData 在「本次結果」顯示「查無資料」，SQL 例外顯示失敗，且後續已選事件仍執行；以零筆、例外及後續事件的服務與頁面情境驗證。

## 3. 等值與交接驗收

- [ ] 3.1 Four event SQL equivalence：在同一固定非正式 Oracle 快照比較 VB6 與 MVC 的四張目標表多重集合，涵蓋 X1 費率邊界、51 優待、預繳 I 和重跑；保存逐欄差異報告。
- [ ] 3.2 核對五張寫入表的正式欄序、精度、約束、索引、同義詞與鎖表權限，以及 SAP 下游讀取時序；以 Oracle 資料字典輸出和維運確認紀錄驗收。
