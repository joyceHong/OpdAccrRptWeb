$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $PSScriptRoot -Parent
$assemblyDirectory = Join-Path $projectRoot 'bin/Debug/net10.0'
Add-Type -Path (Join-Path $assemblyDirectory 'FemhDb.dll')
Add-Type -Path (Join-Path $assemblyDirectory 'Oracle.ManagedDataAccess.dll')
$configuration = Get-Content (Join-Path $projectRoot 'appsettings.json') -Raw | ConvertFrom-Json
$endpoint = $configuration.DatabaseConnections.DbTest3
$connectionString = [FemhDb.DbUser]::GetConnectionString(
    $endpoint.DatabaseName, $endpoint.ApplicationName, [FemhDb.DbUser+ApplicationType]::Oracle)
$connection = [Oracle.ManagedDataAccess.Client.OracleConnection]::new($connectionString)

function New-DbCommand {
    param(
        [string]$Sql,
        [System.Collections.IDictionary]$Parameters = @{},
        [Oracle.ManagedDataAccess.Client.OracleTransaction]$Transaction = $null
    )
    $command = $connection.CreateCommand()
    $command.CommandText = $Sql
    $command.BindByName = $true
    if ($Transaction) { $command.Transaction = $Transaction }
    foreach ($name in $Parameters.Keys) {
        $value = $Parameters[$name]
        $type = [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2
        if ($value -is [int]) { $type = [Oracle.ManagedDataAccess.Client.OracleDbType]::Int32 }
        elseif ($value -is [decimal]) { $type = [Oracle.ManagedDataAccess.Client.OracleDbType]::Decimal }
        $parameter = $command.Parameters.Add($name, $type)
        $parameter.Value = if ($null -eq $value) { [DBNull]::Value } else { $value }
    }
    return $command
}

function Invoke-Scalar {
    param(
        [string]$Sql,
        [System.Collections.IDictionary]$Parameters = @{},
        [Oracle.ManagedDataAccess.Client.OracleTransaction]$Transaction = $null
    )
    $command = New-DbCommand $Sql $Parameters $Transaction
    try { return $command.ExecuteScalar() }
    finally { $command.Dispose() }
}

function Invoke-Change {
    param(
        [string]$Sql,
        [System.Collections.IDictionary]$Parameters,
        [Oracle.ManagedDataAccess.Client.OracleTransaction]$Transaction
    )
    $command = New-DbCommand $Sql $Parameters $Transaction
    try {
        if ($command.ExecuteNonQuery() -ne 1) { throw 'C24 住院結果更新筆數不正確。' }
    }
    finally { $command.Dispose() }
}

$date = '1151030'
$mrn = 'T24CMP0002'
$connection.Open()
try {
    if ([string](Invoke-Scalar "SELECT SYS_CONTEXT('USERENV','DB_NAME') FROM dual") -ne 'PDBTEST3' -or
        [string](Invoke-Scalar "SELECT SYS_CONTEXT('USERENV','SERVICE_NAME') FROM dual") -ne 'DBTEST3' -or
        [string](Invoke-Scalar 'SELECT USER FROM dual') -ne 'GUID_AP01') {
        throw '資料庫身分與預期的 DBTEST3 / GUID_AP01 不符，已停止。'
    }

    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::ReadCommitted)
    try {
        $lock = New-DbCommand 'LOCK TABLE IpdRecRpt_PDebtDM IN SHARE ROW EXCLUSIVE MODE' @{} $transaction
        try { [void]$lock.ExecuteNonQuery() }
        finally { $lock.Dispose() }

        $summaryCount = [decimal](Invoke-Scalar @'
SELECT COUNT(*) FROM IpdRecRpt_PDebtDM_S
WHERE chAccDate=:accounting_date AND chOp1RoomType IS NULL
AND NVL(rlOp1SubDebt_S,0)=0 AND NVL(rlOp1SubPay_S,0)=0
AND NVL(rlOp1SubSub_S,0)=0
'@ @{ accounting_date = $date } $transaction)
        $existingDetails = [decimal](Invoke-Scalar 'SELECT COUNT(*) FROM IpdRecRpt_PDebtDM WHERE chOp1Date=:accounting_date' @{ accounting_date = $date } $transaction)
        $sourceOrders = [decimal](Invoke-Scalar @'
SELECT COUNT(*) FROM IpdOrdTbl
WHERE chOp1Date=:accounting_date AND chOp1Room='T24I01' AND intOp1No=9902
AND ((chOp4OrdNo='C24I001' AND rlOp4Sub6=1200)
  OR (chOp4OrdNo='ACC-69' AND rlOp4Sub1=400))
'@ @{ accounting_date = $date } $transaction)
        $sourcePatient = [decimal](Invoke-Scalar @'
SELECT COUNT(*) FROM IpdBasicTbl b JOIN OpdMrBasicTbl m
ON RTRIM(b.chOp1MrNo)=m.chMrNo
WHERE b.chOp1Date=:accounting_date AND b.chOp1Room='T24I01'
AND b.intOp1No=9902 AND b.chOp1MrNo=:mrn
'@ @{ accounting_date = $date; mrn = $mrn } $transaction)
        if ($summaryCount -ne 1 -or $existingDetails -ne 0 -or
            $sourceOrders -ne 2 -or $sourcePatient -ne 1) {
            throw "C24 住院既有資料不符合預期，未更新；空彙總=$summaryCount，既有明細=$existingDetails，來源醫囑=$sourceOrders，病患=$sourcePatient。"
        }

        $detailSql = @'
INSERT INTO IpdRecRpt_PDebtDM
    (chOp1Date,chOp1Date2,chOp1RoomType,chOp1RoomTypeName,chOp1MrNo,
     chOp1PName,chOp1PTel,chOp4PSec,chOp4PFin1,chOp1InSeq,
     chOp1Dct1,chOp1DctName1,rlOp1Sub6,rlOp1Sub3,rlOp1SubAMT,chCUser)
VALUES
    (:accounting_date,:visit_date,:room_type,:room_name,:mrn,
     :patient_name,:phone,:section,:fin1,:card_sequence,
     :charge_code,:charge_name,:signed_amount,:discount,:amount_due,:created_by)
'@
        $baseParameters = @{
            accounting_date = $date; visit_date = $date; room_type = 'I'; room_name = '住院'
            mrn = $mrn; patient_name = 'C24住院測試病例'; phone = '0912******'
            section = '11910'; fin1 = '01'; card_sequence = '001'; created_by = 'C24TEST'
            discount = [decimal]0
        }
        foreach ($row in @(
            @{ charge_code = '01'; charge_name = '藥品費'; signed_amount = [decimal]1200; amount_due = [decimal]1200 },
            @{ charge_code = '69'; charge_name = '應收帳款收回'; signed_amount = [decimal]-400; amount_due = [decimal]-400 }
        )) {
            $parameters = @{} + $baseParameters
            foreach ($name in $row.Keys) { $parameters[$name] = $row[$name] }
            Invoke-Change $detailSql $parameters $transaction
        }

        Invoke-Change @'
UPDATE IpdRecRpt_PDebtDM_S SET
    chOp1RoomType='I',chOp1RoomTypeName='住院',
    rlOp1SubDebt_S=1200,rlOp1SubDebt_C=1,
    rlOp1SubPay_S=400,rlOp1SubPay_C=1,
    rlOp1SubSub_S=800,rlOp1SubSub_C=0
WHERE chAccDate=:accounting_date AND chOp1RoomType IS NULL
AND NVL(rlOp1SubDebt_S,0)=0 AND NVL(rlOp1SubPay_S,0)=0
AND NVL(rlOp1SubSub_S,0)=0
'@ @{ accounting_date = $date } $transaction

        $visibleDetails = [decimal](Invoke-Scalar 'SELECT COUNT(*) FROM IpdRecRpt_PDebtDM WHERE chOp1Date=:accounting_date AND chOp1MrNo=:mrn' @{ accounting_date = $date; mrn = $mrn } $transaction)
        $visibleSummary = [decimal](Invoke-Scalar 'SELECT COUNT(*) FROM IpdRecRpt_PDebtDM_S WHERE chAccDate=:accounting_date AND chOp1RoomType=:room_type AND rlOp1SubDebt_S=1200 AND rlOp1SubPay_S=400 AND rlOp1SubSub_S=800' @{ accounting_date = $date; room_type = 'I' } $transaction)
        if ($visibleDetails -ne 2 -or $visibleSummary -ne 1) {
            throw 'C24 住院結果驗證未通過，已回滾。'
        }

        $transaction.Commit()
        Write-Output 'DBTEST3 C24 住院舊結果已補齊：2026-10-30，明細 2 筆，欠款 1200、補繳 400、未清 800。'
    }
    catch {
        $transaction.Rollback()
        throw
    }
    finally { $transaction.Dispose() }
}
finally { $connection.Dispose() }

