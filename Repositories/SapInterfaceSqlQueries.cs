namespace OpdAccrRptWeb.Repositories;

// SQL is transcribed from ISAP/01_ASPNET_Core_MVC_SPEC.md section 8.
internal static class SapInterfaceSqlQueries
{
    internal const string DefaultDate = """
        -- 查詢預設日期；rownum=1 沒有外層排序，結果不保證是最早未完成日。
        with tmp_a as
         (
         select datadate,count(*) as cnt
         From logday
         where datadate>=to_char(to_char(sysdate-30,'YYYYMMDD')-19110000)
         and event in ('SAPCASH','SAPCONS','SAPACC','SAPREV2')
         group by datadate
         )
         ,tmp_b as
         (
         select datadate,case when cnt<4 then 3 else 4 end as cnt
         From tmp_a
         )
         ,tmp_c as
         (
         select datadate,cnt,case when cnt<4 then min(datadate) over (partition by cnt order by cnt) else to_char(to_char(to_date(max(datadate) over (partition by cnt order by cnt)+19110000,'YYYYMMDD')+1,'YYYYMMDD')-19110000) end as rundate
         From tmp_b
         )
         select rundate from tmp_c
         where rownum=1
        """;

    internal const string DeleteLog = """
        -- 只刪目前事件與日期且 ok=Y 的完成紀錄。
        delete from logday
         where datadate=:work_roc
         and event=:event
         and ok='Y'
        """;

    internal const string CheckLog = """
        -- 檢查目前事件與日期是否已標記完成。
        select count(*) from logday
         where datadate=:work_roc
         and event=:event
         and ok='Y'
        """;

    internal const string InsertLog = """
        -- 目標表所有插入成功後才新增完成紀錄；舊程式無明確交易。
        insert into logday(datadate,event,ok)
         values(:work_roc,:event,'Y')
        """;

    internal const string DeleteRev = """
        -- SAPREV2 重跑只清 19999 部門的指定科目及所有 51；注意刪除範圍。
        delete from saprevtbl
         where tdate between :start_gregorian and :end_gregorian
         and
         (
         (deptno ='19999' and accno in ('49','66','67','X1'))
         or
         (accno='51')
         )
        """;

    internal const string InsertRevCharges = """
        -- 第一筆 SAPREV2 INSERT：49/66/67 與 X1；X1 先 SUM 再依日期費率 ROUND。
        insert into saprevtbl
         with tmp_a as
         (
         select chOp1Date
         ,decode(chOp1RoomType,'0','A','1','B') as chOp1RoomType,Substr(chOp1Dct,1,2) chOp1Dct
         ,case when chOp1Fin1='35' and chOp1Date>='1000901' then 'A' else decode(chOp1Fin1,'01','A','30','B','C') end as chOp1Fin1
         ,sum(intAMT) intAMT
         From OpdTranColeTbl
         where chOp1Date between :start_roc and :end_roc
         and chOp1RoomType in ('0','1')
         group by chOp1Date
         ,decode(chOp1RoomType,'0','A','1','B'),chOp1Dct
         ,case when chOp1Fin1='35' and chOp1Date>='1000901' then 'A' else decode(chOp1Fin1,'01','A','30','B','C') end
         Having Sum(intAMT) <> 0
         Union All
         select chOp1Date
         ,decode(chOp1RoomType,'2','C','3','C','4','D','5','D') as chOp1RoomType,Substr(chOp1Dct,1,2) chOp1Dct
         ,decode(chOp1Fin1,'01','A','30','B','35','B') as chOp1Fin1,sum(intAMT) intAMT
         From IpdTranColeTbl
         where chOp1Date between :start_roc and :end_roc
         and chOp1RoomType in ('2','3')
         group by chOp1Date
         ,decode(chOp1RoomType,'2','C','3','C','4','D','5','D'),chOp1Dct
         ,decode(chOp1Fin1,'01','A','30','B','35','B')
         Having Sum(intAMT) <> 0
         )
         select to_char(chop1date+19110000) as tdate,'19999' as chnewsecno,chop1dct as accno,decode(chop1roomtype,'A','6','B','9','C','3','D') as ptype,intamt as amt
         From tmp_a
         Where
         (
         (chop1roomtype='A' and chop1dct in ('49','67'))
         or
         (chop1roomtype='B' and chop1dct in ('49','67'))
         or
         (chop1roomtype='C' and chop1dct in ('49','66'))
         )
         Union All
         select to_char(chop1date+19110000) as tdate,'19999' as chnewsecno,'X1' as accno,decode(chop1roomtype,'A','6','B','9','C','3','D') as ptype,round(sum(intamt)*
         (case when to_char(chop1date+19110000)<='20141231' then 0.029
         when to_char(chop1date+19110000)<='20151231' then 0.04
         when to_char(chop1date+19110000)<='20161231' then 0.03
         when to_char(chop1date+19110000)<='20171231' then 0.02
         when to_char(chop1date+19110000)<='20181231' then 0.015
         when to_char(chop1date+19110000)<='20190131' then 0.025
         when to_char(chop1date+19110000)<='20191231' then 0.02
         when to_char(chop1date+19110000)<='20200531' then 0.07
         when to_char(chop1date+19110000)<='20210331' then 0.025
         when to_char(chop1date+19110000)<='20210430' then 0.06
         when to_char(chop1date+19110000)<='20210531' then 0.05
         when to_char(chop1date+19110000)<='20211231' then 0.01
         when to_char(chop1date+19110000)<='20220731' then 0.037
         when to_char(chop1date+19110000)<='20221231' then 0.01
         when to_char(chop1date+19110000)<='20230228' then 0.035
         when to_char(chop1date+19110000)<='20230630' then 0.02
         when to_char(chop1date+19110000)<='20230930' then 0.0851
         when to_char(chop1date+19110000)<='20231231' then 0.0644
         when to_char(chop1date+19110000)<='20240105' then 0.0271
         when to_char(chop1date+19110000)<='20240331' then 0.035
         when to_char(chop1date+19110000)<='20240702' then 0.0366
         when to_char(chop1date+19110000)<='20240704' then 0.0511
         when to_char(chop1date+19110000)<='20241003' then 0.0542
         when to_char(chop1date+19110000)<='20241231' then 0.0676
         when to_char(chop1date+19110000)<='20250331' then 0.035
         when to_char(chop1date+19110000)<='20250630' then 0.0471
         when to_char(chop1date+19110000)<='20250930' then 0.01
         when to_char(chop1date+19110000)<='20251106' then 0.018
         when to_char(chop1date+19110000)<='20260121' then 0.005
         when to_char(chop1date+19110000)<='20260131' then 0.045
         when to_char(chop1date+19110000)<='20260331' then 0.0299
         when to_char(chop1date+19110000)<='20260630' then 0.0314
         else 0.0316 end)
         ) as amt
         From tmp_a
         Where
         (
         (chop1roomtype='A' and chop1dct in ('49','67'))
         or
         (chop1roomtype='B' and chop1dct in ('49','67'))
         or
         (chop1roomtype='C' and chop1dct in ('49','66'))
         )
         group by to_char(chop1date+19110000),decode(chop1roomtype,'A','6','B','9','C','3','D')
        """;

    internal const string InsertRevInpatientDiscount = """
        -- 第二筆 SAPREV2 INSERT：住院／出院 51 優待；過往已收款部分以負數沖回。
        insert into saprevtbl
         with tmp_a as
         (
         select to_char(to_number(Acc_Date)+19110000) as tdate,coalesce(rtrim(chOp4EXESEC),rtrim(chop3psec)) as deptno,'51' as accno
         ,case when Patient_ID2='01' then '1'
         when Patient_ID2 in ('30','35') and selfpay_mark='1' then '2'
         when Patient_ID2 in ('30','35') and selfpay_mark='3' then '3'
         Else 'O'
         end As ptype
         ,sum(Vip_AMT) as amt
         From
         (
         select /*+index(d,CON_IPDDRG)*/ '4' Noon_Type,:start_roc Acc_Date,d.chop3pfin1 Patient_ID2,substr(d.chop3dct,1,2) Pat_Rec_ID,decode(d.chop3spay,'0','1','4','1','3') selfpay_mark ,d.chop3drgno ,'' as chOp4EXESEC ,d.chop3psec , decode(substr(d.chop3dct,1,2),'49',sum(d.rlop3sub2)*(-1) ,sum(d.rlop3sub1)+sum(d.rlop3sub2)+sum(d.rlop3sub3)+sum(d.rlop3sub5)+sum(d.rlop3sub6)+sum(d.rlop3sub4)) SubTot ,sum(d.rlop3sub1) Pay_AMT,sum(d.rlop3sub2) Charge_AMT,sum(d.rlop3sub3) Vip_AMT ,sum(d.rlop3sub5) Vip_AMT8,sum(d.rlop3sub6) Debt,sum(d.rlop3sub4) Charity_AMT from ipddrgtbl d ,( select a.chop1date,a.chop1time,a.chop1room,a.intop1no,max(a.vchaccdate||substr(a.vchop2recctime,1,4)) as vchop2recctime from ipdacccasedaytbl a where a.vchaccdate=:start_roc and a.vchaccmrno not in ('C36979','1000000') and a.vchaccbid<>'預繳' group by a.chop1date,a.chop1time,a.chop1room,a.intop1no )j
         Where D.chop1date = J.chop1date and d.chop1time=j.chop1time and d.chop1room=j.chop1room and d.intop1no=j.intop1no and d.chop3idate<=j.vchop2recctime and rtrim(d.chop3idate) is not null and (d.chop3dcdate>j.vchop2recctime or rtrim(d.chop3dcdate) is null) and (d.chop3proj not in ('I','D') or rtrim(d.chop3proj) is null) and (d.chop3rep3flg<>'S' or rtrim(d.chop3rep3flg) is null) and (d.chop3stat not in ('09','10','11','12') or rtrim(d.chop3stat) is null) group by d.chop3pfin1,substr(d.chop3dct,1,2),decode(d.chop3spay,'0','1','4','1','3'),d.chop3drgno,d.chop3psec
         Union All
         select /*+index(o,CON_IPDORD)*/ '4' Noon_Type,:start_roc Acc_Date,o.chop4pfin1 Patient_ID2,substr(o.chop4dct,1,2) Pat_Rec_ID,decode(o.chop4spay,'0','1','4','1','3') selfpay_mark ,o.chop4ordno ,coalesce(o.chOp4EXESEC,o.chop4psec) ,o.chop4psec , decode(substr(o.chop4dct,1,2),'49',sum(o.rlop4sub2)*(-1) ,sum(o.rlop4sub1)+sum(o.rlop4sub2)+sum(o.rlop4sub3)+sum(o.rlop4sub5)+sum(o.rlop4sub6)+sum(o.rlop4sub4)) SubTot ,sum(o.rlop4sub1) Pay_AMT,sum(o.rlop4sub2) Charge_AMT,sum(o.rlop4sub3) Vip_AMT ,sum(o.rlop4sub5) Vip_AMT8,sum(o.rlop4sub6) Debt,sum(o.rlop4sub4) Charity_AMT from ipdordtbl o ,( select a.chop1date,a.chop1time,a.chop1room,a.intop1no,max(a.vchaccdate||substr(a.vchop2recctime,1,4)) as vchop2recctime from ipdacccasedaytbl a where a.vchaccdate=:start_roc and a.vchaccmrno not in ('C36979','1000000') and a.vchaccbid<>'預繳' group by a.chop1date,a.chop1time,a.chop1room,a.intop1no )j
         Where o.chop1date = J.chop1date and o.chop1time=j.chop1time and o.chop1room=j.chop1room and o.intop1no=j.intop1no and o.chop4idate<=j.vchop2recctime and rtrim(o.chop4idate) is not null and (o.chop4dcdate>j.vchop2recctime or rtrim(o.chop4dcdate) is null) and (o.chop4proj not in ('I','D') or rtrim(o.chop4proj) is null) group by o.chop4pfin1,substr(o.chop4dct,1,2),decode(o.chop4spay,'0','1','4','1','3'),o.chop4ordno,coalesce(o.chOp4EXESEC,o.chop4psec),o.chop4psec
         Union All
         select /*+index(d,CON_IPDDRG)*/ '5' Noon_Type,:start_roc Acc_Date,d.chop3pfin1 Patient_ID2,substr(d.chop3dct,1,2) Pat_Rec_ID,decode(d.chop3spay,'0','1','4','1','3') selfpay_mark ,d.chop3drgno ,'' as chOp4EXESEC ,d.chop3psec , (decode(substr(d.chop3dct,1,2),'49',sum(d.rlop3sub2)*(-1) ,sum(d.rlop3sub1)+sum(d.rlop3sub2)+sum(d.rlop3sub3)+sum(d.rlop3sub5)+sum(d.rlop3sub6)+sum(d.rlop3sub4)))*(-1) SubTot ,sum(d.rlop3sub1)*(-1) Pay_AMT,sum(d.rlop3sub2)*(-1) Charge_AMT,sum(d.rlop3sub3)*(-1) Vip_AMT ,sum(d.rlop3sub5)*(-1) Vip_AMT8,sum(d.rlop3sub6)*(-1) Debt,sum(d.rlop3sub4)*(-1) Charity_AMT from ipddrgtbl d ,( select a.chop1date,a.chop1time,a.chop1room,a.intop1no,max(a.vchaccdate||substr(a.vchop2recctime,1,4)) as vchop2recctime from ipdacccasedaytbl a     ,(     select distinct a.chop1date,a.chop1time,a.chop1room,a.intop1no     from ipdacccasedaytbl a     where a.vchaccdate=:start_roc     and a.vchaccmrno not in ('C36979','1000000')     and a.vchaccbid<>'預繳'     )j
         Where A.chop1date = J.chop1date And A.chop1time = J.chop1time And A.chop1room=j.chop1room and a.intop1no=j.intop1no and a.vchaccdate<:start_roc and a.vchaccbid<>'預繳' group by a.chop1date,a.chop1time,a.chop1room,a.intop1no )j Where D.chop1date = J.chop1date and d.chop1time=j.chop1time and d.chop1room=j.chop1room and d.intop1no=j.intop1no and d.chop3idate<=j.vchop2recctime and rtrim(d.chop3idate) is not null and (d.chop3dcdate>j.vchop2recctime or rtrim(d.chop3dcdate) is null) and (d.chop3proj not in ('I','D') or rtrim(d.chop3proj) is null) and (d.chop3rep3flg<>'S' or rtrim(d.chop3rep3flg) is null) and (d.chop3stat not in ('09','10','11','12') or rtrim(d.chop3stat) is null) group by d.chop3pfin1,substr(d.chop3dct,1,2),decode(d.chop3spay,'0','1','4','1','3'),d.chop3drgno,d.chop3psec
         Union All
         select /*+index(o,CON_IPDORD)*/ '5' Noon_Type,:start_roc Acc_Date,o.chop4pfin1 Patient_ID2,substr(o.chop4dct,1,2) Pat_Rec_ID,decode(o.chop4spay,'0','1','4','1','3') selfpay_mark ,o.chop4ordno ,coalesce(o.chOp4EXESEC,o.chop4psec) ,o.chop4psec ,(decode(substr(o.chop4dct,1,2),'49',sum(o.rlop4sub2)*(-1) ,sum(o.rlop4sub1)+sum(o.rlop4sub2)+sum(o.rlop4sub3)+sum(o.rlop4sub5)+sum(o.rlop4sub6)+sum(o.rlop4sub4)))*(-1) SubTot ,sum(o.rlop4sub1)*(-1) Pay_AMT,sum(o.rlop4sub2)*(-1) Charge_AMT,sum(o.rlop4sub3)*(-1) Vip_AMT ,sum(o.rlop4sub5)*(-1) Vip_AMT8,sum(o.rlop4sub6)*(-1) Debt,sum(o.rlop4sub4)*(-1) Charity_AMT from ipdordtbl o ,( select a.chop1date,a.chop1time,a.chop1room,a.intop1no,max(a.vchaccdate||substr(a.vchop2recctime,1,4)) as vchop2recctime from ipdacccasedaytbl a     ,(     select distinct a.chop1date,a.chop1time,a.chop1room,a.intop1no     from ipdacccasedaytbl a     where a.vchaccdate=:start_roc     and a.vchaccmrno not in ('C36979','1000000')     and a.vchaccbid<>'預繳'     )j
         Where A.chop1date = J.chop1date And A.chop1time = J.chop1time and a.chop1room=j.chop1room and a.intop1no=j.intop1no and a.vchaccdate<:start_roc and a.vchaccbid<>'預繳' group by a.chop1date,a.chop1time,a.chop1room,a.intop1no )j Where o.chop1date = J.chop1date and o.chop1time=j.chop1time and o.chop1room=j.chop1room and o.intop1no=j.intop1no and o.chop4idate<=j.vchop2recctime and rtrim(o.chop4idate) is not null and (o.chop4dcdate>j.vchop2recctime or rtrim(o.chop4dcdate) is null) and (o.chop4proj not in ('I','D') or rtrim(o.chop4proj) is null) group by o.chop4pfin1,substr(o.chop4dct,1,2),decode(o.chop4spay,'0','1','4','1','3'),o.chop4ordno,coalesce(o.chOp4EXESEC,o.chop4psec),o.chop4psec
         )
         group by to_char(to_number(Acc_Date)+19110000),coalesce(rtrim(chOp4EXESEC),rtrim(chop3psec))
         ,case when Patient_ID2='01' then '1'
         when Patient_ID2 in ('30','35') and selfpay_mark='1' then '2'
         when Patient_ID2 in ('30','35') and selfpay_mark='3' then '3'
         Else 'O'
         End
         Having Sum(Vip_AMT) <> 0
         )
         select A.tdate,coalesce(substr(rtrim(B.chnewsecno),1,5),'19999')||decode(coalesce(substr(rtrim(B.chnewsecno),1,5),'19999'),'19999','','D') as chnewsecno,A.accno,A.ptype,sum(A.amt) as amt
         from tmp_a A
         left outer join gensectiontbl B
         on (rpad(A.deptno,7,' ')=B.chsecno)
         group by A.tdate,coalesce(substr(rtrim(B.chnewsecno),1,5),'19999')||decode(coalesce(substr(rtrim(B.chnewsecno),1,5),'19999'),'19999','','D'),A.accno,A.ptype
        """;

    internal const string InsertRevOutpatientDiscount = """
        -- 第三筆 SAPREV2 INSERT：門急 51 優待，含科別轉換與場所代碼。
        insert into saprevtbl
         with tmp_a as
         (
         select to_char(to_number(A.acc_date)+19110000) as tdate
         ,case when coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))='0298' then '12150'
         when A.noon_type='1' and coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))='0201' then '11910'
         when A.noon_type='1' and coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))='0281' and (B.chusersector<>'0281' or rtrim(B.chusersector) is null) then '11920'
         when A.noon_type='1' and coalesce(rtrim(A.exe_dept),rtrim(A.subject_id)) in ('0221','0220') then '11930'
         when A.noon_type='1' and coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))='0230' then '11309'
         else coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))
         end As deptno
         ,'51' as accno
         ,case when A.noon_type='0' and A.patient_id2 in ('01','35') then '4'
         when A.noon_type='0' and A.patient_id2='30' and A.selfpay_mark in ('0','4') then '5'
         when A.noon_type='0' and A.patient_id2='30' and A.selfpay_mark not in ('0','4') then '6'
         when A.noon_type='1' and A.patient_id2 in ('01','35') then '7'
         when A.noon_type='1' and A.patient_id2='30' and A.selfpay_mark in ('0','4') then '8'
         when A.noon_type='1' and A.patient_id2='30' and A.selfpay_mark not in ('0','4') then '9'
         Else 'O'
         end As ptype
         ,sum(A.vip_amt) as amt
         From opdtransfermtbl A
         left outer join genuserprofile1 B
         on (coalesce(rtrim(A.exe_phy_id_d),rtrim(A.exe_phy_id))=rtrim(B.chuserid))
         where A.acc_date=:start_roc
         and (A.sign_flag not in ('R','S','T','U','V') or rtrim(A.sign_flag) is null)
         and (A.partial_type<>'2' or rtrim(A.partial_type) is null)
         group by to_char(to_number(A.acc_date)+19110000)
         ,case when coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))='0298' then '12150'
         when A.noon_type='1' and coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))='0201' then '11910'
         when A.noon_type='1' and coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))='0281' and (B.chusersector<>'0281' or rtrim(B.chusersector) is null) then '11920'
         when A.noon_type='1' and coalesce(rtrim(A.exe_dept),rtrim(A.subject_id)) in ('0221','0220') then '11930'
         when A.noon_type='1' and coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))='0230' then '11309'
         else coalesce(rtrim(A.exe_dept),rtrim(A.subject_id))
         End
         ,case when A.noon_type='0' and A.patient_id2 in ('01','35') then '4'
         when A.noon_type='0' and A.patient_id2='30' and A.selfpay_mark in ('0','4') then '5'
         when A.noon_type='0' and A.patient_id2='30' and A.selfpay_mark not in ('0','4') then '6'
         when A.noon_type='1' and A.patient_id2 in ('01','35') then '7'
         when A.noon_type='1' and A.patient_id2='30' and A.selfpay_mark in ('0','4') then '8'
         when A.noon_type='1' and A.patient_id2='30' and A.selfpay_mark not in ('0','4') then '9'
         Else 'O'
         End
         Having Sum(A.vip_amt) <> 0
         )
         select AA.tdate,nvl(rtrim(C.chnewplano),'19999')||decode(nvl(rtrim(C.chnewplano),'19999'),'19999','','C') as chnewsecno,AA.accno,AA.ptype,sum(AA.amt) as amt
         from tmp_a AA
         left outer join genplacetbl C
         on (rpad(AA.deptno,10,' ')=C.chplano)
         group by AA.tdate,nvl(rtrim(C.chnewplano),'19999')||decode(nvl(rtrim(C.chnewplano),'19999'),'19999','','C'),AA.accno,AA.ptype
        """;

    internal const string DeleteAcc = """
        -- SAPACC 重跑刪除指定日期全部資料。
        delete from sapacctbl
         where tdate between :start_gregorian and :end_gregorian
        """;

    internal const string InsertAccOutpatient = """
        -- 第一筆 SAPACC INSERT：門急收入及 Z4 至 Z7；保留 UNION ALL 與正負號。
        insert into sapacctbl
         with tmp_a as
         (
         select chOp1Date
         ,decode(chOp1RoomType,'0','A','1','B') as chOp1RoomType,Substr(chOp1Dct,1,2) chOp1Dct
         ,case when chOp1Fin1='35' and chOp1Date>='1000901' then 'A' else decode(chOp1Fin1,'01','A','30','B','C') end as chOp1Fin1
         ,sum(intAMT) intAMT
         From OpdTranColeTbl
         where chOp1Date between :start_roc and :end_roc
         and chOp1RoomType in ('0','1')
         group by chOp1Date
         ,decode(chOp1RoomType,'0','A','1','B'),chOp1Dct
         ,case when chOp1Fin1='35' and chOp1Date>='1000901' then 'A' else decode(chOp1Fin1,'01','A','30','B','C') end
         Having Sum(intAMT) <> 0
         )
         select to_char(chop1date+19110000) as tdate,chOp1RoomType,chOp1Dct,chOp1Fin1,intAMT
         From tmp_a
         Where
         (
         (chop1roomtype='A' and chop1dct in ('25','49','51','59','60','67','69','75'))
         or
         (chop1roomtype='B' and chop1dct in ('25','49','51','67','69','75'))
         )
         Union All
         select to_char(chop1date+19110000),chop1roomtype,decode(chop1roomtype,'A','Z6','B','Z7'),chop1fin1,sum(intamt)
         From tmp_a
         where chop1dct between '01' and '50' and chop1dct<>'49'
         group by to_char(chop1date+19110000),chop1roomtype,decode(chop1roomtype,'A','Z6','B','Z7'),chop1fin1
         Union All
         select to_char(chop1date+19110000),chop1roomtype,decode(chop1roomtype,'A','Z4','B','Z5'),chop1fin1,sum(decode(chop1dct,'67',-intamt,intamt))
         From tmp_a
         Where
         (
         (chop1dct between '01' and '50' and chop1dct<>'49')
         or
         (chop1dct='67')
         )
         group by to_char(chop1date+19110000),chop1roomtype,decode(chop1roomtype,'A','Z4','B','Z5'),chop1fin1
        """;

    internal const string InsertAccInpatient = """
        -- 第二筆 SAPACC INSERT：住出院收入、62、Z1 至 Z3。
        insert into sapacctbl
         with tmp_a as
         (
         select chOp1Date
         ,decode(chOp1RoomType,'2','C','3','C','4','D','5','D') as chOp1RoomType,Substr(chOp1Dct,1,2) chOp1Dct
         ,decode(chOp1Fin1,'01','A','30','B','35','B') as chOp1Fin1,sum(intAMT) intAMT
         From IpdTranColeTbl
         where chOp1Date between :start_roc and :end_roc
         and chOp1RoomType in ('2','3','4','5')
         group by chOp1Date
         ,decode(chOp1RoomType,'2','C','3','C','4','D','5','D'),chOp1Dct
         ,decode(chOp1Fin1,'01','A','30','B','35','B')
         Having Sum(intAMT) <> 0
         )
         select to_char(chOp1Date+19110000) as tdate,chOp1RoomType,chOp1Dct,chOp1Fin1,intAMT
         From tmp_a
         Where
         (
         (chop1roomtype='C' and chop1dct in ('49','66'))
         or
         (chop1roomtype='D' and chop1dct in ('49','51','57','63','75'))
         )
         Union All
         select to_char(chop1date+19110000),chop1roomtype,'62',chop1fin1,sum(intamt)
         From tmp_a
         where chop1dct in ('62','51','63')
         group by to_char(chop1date+19110000),chop1roomtype,chop1fin1
         Union All
         select to_char(chop1date+19110000),chop1roomtype,'Z1',chop1fin1,sum(intamt)
         From tmp_a
         where chop1dct between '01' and '50' and chop1dct<>'49'
         and chop1roomtype='C'
         group by to_char(chop1date+19110000),chop1roomtype,chop1fin1
         Union All
         select to_char(chop1date+19110000),'C','Z2',chop1fin1,sum(case when chop1dct between '01' and '50' and chop1dct<>'49' then intamt else -intamt end)
         From tmp_a
         Where
         (chop1dct between '01' and '50' and chop1dct<>'49'
         or chop1dct in ('49','62','51','63','66')
         )
         and chop1roomtype='C'
         group by to_char(chop1date+19110000),chop1fin1
         Union All
         select to_char(chop1date+19110000),'D','Z3',chop1fin1,sum(case when chop1dct in ('49','62') then intamt else -intamt end)
         From tmp_a
         where chop1dct in ('49','62'
         ,'56','58','60','69','75','80')
         and chop1roomtype='D'
         group by to_char(chop1date+19110000),chop1fin1
        """;

    internal const string DeleteContract = """
        -- SAPCONS 重跑刪除指定日期全部資料。
        delete from sapcontracttbl
         where tdate between :start_gregorian and :end_gregorian
        """;

    internal const string InsertContract = """
        -- SAPCONS 插入門急與住院合約；原碼只使用起日篩選來源。
        insert into sapcontracttbl
         select to_char(substr(chDateFlag,1,7)+19110000) as chDateFlag,'A' as roomtype
         ,chOp1PFin2,chOp1PFin2Nm,sum(rlOp1Sub2) as rlOp1Sub2
         From OpdRecRpt_PFin2SumDM1
         where chDateFlag like (:start_roc || '%')
         and chop1roomtype is null
         group by to_char(substr(chDateFlag,1,7)+19110000),chOp1PFin2,chOp1PFin2Nm
         Union All
         select to_char(substr(chDateFlag,1,7)+19110000),'B'
         ,chOp1PFin2,chOp1PFin2Nm,sum(rlOp1Sub2)
         From IpdRecRpt_PFin2SumDM1
         where chDateFlag like (:start_roc || '%')
         and chop1roomtype is null
         group by to_char(substr(chDateFlag,1,7)+19110000),chOp1PFin2,chOp1PFin2Nm
        """;

    internal const string DeleteCash = """
        -- SAPCASH 重跑刪除指定日期全部資料。
        delete from sapcashtbl
         where tdate between :start_gregorian and :end_gregorian
        """;

    internal const string InsertCash = """
        -- SAPCASH 插入 A 至 M；預繳 I 僅加總原碼指定的八項。
        insert into sapcashtbl(tdate,roomtype,cashtype,amt)
         with tmp_a as
         (
         select chAccDate
         ,case when Substr(chAccSeqNo,1,1)='E' and chAccBid='合約' then '急診合約'
         when Substr(chAccSeqNo,1,1)='I' and chAccBid='合約' then '住院合約'
         when chAccBid='合約' then '門診合約'
         when Substr(chAccSeqNo,1,1)='I' and chAccBid='預繳' then '住院預繳'
         when Substr(chAccSeqNo,1,1)='E' then '急診'
         when Substr(chAccSeqNo,1,1)='I' then '住院'
         Else '門診'
         End
         as chAccBid
         ,sum(intAccDet) intAccDet
         ,sum(decode(rtrim(chAccBid),'繳欠',0,intAccCash1)) intAccCash1,sum(intAccCash2) intAccCash2,sum(intAccCash3) intAccCash3,sum(intAccCash4) intAccCash4,sum(intSocFree) intSocFree,sum(intAccCash5) intAccCash5,sum(intAccCash6) intAccCash6
         ,sum(decode(rtrim(chAccBid),'繳欠',intAccCash1,intOp2AMT50)) intOp2AMT50,sum(intOp2AMT51) intOp2AMT51,sum(intOp2AMT52) intOp2AMT52
         ,sum(intAccCash7) intAccCash7,sum(intAccCash8) intAccCash8,sum(intAccCash9) intAccCash9,sum(intAccCash10) intAccCash10
         From GenAccCaseDayTbl
         where chAccDate between :start_roc and :end_roc
         and chAccMrNo not in ('C36979','1000000')
         group by chAccDate
         ,case when Substr(chAccSeqNo,1,1)='E' and chAccBid='合約' then '急診合約'
         when Substr(chAccSeqNo,1,1)='I' and chAccBid='合約' then '住院合約'
         when chAccBid='合約' then '門診合約'
         when Substr(chAccSeqNo,1,1)='I' and chAccBid='預繳' then '住院預繳'
         when Substr(chAccSeqNo,1,1)='E' then '急診'
         when Substr(chAccSeqNo,1,1)='I' then '住院'
         Else '門診'
         End
         Union All
         select vchAccDate as chAccDate
         ,case when Substr(vchAccSeqNo,1,1)='E' and vchAccBid='合約' then '急診合約'
         when Substr(vchAccSeqNo,1,1)='I' and vchAccBid='合約' then '住院合約'
         when vchAccBid='合約' then '門診合約'
         when Substr(vchAccSeqNo,1,1)='I' and vchAccBid='預繳' then '住院預繳'
         when Substr(vchAccSeqNo,1,1)='E' then '急診'
         when Substr(vchAccSeqNo,1,1)='I' then '住院'
         Else '門診'
         End
         as chAccBid
         ,sum(intAccDet) intAccDet
         ,sum(decode(rtrim(vchAccBid),'繳欠',0,intAccCash1)) intAccCash1,sum(intAccCash2) intAccCash2,sum(intAccCash3) intAccCash3,sum(intAccCash4) intAccCash4,sum(intSocFree) intSocFree,sum(intAccCash5) intAccCash5,sum(intAccCash6) intAccCash6
         ,sum(decode(rtrim(vchAccBid),'繳欠',intAccCash1,intOp2AMT50)) intOp2AMT50,sum(intOp2AMT51) intOp2AMT51,sum(intOp2AMT52) intOp2AMT52
         ,sum(intAccCash7) intAccCash7,sum(intAccCash8) intAccCash8,sum(intAccCash9) intAccCash9,sum(intAccCash10) intAccCash10
         From IpdAccCaseDayTbl
         where vchAccDate between :start_roc and :end_roc
         and vchAccMrNo not in ('C36979','1000000')
         group by vchAccDate
         ,case when Substr(vchAccSeqNo,1,1)='E' and vchAccBid='合約' then '急診合約'
         when Substr(vchAccSeqNo,1,1)='I' and vchAccBid='合約' then '住院合約'
         when vchAccBid='合約' then '門診合約'
         when Substr(vchAccSeqNo,1,1)='I' and vchAccBid='預繳' then '住院預繳'
         when Substr(vchAccSeqNo,1,1)='E' then '急診'
         when Substr(vchAccSeqNo,1,1)='I' then '住院'
         Else '門診'
         End
         )
         ,tmp_b as
         (
         select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D') as roomtype,chaccbid,'A' as cashtype,'現金收入',intacccash1 from tmp_a where nvl(intacccash1,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'B','補繳現金',intop2amt50 from tmp_a where nvl(intop2amt50,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'C','金融卡',intacccash4 from tmp_a where nvl(intacccash4,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'D','支票',intacccash2 from tmp_a where nvl(intacccash2,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'E','社服補助',intacccash3 from tmp_a where nvl(intacccash3,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'F','消費券',intsocfree from tmp_a where nvl(intsocfree,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'G','醫糾補助',intacccash5 from tmp_a where nvl(intacccash5,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'H','信用卡',intacccash6 from tmp_a where nvl(intacccash6,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'I','住院預繳',nvl(intacccash1,0)+nvl(intop2amt50,0)+nvl(intacccash4,0)+nvl(intacccash2,0)+nvl(intacccash3,0)+nvl(intsocfree,0)+nvl(intacccash5,0)+nvl(intacccash6,0) from tmp_a where chaccbid='住院預繳'
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'J','遠鑫卡',intacccash7 from tmp_a where nvl(intacccash7,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'K','振興券',intacccash8 from tmp_a where nvl(intacccash8,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'L','保險支付',intacccash9 from tmp_a where nvl(intacccash9,0)<>0
         union all select chaccdate,decode(substr(chaccbid,1,2),'門診','A','急診','B','住院','C','D'),chaccbid,'M','病人銀行帳戶扣款',intacccash10 from tmp_a where nvl(intacccash10,0)<>0
         )
         select to_char(chaccdate+19110000) as tdate,roomtype,cashtype,sum(intacccash1) as intacccash1
         From tmp_b
         group by to_char(chaccdate+19110000),roomtype,cashtype
        """;

}
