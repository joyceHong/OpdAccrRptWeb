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
        $type = if ($value -is [int]) {
            [Oracle.ManagedDataAccess.Client.OracleDbType]::Int32
        } elseif ($value -is [decimal]) {
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

function Add-Row {
    param(
        [string]$Sql,
        [System.Collections.IDictionary]$Parameters,
        [Oracle.ManagedDataAccess.Client.OracleTransaction]$Transaction
    )

    $command = New-DbCommand $Sql $Parameters $Transaction
    try {
        if ($command.ExecuteNonQuery() -ne 1) { throw 'C214 健保上游測試資料新增筆數不正確。' }
    }
    finally { $command.Dispose() }
}

$visitDate = '1151102' # 2026-11-02
$medicalRecordNo = 'T214INS001'
$visit = @{ visit_date = $visitDate; visit_time = '1'; visit_room = 'EEEE  '; visit_no = 9914 }
$patient = @{} + $visit
$patient.room_type = 'E'
$patient.mrn = $medicalRecordNo
$patient.patient_name = 'C214健保測試'
$patient.section = '11910'
$patient.fin1 = '30'
$patient.card_sequence = '001'

$connection.Open()
try {
    if ([string](Get-Scalar "SELECT SYS_CONTEXT('USERENV','DB_NAME') FROM dual") -ne 'PDBTEST3' -or
        [string](Get-Scalar "SELECT SYS_CONTEXT('USERENV','SERVICE_NAME') FROM dual") -ne 'DBTEST3' -or
        [string](Get-Scalar 'SELECT USER FROM dual') -ne 'GUID_AP01') {
        throw '資料庫身分與預期的 DBTEST3 / GUID_AP01 不符，已停止。'
    }

    $checks = @(
        @{ sql = 'SELECT COUNT(*) FROM OpdMrBasicTbl WHERE chMrNo=:mrn'; parameters = @{ mrn = $medicalRecordNo } },
        @{ sql = 'SELECT COUNT(*) FROM OpdBasicTbl WHERE chOp1MrNo=:mrn'; parameters = @{ mrn = $medicalRecordNo } },
        @{ sql = 'SELECT COUNT(*) FROM OpdRegPtnTbl WHERE chOp0PMrNo=:mrn'; parameters = @{ mrn = $medicalRecordNo } },
        @{ sql = 'SELECT COUNT(*) FROM OpdOrdTbl WHERE chOp1Date=:visit_date AND chOp1Time=:visit_time AND chOp1Room=:visit_room AND intOp1No=:visit_no'; parameters = $visit }
    )
    foreach ($check in $checks) {
        if ([decimal](Get-Scalar $check.sql $check.parameters) -ne 0) {
            throw 'C214 健保上游測試鍵值已存在，未新增任何資料。'
        }
    }

    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::ReadCommitted)
    try {
        Add-Row 'INSERT INTO OpdMrBasicTbl (chMrNo,chTelH) VALUES (:mrn,:phone)' @{
            mrn = $medicalRecordNo; phone = '0912000014'
        } $transaction

        Add-Row @'
INSERT INTO OpdBasicTbl
    (chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp1RoomType,chOp1MrNo,
     chOp1PName,chOp1Sec,chOp1PFin1,chOp1InSeq)
VALUES
    (:visit_date,:visit_time,:visit_room,:visit_no,:room_type,:mrn,
     :patient_name,:section,:fin1,:card_sequence)
'@ $patient $transaction

        Add-Row @'
INSERT INTO OpdRegPtnTbl
    (chOp0Date,chOp0Time,chOp0Room,intOp0No,chOp0RoomType,chOp0PMrNo,
     chOp0PName,chOp0SecNo,chOp0Fin1,chOp0Seq)
VALUES
    (:visit_date,:visit_time,:visit_room,:visit_no,:room_type,:mrn,
     :patient_name,:section,:fin1,:card_sequence)
'@ $patient $transaction

        $order = @{} + $visit
        $order.fin1 = '30'
        $order.created_by = 'C214TEST'
        $order.created_at = $visitDate + '0000'
        $order.order_no = 'C214I01'
        $order.charge_code = '01'
        $order.sub1 = [decimal]540
        $order.sub3 = [decimal]0
        $order.sub6 = [decimal]540
        Add-Row @'
INSERT INTO OpdOrdTbl
    (chOp1Date,chOp1Time,chOp1Room,intOp1No,intOp1OrdSeq,chOp4PFin1,
     chOp4CUser,chOp4IDate,chOp4OrdNo,chOp4Dct,rlOp4Sub1,rlOp4Sub3,rlOp4Sub6)
VALUES
    (:visit_date,:visit_time,:visit_room,:visit_no,1,:fin1,
     :created_by,:created_at,:order_no,:charge_code,:sub1,:sub3,:sub6)
'@ $order $transaction

        $candidate = @{} + $visit
        $candidate.accounting_date = $visitDate
        $candidate.day_begin = $visitDate + '0000'
        $candidate.day_end = $visitDate + '2359'
        $candidate.fin1 = '30'
        $candidate.amount = [decimal]540
        $candidateCount = [decimal](Get-Scalar ('SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::A02 + ') WHERE VisitDate=:visit_date AND VisitTime=:visit_time AND VisitRoom=:visit_room AND VisitNo=:visit_no AND PayerClassCode=:fin1 AND QuerySub6=:amount') $candidate $transaction)
        $patientParameters = @{} + $visit
        $patientParameters.mrn = $medicalRecordNo
        $patientParameters.room_type = 'E'
        $patientParameters.fin1 = '30'
        $patientCount = [decimal](Get-Scalar ('SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::E03 + ') WHERE MedicalRecordNo=:mrn AND RoomType=:room_type AND PatientFin1=:fin1') $patientParameters $transaction)
        if ($candidateCount -ne 1 -or $patientCount -ne 1) {
            throw "C24 上游驗證未通過，已回滾；醫令候選=$candidateCount，病患補值=$patientCount。"
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
    $day = @{ accounting_date = $visitDate; day_begin = $visitDate + '0000'; day_end = $visitDate + '2359' }
    $count = [decimal](Get-Scalar ('SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::A02 + ') WHERE PayerClassCode=''30'' AND QuerySub6=540 AND VisitDate=''1151102'' AND VisitNo=9914') $day)
    if ($count -ne 1) { throw '提交後 C24 健保來源驗證失敗。' }
    Write-Output 'DBTEST3 C24 健保上游資料已寫入：115/11/02、急診、T214INS001、應收 540。'
}
finally { $connection.Dispose() }
