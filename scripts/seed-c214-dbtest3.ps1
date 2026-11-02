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
        $type = if ($value -is [decimal]) {
            [Oracle.ManagedDataAccess.Client.OracleDbType]::Decimal
        } else {
            [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2
        }
        $parameter = $command.Parameters.Add($name, $type)
        $parameter.Value = $value
    }
    return $command
}

function Get-Scalar {
    param(
        [string]$Sql,
        [System.Collections.IDictionary]$Parameters = @{},
        [Oracle.ManagedDataAccess.Client.OracleTransaction]$Transaction = $null
    )

    $command = New-DbCommand $Sql $Parameters $Transaction
    try { return $command.ExecuteScalar() }
    finally { $command.Dispose() }
}

$accountingDate = '1151102' # 2026-11-02
$rows = @(
    @{ mrn = 'T214SELF01'; patient_name = 'C214自費測試'; room_type = 'O'; room_name = '門診'; fin1 = '01'; amount = [decimal]360 },
    @{ mrn = 'T214INS001'; patient_name = 'C214健保測試'; room_type = 'E'; room_name = '急診'; fin1 = '30'; amount = [decimal]540 }
)
$insertSql = @'
INSERT INTO OpdRecRpt_PDebtDM
    (chOp1Date,chOp1Date2,chOp1RoomType,chOp1RoomTypeName,chOp1MrNo,
     chOp1PName,chOp4PSec,chOp4PFin1,chOp1InSeq,chOp1Dct1,
     chOp1DctName1,rlOp1Sub6,rlOp1Sub3,rlOp1SubAMT,chDC,chCUser)
VALUES
    (:accounting_date,:visit_date,:room_type,:room_name,:mrn,
     :patient_name,:section,:fin1,:card_sequence,:charge_code,
     :charge_name,:amount,0,:amount,'0','C214TEST')
'@
$verifySql = @'
SELECT SUM(rlOp1Sub6)
FROM OpdRecRpt_PDebtDM
WHERE chOp1Date BETWEEN '1040101' AND :end_date
  AND chOp1Date2 >= '1040101'
  AND (chDC='0' OR RTRIM(chDC) IS NULL)
  AND chOp4PFin1 IN (:fin1)
  AND chOp1MrNo = :mrn
GROUP BY chOp1MrNo,chOp1Date2
HAVING ABS(SUM(rlOp1Sub6)) > 10
'@

$connection.Open()
try {
    if ([string](Get-Scalar "SELECT SYS_CONTEXT('USERENV','DB_NAME') FROM dual") -ne 'PDBTEST3' -or
        [string](Get-Scalar "SELECT SYS_CONTEXT('USERENV','SERVICE_NAME') FROM dual") -ne 'DBTEST3' -or
        [string](Get-Scalar 'SELECT USER FROM dual') -ne 'GUID_AP01') {
        throw '資料庫身分與預期的 DBTEST3 / GUID_AP01 不符，已停止。'
    }

    foreach ($row in $rows) {
        if ([decimal](Get-Scalar 'SELECT COUNT(*) FROM OpdRecRpt_PDebtDM WHERE chOp1MrNo=:mrn' @{ mrn = $row.mrn }) -ne 0) {
            throw "C214 測試病歷號 $($row.mrn) 已存在，未新增任何資料。"
        }
    }

    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::ReadCommitted)
    try {
        foreach ($row in $rows) {
            $parameters = @{} + $row
            $parameters.accounting_date = $accountingDate
            $parameters.visit_date = $accountingDate
            $parameters.section = '11910'
            $parameters.card_sequence = '001'
            $parameters.charge_code = '01'
            $parameters.charge_name = '藥品費'
            $command = New-DbCommand $insertSql $parameters $transaction
            try {
                if ($command.ExecuteNonQuery() -ne 1) { throw "C214 $($row.mrn) 寫入筆數不正確。" }
            }
            finally { $command.Dispose() }
        }

        foreach ($row in $rows) {
            $actual = [decimal](Get-Scalar $verifySql @{
                end_date = $accountingDate; fin1 = $row.fin1; mrn = $row.mrn
            } $transaction)
            if ($actual -ne $row.amount) {
                throw "C214 $($row.mrn) 預期 $($row.amount)，實際 $actual；已回滾。"
            }
        }
        $transaction.Commit()
    }
    catch {
        $transaction.Rollback()
        throw
    }
    finally { $transaction.Dispose() }

    $connection.Close()
    $connection.Open()
    foreach ($row in $rows) {
        $actual = [decimal](Get-Scalar $verifySql @{
            end_date = $accountingDate; fin1 = $row.fin1; mrn = $row.mrn
        })
        if ($actual -ne $row.amount) { throw "提交後 C214 $($row.mrn) 驗證失敗。" }
        Write-Output "C214 $($row.mrn)：身分 $($row.fin1)，應收餘額 $actual，截止日 115/11/02。"
    }
}
finally { $connection.Dispose() }
