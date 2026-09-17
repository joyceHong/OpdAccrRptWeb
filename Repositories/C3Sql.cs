using System.Collections.Concurrent;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public static class C3Sql
{
    private static readonly string RoomSlots = string.Join(',', Enumerable.Range(1, 50).Select(i => $":room_{i:00}"));
    private static readonly string ChargeSlots = string.Join(',', Enumerable.Range(1, 50).Select(i => $":charge_{i:00}"));
    private static readonly ConcurrentDictionary<(CareSource Source, DepartmentFilterMode Mode), string> Cache = new();

    public static string Opd => Get(CareSource.O, DepartmentFilterMode.None);
    public static string Ipd => Get(CareSource.I, DepartmentFilterMode.None);

    public static string Get(CareSource source, DepartmentFilterMode mode) =>
        Cache.GetOrAdd((source, mode), static key => key.Source switch
        {
            CareSource.O => Build("OpdOrdTbl", "OpdBasicTbl", includeSystem: true,
                DepartmentPredicate(key.Mode)),
            CareSource.I => Build("IpdOrdTbl", "IpdBasicTbl", includeSystem: false,
                "AND (:department_code IS NULL OR RTRIM(B.chStation)=:department_code)"),
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        });

    private static string Build(string orderTable, string basicTable, bool includeSystem,
        string departmentPredicate)
    {
        string systemColumn = includeSystem ? "B.chOp4Sys" : "CAST(NULL AS CHAR(2)) AS chOp4Sys";
        return $"""
            WITH movement_rows AS (
              SELECT 1 movement_type,B.rlOp4OrdTot signed_quantity,C.chOp1Room,B.chStation,
                     B.chOp4OrdNo,C.chOp1Sec,{systemColumn},B.chOp4Dct,C.chOp1MrNo,
                     C.chOp1PName,C.chOp1DrName,B.chOp4SPay,A.chOrdCName,A.chOrdInv,
                     A.chOrdInvAdd,B.chOp4Proj,C.chOp1Clamc
              FROM GenOrdBasicTbl A JOIN {orderTable} B ON B.chOp4OrdNo=A.chOrdNo
              JOIN {basicTable} C ON B.chOp1Date=C.chOp1Date AND B.chOp1Time=C.chOp1Time
               AND B.chOp1Room=C.chOp1Room AND B.intOp1No=C.intOp1No
              WHERE B.chOp4IDate BETWEEN :day_begin AND :day_end
                AND (B.chOp4DCDate NOT LIKE :run_date||'%' OR RTRIM(B.chOp4DCDate) IS NULL)
                {departmentPredicate}
              UNION ALL
              SELECT 2,B.rlOp4OrdTot*-1,C.chOp1Room,B.chStation,B.chOp4OrdNo,C.chOp1Sec,
                     {systemColumn},B.chOp4Dct,C.chOp1MrNo,C.chOp1PName,C.chOp1DrName,
                     B.chOp4SPay,A.chOrdCName,A.chOrdInv,A.chOrdInvAdd,B.chOp4Proj,C.chOp1Clamc
              FROM GenOrdBasicTbl A JOIN {orderTable} B ON B.chOp4OrdNo=A.chOrdNo
              JOIN {basicTable} C ON B.chOp1Date=C.chOp1Date AND B.chOp1Time=C.chOp1Time
               AND B.chOp1Room=C.chOp1Room AND B.intOp1No=C.intOp1No
              WHERE B.chOp4DCDate BETWEEN :day_begin AND :day_end
                AND RTRIM(B.chOp4IDate) IS NOT NULL AND B.chOp4IDate NOT LIKE :run_date||'%'
                {departmentPredicate}
            ), filtered_rows AS (
              SELECT M.* FROM movement_rows M WHERE RTRIM(M.chOrdInv) IS NOT NULL
               AND M.chOp1MrNo NOT IN ('C36979','1000000')
               AND ((:logistics_type=1 AND M.chOrdInvAdd='0') OR (:logistics_type=2 AND M.chOrdInvAdd IN ('1','2')) OR (:logistics_type=0 AND M.chOrdInvAdd<='2'))
               AND ((:run_date>='1030401' AND SUBSTR(M.chOp4Dct,1,2) IN ('07','19','18')) OR (:run_date<'1030401' AND SUBSTR(M.chOp4Dct,1,2) IN ('07','19')))
               AND (M.chOp4Proj NOT IN ('S','D') OR RTRIM(M.chOp4Proj) IS NULL)
               AND (:has_room_filter=0 OR RTRIM(M.chOp1Room) IN ({RoomSlots}))
               AND (:has_charge_filter=0 OR RTRIM(M.chOp4OrdNo) IN ({ChargeSlots}))
            )
            SELECT M.movement_type,M.chOp1Room,M.chStation,M.chOp4OrdNo,M.chOp1Sec,M.chOp4Sys,
              CASE WHEN :detail_type=1 THEN M.chOp4Dct END chOp4Dct,
              CASE WHEN :detail_type=1 THEN M.chOp1MrNo END chOp1MrNo,
              CASE WHEN :detail_type=1 THEN M.chOp1PName END chOp1PName,
              CASE WHEN :detail_type=1 THEN M.chOp1DrName END chOp1DrName,
              CASE WHEN :detail_type=1 THEN M.chOp4SPay END chOp4SPay,
              M.chOrdCName,M.chOrdInv,M.chOrdInvAdd,M.chOp4Proj,M.chOp1Clamc,
              SUM(M.signed_quantity) rlOp4OrdTot
            FROM filtered_rows M
            GROUP BY M.movement_type,M.chOp1Room,M.chStation,M.chOp4OrdNo,M.chOp1Sec,M.chOp4Sys,
              CASE WHEN :detail_type=1 THEN M.chOp4Dct END,
              CASE WHEN :detail_type=1 THEN M.chOp1MrNo END,
              CASE WHEN :detail_type=1 THEN M.chOp1PName END,
              CASE WHEN :detail_type=1 THEN M.chOp1DrName END,
              CASE WHEN :detail_type=1 THEN M.chOp4SPay END,
              M.chOrdCName,M.chOrdInv,M.chOrdInvAdd,M.chOp4Proj,M.chOp1Clamc
            ORDER BY M.movement_type,M.chStation,M.chOp4OrdNo
            """;
    }

    private static string DepartmentPredicate(DepartmentFilterMode mode) => mode switch
    {
        DepartmentFilterMode.None => string.Empty,
        DepartmentFilterMode.Station => "AND B.chStation=:department_code",
        DepartmentFilterMode.Sys56 => "AND B.chOp4Sys='56'",
        DepartmentFilterMode.Sys53Wound => "AND (B.chOp4Sys='53' OR C.chOp1Room='5D103' OR C.chOp1Sec='0283E')",
        DepartmentFilterMode.Sys52 => "AND B.chOp4Sys='52'",
        DepartmentFilterMode.Sys51 => "AND B.chOp4Sys='51'",
        DepartmentFilterMode.Sys39 => "AND B.chOp4Sys='39'",
        DepartmentFilterMode.Sys38 => "AND B.chOp4Sys='38'",
        DepartmentFilterMode.Hd3 => "AND ((C.chOp1Sec='0212*' AND C.chOp1Room LIKE '3J%') OR B.chOp4Sys='43')",
        DepartmentFilterMode.Hd4 => "AND ((C.chOp1Sec='0212*' AND C.chOp1Room LIKE '4J%') OR B.chOp4Sys='36')",
        DepartmentFilterMode.Hd5 => "AND ((C.chOp1Sec='0212*' AND (C.chOp1Room LIKE '5J%' OR C.chOp1Room LIKE '5F%')) OR B.chOp4Sys='42')",
        DepartmentFilterMode.Pd => "AND (C.chOp1Sec='0205A' OR B.chOp4Sys='37')",
        DepartmentFilterMode.Er => "AND ((C.chOp1Room='0000' AND B.chOp4Sys NOT IN ('5','7','14','16','18','19')) OR C.chOp1Sec='0291')",
        DepartmentFilterMode.Anesthesia => "AND (B.chOp4Sys='7' OR C.chOp1Sec='0330')",
        DepartmentFilterMode.OperatingRoom => "AND (((C.chOp1Room='3F1' OR C.chOp1Room LIKE 'OP_%') AND B.chOp4Sys<>'7' AND RTRIM(B.chStation) IS NULL) OR (C.chOp1Room='0000' AND B.chOp4Sys='5') OR B.chStation='0532' OR B.chStation LIKE '1OR%')",
        DepartmentFilterMode.Endoscopy => "AND ((C.chOp1Room LIKE '7F%' AND B.chOp4Sys<>'7') OR B.chOp4Sys='14')",
        DepartmentFilterMode.Beauty => "AND (((C.chOp1Room LIKE '4F8%' AND C.chOp1Room<>'4F8' AND B.chOp4Sys<>'7' AND RTRIM(B.chStation) IS NULL) OR B.chOp4Sys='18') OR (C.chOp1Sec IN ('0283A','0283B','0283C','0296','0296A','0296B','0296C') AND RTRIM(B.chStation) IS NULL) OR B.chStation='0296')",
        DepartmentFilterMode.Cath => "AND (((C.chOp1Room LIKE '3F5%' AND B.chOp4Sys<>'7') OR B.chOp4Sys='19') OR C.chOp1Sec='0401B')",
        DepartmentFilterMode.Radiology => "AND (B.chOp4Sys='16' OR C.chOp1Sec LIKE '0340%')",
        DepartmentFilterMode.RadiologyTechnology => "AND B.chOp4Sys='27'",
        DepartmentFilterMode.Ent => "AND ((C.chOp1Room='20A' AND B.chOp4Sys<>'7') OR C.chOp1Sec='0250B')",
        DepartmentFilterMode.Delivery => "AND ((C.chOp1Room='4F7' AND B.chOp4Sys<>'7') OR B.chOp4Sys='31')",
        DepartmentFilterMode.Pharmacy => "AND B.chOp4Sys='41'",
        DepartmentFilterMode.Dispensing => "AND :run_date>='1061201' AND LNNVL(C.chOp1Room='0000') AND B.chOp4OrdNo IN ('DS0.5','DS1I','PN32G4BD','PN31GBD')",
        DepartmentFilterMode.GeneralLocation => GeneralLocationPredicate,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private const string GeneralLocationPredicate = """
        AND B.chOp4Sys NOT IN ('56','53','52','51','39','38','43','36','42','37','5','7','14','16','18','19','27','31','41')
        AND (:department_code='0512' OR B.chOp4Sys<>'46') AND (B.chStation NOT LIKE '1OR%' OR RTRIM(B.chStation) IS NULL)
        AND C.chOp1Room NOT IN ('0000','3F1','20A','4F7') AND C.chOp1Room NOT LIKE 'OP_%'
        AND (B.chStation<>'0532' OR RTRIM(B.chStation) IS NULL) AND C.chOp1Room NOT LIKE '7F%'
        AND (C.chOp1Room NOT LIKE '4F8%' OR C.chOp1Room='4F8') AND (B.chStation<>'0296' OR RTRIM(B.chStation) IS NULL)
        AND C.chOp1Room NOT LIKE '3F5%'
        AND (:run_date<'1061201' OR NOT (LNNVL(C.chOp1Room='0000') AND B.chOp4OrdNo IN ('DS0.5','DS1I','PN32G4BD','PN31GBD')))
        AND ((:department_code='0512' AND (B.chOp4Sys='46' OR (C.chOp1Sec IN (SELECT chSecNo FROM GenSectionTbl WHERE RTRIM(chLocation)=:department_code) AND (B.chStation=:department_code OR RTRIM(B.chStation) IS NULL))) AND LNNVL(C.chOp1Room='5D103') AND LNNVL(C.chOp1Sec='0283E'))
          OR (:department_code<>'0512' AND C.chOp1Sec IN (SELECT chSecNo FROM GenSectionTbl WHERE RTRIM(chLocation)=:department_code)))
        """;
}
