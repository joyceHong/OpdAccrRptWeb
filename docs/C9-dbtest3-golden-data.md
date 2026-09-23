# C9 DBTEST3 對照測試資料

## 查詢方式

- Database：`DBTEST3`
- Gregorian date：`2026-09-22`
- ROC date：`1150922`
- 測試就診鍵：`chOp1Date=1150922`, `chOp1Time=T`, `chOp1Room=C9TST1`, `intOp1No=990901`
- 病歷號／姓名皆為合成測試值：`C9T115922`／`C9測試病患`
- VB6 C9 起日、迄日都輸入 `1150922`；Web C9 起日、迄日都選 `2026-09-22`。

## 預期納入明細

| 收費科目代碼 | 名稱 | 折扣額 Sub3 | 應付金額 Sub5 | 總金額 |
| --- | --- | ---: | ---: | ---: |
| C9A0001 | C9正常耗材 | 10.25 | 89.75 | 100.00 |
| C9A0001 | C9同碼異名 | -3.00 | 7.50 | 4.50 |
| C9A0002 | C9金額皆空 | 0.00 | 0.00 | 0.00 |
| C9A0003 | C9負應付額 | 0.00 | -12.50 | -12.50 |
| C9A0004 | C9空白Proj | 1.20 | 3.40 | 4.60 |

預期符合 C9 SQL 的列數為 `5`。僅供整批檢核的三欄合計分別為：折扣額 `8.45`、應付金額 `88.15`、總金額 `96.60`；正式 C9 報表不顯示 grand total。

## 預期分組小計

| 收費科目代碼 | 明細筆數 | 折扣額小計 | 應付金額小計 | 總金額小計 |
| --- | ---: | ---: | ---: | ---: |
| C9A0001 | 2 | 7.25 | 97.25 | 104.50 |
| C9A0002 | 1 | 0.00 | 0.00 | 0.00 |
| C9A0003 | 1 | 0.00 | -12.50 | -12.50 |
| C9A0004 | 1 | 1.20 | 3.40 | 4.60 |

`C9A0001` 刻意使用同代碼、不同名稱，兩套報表都必須只按代碼形成同一組。

## 預期排除明細

| 收費科目代碼 | 排除原因 |
| --- | --- |
| C9X0001 | `chOp4SPay='5'`，不是 SPay 6 |
| C9X0002 | `chOp4Stat='DC'` |
| C9X0003 | `chOp4Proj='I'` |
| C9X0004 | `chOp4Proj='S'` |
| C9X0005 | `chOp4Stat IS NULL`，依 Oracle 三值邏輯被排除 |

## 驗收後清除

必須依下列順序，在同一 transaction 執行並確認鍵值後才 commit：

```sql
DELETE FROM OpdOrdTbl
WHERE chOp1Date = '1150922'
  AND chOp1Time = 'T'
  AND chOp1Room = 'C9TST1'
  AND intOp1No = 990901;

DELETE FROM OpdBasicTbl
WHERE chOp1Date = '1150922'
  AND chOp1Time = 'T'
  AND chOp1Room = 'C9TST1'
  AND intOp1No = 990901;
```
