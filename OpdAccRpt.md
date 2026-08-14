# OpdAccRpt VB6 功能與 SQL 清冊

> 最近重新盤點：2026-08-06。此次以目前工作區的 VB6 `FrmTreeView.Form_Load`、`CmdPreview_Click` 與各 Module 實際程序重新核對；健康管理中心 C17 沿用既有盤點，不重複改寫 SQL。

## 1. 文件目的與範圍

本文件盤點 `D:\joyce\coding\OpdAccRpt` 中 VB6 專案 `source\OpdAccRpt\ReportProject1.vbp` 的功能入口、主要程序、SQL 資料來源、資料異動、Access 暫存表、Crystal Reports 與外部元件。

盤點基準：

- VB6 專案：17 個 Module、1 個 Class、9 個 Form，約 5 萬行程式。
- 主程式：`source\OpdAccRpt\mdiOpdStats.frm`，啟動物件為 `MDIForm1`。
- 主報表選擇：`source\OpdAccRpt\FrmTreeView.frm`。
- 報表核心：`module\OpdAccRpt\OpdRecReport.bas`、`Module1.bas`、`Module2.bas`、`Module3.bas`、`Module13.bas`、`Module14.bas`、`Module25.bas`。
- 原始碼以 CP950/Big5 解碼後分析。

父節點文件與本清冊的關係：

| 父節點          | 文件                     | 本次狀態                                   |
| ------------ | ---------------------- | -------------------------------------- |
| R 報表彙整表      | [報表彙整表.md](報表彙整表.md)   | 已重新核對所有可見 Key、入口程序及停用節點                |
| C2 會計報表      | [會計報表.md](會計報表.md)     | 已依 C21～C214 重新核對；C26 為殘留入口、正常選單不可見     |
| C14 催款組報表    | [催款組報表.md](催款組報表.md)   | 已依 C141～C144 重新核對；C141/C142 SQL 位於外部元件 |
| C17 健康管理中心報表 | [健康中心報表.md](健康中心報表.md) | 先前已完成，本次依需求跳過                          |

注意事項：

- 本清冊是靜態程式分析，不等於正式資料庫 schema 文件。
- `strOpdIpd & "pd..."` 是動態表名：`strOpdIpd="O"` 時為 `Opd...`，`strOpdIpd="I"` 時為 `Ipd...`。
- 許多 SQL 以多行字串、條件分支、`UNION ALL`、Oracle hint 或暫存 CTE 組成；本文件列主要用途、來源/目的表與關鍵條件，不逐字複製全部 SQL。
- Crystal `.rpt` 可能另含公式、子報表或內嵌資料來源，必須用相容的 Crystal Reports Designer 再確認。
- `IPDReceivable.dll`、`OPDPrice40.dll` 等院內 DLL 是黑箱依賴，無法只由本 repo 還原內部 SQL。

## 2. 系統架構與資料來源

| 層次       | 技術/位置                                                 | 用途                                      |
| -------- | ----------------------------------------------------- | --------------------------------------- |
| 主資料庫     | Oracle/ODBC DSN `DB_GEN`，RDO/ADO                      | 門急診、住院、批價、掛號、病歷、欠款、會計、庫存及 SAP 中介資料      |
| 報表暫存     | `report\OPDACCRPT\ReportDB.mdb`                       | 將 Oracle 查詢結果轉成 Crystal Report 可讀的本機暫存表 |
| 統計暫存     | `report\OPDACCRPT\OpdStats.mdb`                       | 醫師日/月統計、門急診統計                           |
| 其他本機 MDB | `InventoryDB.mdb`、`Reg.mdb`、`Receipt.mdb`、`Trans.mdb` | 庫存、掛號、收據及資料搬移；部分檔案不在目前報表目錄              |
| 報表引擎     | `Crystl32.ocx` + `.rpt`                               | 預覽、列印與公式參數                              |
| 匯出       | Excel COM Automation                                  | 健管中心、合約單位等查詢結果匯出                        |
| 外部整合     | SAP 中介表、SMTP、院內 DLL                                   | SAP 過帳、Email、病歷/掛號/應收功能                 |

## 3. 主程式與共用功能

### 3.1 啟動、登入與權限

| 功能     | 程式入口                             | SQL/資料表                                                           | 行為                                |
| ------ | -------------------------------- | ----------------------------------------------------------------- | --------------------------------- |
| 程式啟動   | `mdiOpdStats.frm / MDIForm_Load` | `GenUserProfile1`、`GenSystemPriTbl`、`GenSectionTbl`、`GenPlaceTbl` | 解析登入參數、建立 RDO/ADO/DAO 連線、載入使用者與權限 |
| SAP 權限 | `mdiOpdStats.frm`                | `GenUserProfile1`                                                 | 依使用者/部門決定 SAP 選單是否可見              |
| 病歷維護權限 | `mdiOpdStats.frm`                | `OpdMRBasicTbl`、`OpdMRCondTbl`、`GenAccItemTbl`                    | 控制病歷查詢與維護功能                       |
| 登出/關閉  | `MDIForm_QueryUnload`            | 無主要 SQL                                                           | 關閉連線、釋放物件，啟動 `loginScreen.exe`    |

### 3.2 主選單功能

| 功能別       | 表單/程序                    | 內容                                            |
| --------- | ------------------------ | --------------------------------------------- |
| 門急診批價統計報表 | `FrmTreeView.frm`        | 36 張可見報表、查詢條件、Crystal 預覽、Excel 匯出             |
| 醫師看診人數日表  | `OpdDocDaySum.frm`       | 醫師/科別每日看診人數統計                                 |
| 醫師看診人數月表  | `frmRpt02.frm`           | 醫師/科別月份統計                                     |
| 門急診日報表    | `frmRpt01.frm`           | 掛號、初複診、門急診人次統計                                |
| 批價查詢      | `frmOpdPriceQuery.frm`   | 病患、就診、醫令、藥品、收據查詢及列印入口                         |
| 病患基本資料維護  | `source\GEN\AdmZ400.frm` | 病歷基本資料、團隊/醫令/欠款相關維護                           |
| 掛號/退掛查詢   | `FrmDCRegNo.frm`         | 掛號資料、退掛、診間/醫師/科別查詢與列印                         |
| 印表機設定     | `mdiOpdStats.frm`        | VB6 CommonDialog 印表機選擇                        |
| SAP 介接    | `frmSAP.frm`             | `SAPCASH`、`SAPCONS`、`SAPACC`、`SAPREV2` 四類資料異動 |
| Email     | `FrmTreeView.subEmail`   | SMTP/院內寄信元件，寄送報表附件                            |

## 4. 主報表樹逐項功能與 SQL

報表樹建立於 `FrmTreeView.Form_Load`，預覽分派於 `FrmTreeView.CmdPreview_Click` 的 `Select Case TreeView1.SelectedItem.Key`。

### 4.0 重新盤點後的樹狀結構與狀態

```text
R 報表彙整表
├─ C1 門急診手術核帳表
├─ C2 會計報表
│  ├─ C21、C22、C23、C24、C25
│  ├─ C27、C28、C29、C211、C212、C213、C214
│  └─ C26 只剩預覽分派，節點建立已註解
├─ C3～C13 各獨立報表
├─ C14 催款組報表
│  └─ C141、C142、C143、C144
├─ C15、C16 各獨立報表
├─ C17 健康管理中心報表
│  └─ C171、C172、C173、C174（本次跳過）
└─ C18、C19 各獨立報表
```

目前正常樹狀選單共有 36 個可選葉節點；若連同已註解的 C26 殘留入口則為 37 個程式分派。父節點 C2、C14、C17 本身不執行報表，只負責容納子節點。

### 4.1 會計與核帳報表

C22～C214 已依目前 VB6 原始碼拆成獨立盤點文件，索引見 [會計報表.md](會計報表.md)。C26 雖無可見樹節點，仍保留程序分析文件。

| Key                            | 報表/功能            | 主要程序                                      | Oracle SQL 來源或異動                                                                                           | Access 暫存/輸出                                                |
| ------------------------------ | ---------------- | ----------------------------------------- | ---------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| [C1](門診手術核帳表.md)<br>           | 門急診手術核帳表         | `SurgicalPrint` (`Module1.bas`)           | `OpdOrdTbl` JOIN `OpdBasicTbl`，LEFT JOIN `GenDoctorTbl`、`IPDPriceDrRatioTbl`、`GenSectionTbl`；依日期、手術醫令、狀態篩選 | 先刪除再清寫 `SurgicalPrintTbl`；Crystal `門診手術核帳表.rpt`             |
| [C21](門急診批價報表.md)<br>          | 批價會計總表           | `SubOpdRecRpt_OpdCSumDM`                  | 讀 `OpdTranColeTbl` 或 `IpdTranColeTbl`；住院單日重跑可先重建 `IpdTranCole*`                                            | 先刪除再清寫 `OpdRecRpt_OpdCSumDM`；Crystal `門急診批價報表.rpt`          |
| [C22](C22_櫃員現金表.md)<br>        | 櫃員現金表            | `CashPrint` (`Module13.bas`)              | `GenAccCaseDayTbl`、`IpdAccCaseDayTbl`、`IpdContractAccTbl`、`GenUserProfile1`；按批價員／日期／診別彙總                   | 先刪除再重建 `CashPrintTbl`；兩種現金繳存單 `.rpt`                        |
| [C23](C23_合約單位記帳表.md)<br>      | 合約單位記帳表          | `SubOpdRecRpt_PFin2SumDM`                 | 逐日刪除並重建 `Opd/IpdRecRpt_PFin2SumDM1`；來源涵蓋醫令、藥品、基本資料、合約身分與沖銷                                                 | 重建 `OpdRecRpt_PFinbSumDMa/b`；合約記帳日／月報                       |
| [C24](C24_欠繳補繳核帳表.md)<br>      | 欠繳補繳核帳表          | `SubOpdRecRpt_PDebtDM_IDate`              | 逐日刪除並重建 `Opd/IpdRecRpt_PDebtDM`、`_S`；混合欠款、醫令、藥品與病患資料                                                       | 重建 Access `PDebtDM`／`PDebtDM_S`；欠補繳核帳報表                     |
| [C25](C25_住院預收醫療費餘額明細表.md)<br> | 住院預收醫療費餘額明細表（月報） | `subIpdAdvPayBalance(...,"IpdAdvPayTbl")` | `IpdAdvPayTbl` 依截止日計算仍有效的預收餘額                                                                              | 先刪除再重建 Access `IpdAdvPayTbl`；`IpdAdvPayBalance.rpt`         |
| [C26](C26_住院應收帳款表.md)<br>註記刪除  | 住院應收帳款表（日報）      | `subIpdReceivable`                        | `IpdBasRepTbl`、`IpdOrdTbl`、`IpdDrgTbl`；樹節點已註解但分派程序仍存在                                                      | 先刪除再重建 `IpdReceivableTbl`；`IpdReceivable.rpt`               |
| [C27](C27_輔具保證金餘額明細表.md)<br>   | 輔具保證金餘額明細表（月報）   | `subIpdAdvPayBalance(...,"OpdAidPayTbl")` | `OpdAidPayTbl` 依截止日計算仍有效的保證金餘額                                                                             | 先刪除再重建 Access `IpdAdvPayTbl`；共用餘額報表版型                       |
| [C28](C28_住院應收帳款餘額明細表.md)<br>  | 住院應收帳款餘額明細表（月報）  | `SubIpdReceivableBalance`                 | `IpdTranColeMrNoTbl`，以分析函數篩選絕對餘額大於 10 的住院案件                                                                | 先刪除再重建 `IpdReceivableBalanceTbl`；`IpdReceivableBalance.rpt` |
| [C29](C29_合約單位收款明細表.md)<br>    | 合約單位收款明細表        | `SubOpdRecRpt_PFin2SumDMAcc`              | `IpdContractAccTbl` JOIN 門／住院基本資料；必要時向 `IpdContractTbl` 新增控制資料                                             | 重建 `OpdRecRpt_PFinbSumDMa`；合約單位收款明細報表                       |
| [C211](C211_合約單位餘額明細表.md)<br>  | 合約單位餘額明細表        | `subPFin2Balance`                         | 刪除並重建 `Opd/IpdContractMrNoTbl` 的每日資料，再計算截止日餘額                                                              | 重建 `IpdReceivableBalanceTbl`；`PFin2Balance.rpt`             |
| [C212](C212_骨庫餘額明細表.md)<br>    | 骨庫餘額明細表          | `subPFin2Balance42`                       | 刪除並重建 `GenContract42MrNoTbl` 的醫令與沖銷資料                                                                      | 重建 `IpdReceivableBalanceTbl`；`PFin2Balance42.rpt`           |
| [C213](C213_收款員現金彙總表.md)<br>   | 收款員現金彙總表         | `CashPrint_All`                           | `GenAccCaseDayTbl`、`IpdAccCaseDayTbl`、`GenAccHappyCashTbl`，以 `ROLLUP` 產生收款員與總計                             | ADO Recordset 直接匯出 Excel，不重建暫存表                             |
| [C214](C214_門急診應收帳款餘額明細表.md)   | 門急診應收帳款餘額明細表     | `SubOpdReceivableBalance`                 | 讀 `OpdRecRpt_PDebtDM`，固定從 `1040101` 累計至迄止日                                                                 | ADO Recordset 直接匯出 Excel，不重建暫存表                             |

### 4.2 計價、材料與明細報表

| Key | 報表/功能         | 主要程序                                                              | Oracle SQL 來源或異動                                                                                                                                                                                                     | Access 暫存/輸出                                                    |
| --- | ------------- | ----------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| C3  | 各護理站計價品彙總/明細表 | `NursingPrint` (`Module3.bas`)                                    | `GenOrdBasicTbl`、`Opd/IpdOrdTbl`、`Opd/IpdBasicTbl`、`GenSectionTbl`；依日期、護理站/診間、科別、批價碼、彙總/明細條件                                                                                                                         | `NursingPrintDetailTbl`、`NursingPrintSumTbl`；護理站計價品明細/彙總 `.rpt` |
| C4  | 門急診材料寄售表及庫存處理 | `MaterialUse`、`MaterialUse1`、`subAvgPrice` 及 `Module3.bas` 多個庫存程序 | 報表來源含 `Opd/IpdOrdTbl`、`Opd/IpdBasicTbl`、`GenOrdBasicTbl`；另會讀寫 `ConsTranTbl`、`ConsRequestTbl`、`InvTChargeTranTbl`、`InvTTranTbl`、`InvTTranDay*Tbl`、`InvTTranMthTbl`、`InvTAvgPriceTbl`、`InvPay*Tbl`、`InvMastBasicTbl` 等 | `MaterialUseTbl`；`門急診材料寄售表.rpt`。此功能含結帳/庫存異動，不是純查詢               |
| C5  | 批價數量查詢表       | `SecOrderPrint` (`Module1.bas`)                                   | 動態讀 `Opd/IpdOrdTbl` 與 `Opd/IpdDrgTbl`，JOIN `Opd/IpdBasicTbl`、`GenSectionTbl`；依門急診別、藥品/醫令、科別、診間、批價碼、收費科目篩選                                                                                                            | `SecOrderPrintTbl`；批價數量每日/明細/彙總 `.rpt`                          |
| C6  | 急診特殊檢查治療查詢表   | `SecOrderPrint`                                                   | 與 C5 共用 SQL，預設急診/特定類型，也可依畫面條件改查門診                                                                                                                                                                                    | `SecOrderPrintTbl`；依條件選擇 C5 系列 `.rpt`                           |
| C7  | 門急診每日批價明細表    | `OrdDetailPrint` (`Module2.bas`)                                  | `OpdBasicTbl`、`OpdOrdTbl`、`OpdDrgTbl`、使用者資料；依日期、時間、輸入人員、藥品/醫令類型篩選                                                                                                                                                    | `OrdDetailPrintTbl`；`門急診每日批價明細表.rpt`                            |
| C8  | 批價補帳明細表       | `Repay` (`Module14.bas`)                                          | `OpdBasicTbl`、`OpdOrdTbl`、`OpdDrgTbl`；篩選補帳/異動日期及狀態                                                                                                                                                                   | `PatchBillTbl`；`批價補帳資料核帳明細表.rpt`                                |
| C9  | 維康耗材記帳月報表     | `SubOpdOrdRpt_SPay6` (`Module25.bas`)                             | `OpdOrdTbl` JOIN `OpdBasicTbl`、`GenSectionTbl`；依特殊支付/記帳碼、日期彙總                                                                                                                                                        | `SPayfTbl`；維康耗材月報表 1/2                                          |

## 4.2.1 C5: 批價數量查詢表:

## (1)、營養科:員工營養品優惠後的價格_SQL_ 門急醫令

```sql
SELECT
    A.chOp4SPay AS SPay,
    SUM(A.rlOp4Tot) AS Qty,
    SUM(A.rlOp4AMT1) AS Original_Insurance_Amt,
    SUM(A.rlOp4AMT2) AS Original_SelfPay_Amt,
    SUM(A.rlOp4Sub3) AS Discount_Amt,
    CASE
        WHEN A.chOp4SPay = '1'
            THEN SUM(A.rlOp4AMT1) - SUM(A.rlOp4Sub3)
        WHEN A.chOp4SPay IN ('0', '4')
            THEN SUM(A.rlOp4AMT2) - SUM(A.rlOp4Sub3)
        ELSE 0
    END AS Final_Amt
FROM OpdOrdTbl  A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No  = B.intOp1No
WHERE B.chOp1Date = :RunDate
  AND (
        A.chOp4PSec = '0430'
        OR A.chOp4OrdNo IN (
            '43-030',
            'F00001-1',
            'F00001-2',
            'F00001-3',
            '43-063',
            '43-032',
            '43-033'
        )
      )
  AND (
        A.chOp4Proj NOT IN ('I', 'D', 'S')
        OR RTRIM(A.chOp4Proj) IS NULL
      )
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
GROUP BY A.chOp4SPay;
```

## (2)、一般的價格

## 2.1 門急診藥品SQL

```sql
SELECT
    B.chOp1Date                         AS VisitDate,
    B.chOp1RoomType                     AS RoomType,
    A.chOp3PSec                         AS PSec,
    A.chOp3DrgNo                        AS ItemNo,
    A.chOp3DrgName                      AS ItemName,
    A.chOp3SPay                         AS SPay,
    A.rlOp3Pric1                        AS InsurancePrice,
    A.rlOp3Pric2                        AS SelfPayPrice,
    SUM(A.rlOp3DrgTot)                  AS Qty,
    SUM(A.rlOp3AMT1)                    AS InsuranceAmt,
    SUM(A.rlOp3AMT2)                    AS SelfPayAmt,
    CASE
        WHEN A.chOp3SPay = '1'
            THEN SUM(A.rlOp3AMT1)
        WHEN A.chOp3SPay IN ('0', '4')
            THEN SUM(A.rlOp3AMT2)
        ELSE 0
    END                                 AS FinalAmt
FROM OpdDrgTbl A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No  = B.intOp1No
WHERE B.chOp1Date = :RunDate

  AND (
        :OpSubCode IS NULL
        OR A.chOp3PSec = :OpSubCode
      )

  AND (
        A.chOp3Proj NOT IN ('I', 'D', 'S')
        OR RTRIM(A.chOp3Proj) IS NULL
      )

  AND (
        A.chOp3Rep3Flg <> 'S'
        OR RTRIM(A.chOp3Rep3Flg) IS NULL
      )

  AND (
        A.chOp3Stat NOT IN ('09', '10', '11', '12')
        OR RTRIM(A.chOp3Stat) IS NULL
      )

  AND A.chOp3Stat <> 'DC'
  AND A.chOp3Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
GROUP BY
    B.chOp1Date,
    B.chOp1RoomType,
    A.chOp3PSec,
    A.chOp3DrgNo,
    A.chOp3DrgName,
    A.chOp3SPay,
    A.rlOp3Pric1,
    A.rlOp3Pric2
ORDER BY
    B.chOp1Date,
    A.chOp3PSec,
    A.chOp3DrgNo;
```

## 2.2 門急診醫令SQL

```sql
SELECT
    B.chOp1Date                         AS VisitDate,
    B.chOp1RoomType                     AS RoomType,
    A.chOp4PSec                         AS PSec,
    A.chOp4ExtNo                        AS ItemNo,
    A.chOp4OrdName                      AS ItemName,
    A.chOp4SPay                         AS SPay,
    A.rlOp4Pric1                        AS InsurancePrice,
    A.rlOp4Pric2                        AS SelfPayPrice,
    SUM(A.rlOp4OrdTot)                  AS Qty,
    SUM(A.rlOp4AMT1)                    AS InsuranceAmt,
    SUM(A.rlOp4AMT2)                    AS SelfPayAmt,
    CASE
        WHEN A.chOp4SPay = '1'
            THEN SUM(A.rlOp4AMT1)
        WHEN A.chOp4SPay IN ('0', '4')
            THEN SUM(A.rlOp4AMT2)
        ELSE 0
    END                                 AS FinalAmt
FROM OpdOrdTbl A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No  = B.intOp1No
WHERE B.chOp1Date = :RunDate
  AND (
        :OpSubCode IS NULL
        OR A.chOp4PSec = :OpSubCode
      )
  AND (
        A.chOp4Proj NOT IN ('I', 'D', 'S')
        OR RTRIM(A.chOp4Proj) IS NULL
      )
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
GROUP BY
    B.chOp1Date,
    B.chOp1RoomType,
    A.chOp4PSec,
    A.chOp4ExtNo,
    A.chOp4OrdName,
    A.chOp4SPay,
    A.rlOp4Pric1,
    A.rlOp4Pric2
ORDER BY
    B.chOp1Date,
    A.chOp4PSec,
    A.chOp4ExtNo;
```

## 2.3 住院－藥品SQL

```sql
SELECT
    SUBSTR(A.chOp3cDate, 1, 7)          AS ChargeDate,
    B.chOp1RoomType                     AS RoomType,
    A.chOp3PSec                         AS PSec,
    A.chOp3DrgNo                        AS ItemNo,
    A.chOp3DrgName                      AS ItemName,
    A.chOp3SPay                         AS SPay,
    A.rlOp3Pric1                        AS InsurancePrice,
    A.rlOp3Pric2                        AS SelfPayPrice,
    SUM(A.rlOp3DrgTot)                  AS Qty,
    SUM(A.rlOp3AMT1)                    AS InsuranceAmt,
    SUM(A.rlOp3AMT2)                    AS SelfPayAmt,
    CASE
        WHEN A.chOp3SPay = '1'
            THEN SUM(A.rlOp3AMT1)
        WHEN A.chOp3SPay IN ('0', '4')
            THEN SUM(A.rlOp3AMT2)
        ELSE 0
    END                                 AS FinalAmt
FROM IpdDrgTbl A
JOIN IpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No  = B.intOp1No
WHERE A.chOp3cDate LIKE :RunDate || '%'

  AND (
        :OpSubCode IS NULL
        OR A.chOp3PSec = :OpSubCode
      )
  AND (
        A.chOp3Proj NOT IN ('I', 'D', 'S')
        OR RTRIM(A.chOp3Proj) IS NULL
      )
  AND (
        A.chOp3Rep3Flg <> 'S'
        OR RTRIM(A.chOp3Rep3Flg) IS NULL
      )
  AND (
        A.chOp3Stat NOT IN ('09', '10', '11', '12')
        OR RTRIM(A.chOp3Stat) IS NULL
      )
  AND A.chOp3Stat <> 'DC'
  AND A.chOp3Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
GROUP BY
    SUBSTR(A.chOp3cDate, 1, 7),
    B.chOp1RoomType,
    A.chOp3PSec,
    A.chOp3DrgNo,
    A.chOp3DrgName,
    A.chOp3SPay,
    A.rlOp3Pric1,
    A.rlOp3Pric2
ORDER BY
    SUBSTR(A.chOp3cDate, 1, 7),
    A.chOp3PSec,
    A.chOp3DrgNo;
```

## 2.4 住院－醫令SQL

```sql
SELECT
    SUBSTR(A.chOp4cDate, 1, 7)          AS ChargeDate,
    B.chOp1RoomType                     AS RoomType,
    A.chOp4PSec                         AS PSec,
    A.chStation                         AS Station,
    A.chOp4ExtNo                        AS ItemNo,
    A.chOp4OrdName                      AS ItemName,
    A.chOp4SPay                         AS SPay,
    A.rlOp4Pric1                        AS InsurancePrice,
    A.rlOp4Pric2                        AS SelfPayPrice,
    SUM(A.rlOp4OrdTot)                  AS Qty,
    SUM(A.rlOp4AMT1)                    AS InsuranceAmt,
    SUM(A.rlOp4AMT2)                    AS SelfPayAmt,
    CASE
        WHEN A.chOp4SPay = '1'
            THEN SUM(A.rlOp4AMT1)
        WHEN A.chOp4SPay IN ('0', '4')
            THEN SUM(A.rlOp4AMT2)
        ELSE 0
    END                                 AS FinalAmt
FROM IpdOrdTbl A
JOIN IpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No  = B.intOp1No
WHERE A.chOp4cDate LIKE :RunDate || '%'
  AND (
        :OpSubCode IS NULL
        OR A.chStation = :OpSubCode
      )
  AND (
        A.chOp4Proj NOT IN ('I', 'D', 'S')
        OR RTRIM(A.chOp4Proj) IS NULL
      )
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
GROUP BY
    SUBSTR(A.chOp4cDate, 1, 7),
    B.chOp1RoomType,
    A.chOp4PSec,
    A.chStation,
    A.chOp4ExtNo,
    A.chOp4OrdName,
    A.chOp4SPay,
    A.rlOp4Pric1,
    A.rlOp4Pric2
ORDER BY
    SUBSTR(A.chOp4cDate, 1, 7),
    A.chStation,
    A.chOp4ExtNo;###
```

### 

### 4.3 應收、收據、社服與催款報表

| Key  | 報表/功能           | 主要程序                                      | Oracle SQL 來源或異動                                                                                                                                              | Access 暫存/輸出                                  |
| ---- | --------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------- |
| C10  | 應收帳款記錄明細表       | `SubOpdRecRpt_PDebtDM_Detal`              | 門診：`OpdBasicTbl`、`GenDebtTbl`、`OpdMRBasicTbl`、`GenSectionTbl`、`OpdOrdTbl`、`OpdDrgTbl`、`GenDctItemTbl`；住院改用 `IpdBasicTbl`、`IpdDebtTbl`、`IpdOrdTbl`、`IpdDrgTbl` | `OpdRecRpt_PDebtDM_Detal`；門急診/住院應收明細 `.rpt`   |
| C11  | 應收帳款催收款月報表      | `subOpdRecRpt_PDebtDM_Total`              | `GenDebtTbl` 或 `IpdDebtTbl` JOIN `IpdBasicTbl`，依年度/月別、診別彙總欠款及病歷人數                                                                                             | `OpdRecRpt_PDebtDM_Total`；`門急診應收帳款催收款月報表.rpt` |
| C12  | 病患醫療費用收據彙總證明    | `SubOpdRecRpt_RecTotal`                   | `OpdMRBasicTbl`、`Opd/IpdBasicTbl`、`OpdRegPtnTbl`、`Opd/IpdOrdTbl`、`Opd/IpdDrgTbl`、`GenDctItemTbl`、`GenSectionTbl`；依病歷號、日期、科別、診別彙總                              | `RecTotal`、`RecTotal_Sum`；收據彙總明細/中英文彙總 `.rpt` |
| C13  | 社服需求急診高危險群個案明細表 | `subSS0000`                               | `OpdRegPtnTbl`、`OpdMRBasicTbl`、`GenFin1Tbl`、`OpdPPayTbl`、`GenDebtTbl`、`OpdEmgSoapDispTbl`                                                                     | `SSErTbl`；`SS0000.rpt`                        |
| C141 | 住院應收帳款排行        | `IPDReceivable.clsReceivable.ShowfrmRec`  | SQL 位於外部 `IPDReceivable.dll`，repo 無法展開                                                                                                                        | 外部 DLL UI/報表                                  |
| C142 | 病患欠醫院費用明細表      | `IPDReceivable.clsReceivable.ShowfrmDebt` | SQL 位於外部 `IPDReceivable.dll`                                                                                                                                  | 外部 DLL UI/報表                                  |
| C143 | 會計餘額 VS 批價欠款表   | `subAccounting_Counter`                   | 住院用 `IpdTranColeMrNoTbl`、`IpdBasicTbl`、`IpdDebtTbl`、`IpdBasic2Tbl`；門診用 `GenDebtTbl`、`OpdBasicTbl`；並與 `OpdRecRpt_PDebtDM` 比較，可產差異/全部帳表                         | 直接 Excel 輸出                                   |
| C144 | 欠款明細報表          | `subDebtDetail`                           | `IpdDebtTbl`/`GenDebtTbl` JOIN `IpdBasicTbl`/`OpdBasicTbl`、`GenSectionTbl`，再 JOIN `Ipd/OpdOrdTbl`、`Ipd/OpdDrgTbl`；依欠款日/就診日及診別整理                               | Excel/報表輸出，使用多層 CTE 暫存結果                      |
| C15  | 社工輔助器具保證金明細表    | `subAidEarnest`                           | `OpdAidPayTbl` JOIN `OpdOrdTbl`；依日期、狀態與保證金醫令分類                                                                                                                | `AidEarnestTbl`；`AidEarnest.rpt`              |
| C16  | 新北市醫療補助費用申請總表   | `subTaipeiSubsidy`                        | `OpdTaipeiSubsidyPtTbl`、`Opd/IpdBasicTbl`、`Opd/IpdOrdTbl`、`OpdMRBasicTbl`；依補助類型、日期、醫令與記帳身分                                                                    | `TaipeiSubsidyTbl`；`Opd/IpdTaipeiSubsidy.rpt` |

### 4.4 健康管理中心及其他報表

健康管理中心 C171～C174 本次未重新展開；下表保留既有結果供主清冊索引，完整 SQL 與欄位邏輯以 [健康中心報表.md](健康中心報表.md) 為準。

| Key  | 報表/功能         | 主要程序               | Oracle SQL 來源                                                                                                                             | 輸出                                                   |
| ---- | ------------- | ------------------ | ----------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| C171 | 健康管理中心明細資料    | `HealthCenter`     | `OpdOrdTbl` UNION ALL `OpdDrgTbl`，各自 JOIN `OpdBasicTbl`，LEFT JOIN `GenDctTypeTbl`、`GenSectionTbl`；條件為健管診間 `6F4/6021` 或科別 `0294%`，排除取消及零數量 | Excel                                                |
| C172 | 健康管理中心金額統計    | `HealthCenter1`    | `OpdOrdTbl` UNION ALL `OpdDrgTbl`、`OpdBasicTbl`、`GenSectionTbl`；依責任中心、日期、批價碼彙總六類金額                                                        | Excel                                                |
| C173 | 健檢人次          | `HealthCount`      | `OpdBasicTbl` JOIN `OpdOrdTbl`、`GenSectionTbl`；取科別 `0294%`、排除特定診間/病歷、取消醫令，按月份與科別計數                                                        | Excel                                                |
| C174 | 健康管理中心合約單位記帳表 | `HealthPFin2SumDM` | 直接 `SELECT ... FROM OpdRecRpt_PFin2SumDM1 WHERE chDateFlag BETWEEN 起日000000 AND 迄日999999`                                                 | Excel；欄位含記帳代碼/名稱、就診日、病歷號、姓名、科別、醫師、會計科目、優待/記帳/總金額、記帳員 |
| C18  | 醫療群會員急診住院查詢   | `ReferralMember`   | `Opd/IpdBasicTbl` JOIN `OpdRegPtnTbl`、`GenReferralMemberTbl`、`GenSectionTbl`；LEFT JOIN `OpdTFHospitalTbl`、`GenICD9Tbl`、`OpdTFTbl`         | Excel                                                |
| C19  | 安全針具使用情形查檢表   | `SafeNeedle`       | `Opd/IpdOrdTbl` JOIN `Opd/IpdBasicTbl`，以 CTE 分組安全針具醫令與病患/護理站資料                                                                            | `SafeNeedleTbl`；`ipdsafeneedle.rpt`                  |

### 4.5 已停用或殘留的報表入口

| Key | 狀態             | 說明                                                                                   |
| --- | -------------- | ------------------------------------------------------------------------------------ |
| C26 | 樹節點已註解，預覽分派仍殘留 | 原功能為「住院應收帳款表（日報）」，呼叫 `subIpdReceivable`。目前使用者無法從正常樹狀選單選取，但移植時應由業務單位確認要刪除、恢復或由其他報表取代。 |

## 5. 醫務統計功能與 SQL

| 功能       | 表單                 | 主要 SQL/表                                                                                             | 暫存/報表                                       |
| -------- | ------------------ | ---------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| 醫師看診人數日表 | `OpdDocDaySum.frm` | `OpdBasicTbl`、`OpdRegPtnTbl`、`OpdRegStatsTbl`、`GenDoctorTbl`、`GenSectionTbl`；依日期、醫師、科別、門急診別與實際看診條件統計 | `DocDaySumTbl`；`opddocdaysum.rpt`           |
| 醫師看診人數月表 | `frmRpt02.frm`     | `OpdBasicTbl`、`OpdRegPtnTbl`、`OpdRegStatsTbl`、`GenDoctorTbl`、`GenSectionTbl`；依月份彙總並執行新科別代碼更新         | `DocMonthSumTbl`；`docmonthsumtbl.rpt`       |
| 門急診日報表   | `frmRpt01.frm`     | `OpdBasicTbl`、`OpdRegPtnTbl`、`OpdRegStatsTbl`、`GenSectionTbl`；統計掛號、初複診、退掛、門急診人次                      | `OpdRegStatsTbl`/本機統計資料；`rptopdstats01.rpt` |

#### 

## 6. 批價查詢、病歷、掛號與收據

### 6.1 前台批價查詢 `frmOpdPriceQuery.frm`

- 病患/掛號：`OpdRegPtnTbl`、`OpdBasicTbl`。
- 批價醫令：`OpdOrdTbl`。
- 藥品：`OpdDrgTbl`。
- 收據：`OpdRecTbl`。
- 查詢條件包含病歷號、就診日、科別、醫師、姓名、身分證與生日。
- 可由查詢結果進一步取得收據明細並呼叫列印流程。

### 6.2 病患基本資料維護 `AdmZ400.frm`

主要資料表：

- `OpdMRBasicTbl`：病患基本資料主檔。
- `GenDebtTbl`：欠款/應收資訊。
- `GenOrdBasicTbl`、`GenDrgBasicTbl`、`GenDrgOrdHisTbl`、`GenMultiOrderTbl`：醫令與藥品歷史/組合資料。
- `AdmLabTeamTbl`、`AdmRadTeamTbl`、`AdmPahTeamTbl`、`AdmSpeTeamTbl`：檢驗、放射、病理、特殊團隊設定。
- `OpdDrPayTbl`：醫師費用相關資料。

此表單約 7,000 行，包含查詢、新增、修改、欄位驗證、權限及多個院內 DLL 整合，不能只以單一 CRUD 表單看待。

### 6.3 掛號/退掛 `FrmDCRegNo.frm` 與 `OpdRegHospital.bas`

主要資料表：

- `OpdRegPtnTbl`、`OpdRegRecTbl`：掛號病患與掛號收據。
- `OpdRegRoomTbl`、`OpdRegRoomTypeTbl`、`OpdRegReplaceRoomTbl`：診間、診別與代診。
- `OpdRegSystemTbl`、`OpdRegTimeTbl`：掛號系統及時段設定。
- `OpdMRBasicTbl`：病患基本資料。
- `GenDoctorTbl`、`GenSectionTbl`、`GenRegRoomDescTbl`、`GenSystemTbl`：醫師、科別、診間說明與系統參數。
- `SeqTable`：序號產生/控制。

另依賴 `Reg.mdb`、掛號報表與院內掛號 DLL。

### 6.4 收據列印 `RecModule.bas`

主要資料表：`OpdRecTbl`、`OpdBasicTbl`、`OpdOrdTbl`、`OpdDrgTbl`、`GenDebtTbl`、`OpdPPayTbl`、`GenDctItemTbl`、`GenDctTypeTbl`、`GenDoctorTbl`、`GenFin1Tbl`、`GenSectionTbl`。另依賴 `Receipt.mdb` 與收據 `.rpt`。

## 7. SAP 介接功能與 SQL 異動

`frmSAP.frm` 不是查詢畫面，而是會執行 `DELETE`/`INSERT` 的正式資料異動流程。執行前會檢查日期、類別、使用者權限及 `logday` 狀態。

| 作業           | 程序/事件        | 來源表                                                                       | 目的表              | 行為                      |
| ------------ | ------------ | ------------------------------------------------------------------------- | ---------------- | ----------------------- |
| SAPREV2／轉撥收入 | `subSAPREV2` | `OpdTranColeTbl`、`IpdTranColeTbl`、科別/場所資料                                 | `SapRevTbl`      | 先刪指定日期，再按會計科目、診別、場所彙總新增 |
| SAPACC／批價收入  | `subSAPACC`  | `OpdTranColeTbl`、`IpdTranColeTbl`、`GenAccCaseDayTbl`、`IpdAccCaseDayTbl` 等 | `SapAccTbl`      | 重建指定日期會計收入資料            |
| SAPCONS／合約記帳 | `subSAPCONS` | `OpdRecRpt_PFin2SumDM1`、`IpdRecRpt_PFin2SumDM1`                           | `SapContractTbl` | 按合約單位、會計科目、日期彙總後重建      |
| SAPCASH／櫃員現金 | `subSAPCASH` | `OpdRecTbl`、`IpdRecTbl`、`GenPayBackTbl`、現金日結相關表                           | `SapCashTbl`     | 重建指定日期現金/退費/批價員彙總       |

其他輔助表：`GenPlaceTbl`、`GenSectionTbl`、`InvSectionTbl`、`OpdTransferMTbl`。SAP 功能必須在非正式環境以交易、權限、重跑與稽核紀錄測試，不能直接在正式環境驗證。

## 8. 材料、寄售與庫存 SQL 清單

`Module3.bas` 涵蓋報表以外的材料/寄售/庫存結帳功能，具有高副作用。

| 功能群     | 主要資料表                                                                                                     |
| ------- | --------------------------------------------------------------------------------------------------------- |
| 寄售基本/交易 | `ConsSuppBasTbl`、`ConsSuppPrdTbl`、`ConsTranTbl`、`ConsRequestTbl`、`ConsRequestPreTbl`                      |
| 庫存基本資料  | `InvMastBasicTbl`、`InvSectionTbl`、`InvParaTbl`、`GenPlaceTbl`                                              |
| 庫存異動    | `InvTTranTbl`、`InvTChargeTranTbl`、`InvTTranDayDTbl`、`InvTTranDaySTbl`、`InvTTranMthTbl`                    |
| 平均成本    | `InvTAvgPriceTbl`、`InvBCostRatioTbl`、`InvDeptUseMonthlyTbl`                                               |
| 採購/付款   | `InvPurHMainTbl`、`InvPurHTbl`、`InvPurHSTbl`、`InvReceiptTbl`、`InvPayTbl`、`InvPayCostTbl`、`InvGenAccPayTbl` |
| 自動補充    | `InvAutoSupTbl`、`DbTest_InvAutoSupTbl`                                                                    |
| 來源醫令    | `Opd/IpdOrdTbl`、`Opd/IpdBasicTbl`、`GenOrdBasicTbl`                                                        |
| 歷史/跨系統  | `His_*Tbl`、`Invent_User01_*Tbl`、`GUID_AP01_*Tbl`、`UnionBargainTbl`                                        |

## 9. SQL 資料表總覽

### 9.1 門急診核心

- 病患：`OpdMRBasicTbl`。
- 掛號：`OpdRegPtnTbl`、`OpdRegRecTbl`、`OpdRegRoomTbl`、`OpdRegRoomTypeTbl`、`OpdRegTimeTbl`。
- 就診：`OpdBasicTbl`。
- 醫令：`OpdOrdTbl`。
- 藥品：`OpdDrgTbl`。
- 收據：`OpdRecTbl`。
- 欠款：`GenDebtTbl`。
- 批價交易彙總：`OpdTranColeTbl`。
- 診斷：`OpdSoapDiagTbl`、`GenICD9Tbl`、`GenICD10Tbl`。

### 9.2 住院核心

- 就醫主檔：`IpdBasicTbl`、`IpdBasic2Tbl`。
- 醫令/藥品：`IpdOrdTbl`、`IpdDrgTbl`。
- 收據/欠款：`IpdRecTbl`、`IpdDebtTbl`。
- 批價交易彙總：`IpdTranColeTbl`、`IpdTranColeMrNoTbl`。
- 預收/應收：`IpdAdvPayTbl`、`IpdReceivableTbl`、`IpdReceivableBalanceTbl`。

### 9.3 共用代碼與主檔

- 科別：`GenSectionTbl`。
- 醫師：`GenDoctorTbl`。
- 收費科目：`GenDctItemTbl`、`GenDctTbl`、`GenIpdDctTbl`。
- 記帳身分：`GenDctTypeTbl`、`GenIpdDctTypeTbl`。
- 健保/身分：`GenFin1Tbl`。
- 醫令主檔：`GenOrdBasicTbl`、`GenOrdBasicExtTbl`。
- 藥品主檔：`GenDrgBasicTbl`、`GenDrgFreqTbl`、`GenDrgSecTbl`。
- 使用者/權限：`GenUserProfile1`、`GenSystemPriTbl`、`GenUserLogTbl`。
- 系統/場所：`GenSystemTbl`、`GenPlaceTbl`。

### 9.4 Oracle 報表彙總表

- `OpdRecRpt_PFin2SumDM1`、`IpdRecRpt_PFin2SumDM1`：合約記帳日資料，也是 C174/SAPCONS 來源。
- `OpdRecRpt_PDebtDM`、`IpdRecRpt_PDebtDM` 及 `_S`：欠繳/補繳與應收資料。
- `OpdTranColeTbl`、`IpdTranColeTbl`：批價會計總表及 SAP 收入來源。
- `GenAccCashDayTbl`、`GenAccCaseDayTbl`、`IpdAccCaseDayTbl`：現金/會計日結。

### 9.5 Access 報表暫存表

- `SurgicalPrintTbl`：C1 手術核帳。
- `OpdRecRpt_OpdCSumDM`：C21 批價會計總表。
- `CashPrintTbl`：C22/現金報表。
- `OpdRecRpt_PFinbSumDMa`、`OpdRecRpt_PFinbSumDMb`：C23 合約記帳日/月報。
- `NursingPrintDetailTbl`、`NursingPrintSumTbl`：C3 護理站計價品。
- `MaterialUseTbl`：C4 材料寄售。
- `SecOrderPrintTbl`：C5/C6 批價數量。
- `OrdDetailPrintTbl`：C7 每日批價明細。
- `PatchBillTbl`：C8 補帳。
- `SPayfTbl`：C9 維康耗材。
- `OpdRecRpt_PDebtDM_Detal`、`OpdRecRpt_PDebtDM_Total`：C10/C11 應收。
- `RecTotal`、`RecTotal_Sum`：C12 醫療費用收據彙總。
- `SSErTbl`：C13 社服急診高風險。
- `AidEarnestTbl`：C15 輔具保證金。
- `TaipeiSubsidyTbl`：C16 醫療補助。
- `SafeNeedleTbl`：C19 安全針具。
- `DocDaySumTbl`、`DocMonthSumTbl`、`OpdRegStatsTbl`：醫務統計。

## 10. Repo 內現有報表資產

`report\OPDACCRPT` 目前包含 22 個 `.rpt`：

- `門診手術核帳表.rpt`
- `門急診批價報表.rpt`
- `現金繳存單_批價員.rpt`
- `現金繳存單_門急診.rpt`
- `門急診合約單位僑保記帳通知單日(月)報表1.rpt`
- `門急診合約單位僑保記帳通知單日(月)報表2.rpt`
- `門急診病患欠繳補繳核帳日(月)報表.rpt`
- `門急診護理站計價品明細表.rpt`
- `門急診護理站計價品彙總表.rpt`
- `門急診材料寄售表.rpt`
- `門急診批價數量每日表.rpt`
- `門急診批價數量明細表.rpt`
- `門急診批價數量彙總表.rpt`
- `門急診每日批價明細表.rpt`
- `批價補帳資料核帳明細表.rpt`
- `維康耗材記帳月報表1.rpt`
- `維康耗材記帳月報表2.rpt`
- `門急診病患應收帳款明細記錄表.rpt`
- `門急診應收帳款催收款月報表.rpt`
- `opddocdaysum.rpt`
- `docmonthsumtbl.rpt`
- `rptopdstats01.rpt`

程式另外引用 `ipdsafeneedle.rpt`、`AidEarnest.rpt`、`SS0000.rpt`、`PFin2Balance*.rpt`、`IpdReceivable*.rpt`、收據及掛號報表等；這些檔案不全在上述目錄，必須從授權部署環境補齊。

## 11. 外部元件與移植風險

| 類型      | 元件/風險                                                                                                                        |
| ------- | ---------------------------------------------------------------------------------------------------------------------------- |
| VB6 控制項 | `Crystl32.ocx`、TrueDBGrid 5/6、Spread、MaskEdBox、RDO Control、RichText、Threed、Tab、CommonControls/CommonDialog                   |
| 資料庫元件   | RDO 2、ADO 2.1、DAO 3.5、ODBC DSN `DB_GEN`                                                                                      |
| Office  | Excel 9 COM；需處理版本、程序釋放與無人值守執行問題                                                                                              |
| 院內 DLL  | `RegForFEH.dll`、`OpdMROptForFEH.dll`、`OPDPrice40.dll`、`IpdReceivable.dll`、`proBasicLog.dll`、`DBUser_VB.dll`、`VbSendMail.dll` |
| 固定路徑    | `C:\Tch\Report\OpdAccRpt`、多個 `C:\Tch\DataBase` MDB、`loginScreen.exe`                                                         |
| 高風險異動   | C4 庫存/寄售結帳、SAP 四類中介表、掛號/退掛、病歷修改、收據列印                                                                                         |

## 12. 驗證建議

每個功能應至少完成以下驗證後，才能宣稱已完整移植：

1. 以相同查詢條件同時執行 VB6 與新版程式。
2. 比對 SQL 參數、資料筆數、金額合計、診別、身分、日期及時間邊界。
3. 比對 Access 暫存表欄位型別及空值/尾端空白處理。
4. 比對 Crystal 報表欄位、分組、公式、頁首、頁尾、總計與列印紙張。
5. 驗證日報/月報、門診/急診/住院/出院及重跑分支。
6. SAP、庫存、掛號、病歷及收據功能必須在非正式環境驗證交易回復、權限、重跑與稽核紀錄。
7. 確認所有缺少的 MDB、RPT、OCX、DLL、ODBC DSN 及印表機設定均有正式部署方案。
