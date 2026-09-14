using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

internal sealed record C23SourceQuery(string Sql, bool IsDischargeSnapshot);

internal static class C23SourceSql
{
    internal static IReadOnlyDictionary<string, string> FinalizedSql =>
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["C23_O_01_credit_ord.sql"] = C23O01CreditOrdSql,
        ["C23_O_02_credit_drg.sql"] = C23O02CreditDrgSql,
        ["C23_O_03_noncredit_ord.sql"] = C23O03NoncreditOrdSql,
        ["C23_O_04_noncredit_drg.sql"] = C23O04NoncreditDrgSql,
        ["C23_I_01_credit_ord.sql"] = C23I01CreditOrdSql,
        ["C23_I_02_credit_drg.sql"] = C23I02CreditDrgSql,
        ["C23_I_03_noncredit_ord.sql"] = C23I03NoncreditOrdSql,
        ["C23_I_04_noncredit_drg.sql"] = C23I04NoncreditDrgSql,
        ["C23_I_05_discharge_snapshot.sql"] = C23I05DischargeSnapshotSql,
        ["C23_O_encounter_date.sql"] = C23OEncounterDateSql,
        ["C23_I_encounter_date.sql"] = C23IEncounterDateSql,
        ["C23_O_general_balance.sql"] = string.Join(";\n", C23OGeneralBalanceCommands),
        ["C23_I_general_balance.sql"] = string.Join(";\n", C23IGeneralBalanceCommands),
        ["C23_O_balance42_rebuild.sql"] = string.Join(";\n", C23OBalance42RebuildCommands),
        ["C23_I_balance42_rebuild.sql"] = string.Join(";\n", C23IBalance42RebuildCommands)
    };

    private static readonly IReadOnlyList<C23SourceQuery> OutpatientQueries =
    [
        new(C23O01CreditOrdSql, false), new(C23O02CreditDrgSql, false),
        new(C23O03NoncreditOrdSql, false), new(C23O04NoncreditDrgSql, false)
    ];

    private static readonly IReadOnlyList<C23SourceQuery> InpatientQueries =
    [
        new(C23I01CreditOrdSql, false), new(C23I02CreditDrgSql, false),
        new(C23I03NoncreditOrdSql, false), new(C23I04NoncreditDrgSql, false),
        new(C23I05DischargeSnapshotSql, true)
    ];

    internal static IReadOnlyList<C23SourceQuery> QueriesFor(string source) => source switch
    {
        C23EncounterSources.Outpatient => OutpatientQueries,
        C23EncounterSources.Inpatient => InpatientQueries,
        _ => throw new ArgumentException("C23 來源僅接受門急診或住院。", nameof(source))
    };

    internal static string EncounterDateFor(string source) => source switch
    {
        C23EncounterSources.Outpatient => C23OEncounterDateSql,
        C23EncounterSources.Inpatient => C23IEncounterDateSql,
        _ => throw new ArgumentException("C23 來源僅接受門急診或住院。", nameof(source))
    };

    internal static IReadOnlyList<string> GeneralBalanceCommandsFor(string source) => source switch
    {
        C23EncounterSources.Outpatient => C23OGeneralBalanceCommands,
        C23EncounterSources.Inpatient => C23IGeneralBalanceCommands,
        _ => throw new ArgumentException("C23 來源僅接受門急診或住院。", nameof(source))
    };

    internal static IReadOnlyList<string> Balance42CommandsFor(string source) => source switch
    {
        C23EncounterSources.Outpatient => C23OBalance42RebuildCommands,
        C23EncounterSources.Inpatient => C23IBalance42RebuildCommands,
        _ => throw new ArgumentException("C23 來源僅接受門急診或住院。", nameof(source))
    };

    private const string C23O01CreditOrdSql = """
SELECT q.*
FROM (
    select chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp4IDate , 1 , 7 ) IDate , substr ( chOp4DCDate , 1 , 7 ) DCDate , chOp4PFin2 , chDctTypeName , chOp4Dct , chDctItemName , chOp4CUser , chOp4OrdNo , sum ( rlOp4Sub5 ) rlOp4Sub5 , sum ( rlOp4Sub3 ) rlOp4Sub3 , A . chop4spay
    from OpdOrdTbl A , (
        select chdcttype , chdctcredflg , chdcttypename
        From GenDctTypeTbl
        where ( chdctcredflg = '1'
            or chdcttype = '05B' )
        Union
        select distinct X . chdcttype , '1' , Y . chdcttypename
        from GenDctTbl X
        join GenDctTypeTbl Y
        on ( X . chdcttype = Y . chdcttype )
        where X . vchcredflg = '1' ) B , GenDctItemTbl C
    where chOp4PFin2 = chDctType
    and chOp4Dct = chDctItem
    and ( chDctCredFlg = '1'
        or ( chDctCredFlg = '0'
            and chDctType = '05B' ) )
    and ( ( chOp1Date < :SDate
            and ( chOp4IDate between ( :SDate || '0000' )
                and ( :SDate || '9999' )
                or ( rtrim ( chOp4IDate ) is not null
                and chOp4DCDate between ( :SDate || '0000' )
                and ( :SDate || '9999' ) ) ) )
        or ( chOp1Date = :SDate
            and chOp4IDate <= ( :SDate || '9999' )
            and rtrim ( chOp4IDate ) is not null ) )
    and rlOp4Sub5 <> 0
    and ( A . chop4proj not in ( 'I' , 'S' , 'D' )
        or rtrim ( A . chop4proj ) is null )
    group by chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp4IDate , 1 , 7 ) , substr ( chOp4DCDate , 1 , 7 ) , chOp4OrdNo , chOp4Dct , chDctItemName , chOp4PFin2 , chDctTypeName , chOp4CUser , chop4spay
) q
WHERE (:PFin2 IS NULL OR :PFin2 NOT IN ('VV','WW','XX','YY','ZZ','UU','TT'))
""";

    private const string C23O02CreditDrgSql = """
SELECT q.*
FROM (
    select chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp3IDate , 1 , 7 ) IDate , substr ( chOp3DCDate , 1 , 7 ) DCDate , chOp3PFin2 chOp4PFin2 , chDctTypeName , chOp3Dct chOp4Dct , chDctItemName , chOp3CUser chOp4CUser , chOp3DrgNo chOp4OrdNo , sum ( rlOp3Sub5 ) rlOp4Sub5 , sum ( rlOp3Sub3 ) rlOp4Sub3 , A . chop3spay as chop4spay
    from OpdDrgTbl A , (
        select chdcttype , chdctcredflg , chdcttypename
        From GenDctTypeTbl
        where ( chdctcredflg = '1'
            or chdcttype = '05B' )
        Union
        select distinct X . chdcttype , '1' , Y . chdcttypename
        from GenDctTbl X
        join GenDctTypeTbl Y
        on ( X . chdcttype = Y . chdcttype )
        where X . vchcredflg = '1' ) B , GenDctItemTbl C
    where chOp3PFin2 = chDctType
    and chOp3Dct = chDctItem
    and ( chDctCredFlg = '1'
        or ( chDctCredFlg = '0'
            and chDctType = '05B' ) )
    and ( ( chOp1Date < :SDate
            and ( chOp3IDate between ( :SDate || '0000' )
                and ( :SDate || '9999' )
                or ( rtrim ( chOp3IDate ) is not null
                and chOp3DCDate between ( :SDate || '0000' )
                and ( :SDate || '9999' ) ) ) )
        or ( chOp1Date = :SDate
            and chOp3IDate <= ( :SDate || '9999' )
            and rtrim ( chOp3IDate ) is not null ) )
    and rlOp3Sub5 <> 0
    and ( chop3stat not in ( '09' , '10' , '11' , '12' )
        or rtrim ( chop3stat ) is null )
    and ( chop3rep3flg <> 'S'
        or rtrim ( chop3rep3flg ) is null )
    and ( A . chop3proj not in ( 'I' , 'S' , 'D' )
        or rtrim ( A . chop3proj ) is null )
    group by chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp3IDate , 1 , 7 ) , substr ( chOp3DCDate , 1 , 7 ) , chOp3DrgNo , chOp3Dct , chDctItemName , chOp3PFin2 , chDctTypeName , chOp3CUser , chop3spay
) q
WHERE (:PFin2 IS NULL OR :PFin2 NOT IN ('VV','WW','XX','YY','ZZ','UU','TT'))
""";

    private const string C23O03NoncreditOrdSql = """
select chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp4IDate , 1 , 7 ) IDate , substr ( chOp4DCDate , 1 , 7 ) DCDate , chOp4PFin2 , chDctTypeName , chOp4Dct , chDctItemName , chOp4CUser , chOp4OrdNo , sum ( rlOp4Sub5 ) rlOp4Sub5 , sum ( rlOp4Sub3 ) rlOp4Sub3 , A . chop4spay
from OpdOrdTbl A , (
    select chdcttype , chdctcredflg , chdcttypename
    From GenDctTypeTbl
    where ( chdctcredflg <> '1'
        or chdcttype <> '05B' )
    minus
    select distinct X . chdcttype , '0' , Y . chdcttypename
    from GenDctTbl X
    join GenDctTypeTbl Y
    on ( X . chdcttype = Y . chdcttype )
    where X . vchcredflg = '1' ) B , GenDctItemTbl C
where chOp4PFin2 = chDctType
and chOp4Dct = chDctItem
and chDctCredFlg <> '1'
and chDctType <> '05B'
and ( chOp4IDate between ( :SDate || '0000' )
    and ( :SDate || '9999' )
    or ( rtrim ( chOp4IDate ) is not null
        and chOp4DCDate between ( :SDate || '0000' )
        and ( :SDate || '9999' ) ) )
and rlOp4Sub5 <> 0
and ( A . chop4proj not in ( 'I' , 'S' , 'D' )
    or rtrim ( A . chop4proj ) is null )
group by chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp4IDate , 1 , 7 ) , substr ( chOp4DCDate , 1 , 7 ) , chOp4OrdNo , chOp4Dct , chDctItemName , chOp4PFin2 , chDctTypeName , chOp4CUser , chop4spay
""";

    private const string C23O04NoncreditDrgSql = """
SELECT q.*
FROM (
    select chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp3IDate , 1 , 7 ) IDate , substr ( chOp3DCDate , 1 , 7 ) DCDate , chOp3PFin2 chOp4PFin2 , chDctTypeName , chOp3Dct chOp4Dct , chDctItemName , chOp3CUser chOp4CUser , chOp3DrgNo chOp4OrdNo , sum ( rlOp3Sub5 ) rlOp4Sub5 , sum ( rlOp3Sub3 ) rlOp4Sub3 , A . chop3spay as chop4spay
    from OpdDrgTbl A , (
        select chdcttype , chdctcredflg , chdcttypename
        From GenDctTypeTbl
        where ( chdctcredflg <> '1'
            or chdcttype <> '05B' )
        minus
        select distinct X . chdcttype , '0' , Y . chdcttypename
        from GenDctTbl X
        join GenDctTypeTbl Y
        on ( X . chdcttype = Y . chdcttype )
        where X . vchcredflg = '1' ) B , GenDctItemTbl C
    where chOp3PFin2 = chDctType
    and chOp3Dct = chDctItem
    and chDctCredFlg <> '1'
    and chDctType <> '05B'
    and ( chOp3IDate between ( :SDate || '0000' )
        and ( :SDate || '9999' )
        or ( rtrim ( chOp3IDate ) is not null
            and chOp3DCDate between ( :SDate || '0000' )
            and ( :SDate || '9999' ) ) )
    and rlOp3Sub5 <> 0
    and ( chop3stat not in ( '09' , '10' , '11' , '12' )
        or rtrim ( chop3stat ) is null )
    and ( chop3rep3flg <> 'S'
        or rtrim ( chop3rep3flg ) is null )
    and ( A . chop3proj not in ( 'I' , 'S' , 'D' )
        or rtrim ( A . chop3proj ) is null )
    group by chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp3IDate , 1 , 7 ) , substr ( chOp3DCDate , 1 , 7 ) , chOp3DrgNo , chOp3Dct , chDctItemName , chOp3PFin2 , chDctTypeName , chOp3CUser , chop3spay
) q
WHERE (:PFin2 IS NULL OR :PFin2 NOT IN ('WW','XX','YY'))
""";

    private const string C23I01CreditOrdSql = """
SELECT q.*
FROM (
    select chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp4IDate , 1 , 7 ) IDate , substr ( chOp4DCDate , 1 , 7 ) DCDate , chOp4PFin2 , chDctTypeName , chOp4Dct , chDctItemName , chOp4CUser , chOp4OrdNo , sum ( rlOp4Sub5 ) rlOp4Sub5 , sum ( rlOp4Sub3 ) rlOp4Sub3 , A . chop4spay , a . vchirbno
    from IpdOrdTbl A , (
        select chdcttype , chdctcredflg , chdcttypename
        From GenIpdDctTypeTbl
        where ( chdctcredflg = '1'
            or chdcttype = '05A' )
        Union
        select distinct X . chdcttype , '1' , Y . chdcttypename
        from GenIpdDctTbl X
        join GenIpdDctTypeTbl Y
        on ( X . chdcttype = Y . chdcttype )
        where X . vchcredflg = '1' ) B , GenDctItemTbl C
    where chOp4PFin2 = chDctType
    and chOp4Dct = chDctItem
    and ( chDctCredFlg = '1'
        or ( chDctCredFlg = '0'
            and chDctType = '05A' ) )
    and ( ( chOp1Date < :SDate
            and ( chOp4IDate between ( :SDate || '0000' )
                and ( :SDate || '9999' )
                or ( rtrim ( chOp4IDate ) is not null
                and chOp4DCDate between ( :SDate || '0000' )
                and ( :SDate || '9999' ) ) ) )
        or ( chOp1Date = :SDate
            and chOp4IDate <= ( :SDate || '9999' )
            and rtrim ( chOp4IDate ) is not null ) )
    and rlOp4Sub5 <> 0
    and ( A . chop4proj not in ( 'I' , 'S' , 'D' )
        or rtrim ( A . chop4proj ) is null )
    group by chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp4IDate , 1 , 7 ) , substr ( chOp4DCDate , 1 , 7 ) , chOp4OrdNo , chOp4Dct , chDctItemName , chOp4PFin2 , chDctTypeName , chOp4CUser , chop4spay , vchirbno
) q
WHERE (:PFin2 IS NULL OR :PFin2 NOT IN ('VV','WW','XX','YY','ZZ','UU','TT'))
""";

    private const string C23I02CreditDrgSql = """
SELECT q.*
FROM (
    select chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp3IDate , 1 , 7 ) IDate , substr ( chOp3DCDate , 1 , 7 ) DCDate , chOp3PFin2 chOp4PFin2 , chDctTypeName , chOp3Dct chOp4Dct , chDctItemName , chOp3CUser chOp4CUser , chOp3DrgNo chOp4OrdNo , sum ( rlOp3Sub5 ) rlOp4Sub5 , sum ( rlOp3Sub3 ) rlOp4Sub3 , A . chop3spay as chop4spay , a . vchirbno
    from IpdDrgTbl A , (
        select chdcttype , chdctcredflg , chdcttypename
        From GenIpdDctTypeTbl
        where ( chdctcredflg = '1'
            or chdcttype = '05A' )
        Union
        select distinct X . chdcttype , '1' , Y . chdcttypename
        from GenIpdDctTbl X
        join GenIpdDctTypeTbl Y
        on ( X . chdcttype = Y . chdcttype )
        where X . vchcredflg = '1' ) B , GenDctItemTbl C
    where chOp3PFin2 = chDctType
    and chOp3Dct = chDctItem
    and ( chDctCredFlg = '1'
        or ( chDctCredFlg = '0'
            and chDctType = '05A' ) )
    and ( ( chOp1Date < :SDate
            and ( chOp3IDate between ( :SDate || '0000' )
                and ( :SDate || '9999' )
                or ( rtrim ( chOp3IDate ) is not null
                and chOp3DCDate between ( :SDate || '0000' )
                and ( :SDate || '9999' ) ) ) )
        or ( chOp1Date = :SDate
            and chOp3IDate <= ( :SDate || '9999' )
            and rtrim ( chOp3IDate ) is not null ) )
    and rlOp3Sub5 <> 0
    and ( chop3stat not in ( '09' , '10' , '11' , '12' )
        or rtrim ( chop3stat ) is null )
    and ( chop3rep3flg <> 'S'
        or rtrim ( chop3rep3flg ) is null )
    and ( A . chop3proj not in ( 'I' , 'S' , 'D' )
        or rtrim ( A . chop3proj ) is null )
    group by chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp3IDate , 1 , 7 ) , substr ( chOp3DCDate , 1 , 7 ) , chOp3DrgNo , chOp3Dct , chDctItemName , chOp3PFin2 , chDctTypeName , chOp3CUser , chop3spay , vchirbno
) q
WHERE (:PFin2 IS NULL OR :PFin2 NOT IN ('VV','WW','XX','YY','ZZ','UU','TT'))
""";

    private const string C23I03NoncreditOrdSql = """
select chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp4IDate , 1 , 7 ) IDate , substr ( chOp4DCDate , 1 , 7 ) DCDate , chOp4PFin2 , chDctTypeName , chOp4Dct , chDctItemName , chOp4CUser , chOp4OrdNo , sum ( rlOp4Sub5 ) rlOp4Sub5 , sum ( rlOp4Sub3 ) rlOp4Sub3 , A . chop4spay , a . vchirbno
from IpdOrdTbl A , (
    select chdcttype , chdctcredflg , chdcttypename
    From GenIpdDctTypeTbl
    where ( chdctcredflg <> '1'
        and chdcttype <> '05A' )
    minus
    select distinct X . chdcttype , '0' , Y . chdcttypename
    from GenIpdDctTbl X
    join GenIpdDctTypeTbl Y
    on ( X . chdcttype = Y . chdcttype )
    where X . vchcredflg = '1' ) B , GenDctItemTbl C
where chOp4PFin2 = chDctType
and chOp4Dct = chDctItem
and chDctCredFlg <> '1'
and chDctType <> '05A'
and ( chOp4IDate between ( :SDate || '0000' )
    and ( :SDate || '9999' )
    or ( rtrim ( chOp4IDate ) is not null
        and chOp4DCDate between ( :SDate || '0000' )
        and ( :SDate || '9999' ) ) )
and rlOp4Sub5 <> 0
and ( A . chop4proj not in ( 'I' , 'S' , 'D' )
    or rtrim ( A . chop4proj ) is null )
group by chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp4IDate , 1 , 7 ) , substr ( chOp4DCDate , 1 , 7 ) , chOp4OrdNo , chOp4Dct , chDctItemName , chOp4PFin2 , chDctTypeName , chOp4CUser , chop4spay , vchirbno
""";

    private const string C23I04NoncreditDrgSql = """
SELECT q.*
FROM (
    select chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp3IDate , 1 , 7 ) IDate , substr ( chOp3DCDate , 1 , 7 ) DCDate , chOp3PFin2 chOp4PFin2 , chDctTypeName , chOp3Dct chOp4Dct , chDctItemName , chOp3CUser chOp4CUser , chOp3DrgNo chOp4OrdNo , sum ( rlOp3Sub5 ) rlOp4Sub5 , sum ( rlOp3Sub3 ) rlOp4Sub3 , A . chop3spay as chop4spay , a . vchirbno
    from IpdDrgTbl A , (
        select chdcttype , chdctcredflg , chdcttypename
        From GenIpdDctTypeTbl
        where ( chdctcredflg <> '1'
            and chdcttype <> '05A' )
        minus
        select distinct X . chdcttype , '0' , Y . chdcttypename
        from GenIpdDctTbl X
        join GenIpdDctTypeTbl Y
        on ( X . chdcttype = Y . chdcttype )
        where X . vchcredflg = '1' ) B , GenDctItemTbl C
    where chOp3PFin2 = chDctType
    and chOp3Dct = chDctItem
    and chDctCredFlg <> '1'
    and chDctType <> '05A'
    and ( chOp3IDate between ( :SDate || '0000' )
        and ( :SDate || '9999' )
        or ( rtrim ( chOp3IDate ) is not null
            and chOp3DCDate between ( :SDate || '0000' )
            and ( :SDate || '9999' ) ) )
    and rlOp3Sub5 <> 0
    and ( chop3stat not in ( '09' , '10' , '11' , '12' )
        or rtrim ( chop3stat ) is null )
    and ( chop3rep3flg <> 'S'
        or rtrim ( chop3rep3flg ) is null )
    and ( A . chop3proj not in ( 'I' , 'S' , 'D' )
        or rtrim ( A . chop3proj ) is null )
    group by chOp1Date , chOp1Time , chOp1Room , intOp1No , substr ( chOp3IDate , 1 , 7 ) , substr ( chOp3DCDate , 1 , 7 ) , chOp3DrgNo , chOp3Dct , chDctItemName , chOp3PFin2 , chDctTypeName , chOp3CUser , chop3spay , vchirbno
) q
WHERE (:PFin2 IS NULL OR :PFin2 NOT IN ('WW','XX','YY'))
""";

    private const string C23I05DischargeSnapshotSql = """
select  d . chOp1Date , d . chOp1Time , d . chOp1Room , d . intOp1No , substr ( d . chOp3IDate , 1 , 7 ) IDate , case when substr ( d . chOp3IDate , 1 , 7 ) = substr ( d . chOp3DCDate , 1 , 7 )
and d . chop3dcdate > j . vchop2recctime then null else substr ( d . chOp3DCDate , 1 , 7 ) end as DCDate , d . chOp3PFin2 chOp4PFin2 , t . chDctTypeName , d . chOp3Dct chOp4Dct , i . chDctItemName , d . chOp3CUser chOp4CUser , d . chOp3DrgNo chOp4OrdNo , sum ( d . rlOp3Sub5 ) rlOp4Sub5 , sum ( d . rlOp3Sub3 ) rlOp4Sub3 , d . chop3spay as chop4spay , d . vchirbno
from ipddrgtbl d , GenIpdDctTypeTbl t , GenDctItemTbl i , (
    select a . chop1date , a . chop1time , a . chop1room , a . intop1no , max ( a . vchaccdate || substr ( a . vchop2recctime , 1 , 4 ) ) as vchop2recctime , sum ( a . intacccashall )
    from ipdacccasedaytbl a
    where a . vchaccdate = :SDate
    and a . vchaccmrno not in ( 'C36979' , '1000000' )
    and a . vchaccbid <> '預繳'
    group by a . chop1date , a . chop1time , a . chop1room , a . intop1no ) j
Where D . chop1date = J . chop1date
and d . chop1time = j . chop1time
and d . chop1room = j . chop1room
and d . intop1no = j . intop1no
and d . chOp3PFin2 = t . chDctType
and d . chOp3Dct = i . chDctItem
and d . chop3idate <= j . vchop2recctime
and rtrim ( d . chop3idate ) is not null
and ( d . chop3dcdate > j . vchop2recctime
    or rtrim ( d . chop3dcdate ) is null )
and ( d . chop3proj not in ( 'I' , 'D' )
    or rtrim ( d . chop3proj ) is null )
and ( d . chop3rep3flg <> 'S'
    or rtrim ( d . chop3rep3flg ) is null )
and ( d . chop3stat not in ( '09' , '10' , '11' , '12' )
    or rtrim ( d . chop3stat ) is null )
and d . rlop3sub5 <> 0
group by d . chOp1Date , d . chOp1Time , d . chOp1Room , d . intOp1No , substr ( d . chOp3IDate , 1 , 7 ) , case when substr ( d . chOp3IDate , 1 , 7 ) = substr ( d . chOp3DCDate , 1 , 7 )
and d . chop3dcdate > j . vchop2recctime then null else substr ( d . chOp3DCDate , 1 , 7 ) end , d . chOp3PFin2 , t . chDctTypeName , d . chOp3Dct , i . chDctItemName , d . chOp3CUser , d . chOp3DrgNo , d . chop3spay , d . vchirbno
Union All
select  o . chOp1Date , o . chOp1Time , o . chOp1Room , o . intOp1No , substr ( o . chOp4IDate , 1 , 7 ) IDate , case when substr ( o . chOp4IDate , 1 , 7 ) = substr ( o . chOp4DCDate , 1 , 7 )
and o . chop4dcdate > j . vchop2recctime then null else substr ( o . chOp4DCDate , 1 , 7 ) end as DCDate , o . chOp4PFin2 , t . chDctTypeName , o . chOp4Dct , i . chDctItemName , o . chOp4CUser , o . chOp4OrdNo , sum ( o . rlOp4Sub5 ) rlOp4Sub5 , sum ( o . rlOp4Sub3 ) rlOp4Sub3 , o . chop4spay , o . vchirbno
from ipdordtbl o , GenIpdDctTypeTbl t , GenDctItemTbl i , (
    select a . chop1date , a . chop1time , a . chop1room , a . intop1no , max ( a . vchaccdate || substr ( a . vchop2recctime , 1 , 4 ) ) as vchop2recctime , sum ( a . intacccashall )
    from ipdacccasedaytbl a
    where a . vchaccdate = :SDate
    and a . vchaccmrno not in ( 'C36979' , '1000000' )
    and a . vchaccbid <> '預繳'
    group by a . chop1date , a . chop1time , a . chop1room , a . intop1no ) j
Where o . chop1date = J . chop1date
and o . chop1time = j . chop1time
and o . chop1room = j . chop1room
and o . intop1no = j . intop1no
and o . chOp4PFin2 = t . chDctType
and o . chOp4Dct = i . chDctItem
and o . chop4idate <= j . vchop2recctime
and rtrim ( o . chop4idate ) is not null
and ( o . chop4dcdate > j . vchop2recctime
    or rtrim ( o . chop4dcdate ) is null )
and ( o . chop4proj not in ( 'I' , 'D' )
    or rtrim ( o . chop4proj ) is null )
and o . rlop4sub5 <> 0
group by o . chOp1Date , o . chOp1Time , o . chOp1Room , o . intOp1No , substr ( o . chOp4IDate , 1 , 7 ) , case when substr ( o . chOp4IDate , 1 , 7 ) = substr ( o . chOp4DCDate , 1 , 7 )
and o . chop4dcdate > j . vchop2recctime then null else substr ( o . chOp4DCDate , 1 , 7 ) end , o . chOp4PFin2 , t . chDctTypeName , o . chOp4Dct , i . chDctItemName , o . chOp4CUser , o . chOp4OrdNo , o . chop4spay , o . vchirbno
Union All
select  d . chOp1Date , d . chOp1Time , d . chOp1Room , d . intOp1No , substr ( d . chOp3IDate , 1 , 7 ) IDate , '' DCDate , d . chOp3PFin2 chOp4PFin2 , t . chDctTypeName , d . chOp3Dct chOp4Dct , i . chDctItemName , d . chOp3CUser chOp4CUser , d . chOp3DrgNo chOp4OrdNo , sum ( d . rlOp3Sub5 ) * ( - 1 ) rlOp4Sub5 , sum ( d . rlOp3Sub3 ) * ( - 1 ) rlOp4Sub3 , d . chop3spay as chop4spay , d . vchirbno
from ipddrgtbl d , GenIpdDctTypeTbl t , GenDctItemTbl i , (
    select a . chop1date , a . chop1time , a . chop1room , a . intop1no , max ( a . vchaccdate || substr ( a . vchop2recctime , 1 , 4 ) ) as vchop2recctime
    from ipdacccasedaytbl a , (
        select distinct a . chop1date , a . chop1time , a . chop1room , a . intop1no
        from ipdacccasedaytbl a
        where a . vchaccdate = :SDate
        and a . vchaccmrno not in ( 'C36979' , '1000000' )
        and a . vchaccbid <> '預繳' ) j
    Where A . chop1date = J . chop1date
    and a . chop1time = j . chop1time
    and a . chop1room = j . chop1room
    and a . intop1no = j . intop1no
    and a . vchaccdate < :SDate
    and a . vchaccbid <> '預繳'
    group by a . chop1date , a . chop1time , a . chop1room , a . intop1no ) j
Where D . chop1date = J . chop1date
and d . chop1time = j . chop1time
and d . chop1room = j . chop1room
and d . intop1no = j . intop1no
and d . chOp3PFin2 = t . chDctType
and d . chOp3Dct = i . chDctItem
and d . chop3idate <= j . vchop2recctime
and rtrim ( d . chop3idate ) is not null
and ( d . chop3dcdate > j . vchop2recctime
    or rtrim ( d . chop3dcdate ) is null )
and ( d . chop3proj not in ( 'I' , 'D' )
    or rtrim ( d . chop3proj ) is null )
and ( d . chop3rep3flg <> 'S'
    or rtrim ( d . chop3rep3flg ) is null )
and ( d . chop3stat not in ( '09' , '10' , '11' , '12' )
    or rtrim ( d . chop3stat ) is null )
and d . rlop3sub5 <> 0
group by d . chOp1Date , d . chOp1Time , d . chOp1Room , d . intOp1No , substr ( d . chOp3IDate , 1 , 7 ) , substr ( d . chOp3DCDate , 1 , 7 ) , d . chOp3PFin2 , t . chDctTypeName , d . chOp3Dct , i . chDctItemName , d . chOp3CUser , d . chOp3DrgNo , d . chop3spay , d . vchirbno
Union All
select  o . chOp1Date , o . chOp1Time , o . chOp1Room , o . intOp1No , substr ( o . chOp4IDate , 1 , 7 ) IDate , '' DCDate , o . chOp4PFin2 , t . chDctTypeName , o . chOp4Dct , i . chDctItemName , o . chOp4CUser , o . chOp4OrdNo , sum ( o . rlOp4Sub5 ) * ( - 1 ) rlOp4Sub5 , sum ( o . rlOp4Sub3 ) * ( - 1 ) rlOp4Sub3 , o . chop4spay , o . vchirbno
from ipdordtbl o , GenIpdDctTypeTbl t , GenDctItemTbl i , (
    select a . chop1date , a . chop1time , a . chop1room , a . intop1no , max ( a . vchaccdate || substr ( a . vchop2recctime , 1 , 4 ) ) as vchop2recctime
    from ipdacccasedaytbl a , (
        select distinct a . chop1date , a . chop1time , a . chop1room , a . intop1no
        from ipdacccasedaytbl a
        where a . vchaccdate = :SDate
        and a . vchaccmrno not in ( 'C36979' , '1000000' )
        and a . vchaccbid <> '預繳' ) j
    Where A . chop1date = J . chop1date
    and a . chop1time = j . chop1time
    and a . chop1room = j . chop1room
    and a . intop1no = j . intop1no
    and a . vchaccdate < :SDate
    and a . vchaccbid <> '預繳'
    group by a . chop1date , a . chop1time , a . chop1room , a . intop1no ) j
Where o . chop1date = J . chop1date
and o . chop1time = j . chop1time
and o . chop1room = j . chop1room
and o . intop1no = j . intop1no
and o . chOp4PFin2 = t . chDctType
and o . chOp4Dct = i . chDctItem
and o . chop4idate <= j . vchop2recctime
and rtrim ( o . chop4idate ) is not null
and ( o . chop4dcdate > j . vchop2recctime
    or rtrim ( o . chop4dcdate ) is null )
and ( o . chop4proj not in ( 'I' , 'D' )
    or rtrim ( o . chop4proj ) is null )
and o . rlop4sub5 <> 0
group by o . chOp1Date , o . chOp1Time , o . chOp1Room , o . intOp1No , substr ( o . chOp4IDate , 1 , 7 ) , substr ( o . chOp4DCDate , 1 , 7 ) , o . chOp4PFin2 , t . chDctTypeName , o . chOp4Dct , i . chDctItemName , o . chOp4CUser , o . chOp4OrdNo , o . chop4spay , o . vchirbno
""";

    private const string C23OEncounterDateSql = """
with tmp_a as
 (
 select
 case when A.chop1room='EEEE' and A.chop4pfin2<>'118' then 'ZZ'
 when A.chop4ordno='00-039' then 'XX'
 when A.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
 when A.chop4ordno in ('102-23','102-24') then 'WW'
 when A.chop4ordno in ('S001A','S001B') then '42'
 when A.chop4spay='6' then 'VV'
 when A.chop4spay='7' then 'TT'
 when A.chop4pfin2='00' then 'UU'
 else rtrim(A.chop4pfin2) end as chop4pfin2
 ,case when A.chop1room='EEEE' and A.chop4pfin2<>'118' then '聯盟代檢'
 when A.chop4ordno='00-039' then '老人照護鑑定'
 when A.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then '殘障鑑定'
 when A.chop4ordno in ('102-23','102-24') then '北縣65歲以上老人健檢'
 when A.chop4ordno in ('S001A','S001B') then '移植用組織捐贈專戶'
 when A.chop4spay='6' then '維康記帳'
 when A.chop4spay='7' then '研究經費'
 when A.chop4pfin2='00' then '其他'
 else rtrim(E.chdcttypename) end as chdcttypename
 ,A.chop1date,B.chop1mrno,B.chop1pname,B.chop1sec,C.chsecname,B.chop1drid,B.chop1drname,A.chop4dct,D.chdctitemname
 ,sum(rlop4sub5) as rlop4sub5
 from opdordtbl A
 join opdbasictbl B
 on (A.chop1date=B.chop1date and A.chop1time=B.chop1time and A.chop1room=B.chop1room and A.intop1no=B.intop1no)
 join gensectiontbl C
 on (B.chop1sec=C.chsecno)
 join gendctitemtbl D
 on (A.chop4dct=D.chdctitem)
 join (select chdcttype,chdcttypename from gendcttypetbl union select chdcttype,chdcttypename from genipddcttypetbl) E
 on (A.chop4pfin2=E.chdcttype)
 WHERE A.chOp1Date BETWEEN :StartDate AND :EndDate
 and lnnvl(A.chop4stat='DC')
 and rtrim(A.chop4idate) is not null
 and A.rlop4sub5<>0
 and (A.chop4proj not in ('I','S','D') or rtrim(A.chop4proj) is null)
 and B.chop1mrno not in ('C36979','1000000')
 Group By
 case when A.chop1room='EEEE' and A.chop4pfin2<>'118' then 'ZZ'
 when A.chop4ordno='00-039' then 'XX'
 when A.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
 when A.chop4ordno in ('102-23','102-24') then 'WW'
 when A.chop4ordno in ('S001A','S001B') then '42'
 when A.chop4spay='6' then 'VV'
 when A.chop4spay='7' then 'TT'
 when A.chop4pfin2='00' then 'UU'
 else rtrim(A.chop4pfin2) end
 ,case when A.chop1room='EEEE' and A.chop4pfin2<>'118' then '聯盟代檢'
 when A.chop4ordno='00-039' then '老人照護鑑定'
 when A.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then '殘障鑑定'
 when A.chop4ordno in ('102-23','102-24') then '北縣65歲以上老人健檢'
 when A.chop4ordno in ('S001A','S001B') then '移植用組織捐贈專戶'
 when A.chop4spay='6' then '維康記帳'
 when A.chop4spay='7' then '研究經費'
 when A.chop4pfin2='00' then '其他'
 else rtrim(E.chdcttypename) end
 ,A.chop1date,B.chop1mrno,B.chop1pname,B.chop1sec,C.chsecname,B.chop1drid,B.chop1drname,A.chop4dct,D.chdctitemname
 Union All
 select
 case when A.chop1room='EEEE' and A.chop3pfin2<>'118' then 'ZZ'
 when A.chop3drgno='00-039' then 'XX'
 when A.chop3drgno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
 when A.chop3drgno in ('102-23','102-24') then 'WW'
 when A.chop3drgno in ('S001A','S001B') then '42'
 when A.chop3spay='6' then 'VV'
 when A.chop3spay='7' then 'TT'
 when A.chop3pfin2='00' then 'UU'
 else rtrim(A.chop3pfin2) end
 ,case when A.chop1room='EEEE' and A.chop3pfin2<>'118' then '聯盟代檢'
 when A.chop3drgno='00-039' then '老人照護鑑定'
 when A.chop3drgno in ('64-001','64-002','64-003','64-004','64-005') then '殘障鑑定'
 when A.chop3drgno in ('102-23','102-24') then '北縣65歲以上老人健檢'
 when A.chop3drgno in ('S001A','S001B') then '移植用組織捐贈專戶'
 when A.chop3spay='6' then '維康記帳'
 when A.chop3spay='7' then '研究經費'
 when A.chop3pfin2='00' then '其他'
 else rtrim(E.chdcttypename) end
 ,A.chop1date,B.chop1mrno,B.chop1pname,B.chop1sec,C.chsecname,B.chop1drid,B.chop1drname,A.chop3dct,D.chdctitemname
 ,sum(rlop3sub5)
 from opddrgtbl A
 join opdbasictbl B
 on (A.chop1date=B.chop1date and A.chop1time=B.chop1time and A.chop1room=B.chop1room and A.intop1no=B.intop1no)
 join gensectiontbl C
 on (B.chop1sec=C.chsecno)
 join gendctitemtbl D
 on (A.chop3dct=D.chdctitem)
 join (select chdcttype,chdcttypename from gendcttypetbl union select chdcttype,chdcttypename from genipddcttypetbl) E
 on (A.chop3pfin2=E.chdcttype)
 WHERE A.chOp1Date BETWEEN :StartDate AND :EndDate
 and lnnvl(A.chop3stat='DC')
 and rtrim(A.chop3idate) is not null
 and A.rlop3sub5<>0
 and (A.chop3proj not in ('I','S','D') or rtrim(A.chop3proj) is null)
 and (A.chop3stat not in ('09','10','11','12') or rtrim(A.chop3stat) is null)
 and lnnvl(A.chop3rep3flg='S')
 and B.chop1mrno not in ('C36979','1000000')
 Group By
 case when A.chop1room='EEEE' and A.chop3pfin2<>'118' then 'ZZ'
 when A.chop3drgno='00-039' then 'XX'
 when A.chop3drgno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
 when A.chop3drgno in ('102-23','102-24') then 'WW'
 when A.chop3drgno in ('S001A','S001B') then '42'
 when A.chop3spay='6' then 'VV'
 when A.chop3spay='7' then 'TT'
 when A.chop3pfin2='00' then 'UU'
 else rtrim(A.chop3pfin2) end
 ,case when A.chop1room='EEEE' and A.chop3pfin2<>'118' then '聯盟代檢'
 when A.chop3drgno='00-039' then '老人照護鑑定'
 when A.chop3drgno in ('64-001','64-002','64-003','64-004','64-005') then '殘障鑑定'
 when A.chop3drgno in ('102-23','102-24') then '北縣65歲以上老人健檢'
 when A.chop3drgno in ('S001A','S001B') then '移植用組織捐贈專戶'
 when A.chop3spay='6' then '維康記帳'
 when A.chop3spay='7' then '研究經費'
 when A.chop3pfin2='00' then '其他'
 else rtrim(E.chdcttypename) end
 ,A.chop1date,B.chop1mrno,B.chop1pname,B.chop1sec,C.chsecname,B.chop1drid,B.chop1drname,A.chop3dct,D.chdctitemname
 )
 select chop1date,chop1mrno,chsecname as chop1psecnm,chop1pname,chop1drname as chop1dridnm,chop4pfin2 as chop1pfin2,chdcttypename as chop1pfin2nm,chop4dct as chop1dct,chdctitemname as chop1dctnm,0 as rlop1sub3,sum(rlop4sub5) as rlop1sub2,sum(rlop4sub5) as rlop1subamt,null as chcuser
 From tmp_a
 WHERE (:Contract IS NULL OR chOp4PFin2 = :Contract)
 group by chop1date,chop1mrno,chsecname,chop1pname,chop1drname,chop4pfin2,chdcttypename,chop4dct,chdctitemname
""";

    private const string C23IEncounterDateSql = """
with tmp_a as
 (
 select
 case when A.chop1room='EEEE' and A.chop4pfin2<>'118' then 'ZZ'
 when A.chop4ordno='00-039' then 'XX'
 when A.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
 when A.chop4ordno in ('102-23','102-24') then 'WW'
 when A.chop4ordno in ('S001A','S001B') then '42'
 when A.chop4spay='6' then 'VV'
 when A.chop4spay='7' then (case when A.vchirbno is not null then A.vchirbno else 'TT' end)
 when A.chop4pfin2='00' then 'UU'
 else rtrim(A.chop4pfin2) end as chop4pfin2
 ,case when A.chop1room='EEEE' and A.chop4pfin2<>'118' then '聯盟代檢'
 when A.chop4ordno='00-039' then '老人照護鑑定'
 when A.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then '殘障鑑定'
 when A.chop4ordno in ('102-23','102-24') then '北縣65歲以上老人健檢'
 when A.chop4ordno in ('S001A','S001B') then '移植用組織捐贈專戶'
 when A.chop4spay='6' then '維康記帳'
 when A.chop4spay='7' then (case when A.vchirbno is not null then E.chdcttypename else '研究經費' end)
 when A.chop4pfin2='00' then '其他'
 else rtrim(E.chdcttypename) end as chdcttypename
 ,A.chop1date,B.chop1mrno,B.chop1pname,B.chop1sec,C.chsecname,B.chop1drid,B.chop1drname,A.chop4dct,D.chdctitemname
 ,sum(rlop4sub5) as rlop4sub5
 from ipdordtbl A
 join ipdbasictbl B
 on (A.chop1date=B.chop1date and A.chop1time=B.chop1time and A.chop1room=B.chop1room and A.intop1no=B.intop1no)
 join gensectiontbl C
 on (B.chop1sec=C.chsecno)
 join gendctitemtbl D
 on (A.chop4dct=D.chdctitem)
 left outer join (select chdcttype,chdcttypename from gendcttypetbl union select chdcttype,chdcttypename from genipddcttypetbl) E
 on (case when A.vchirbno is not null then rpad(A.vchirbno,3,' ') else A.chop4pfin2 end=E.chdcttype)
 WHERE A.chOp1Date BETWEEN :StartDate AND :EndDate
 and lnnvl(A.chop4stat='DC')
 and rtrim(A.chop4idate) is not null
 and A.rlop4sub5<>0
 and (A.chop4proj not in ('I','S','D') or rtrim(A.chop4proj) is null)
 and B.chop1mrno not in ('C36979','1000000')
 Group By
 case when A.chop1room='EEEE' and A.chop4pfin2<>'118' then 'ZZ'
 when A.chop4ordno='00-039' then 'XX'
 when A.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
 when A.chop4ordno in ('102-23','102-24') then 'WW'
 when A.chop4ordno in ('S001A','S001B') then '42'
 when A.chop4spay='6' then 'VV'
 when A.chop4spay='7' then (case when A.vchirbno is not null then A.vchirbno else 'TT' end)
 when A.chop4pfin2='00' then 'UU'
 else rtrim(A.chop4pfin2) end
 ,case when A.chop1room='EEEE' and A.chop4pfin2<>'118' then '聯盟代檢'
 when A.chop4ordno='00-039' then '老人照護鑑定'
 when A.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then '殘障鑑定'
 when A.chop4ordno in ('102-23','102-24') then '北縣65歲以上老人健檢'
 when A.chop4ordno in ('S001A','S001B') then '移植用組織捐贈專戶'
 when A.chop4spay='6' then '維康記帳'
 when A.chop4spay='7' then (case when A.vchirbno is not null then E.chdcttypename else '研究經費' end)
 when A.chop4pfin2='00' then '其他'
 else rtrim(E.chdcttypename) end
 ,A.chop1date,B.chop1mrno,B.chop1pname,B.chop1sec,C.chsecname,B.chop1drid,B.chop1drname,A.chop4dct,D.chdctitemname
 Union All
 select
 case when A.chop1room='EEEE' and A.chop3pfin2<>'118' then 'ZZ'
 when A.chop3drgno='00-039' then 'XX'
 when A.chop3drgno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
 when A.chop3drgno in ('102-23','102-24') then 'WW'
 when A.chop3drgno in ('S001A','S001B') then '42'
 when A.chop3spay='6' then 'VV'
 when A.chop3spay='7' then (case when A.vchirbno is not null then A.vchirbno else 'TT' end)
 when A.chop3pfin2='00' then 'UU'
 else rtrim(A.chop3pfin2) end
 ,case when A.chop1room='EEEE' and A.chop3pfin2<>'118' then '聯盟代檢'
 when A.chop3drgno='00-039' then '老人照護鑑定'
 when A.chop3drgno in ('64-001','64-002','64-003','64-004','64-005') then '殘障鑑定'
 when A.chop3drgno in ('102-23','102-24') then '北縣65歲以上老人健檢'
 when A.chop3drgno in ('S001A','S001B') then '移植用組織捐贈專戶'
 when A.chop3spay='6' then '維康記帳'
 when A.chop3spay='7' then (case when A.vchirbno is not null then E.chdcttypename else '研究經費' end)
 when A.chop3pfin2='00' then '其他'
 else rtrim(E.chdcttypename) end
 ,A.chop1date,B.chop1mrno,B.chop1pname,B.chop1sec,C.chsecname,B.chop1drid,B.chop1drname,A.chop3dct,D.chdctitemname
 ,sum(rlop3sub5)
 from ipddrgtbl A
 join ipdbasictbl B
 on (A.chop1date=B.chop1date and A.chop1time=B.chop1time and A.chop1room=B.chop1room and A.intop1no=B.intop1no)
 join gensectiontbl C
 on (B.chop1sec=C.chsecno)
 join gendctitemtbl D
 on (A.chop3dct=D.chdctitem)
 join (select chdcttype,chdcttypename from gendcttypetbl union select chdcttype,chdcttypename from genipddcttypetbl) E
 on (A.chop3pfin2=E.chdcttype)
 WHERE A.chOp1Date BETWEEN :StartDate AND :EndDate
 and lnnvl(A.chop3stat='DC')
 and rtrim(A.chop3idate) is not null
 and A.rlop3sub5<>0
 and (A.chop3proj not in ('I','S','D') or rtrim(A.chop3proj) is null)
 and (A.chop3stat not in ('09','10','11','12') or rtrim(A.chop3stat) is null)
 and lnnvl(A.chop3rep3flg='S')
 and B.chop1mrno not in ('C36979','1000000')
 Group By
 case when A.chop1room='EEEE' and A.chop3pfin2<>'118' then 'ZZ'
 when A.chop3drgno='00-039' then 'XX'
 when A.chop3drgno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
 when A.chop3drgno in ('102-23','102-24') then 'WW'
 when A.chop3drgno in ('S001A','S001B') then '42'
 when A.chop3spay='6' then 'VV'
 when A.chop3spay='7' then (case when A.vchirbno is not null then A.vchirbno else 'TT' end)
 when A.chop3pfin2='00' then 'UU'
 else rtrim(A.chop3pfin2) end
 ,case when A.chop1room='EEEE' and A.chop3pfin2<>'118' then '聯盟代檢'
 when A.chop3drgno='00-039' then '老人照護鑑定'
 when A.chop3drgno in ('64-001','64-002','64-003','64-004','64-005') then '殘障鑑定'
 when A.chop3drgno in ('102-23','102-24') then '北縣65歲以上老人健檢'
 when A.chop3drgno in ('S001A','S001B') then '移植用組織捐贈專戶'
 when A.chop3spay='6' then '維康記帳'
 when A.chop3spay='7' then (case when A.vchirbno is not null then E.chdcttypename else '研究經費' end)
 when A.chop3pfin2='00' then '其他'
 else rtrim(E.chdcttypename) end
 ,A.chop1date,B.chop1mrno,B.chop1pname,B.chop1sec,C.chsecname,B.chop1drid,B.chop1drname,A.chop3dct,D.chdctitemname
 )
 select chop1date,chop1mrno,chsecname as chop1psecnm,chop1pname,chop1drname as chop1dridnm,chop4pfin2 as chop1pfin2,chdcttypename as chop1pfin2nm,chop4dct as chop1dct,chdctitemname as chop1dctnm,0 as rlop1sub3,sum(rlop4sub5) as rlop1sub2,sum(rlop4sub5) as rlop1subamt,null as chcuser
 From tmp_a
 WHERE (:Contract IS NULL OR chOp4PFin2 = :Contract)
 group by chop1date,chop1mrno,chsecname,chop1pname,chop1drname,chop4pfin2,chdcttypename,chop4dct,chdctitemname
""";

    private const string C23OGeneralBalanceCommand1 = """
DELETE FROM OPDCONTRACTMRNOTBL c
 where chidate= :AccountingDate
""";

    private const string C23OGeneralBalanceCommand2 = """
INSERT INTO OPDCONTRACTMRNOTBL (chFin2, chIDate, chType, chDate, chTime, chRoom, intNo, chStat, chMrNo, intSelfAmt, intClaimAmt)
SELECT rtrim(chop3pfin2),chop3idate,chtype,chop1date,chop1time,rtrim(chop1room),intop1no,chstat,rtrim(chop1mrno)
 ,sum(decode(chop3pfin1,'01',rlop3sub5,0)) as intselfamt,sum(decode(chop3pfin1,'30',rlop3sub5,0)) AS intClaimAmt
 From
 (
 select
 case when b.chop1room='EEEE' and d.chop3pfin2<>'118' then 'ZZ'
     when d.chop3spay='6' then 'VV'
     when d.chop3spay='7' then 'TT'
     when d.chop3pfin2='00' then 'UU'
     Else D.chop3pfin2
 End
 as chop3pfin2
 ,:AccountingDate as chop3idate,'D' as chtype,b.chop1date,b.chop1time,b.chop1room,b.intop1no,'0' as chstat,b.chop1mrno,decode(d.chop3pfin1,'35','30',d.chop3pfin1) as chop3pfin1,d.rlop3sub5
 from Opddrgtbl d,Opdbasictbl b
 Where D.chop1date = B.chop1date
 and d.chop1time=b.chop1time
 and d.chop1room=b.chop1room
 and d.intop1no=b.intop1no
 and
 (
 (d.chop1date< :AccountingDate
 and d.chop3idate BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and (d.chop3dcdate NOT BETWEEN :AccountingDayStart AND :AccountingDayEnd or rtrim(d.chop3dcdate) is null))
 or
 (d.chop1date= :AccountingDate
 and d.chop3idate<= :AccountingDayEnd and rtrim(d.chop3idate) is not null
 and (substr(d.chop3dcdate,1,7)> :AccountingDate or rtrim(d.chop3dcdate) is null))
 )
 and d.rlOp3Sub5<>0
 and (d.chop3proj not in ('I','S','D') or rtrim(d.chop3proj) is null)
 and (d.chop3rep3flg<>'S' or rtrim(d.chop3rep3flg) is null)
 and (d.chop3stat not in ('09','10','11','12') or rtrim(d.chop3stat) is null)
 and b.chop1mrno not in ('C36979','1000000')
 Union All
 select
 case when b.chop1room='EEEE' and o.chop4pfin2<>'118' then 'ZZ'
     when o.chop4ordno='00-039' then 'XX'
     when o.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
     when o.chop4ordno in ('102-23','102-24') then 'WW'
     when o.chop4ordno in ('S001A','S001B') then '42'
     when o.chop4spay='6' then 'VV'
     when o.chop4spay='7' then 'TT'
     when o.chop4pfin2='00' then 'UU'
     Else o.chop4pfin2
 End
 ,:AccountingDate,decode(substr(o.chop4dct,1,2),'64','C','D'),b.chop1date,b.chop1time,b.chop1room,b.intop1no,'0',b.chop1mrno,decode(o.chop4pfin1,'35','30',o.chop4pfin1),decode(substr(o.chop4dct,1,2),'64',-o.rlop4sub1,o.rlop4sub5)
 from Opdordtbl o,Opdbasictbl b
 Where o.chop1date = B.chop1date
 and o.chop1time=b.chop1time
 and o.chop1room=b.chop1room
 and o.intop1no=b.intop1no
 and
 (
 (o.chop1date< :AccountingDate
 and o.chop4idate BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and (o.chop4dcdate NOT BETWEEN :AccountingDayStart AND :AccountingDayEnd or rtrim(o.chop4dcdate) is null))
 or
 (o.chop1date= :AccountingDate
 and o.chop4idate<= :AccountingDayEnd and rtrim(o.chop4idate) is not null
 and (substr(o.chop4dcdate,1,7)> :AccountingDate or rtrim(o.chop4dcdate) is null))
 )
 and (o.rlOp4Sub5<>0 or o.chop4dct='64')
 and (o.chop4proj not in ('I','S','D') or rtrim(o.chop4proj) is null)
 and b.chop1mrno not in ('C36979','1000000')
 Union All
 select
 case when b.chop1room='EEEE' and d.chop3pfin2<>'118' then 'ZZ'
     when d.chop3spay='6' then 'VV'
     when d.chop3spay='7' then 'TT'
     when d.chop3pfin2='00' then 'UU'
     Else D.chop3pfin2
 End
 ,:AccountingDate,'D',b.chop1date,b.chop1time,b.chop1room,b.intop1no,'0',b.chop1mrno,decode(d.chop3pfin1,'35','30',d.chop3pfin1),-d.rlop3sub5
 from Opddrgtbl d,Opdbasictbl b
 Where D.chop1date = B.chop1date
 and d.chop1time=b.chop1time
 and d.chop1room=b.chop1room
 and d.intop1no=b.intop1no
 and
 (
 (d.chop1date< :AccountingDate
 and d.chop3dcdate BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and d.chop3idate NOT BETWEEN :AccountingDayStart AND :AccountingDayEnd)
 and rtrim(d.chop3idate) is not null
 )
 and d.rlOp3Sub5<>0
 and (d.chop3proj not in ('I','S','D') or rtrim(d.chop3proj) is null)
 and (d.chop3rep3flg<>'S' or rtrim(d.chop3rep3flg) is null)
 and (d.chop3stat not in ('09','10','11','12') or rtrim(d.chop3stat) is null)
 and b.chop1mrno not in ('C36979','1000000')
 Union All
 select
 case when b.chop1room='EEEE' and o.chop4pfin2<>'118' then 'ZZ'
     when o.chop4ordno='00-039' then 'XX'
     when o.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
     when o.chop4ordno in ('102-23','102-24') then 'WW'
     when o.chop4ordno in ('S001A','S001B') then '42'
     when o.chop4spay='6' then 'VV'
     when o.chop4spay='7' then 'TT'
     when o.chop4pfin2='00' then 'UU'
     Else o.chop4pfin2
 End
 ,:AccountingDate,decode(substr(o.chop4dct,1,2),'64','C','D'),b.chop1date,b.chop1time,b.chop1room,b.intop1no,'0',b.chop1mrno,decode(o.chop4pfin1,'35','30',o.chop4pfin1),decode(substr(o.chop4dct,1,2),'64',o.rlop4sub1,-o.rlop4sub5)
 from Opdordtbl o,Opdbasictbl b
 Where o.chop1date = B.chop1date
 and o.chop1time=b.chop1time
 and o.chop1room=b.chop1room
 and o.intop1no=b.intop1no
 and
 (
 (o.chop1date< :AccountingDate
 and o.chop4dcdate BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and o.chop4idate NOT BETWEEN :AccountingDayStart AND :AccountingDayEnd)
 and rtrim(o.chop4idate) is not null
 )
 and (o.rlOp4Sub5<>0 or o.chop4dct='64')
 and (o.chop4proj not in ('I','S','D') or rtrim(o.chop4proj) is null)
 and b.chop1mrno not in ('C36979','1000000')
 )
 group by rtrim(chop3pfin2),chop3idate,chtype,chop1date,chop1time,rtrim(chop1room),intop1no,chstat,rtrim(chop1mrno)
 having sum(rlop3sub5)<>0
""";

    private static readonly IReadOnlyList<string> C23OGeneralBalanceCommands =
        [C23OGeneralBalanceCommand1, C23OGeneralBalanceCommand2];

    private const string C23IGeneralBalanceCommand1 = """
DELETE FROM IPDCONTRACTMRNOTBL c
 where chidate= :AccountingDate
""";

    private const string C23IGeneralBalanceCommand2 = """
INSERT INTO IPDCONTRACTMRNOTBL (chFin2, chIDate, chType, chDate, chTime, chRoom, intNo, chStat, chMrNo, intSelfAmt, intClaimAmt)
SELECT rtrim(chop3pfin2),chop3idate,chtype,chop1date,chop1time,rtrim(chop1room),intop1no,chstat,rtrim(chop1mrno)
 ,sum(decode(chop3pfin1,'01',rlop3sub5,0)) as intselfamt,sum(decode(chop3pfin1,'30',rlop3sub5,0)) AS intClaimAmt
 From
 (
 select
 case when b.chop1room='EEEE' and d.chop3pfin2<>'118' then 'ZZ'
     when d.chop3spay='6' then 'VV'
     when d.chop3spay='7' and t.chDctType is not null then t.chDctType
     when d.chop3spay='7' then 'TT'
     when d.chop3pfin2='00' then 'UU'
     Else D.chop3pfin2
 End
 as chop3pfin2
 ,:AccountingDate as chop3idate,'D' as chtype,b.chop1date,b.chop1time,b.chop1room,b.intop1no,'0' as chstat,b.chop1mrno,decode(d.chop3pfin1,'35','30',d.chop3pfin1) as chop3pfin1,d.rlop3sub5
 from Ipddrgtbl d
 join Ipdbasictbl b
 on (D.chop1date = B.chop1date and d.chop1time=b.chop1time and d.chop1room=b.chop1room and d.intop1no=b.intop1no)
 left outer join (select chDctType from GenDctTypeTbl where chDctCredFlg='1') t
 on (d.vchirbno=rtrim(t.chDctType))
 where 1=1
 and d.chop3idate BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and (d.chop3dcdate NOT BETWEEN :AccountingDayStart AND :AccountingDayEnd or rtrim(d.chop3dcdate) is null)
 and d.rlOp3Sub5<>0
 and (d.chop3proj not in ('I','S','D') or rtrim(d.chop3proj) is null)
 and (d.chop3rep3flg<>'S' or rtrim(d.chop3rep3flg) is null)
 and (d.chop3stat not in ('09','10','11','12') or rtrim(d.chop3stat) is null)
 and b.chop1mrno not in ('C36979','1000000')
 Union All
 select
 case when b.chop1room='EEEE' and o.chop4pfin2<>'118' then 'ZZ'
     when o.chop4ordno='00-039' then 'XX'
     when o.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
     when o.chop4ordno in ('102-23','102-24') then 'WW'
     when o.chop4ordno in ('S001A','S001B') then '42'
     when o.chop4spay='6' then 'VV'
     when o.chop4spay='7' and t.chDctType is not null then t.chDctType
     when o.chop4spay='7' then 'TT'
     when o.chop4pfin2='00' then 'UU'
     Else o.chop4pfin2
 End
 ,:AccountingDate,decode(substr(o.chop4dct,1,2),'64','C','D'),b.chop1date,b.chop1time,b.chop1room,b.intop1no,'0',b.chop1mrno,decode(o.chop4pfin1,'35','30',o.chop4pfin1),decode(substr(o.chop4dct,1,2),'64',-o.rlop4sub1,o.rlop4sub5)
 from Ipdordtbl o
 join Ipdbasictbl b
 on (o.chop1date = B.chop1date and o.chop1time=b.chop1time and o.chop1room=b.chop1room and o.intop1no=b.intop1no)
 left outer join (select chDctType from GenDctTypeTbl where chDctCredFlg='1') t
 on (o.vchirbno=rtrim(t.chDctType))
 where 1=1
 and o.chop4idate BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and (o.chop4dcdate NOT BETWEEN :AccountingDayStart AND :AccountingDayEnd or rtrim(o.chop4dcdate) is null)
 and (o.rlOp4Sub5<>0 or o.chop4dct='64')
 and (o.chop4proj not in ('I','S','D') or rtrim(o.chop4proj) is null)
 and b.chop1mrno not in ('C36979','1000000')
 Union All
 select
 case when b.chop1room='EEEE' and d.chop3pfin2<>'118' then 'ZZ'
     when d.chop3spay='6' then 'VV'
     when d.chop3spay='7' and t.chDctType is not null then t.chDctType
     when d.chop3spay='7' then 'TT'
     when d.chop3pfin2='00' then 'UU'
     Else D.chop3pfin2
 End
 ,:AccountingDate,'D',b.chop1date,b.chop1time,b.chop1room,b.intop1no,'0',b.chop1mrno,decode(d.chop3pfin1,'35','30',d.chop3pfin1),-d.rlop3sub5
 from Ipddrgtbl d
 join Ipdbasictbl b
 on (D.chop1date = B.chop1date and d.chop1time=b.chop1time and d.chop1room=b.chop1room and d.intop1no=b.intop1no)
 left outer join (select chDctType from GenDctTypeTbl where chDctCredFlg='1') t
 on (d.vchirbno=rtrim(t.chDctType))
 where 1=1
 and d.chop3dcdate BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and d.chop3idate NOT BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and rtrim(d.chop3idate) is not null
 and d.rlOp3Sub5<>0
 and (d.chop3proj not in ('I','S','D') or rtrim(d.chop3proj) is null)
 and (d.chop3rep3flg<>'S' or rtrim(d.chop3rep3flg) is null)
 and (d.chop3stat not in ('09','10','11','12') or rtrim(d.chop3stat) is null)
 and b.chop1mrno not in ('C36979','1000000')
 Union All
 select
 case when b.chop1room='EEEE' and o.chop4pfin2<>'118' then 'ZZ'
     when o.chop4ordno='00-039' then 'XX'
     when o.chop4ordno in ('64-001','64-002','64-003','64-004','64-005') then 'YY'
     when o.chop4ordno in ('102-23','102-24') then 'WW'
     when o.chop4ordno in ('S001A','S001B') then '42'
     when o.chop4spay='6' then 'VV'
     when o.chop4spay='7' and t.chDctType is not null then t.chDctType
     when o.chop4spay='7' then 'TT'
     when o.chop4pfin2='00' then 'UU'
     Else o.chop4pfin2
 End
 ,:AccountingDate,decode(substr(o.chop4dct,1,2),'64','C','D'),b.chop1date,b.chop1time,b.chop1room,b.intop1no,'0',b.chop1mrno,decode(o.chop4pfin1,'35','30',o.chop4pfin1),decode(substr(o.chop4dct,1,2),'64',o.rlop4sub1,-o.rlop4sub5)
 from Ipdordtbl o
 join Ipdbasictbl b
 on (o.chop1date = B.chop1date and o.chop1time=b.chop1time and o.chop1room=b.chop1room and o.intop1no=b.intop1no)
 left outer join (select chDctType from GenDctTypeTbl where chDctCredFlg='1') t
 on (o.vchirbno=rtrim(t.chDctType))
 where 1=1
 and o.chop4dcdate BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and o.chop4idate NOT BETWEEN :AccountingDayStart AND :AccountingDayEnd
 and rtrim(o.chop4idate) is not null
 and (o.rlOp4Sub5<>0 or o.chop4dct='64')
 and (o.chop4proj not in ('I','S','D') or rtrim(o.chop4proj) is null)
 and b.chop1mrno not in ('C36979','1000000')
 )
 group by rtrim(chop3pfin2),chop3idate,chtype,chop1date,chop1time,rtrim(chop1room),intop1no,chstat,rtrim(chop1mrno)
 having sum(rlop3sub5)<>0
""";

    private static readonly IReadOnlyList<string> C23IGeneralBalanceCommands =
        [C23IGeneralBalanceCommand1, C23IGeneralBalanceCommand2];

    private const string C23OBalance42RebuildCommand1 = """
DELETE FROM GenContract42MrNoTbl
WHERE chIDate = :SDate
  AND chRoomType IN ('E','R')
  AND chType = 'D'
""";

    private const string C23OBalance42RebuildCommand2 = """
INSERT INTO GenContract42MrNoTbl
(
    chIDate, chRoomType, chType,
    chDate, chTime, chRoom, intNo,
    chMrNo, chPName, chFin1, intAmt
)
SELECT
    x.chIDate,
    CASE
        WHEN x.chOp1Time = '0' THEN 'I'
        WHEN RTRIM(x.chOp1Room) = '0000' THEN 'E'
        ELSE 'R'
    END AS chRoomType,
    x.chType,
    x.chOp1Date,
    x.chOp1Time,
    RTRIM(x.chOp1Room),
    x.intOp1No,
    RTRIM(x.chOp1MrNo),
    RTRIM(x.chOp1PName),
    x.chOp4PFin1,
    SUM(x.rlOp4Sub1 + x.rlOp4Sub6) AS intAmt
FROM
(
    
    SELECT
        :SDate AS chIDate, 'D' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(o.chOp4PFin1, '35', '30', o.chOp4PFin1) AS chOp4PFin1,
        o.rlOp4Sub1, o.rlOp4Sub6
    FROM OpdOrdTbl o, OpdBasicTbl b
    WHERE o.chOp1Date = b.chOp1Date
      AND o.chOp1Time = b.chOp1Time
      AND o.chOp1Room = b.chOp1Room
      AND o.intOp1No = b.intOp1No
      AND
      (
          (o.chOp1Date < :SDate
           AND o.chOp4IDate LIKE :SDate || '%'
           AND (o.chOp4DCDate NOT LIKE :SDate || '%' OR RTRIM(o.chOp4DCDate) IS NULL))
          OR
          (o.chOp1Date = :SDate
           AND o.chOp4IDate <= :SDate || '9999'
           AND RTRIM(o.chOp4IDate) IS NOT NULL
           AND (SUBSTR(o.chOp4DCDate,1,7) > :SDate OR RTRIM(o.chOp4DCDate) IS NULL))
      )
      AND o.chOp4OrdNo = '287-001'
      AND (o.rlOp4Sub1 <> 0 OR o.rlOp4Sub6 <> 0)
      AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')

    UNION ALL

    
    SELECT
        :SDate AS chIDate, 'D' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(o.chOp4PFin1, '35', '30', o.chOp4PFin1) AS chOp4PFin1,
        -o.rlOp4Sub1 AS rlOp4Sub1,
        -o.rlOp4Sub6 AS rlOp4Sub6
    FROM OpdOrdTbl o, OpdBasicTbl b
    WHERE o.chOp1Date = b.chOp1Date
      AND o.chOp1Time = b.chOp1Time
      AND o.chOp1Room = b.chOp1Room
      AND o.intOp1No = b.intOp1No
      AND o.chOp1Date < :SDate
      AND o.chOp4DCDate LIKE :SDate || '%'
      AND o.chOp4IDate NOT LIKE :SDate || '%'
      AND RTRIM(o.chOp4IDate) IS NOT NULL
      AND o.chOp4OrdNo = '287-001'
      AND (o.rlOp4Sub1 <> 0 OR o.rlOp4Sub6 <> 0)
      AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')
) x
GROUP BY
    x.chIDate,
    CASE
        WHEN x.chOp1Time = '0' THEN 'I'
        WHEN RTRIM(x.chOp1Room) = '0000' THEN 'E'
        ELSE 'R'
    END,
    x.chType,
    x.chOp1Date,
    x.chOp1Time,
    RTRIM(x.chOp1Room),
    x.intOp1No,
    RTRIM(x.chOp1MrNo),
    RTRIM(x.chOp1PName),
    x.chOp4PFin1
HAVING SUM(x.rlOp4Sub1 + x.rlOp4Sub6) <> 0
""";

    private const string C23OBalance42RebuildCommand3 = """
DELETE FROM GenContract42MrNoTbl
WHERE chIDate = :SDate
  AND chRoomType IN ('E','R')
  AND chType = 'C'
""";

    private const string C23OBalance42RebuildCommand4 = """
INSERT INTO GenContract42MrNoTbl
(
    chIDate, chRoomType, chType,
    chDate, chTime, chRoom, intNo,
    chMrNo, chPName, chFin1, intAmt
)
SELECT
    x.chIDate,
    CASE
        WHEN x.chOp1Time = '0' THEN 'I'
        WHEN RTRIM(x.chOp1Room) = '0000' THEN 'E'
        ELSE 'R'
    END AS chRoomType,
    x.chType,
    x.chOp1Date,
    x.chOp1Time,
    RTRIM(x.chOp1Room),
    x.intOp1No,
    RTRIM(x.chOp1MrNo),
    RTRIM(x.chOp1PName),
    x.chOp3PFin1,
    -SUM(x.rlOp3Sub5) AS intAmt
FROM
(
    
    SELECT
        :SDate AS chIDate, 'C' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(d.chOp3PFin1, '35', '30', d.chOp3PFin1) AS chOp3PFin1,
        d.rlOp3Sub5
    FROM OpdDrgTbl d, OpdBasicTbl b
    WHERE d.chOp1Date = b.chOp1Date
      AND d.chOp1Time = b.chOp1Time
      AND d.chOp1Room = b.chOp1Room
      AND d.intOp1No = b.intOp1No
      AND
      (
          (d.chOp1Date < :SDate
           AND d.chOp3IDate LIKE :SDate || '%'
           AND (d.chOp3DCDate NOT LIKE :SDate || '%' OR RTRIM(d.chOp3DCDate) IS NULL))
          OR
          (d.chOp1Date = :SDate
           AND d.chOp3IDate <= :SDate || '9999'
           AND RTRIM(d.chOp3IDate) IS NOT NULL
           AND (SUBSTR(d.chOp3DCDate,1,7) > :SDate OR RTRIM(d.chOp3DCDate) IS NULL))
      )
      AND d.chOp3PFin2 = '42'
      AND d.rlOp3Sub5 <> 0
      AND (d.chOp3Proj NOT IN ('I','S','D') OR RTRIM(d.chOp3Proj) IS NULL)
      AND (d.chOp3Rep3Flg <> 'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
      AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')

    UNION ALL

    
    SELECT
        :SDate AS chIDate, 'C' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(o.chOp4PFin1, '35', '30', o.chOp4PFin1) AS chOp3PFin1,
        o.rlOp4Sub5 AS rlOp3Sub5
    FROM OpdOrdTbl o, OpdBasicTbl b
    WHERE o.chOp1Date = b.chOp1Date
      AND o.chOp1Time = b.chOp1Time
      AND o.chOp1Room = b.chOp1Room
      AND o.intOp1No = b.intOp1No
      AND
      (
          (o.chOp1Date < :SDate
           AND o.chOp4IDate LIKE :SDate || '%'
           AND (o.chOp4DCDate NOT LIKE :SDate || '%' OR RTRIM(o.chOp4DCDate) IS NULL))
          OR
          (o.chOp1Date = :SDate
           AND o.chOp4IDate <= :SDate || '9999'
           AND RTRIM(o.chOp4IDate) IS NOT NULL
           AND (SUBSTR(o.chOp4DCDate,1,7) > :SDate OR RTRIM(o.chOp4DCDate) IS NULL))
      )
      AND (o.chOp4PFin2 = '42' OR o.chOp4OrdNo IN ('S001A','S001B'))
      AND o.rlOp4Sub5 <> 0
      AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')

    UNION ALL

    
    SELECT
        :SDate AS chIDate, 'C' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(d.chOp3PFin1, '35', '30', d.chOp3PFin1) AS chOp3PFin1,
        -d.rlOp3Sub5 AS rlOp3Sub5
    FROM OpdDrgTbl d, OpdBasicTbl b
    WHERE d.chOp1Date = b.chOp1Date
      AND d.chOp1Time = b.chOp1Time
      AND d.chOp1Room = b.chOp1Room
      AND d.intOp1No = b.intOp1No
      AND d.chOp1Date < :SDate
      AND d.chOp3DCDate LIKE :SDate || '%'
      AND d.chOp3IDate NOT LIKE :SDate || '%'
      AND RTRIM(d.chOp3IDate) IS NOT NULL
      AND d.chOp3PFin2 = '42'
      AND d.rlOp3Sub5 <> 0
      AND (d.chOp3Proj NOT IN ('I','S','D') OR RTRIM(d.chOp3Proj) IS NULL)
      AND (d.chOp3Rep3Flg <> 'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
      AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')

    UNION ALL

    
    SELECT
        :SDate AS chIDate, 'C' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(o.chOp4PFin1, '35', '30', o.chOp4PFin1) AS chOp3PFin1,
        -o.rlOp4Sub5 AS rlOp3Sub5
    FROM OpdOrdTbl o, OpdBasicTbl b
    WHERE o.chOp1Date = b.chOp1Date
      AND o.chOp1Time = b.chOp1Time
      AND o.chOp1Room = b.chOp1Room
      AND o.intOp1No = b.intOp1No
      AND o.chOp1Date < :SDate
      AND o.chOp4DCDate LIKE :SDate || '%'
      AND o.chOp4IDate NOT LIKE :SDate || '%'
      AND RTRIM(o.chOp4IDate) IS NOT NULL
      AND (o.chOp4PFin2 = '42' OR o.chOp4OrdNo IN ('S001A','S001B'))
      AND o.rlOp4Sub5 <> 0
      AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')
) x
GROUP BY
    x.chIDate,
    CASE
        WHEN x.chOp1Time = '0' THEN 'I'
        WHEN RTRIM(x.chOp1Room) = '0000' THEN 'E'
        ELSE 'R'
    END,
    x.chType,
    x.chOp1Date,
    x.chOp1Time,
    RTRIM(x.chOp1Room),
    x.intOp1No,
    RTRIM(x.chOp1MrNo),
    RTRIM(x.chOp1PName),
    x.chOp3PFin1
HAVING SUM(x.rlOp3Sub5) <> 0
""";

    private static readonly IReadOnlyList<string> C23OBalance42RebuildCommands =
        [C23OBalance42RebuildCommand1, C23OBalance42RebuildCommand2, C23OBalance42RebuildCommand3, C23OBalance42RebuildCommand4];

    private const string C23IBalance42RebuildCommand1 = """
DELETE FROM GenContract42MrNoTbl
WHERE chIDate = :SDate
  AND chRoomType = 'I'
  AND chType = 'D'
""";

    private const string C23IBalance42RebuildCommand2 = """
INSERT INTO GenContract42MrNoTbl
(
    chIDate, chRoomType, chType,
    chDate, chTime, chRoom, intNo,
    chMrNo, chPName, chFin1, intAmt
)
SELECT
    x.chIDate,
    CASE
        WHEN x.chOp1Time = '0' THEN 'I'
        WHEN RTRIM(x.chOp1Room) = '0000' THEN 'E'
        ELSE 'R'
    END AS chRoomType,
    x.chType,
    x.chOp1Date,
    x.chOp1Time,
    RTRIM(x.chOp1Room),
    x.intOp1No,
    RTRIM(x.chOp1MrNo),
    RTRIM(x.chOp1PName),
    x.chOp4PFin1,
    SUM(x.rlOp4Sub1 + x.rlOp4Sub6) AS intAmt
FROM
(
    
    SELECT
        :SDate AS chIDate,
        'D' AS chType,
        b.chOp1Date,
        b.chOp1Time,
        b.chOp1Room,
        b.intOp1No,
        b.chOp1MrNo,
        b.chOp1PName,
        DECODE(o.chOp4PFin1, '35', '30', o.chOp4PFin1) AS chOp4PFin1,
        o.rlOp4Sub1,
        o.rlOp4Sub6
    FROM IpdOrdTbl o, IpdBasicTbl b
    WHERE o.chOp1Date = b.chOp1Date
      AND o.chOp1Time = b.chOp1Time
      AND o.chOp1Room = b.chOp1Room
      AND o.intOp1No = b.intOp1No
      AND o.chOp4IDate LIKE :SDate || '%'
      AND (o.chOp4DCDate NOT LIKE :SDate || '%' OR RTRIM(o.chOp4DCDate) IS NULL)
      AND o.chOp4OrdNo = '287-001'
      AND (o.rlOp4Sub1 <> 0 OR o.rlOp4Sub6 <> 0)
      AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')

    UNION ALL

    
    SELECT
        :SDate AS chIDate,
        'D' AS chType,
        b.chOp1Date,
        b.chOp1Time,
        b.chOp1Room,
        b.intOp1No,
        b.chOp1MrNo,
        b.chOp1PName,
        DECODE(o.chOp4PFin1, '35', '30', o.chOp4PFin1) AS chOp4PFin1,
        -o.rlOp4Sub1 AS rlOp4Sub1,
        -o.rlOp4Sub6 AS rlOp4Sub6
    FROM IpdOrdTbl o, IpdBasicTbl b
    WHERE o.chOp1Date = b.chOp1Date
      AND o.chOp1Time = b.chOp1Time
      AND o.chOp1Room = b.chOp1Room
      AND o.intOp1No = b.intOp1No
      AND o.chOp4DCDate LIKE :SDate || '%'
      AND o.chOp4IDate NOT LIKE :SDate || '%'
      AND RTRIM(o.chOp4IDate) IS NOT NULL
      AND o.chOp4OrdNo = '287-001'
      AND (o.rlOp4Sub1 <> 0 OR o.rlOp4Sub6 <> 0)
      AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')
) x
GROUP BY
    x.chIDate,
    CASE
        WHEN x.chOp1Time = '0' THEN 'I'
        WHEN RTRIM(x.chOp1Room) = '0000' THEN 'E'
        ELSE 'R'
    END,
    x.chType,
    x.chOp1Date,
    x.chOp1Time,
    RTRIM(x.chOp1Room),
    x.intOp1No,
    RTRIM(x.chOp1MrNo),
    RTRIM(x.chOp1PName),
    x.chOp4PFin1
HAVING SUM(x.rlOp4Sub1 + x.rlOp4Sub6) <> 0
""";

    private const string C23IBalance42RebuildCommand3 = """
DELETE FROM GenContract42MrNoTbl
WHERE chIDate = :SDate
  AND chRoomType = 'I'
  AND chType = 'C'
""";

    private const string C23IBalance42RebuildCommand4 = """
INSERT INTO GenContract42MrNoTbl
(
    chIDate, chRoomType, chType,
    chDate, chTime, chRoom, intNo,
    chMrNo, chPName, chFin1, intAmt
)
SELECT
    x.chIDate,
    CASE
        WHEN x.chOp1Time = '0' THEN 'I'
        WHEN RTRIM(x.chOp1Room) = '0000' THEN 'E'
        ELSE 'R'
    END AS chRoomType,
    x.chType,
    x.chOp1Date,
    x.chOp1Time,
    RTRIM(x.chOp1Room),
    x.intOp1No,
    RTRIM(x.chOp1MrNo),
    RTRIM(x.chOp1PName),
    x.chOp3PFin1,
    -SUM(x.rlOp3Sub5) AS intAmt
FROM
(
    
    SELECT
        :SDate AS chIDate, 'C' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(d.chOp3PFin1, '35', '30', d.chOp3PFin1) AS chOp3PFin1,
        d.rlOp3Sub5
    FROM IpdDrgTbl d, IpdBasicTbl b
    WHERE d.chOp1Date = b.chOp1Date
      AND d.chOp1Time = b.chOp1Time
      AND d.chOp1Room = b.chOp1Room
      AND d.intOp1No = b.intOp1No
      AND d.chOp3IDate LIKE :SDate || '%'
      AND (d.chOp3DCDate NOT LIKE :SDate || '%' OR RTRIM(d.chOp3DCDate) IS NULL)
      AND d.chOp3PFin2 = '42'
      AND d.rlOp3Sub5 <> 0
      AND (d.chOp3Proj NOT IN ('I','S','D') OR RTRIM(d.chOp3Proj) IS NULL)
      AND (d.chOp3Rep3Flg <> 'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
      AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')

    UNION ALL

    
    SELECT
        :SDate AS chIDate, 'C' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(o.chOp4PFin1, '35', '30', o.chOp4PFin1) AS chOp3PFin1,
        o.rlOp4Sub5 AS rlOp3Sub5
    FROM IpdOrdTbl o, IpdBasicTbl b
    WHERE o.chOp1Date = b.chOp1Date
      AND o.chOp1Time = b.chOp1Time
      AND o.chOp1Room = b.chOp1Room
      AND o.intOp1No = b.intOp1No
      AND o.chOp4IDate LIKE :SDate || '%'
      AND (o.chOp4DCDate NOT LIKE :SDate || '%' OR RTRIM(o.chOp4DCDate) IS NULL)
      AND (o.chOp4PFin2 = '42' OR o.chOp4OrdNo IN ('S001A','S001B'))
      AND o.rlOp4Sub5 <> 0
      AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')

    UNION ALL

    
    SELECT
        :SDate AS chIDate, 'C' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(d.chOp3PFin1, '35', '30', d.chOp3PFin1) AS chOp3PFin1,
        -d.rlOp3Sub5 AS rlOp3Sub5
    FROM IpdDrgTbl d, IpdBasicTbl b
    WHERE d.chOp1Date = b.chOp1Date
      AND d.chOp1Time = b.chOp1Time
      AND d.chOp1Room = b.chOp1Room
      AND d.intOp1No = b.intOp1No
      AND d.chOp3DCDate LIKE :SDate || '%'
      AND d.chOp3IDate NOT LIKE :SDate || '%'
      AND RTRIM(d.chOp3IDate) IS NOT NULL
      AND d.chOp3PFin2 = '42'
      AND d.rlOp3Sub5 <> 0
      AND (d.chOp3Proj NOT IN ('I','S','D') OR RTRIM(d.chOp3Proj) IS NULL)
      AND (d.chOp3Rep3Flg <> 'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
      AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')

    UNION ALL

    
    SELECT
        :SDate AS chIDate, 'C' AS chType,
        b.chOp1Date, b.chOp1Time, b.chOp1Room, b.intOp1No,
        b.chOp1MrNo, b.chOp1PName,
        DECODE(o.chOp4PFin1, '35', '30', o.chOp4PFin1) AS chOp3PFin1,
        -o.rlOp4Sub5 AS rlOp3Sub5
    FROM IpdOrdTbl o, IpdBasicTbl b
    WHERE o.chOp1Date = b.chOp1Date
      AND o.chOp1Time = b.chOp1Time
      AND o.chOp1Room = b.chOp1Room
      AND o.intOp1No = b.intOp1No
      AND o.chOp4DCDate LIKE :SDate || '%'
      AND o.chOp4IDate NOT LIKE :SDate || '%'
      AND RTRIM(o.chOp4IDate) IS NOT NULL
      AND (o.chOp4PFin2 = '42' OR o.chOp4OrdNo IN ('S001A','S001B'))
      AND o.rlOp4Sub5 <> 0
      AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
      AND b.chOp1MrNo NOT IN ('C36979','1000000')
) x
GROUP BY
    x.chIDate,
    CASE
        WHEN x.chOp1Time = '0' THEN 'I'
        WHEN RTRIM(x.chOp1Room) = '0000' THEN 'E'
        ELSE 'R'
    END,
    x.chType,
    x.chOp1Date,
    x.chOp1Time,
    RTRIM(x.chOp1Room),
    x.intOp1No,
    RTRIM(x.chOp1MrNo),
    RTRIM(x.chOp1PName),
    x.chOp3PFin1
HAVING SUM(x.rlOp3Sub5) <> 0
""";

    private static readonly IReadOnlyList<string> C23IBalance42RebuildCommands =
        [C23IBalance42RebuildCommand1, C23IBalance42RebuildCommand2, C23IBalance42RebuildCommand3, C23IBalance42RebuildCommand4];

}
