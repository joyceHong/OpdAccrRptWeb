using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C23ContractAccountingRepository(IConnectionStringProvider connectionStringProvider)
    : IC23ContractAccountingRepository
{
    internal static readonly IReadOnlyList<C23ContractOption> SpecialContracts =
    [
        new("TT", "研究經費"), new("UU", "其他"), new("VV", "維康記帳"),
        new("WW", "北縣65歲以上老人健檢"), new("XX", "老人照護鑑定"),
        new("YY", "殘障鑑定"), new("ZZ", "聯盟代檢")
    ];
    internal const string ContractsSql = """
        SELECT RTRIM(chDctType) AS Code, MAX(RTRIM(chDctTypeName)) AS Name
        FROM GenDctTypeTbl
        WHERE RTRIM(chDctType) IS NOT NULL
        GROUP BY RTRIM(chDctType)
        ORDER BY RTRIM(chDctType)
        """;

    internal const string BasicSqlTemplate = """
        SELECT RTRIM(b.chOp1MrNo) AS MedicalRecordNumber, RTRIM(b.chOp1PName) AS PatientName,
               RTRIM(b.chOp1DrName) AS DoctorName, RTRIM(s.chSecName) AS DepartmentName
        FROM {BASIC} b LEFT JOIN GenSectionTbl s ON b.chOp1Sec=s.chSecNo
        WHERE b.chOp1Date=:visitDate AND b.chOp1Time=:visitTime
          AND b.chOp1Room=:visitRoom AND b.intOp1No=:visitNumber
        """;

    internal const string InsertIntermediateTemplate = """
        INSERT INTO {TARGET}
          (chOp1Date2,chOp1MrNo2,chOp1RoomTypeName2,chOp1Date,chOp1RoomType,chOp1RoomTypeName,
           chOp1PFin2,chOp1PFin2Nm,chOp1MrNo,chOp1PName,chOp1PSecNm,chOp1DrIDNm,
           chOp1Dct,chOp1DctNm,rlOp1Sub3,rlOp1Sub2,rlOp1SubAMT,chCUser,intFlag,chDateFlag)
        VALUES
          (:AccountingDate,:MedicalRecordNumber,:RoomTypeName,:AccountingDate,:RoomType,:RoomTypeName,
           :ContractCode,:ContractName,:MedicalRecordNumber,:PatientName,:DepartmentName,:DoctorName,
           :BillingCode,:BillingName,:DiscountAmount,:ContractAmount,:GrossAmount,:Creator,0,:DateFlag)
        """;

    internal const string InsertContractHeaderSql = """
        INSERT INTO IpdContractTbl
            (chOp1Date,chOp1Time,chOp1Room,intOp1No,vchRoomType,vchPrintFlag,vchPayBack,intOp2Amt,vchStat)
        VALUES (:dateValue,:timeValue,:roomValue,:numberValue,:roomType,'0','0',0,'00')
        """;

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<C23ContractAccountingReportViewModel>();

    public IReadOnlyList<C23ContractOption> GetContracts()
    {
        using IDbConnection connection = CreateConnection();
        return MergeContracts(connection.Query<C23ContractOption>(ContractsSql));
    }

    internal static IReadOnlyList<C23ContractOption> MergeContracts(IEnumerable<C23ContractOption> masterContracts) =>
        masterContracts.Concat(SpecialContracts)
            .GroupBy(option => option.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.FirstOrDefault(option => !string.IsNullOrWhiteSpace(option.Name)) ?? group.First())
            .OrderBy(option => option.Code, StringComparer.Ordinal)
            .ToList();

    public int GetCount(SearchReportCondition condition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM ({GetBaseSql(condition)}) C23Rows", CreateParameters(condition));
    }

    public List<C23ContractAccountingReportViewModel> GetPage(SearchReportCondition condition)
    {
        var sql = $"SELECT * FROM ({GetBaseSql(condition)}) C23Rows ORDER BY AccountingDate, ContractCode, MedicalRecordNumber, BillingCode OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";
        using IDbConnection connection = CreateConnection();
        return connection.Query<C23ContractAccountingReportViewModel>(sql, CreateParameters(condition)).ToList();
    }

    public bool HasIntermediateData(string encounterSource, string rocDate)
    {
        var table = GetIntermediateTable(encounterSource);
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {table} WHERE chDateFlag BETWEEN :dateStart AND :dateEnd",
            new { dateStart = rocDate + "000000", dateEnd = rocDate + "999999" }) > 0;
    }

    public void RebuildSingleDay(string encounterSource, string rocDate, string? contractCode)
    {
        var outpatient = encounterSource == C23EncounterSources.Outpatient;
        if (!outpatient && encounterSource != C23EncounterSources.Inpatient)
            throw new ArgumentException("C23 來源僅接受門急診或住院。", nameof(encounterSource));
        using var connection = (OracleConnection)CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var commands = C23RebuildSql.GetCommands(encounterSource, rocDate);
        ExecuteAtomically(commands,
            command => connection.Execute(command, new
            {
                dateStart = rocDate + "000000", dateEnd = rocDate + "999999",
                AccountingDate = rocDate, AccountingDayStart = rocDate + "000000",
                AccountingDayEnd = rocDate + "999999", SDate = rocDate
            }, transaction),
            () => RebuildIntermediate(connection, transaction, encounterSource, rocDate, contractCode),
            transaction.Commit, transaction.Rollback);
    }

    internal static string GetBaseSql(SearchReportCondition condition)
    {
        if (condition.DateMode == C23DateModes.EncounterDate)
        {
            return GetEncounterDateSql(condition.EncounterSource!);
        }

        var table = GetIntermediateTable(condition.EncounterSource!);
        var detail = $"""
            SELECT chOp1Date2 AS AccountingDate, RTRIM(chOp1PFin2) AS ContractCode,
                   RTRIM(chOp1PFin2Nm) AS ContractName, RTRIM(chOp1MrNo) AS MedicalRecordNumber,
                   RTRIM(chOp1PName) AS PatientName, RTRIM(chOp1PSecNm) AS DepartmentName,
                   RTRIM(chOp1Dct) AS BillingCode, RTRIM(chOp1DctNm) AS BillingName,
                   NVL(rlOp1SubAMT, 0) AS GrossAmount,
                   TO_NUMBER(NVL(NULLIF(TRIM(rlOp1Sub2), ''), '0')) AS ContractAmount,
                   NVL(rlOp1Sub3, 0) AS DiscountAmount, 'Detail' AS ResultType
            FROM {table}
            WHERE chDateFlag BETWEEN :dateFlagStart AND :dateFlagEnd
              AND (:contract IS NULL OR RTRIM(chOp1PFin2) = :contract)
              AND ((:roomType = '2' AND chOp1RoomType = '2') OR (:roomType = 'N' AND chOp1RoomType IS NULL))
            """;
        if (condition.StartDate == condition.EndDate)
        {
            return detail;
        }
        return $"""
            SELECT :startDate || '-' || :endDate AS AccountingDate, RTRIM(chOp1PFin2) AS ContractCode,
                   MAX(RTRIM(chOp1PFin2Nm)) AS ContractName, '' AS MedicalRecordNumber,
                   '' AS PatientName, '' AS DepartmentName, RTRIM(chOp1Dct) AS BillingCode,
                   MAX(RTRIM(chOp1DctNm)) AS BillingName, SUM(NVL(rlOp1SubAMT, 0)) AS GrossAmount,
                   SUM(TO_NUMBER(NVL(NULLIF(TRIM(rlOp1Sub2), ''), '0'))) AS ContractAmount,
                   SUM(NVL(rlOp1Sub3, 0)) AS DiscountAmount, 'Summary' AS ResultType
            FROM {table}
            WHERE chDateFlag BETWEEN :dateFlagStart AND :dateFlagEnd
              AND (:contract IS NULL OR RTRIM(chOp1PFin2) = :contract)
              AND ((:roomType = '2' AND chOp1RoomType = '2') OR (:roomType = 'N' AND chOp1RoomType IS NULL))
            GROUP BY RTRIM(chOp1PFin2), RTRIM(chOp1Dct)
            UNION ALL
            {detail}
            """;
    }

    internal static object CreateParameters(SearchReportCondition condition)
    {
        var pageNumber = condition.PageNumber ?? 1;
        var pageSize = condition.PageSize ?? 10;
        return new
        {
            startDate = ToRocDate(DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd")),
            endDate = ToRocDate(DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd")),
            dateFlagStart = ToRocDate(DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd")) + "000000",
            dateFlagEnd = ToRocDate(DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd")) + "999999",
            contract = string.IsNullOrWhiteSpace(condition.ContractCode) ? null : condition.ContractCode.Trim(),
            roomType = condition.InpatientType == C23InpatientTypes.Discharged ? "2" : "N",
            inpatientType = condition.DateMode == C23DateModes.EncounterDate ? null : condition.InpatientType,
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    internal static string ToRocDate(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToRocDateString();

    internal static void ExecuteAtomically(
        IReadOnlyList<string> commands, Action<string> execute, Action rebuildIntermediate,
        Action commit, Action rollback)
    {
        try
        {
            execute(commands[0]);
            rebuildIntermediate();
            foreach (var command in commands.Skip(1)) execute(command);
            commit();
        }
        catch
        {
            rollback();
            throw;
        }
    }

    private static string GetIntermediateTable(string source) => source switch
    {
        C23EncounterSources.Outpatient => "OpdRecRpt_PFin2SumDM1",
        C23EncounterSources.Inpatient => "IpdRecRpt_PFin2SumDM1",
        _ => throw new ArgumentException("C23 來源僅接受門急診或住院。", nameof(source))
    };

    private static string GetEncounterDateSql(string source)
    {
        return $"""
            SELECT chop1date AS AccountingDate, RTRIM(chop1pfin2) AS ContractCode,
                   RTRIM(chop1pfin2nm) AS ContractName, RTRIM(chop1mrno) AS MedicalRecordNumber,
                   RTRIM(chop1pname) AS PatientName, RTRIM(chop1psecnm) AS DepartmentName,
                   RTRIM(chop1dct) AS BillingCode, RTRIM(chop1dctnm) AS BillingName,
                   NVL(rlop1subamt,0) AS GrossAmount, NVL(rlop1sub2,0) AS ContractAmount,
                   NVL(rlop1sub3,0) AS DiscountAmount, 'Detail' AS ResultType
            FROM ({C23SourceSql.EncounterDateFor(source)})
            """;
    }

    private static void RebuildIntermediate(
        OracleConnection connection, OracleTransaction transaction,
        string source, string rocDate, string? selectedContract)
    {
        var outpatient = source == C23EncounterSources.Outpatient;
        var basicSql = BasicSqlTemplate.Replace("{BASIC}", outpatient ? "OpdBasicTbl" : "IpdBasicTbl", StringComparison.Ordinal);
        var insertSql = InsertIntermediateTemplate.Replace("{TARGET}", outpatient ? "OpdRecRpt_PFin2SumDM1" : "IpdRecRpt_PFin2SumDM1", StringComparison.Ordinal);
        var sequence = 0;
        var contractHeaderKeys = new HashSet<string>(StringComparer.Ordinal);
        var writesContractHeaders = string.CompareOrdinal(rocDate, outpatient ? "0980901" : "0980801") >= 0;
        foreach (var sourceQuery in C23SourceSql.QueriesFor(source))
        {
            var rows = connection.Query<C23SourceRow>(sourceQuery.Sql,
                new { SDate = rocDate, PFin2 = string.IsNullOrWhiteSpace(selectedContract) ? null : selectedContract.Trim() }, transaction);
            foreach (var row in rows)
            {
                var headerKey = $"{row.ChOp1Date}\u001f{row.ChOp1Time}\u001f{row.ChOp1Room}\u001f{row.IntOp1No}";
                if (writesContractHeaders && contractHeaderKeys.Add(headerKey))
                {
                    connection.Execute(InsertContractHeaderSql, new
                    {
                        dateValue = row.ChOp1Date, timeValue = row.ChOp1Time,
                        roomValue = row.ChOp1Room, numberValue = row.IntOp1No,
                        roomType = row.ChOp1Time == "0" ? "I" : row.ChOp1Room.Trim() == "0000" ? "E" : "R"
                    }, transaction);
                }
                var effectiveIn = MaxDate(row.IDate, row.ChOp1Date);
                var effectiveOut = string.IsNullOrWhiteSpace(row.DCDate) ? null : MaxDate(row.DCDate, row.ChOp1Date);
                if (effectiveIn == effectiveOut) continue;
                var basic = connection.QuerySingleOrDefault<C23BasicRow>(basicSql, new
                {
                    visitDate = row.ChOp1Date, visitTime = row.ChOp1Time,
                    visitRoom = row.ChOp1Room, visitNumber = row.IntOp1No
                }, transaction);
                if (basic is null || basic.MedicalRecordNumber is "2006010" or "1999999") continue;
                var calculated = C23AccountingCalculationService.Calculate(new(
                    rocDate, row.ChOp1Room, row.ChOp4PFin2, row.ChOp4OrdNo,
                    row.ChOp4SPay, row.VchIrbNo, !outpatient, row.RlOp4Sub3, row.RlOp4Sub5,
                    effectiveOut == rocDate));
                if (selectedContract is not null && calculated.ContractCode != selectedContract.Trim()) continue;
                sequence++;
                connection.Execute(insertSql, new
                {
                    AccountingDate = rocDate,
                    basic.MedicalRecordNumber, basic.PatientName, basic.DepartmentName, basic.DoctorName,
                    RoomType = sourceQuery.IsDischargeSnapshot ? "2" : null,
                    RoomTypeName = sourceQuery.IsDischargeSnapshot ? "出院" : outpatient ? "門急診" : "住院",
                    calculated.ContractCode,
                    ContractName = string.IsNullOrEmpty(calculated.ContractName) ? row.ChDctTypeName.Trim() : calculated.ContractName,
                    BillingCode = row.ChOp4Dct.Trim(), BillingName = row.ChDctItemName.Trim(),
                    calculated.DiscountAmount, calculated.ContractAmount, calculated.GrossAmount,
                    Creator = row.ChOp4CUser.Trim(), DateFlag = rocDate + sequence.ToString("D6")
                }, transaction);
            }
        }
    }

    private static string MaxDate(string? value, string visitDate)
    {
        var date = string.IsNullOrWhiteSpace(value) ? visitDate : value.Trim()[..Math.Min(7, value.Trim().Length)];
        return string.CompareOrdinal(date, visitDate) < 0 ? visitDate : date;
    }

    private IDbConnection CreateConnection() => new OracleConnection(connectionStringProvider.GetConnectionString());
}
