$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $PSScriptRoot -Parent
$assemblyDirectory = Join-Path $projectRoot 'bin/Debug/net10.0'
Add-Type -Path (Join-Path $assemblyDirectory 'FemhDb.dll')
Add-Type -Path (Join-Path $assemblyDirectory 'Oracle.ManagedDataAccess.dll')
Add-Type -Path (Join-Path $assemblyDirectory 'OpdAccrRptWeb.dll')

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

function Add-Row {
    param(
        [string]$Sql,
        [System.Collections.IDictionary]$Parameters,
        [Oracle.ManagedDataAccess.Client.OracleTransaction]$Transaction
    )
    $command = New-DbCommand $Sql $Parameters $Transaction
    try {
        if ($command.ExecuteNonQuery() -ne 1) { throw 'C24 住院測試資料新增筆數不正確。' }
    }
    finally { $command.Dispose() }
}

$visitDate = '1151030' # 2026-10-30
$visitTime = '1'
$visitRoom = 'T24I01'
$visitNo = 9902
$medicalRecordNo = 'T24CMP0002'
$debtSequence = 'T24IPD0002'
$visitParameters = @{
    visit_date = $visitDate
    visit_time = $visitTime
    visit_room = $visitRoom
    visit_no = $visitNo
}
$dayParameters = @{
    accounting_date = $visitDate
    day_begin = $visitDate + '0000'
    day_end = $visitDate + '2359'
}

$connection.Open()
try {
    $databaseName = [string](Get-Scalar "SELECT SYS_CONTEXT('USERENV','DB_NAME') FROM dual")
    $serviceName = [string](Get-Scalar "SELECT SYS_CONTEXT('USERENV','SERVICE_NAME') FROM dual")
    $loginUser = [string](Get-Scalar 'SELECT USER FROM dual')
    if ($databaseName -ne 'PDBTEST3' -or $serviceName -ne 'DBTEST3' -or $loginUser -ne 'GUID_AP01') {
        throw '資料庫身分與預期的 DBTEST3 / GUID_AP01 不符，已停止。'
    }

    $checks = @(
        @{ sql = 'SELECT COUNT(*) FROM OpdMrBasicTbl WHERE chMrNo=:mrn'; parameters = @{ mrn = $medicalRecordNo } },
        @{ sql = 'SELECT COUNT(*) FROM IpdBasicTbl WHERE chOp1Date=:visit_date AND chOp1Time=:visit_time AND chOp1Room=:visit_room AND intOp1No=:visit_no'; parameters = $visitParameters },
        @{ sql = 'SELECT COUNT(*) FROM IpdOrdTbl WHERE chOp1Date=:visit_date AND chOp1Time=:visit_time AND chOp1Room=:visit_room AND intOp1No=:visit_no'; parameters = $visitParameters },
        @{ sql = 'SELECT COUNT(*) FROM IpdDebtTbl WHERE chDebSeq=:debt_sequence'; parameters = @{ debt_sequence = $debtSequence } },
        @{ sql = 'SELECT COUNT(*) FROM IpdBasicTbl WHERE chOp1MrNo=:mrn'; parameters = @{ mrn = $medicalRecordNo } }
    )
    foreach ($check in $checks) {
        if ([decimal](Get-Scalar $check.sql $check.parameters) -ne 0) {
            throw 'C24 住院測試鍵值已存在，未新增任何資料。'
        }
    }

    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::ReadCommitted)
    try {
        Add-Row 'INSERT INTO OpdMrBasicTbl (chMrNo,chTelH) VALUES (:mrn,:phone)' @{
            mrn = $medicalRecordNo; phone = '0912000002'
        } $transaction

        $basicParameters = @{} + $visitParameters
        $basicParameters.room_type = 'I'
        $basicParameters.mrn = $medicalRecordNo
        $basicParameters.patient_name = 'C24住院測試病例'
        $basicParameters.section = '0201'
        $basicParameters.fin1 = '01'
        $basicParameters.card_sequence = '001'
        Add-Row @'
INSERT INTO IpdBasicTbl
    (chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp1RoomType,chOp1MrNo,
     chOp1PName,chOp1Sec,chOp1PFin1,chOp1InSeq)
VALUES
    (:visit_date,:visit_time,:visit_room,:visit_no,:room_type,:mrn,
     :patient_name,:section,:fin1,:card_sequence)
'@ $basicParameters $transaction

        $orderSql = @'
INSERT INTO IpdOrdTbl
    (chOp1Date,chOp1Time,chOp1Room,intOp1No,intOp1OrdSeq,chOp4PFin1,
     chOp4CUser,chOp4IDate,chOp4OrdNo,chOp4Dct,rlOp4Sub1,rlOp4Sub3,rlOp4Sub6)
VALUES
    (:visit_date,:visit_time,:visit_room,:visit_no,:order_sequence,:fin1,
     :created_by,:created_at,:order_no,:charge_code,:sub1,:sub3,:sub6)
'@
        $orders = @(
            @{ order_sequence = 1; order_no = 'C24I001'; charge_code = '01'; sub1 = [decimal]1200; sub3 = [decimal]0; sub6 = [decimal]1200 },
            @{ order_sequence = 2; order_no = 'ACC-69'; charge_code = '69'; sub1 = [decimal]400; sub3 = [decimal]0; sub6 = [decimal]0 }
        )
        foreach ($order in $orders) {
            $parameters = @{} + $visitParameters
            $parameters.fin1 = '01'
            $parameters.created_by = 'C24TEST'
            $parameters.created_at = $visitDate + '0000'
            foreach ($name in $order.Keys) { $parameters[$name] = $order[$name] }
            Add-Row $orderSql $parameters $transaction
        }

        $billingParameters = @{} + $visitParameters
        $billingParameters.debt_sequence = $debtSequence
        $billingParameters.bill_date = $visitDate
        $billingParameters.bill_man = 'C24TEST'
        $billingParameters.amount = [decimal]800
        Add-Row @'
INSERT INTO IpdDebtTbl
    (chDebSeq,chOp1Date,chOp1Time,chOp1Room,intOp1No,vchBillDate,vchBillMan,rlDebtAMT)
VALUES
    (:debt_sequence,:visit_date,:visit_time,:visit_room,:visit_no,:bill_date,:bill_man,:amount)
'@ $billingParameters $transaction

        $candidateFilter = 'VisitDate=:visit_date AND VisitTime=:visit_time AND VisitRoom=:visit_room AND VisitNo=:visit_no'
        $candidateParameters = @{} + $dayParameters
        foreach ($name in $visitParameters.Keys) { $candidateParameters[$name] = $visitParameters[$name] }
        $debtCount = [decimal](Get-Scalar ('SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::A05 + ') WHERE ' + $candidateFilter + ' AND QuerySub6=1200') $candidateParameters $transaction)
        $paymentCount = [decimal](Get-Scalar ('SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::A06 + ') WHERE ' + $candidateFilter + ' AND QuerySub6=-400') $candidateParameters $transaction)
        $patientCount = [decimal](Get-Scalar ('SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::E01 + ') WHERE MedicalRecordNo=:mrn') (@{} + $visitParameters + @{ mrn = $medicalRecordNo }) $transaction)
        $billingCount = [decimal](Get-Scalar ('SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::B02 + ') WHERE MedicalRecordNo=:mrn AND Amount=800') @{
            start_date = $visitDate; end_date = $visitDate; mr_no = $null; mrn = $medicalRecordNo
        } $transaction)
        if ($debtCount -ne 1 -or $paymentCount -ne 1 -or $patientCount -ne 1 -or $billingCount -ne 1) {
            throw "C24 住院來源驗證未通過，已回滾；欠款=$debtCount，補繳=$paymentCount，病患=$patientCount，批價=$billingCount。"
        }

        $transaction.Commit()
        Write-Output 'DBTEST3 C24 住院資料已寫入：2026-10-30、病歷號 T24CMP0002。'
        Write-Output '會計模式：欠款 1200、補繳 400、未清 800；批價模式：欠款 800。'
    }
    catch {
        $transaction.Rollback()
        throw
    }
    finally { $transaction.Dispose() }
}
finally { $connection.Dispose() }
