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
        $parameter = $command.Parameters.Add($name, [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2)
        if ($value -is [int]) { $parameter.OracleDbType = [Oracle.ManagedDataAccess.Client.OracleDbType]::Int32 }
        elseif ($value -is [decimal]) { $parameter.OracleDbType = [Oracle.ManagedDataAccess.Client.OracleDbType]::Decimal }
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

$visitDate = '1151030' # 2026-10-30
$visitTime = '1'
$visitRoom = 'T24O01'
$visitNo = 9901
$medicalRecordNo = 'T24CMP0001'
$dayBegin = $visitDate + '0000'
$dayEnd = $visitDate + '2359'
$visitParameters = @{
    visit_date = $visitDate
    visit_time = $visitTime
    visit_room = $visitRoom
    visit_no = $visitNo
}

$connection.Open()
try {
    $databaseName = [string](Get-Scalar "SELECT SYS_CONTEXT('USERENV','DB_NAME') FROM dual")
    $serviceName = [string](Get-Scalar "SELECT SYS_CONTEXT('USERENV','SERVICE_NAME') FROM dual")
    $loginUser = [string](Get-Scalar 'SELECT USER FROM dual')
    if ($databaseName -ne 'PDBTEST3' -or $serviceName -ne 'DBTEST3' -or $loginUser -ne 'GUID_AP01') {
        throw '資料庫身分與預期的 DBTEST3 / GUID_AP01 不符，已停止。'
    }

    $existingBasic = [decimal](Get-Scalar @'
SELECT COUNT(*) FROM OpdBasicTbl
WHERE chOp1Date=:visit_date AND chOp1Time=:visit_time
  AND chOp1Room=:visit_room AND intOp1No=:visit_no
'@ $visitParameters)
    $existingOrders = [decimal](Get-Scalar @'
SELECT COUNT(*) FROM OpdOrdTbl
WHERE chOp1Date=:visit_date AND chOp1Time=:visit_time
  AND chOp1Room=:visit_room AND intOp1No=:visit_no
'@ $visitParameters)
    $existingRegistration = [decimal](Get-Scalar @'
SELECT COUNT(*) FROM OpdRegPtnTbl
WHERE chOp0Date=:visit_date AND chOp0Time=:visit_time
  AND chOp0Room=:visit_room AND intOp0No=:visit_no
'@ $visitParameters)
    $existingMrn = [decimal](Get-Scalar 'SELECT COUNT(*) FROM OpdBasicTbl WHERE chOp1MrNo=:mrn' @{ mrn = $medicalRecordNo })
    if ($existingBasic -ne 0 -or $existingOrders -ne 0 -or
        $existingRegistration -ne 0 -or $existingMrn -ne 0) {
        throw 'C24 測試鍵值已存在，未新增任何資料。'
    }

    $transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::ReadCommitted)
    try {
        $basicParameters = @{} + $visitParameters
        $basicParameters.room_type = 'O'
        $basicParameters.mrn = $medicalRecordNo
        $basicParameters.patient_name = 'C24測試病例'
        $basicParameters.section = '0201'
        $basicParameters.fin1 = '01'
        $basicParameters.card_sequence = '001'
        $basicSql = @'
INSERT INTO OpdBasicTbl
    (chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp1RoomType,chOp1MrNo,
     chOp1PName,chOp1Sec,chOp1PFin1,chOp1InSeq)
VALUES
    (:visit_date,:visit_time,:visit_room,:visit_no,:room_type,:mrn,
     :patient_name,:section,:fin1,:card_sequence)
'@
        $command = New-DbCommand $basicSql $basicParameters $transaction
        try { if ($command.ExecuteNonQuery() -ne 1) { throw '新增測試就診失敗。' } }
        finally { $command.Dispose() }

        $registrationParameters = @{} + $visitParameters
        $registrationParameters.room_type = 'O'
        $registrationParameters.mrn = $medicalRecordNo
        $registrationParameters.patient_name = 'C24測試病例'
        $registrationParameters.section = '0201'
        $registrationParameters.fin1 = '01'
        $registrationParameters.card_sequence = '001'
        $registrationSql = @'
INSERT INTO OpdRegPtnTbl
    (chOp0Date,chOp0Time,chOp0Room,intOp0No,chOp0RoomType,chOp0PMrNo,
     chOp0PName,chOp0SecNo,chOp0Fin1,chOp0Seq)
VALUES
    (:visit_date,:visit_time,:visit_room,:visit_no,:room_type,:mrn,
     :patient_name,:section,:fin1,:card_sequence)
'@
        $command = New-DbCommand $registrationSql $registrationParameters $transaction
        try { if ($command.ExecuteNonQuery() -ne 1) { throw '新增測試掛號資料失敗。' } }
        finally { $command.Dispose() }

        $orderSql = @'
INSERT INTO OpdOrdTbl
    (chOp1Date,chOp1Time,chOp1Room,intOp1No,intOp1OrdSeq,chOp4PFin1,
     chOp4CUser,chOp4IDate,chOp4OrdNo,chOp4Dct,rlOp4Sub1,rlOp4Sub3,rlOp4Sub6)
VALUES
    (:visit_date,:visit_time,:visit_room,:visit_no,:order_sequence,:fin1,
     :created_by,:created_at,:order_no,:charge_code,:sub1,:sub3,:sub6)
'@
        $orders = @(
            @{ order_sequence = 1; order_no = 'C24D001'; charge_code = '01'; sub1 = [decimal]1000; sub3 = [decimal]0; sub6 = [decimal]1000 },
            @{ order_sequence = 2; order_no = 'ACC-69'; charge_code = '69'; sub1 = [decimal]300; sub3 = [decimal]0; sub6 = [decimal]0 }
        )
        foreach ($order in $orders) {
            $orderParameters = @{} + $visitParameters
            $orderParameters.fin1 = '01'
            $orderParameters.created_by = 'C24TEST'
            $orderParameters.created_at = $dayBegin
            foreach ($name in $order.Keys) { $orderParameters[$name] = $order[$name] }
            $command = New-DbCommand $orderSql $orderParameters $transaction
            try { if ($command.ExecuteNonQuery() -ne 1) { throw '新增測試醫囑失敗。' } }
            finally { $command.Dispose() }
        }

        $accountingParameters = @{
            accounting_date = $visitDate
            day_begin = $dayBegin
            day_end = $dayEnd
        }
        $debtSql = 'SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::A02 + ') WHERE QuerySub6=1000'
        $paymentSql = 'SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::A03 + ') WHERE QuerySub6=-300'
        $patientSql = 'SELECT COUNT(*) FROM (' + [OpdAccrRptWeb.Repositories.C24Sql]::E02 + ') WHERE MedicalRecordNo=:mrn'
        $patientParameters = @{} + $visitParameters
        $patientParameters.mrn = $medicalRecordNo
        $debtCount = [decimal](Get-Scalar $debtSql $accountingParameters $transaction)
        $paymentCount = [decimal](Get-Scalar $paymentSql $accountingParameters $transaction)
        $patientCount = [decimal](Get-Scalar $patientSql $patientParameters $transaction)
        if ($debtCount -ne 1 -or $paymentCount -ne 1 -or $patientCount -ne 1) {
            throw "C24 來源驗證未通過，已回滾；欠款候選=$debtCount，補繳候選=$paymentCount，病患補值=$patientCount。"
        }

        $transaction.Commit()
        Write-Output 'DBTEST3 C24 來源測試資料已寫入：2026-10-30、門診、病歷號 T24CMP0001。'
        Write-Output '來源：OpdBasicTbl 1 筆、OpdRegPtnTbl 1 筆、OpdOrdTbl 2 筆；預期欠款 1000、補繳 300、未清 700。'
    }
    catch {
        $transaction.Rollback()
        throw
    }
    finally { $transaction.Dispose() }
}
finally { $connection.Dispose() }
