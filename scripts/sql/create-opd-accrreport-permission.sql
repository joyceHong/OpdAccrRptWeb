-- Oracle 11g+; create in the GUID_AP01 application schema on DbTest3.
-- GUID_AP01 has quota on TBS_GUID/TBS_IDX_GUID, not on its default GENDATA4.
-- Grant IDs are assigned by the application under an exclusive table lock.
-- NMENUCODE stores the report code from ReportCatalogService (for example C21 or C214).
-- NBTNPERMISSION bits: query=1, Excel=2, print=4, rebuild=8, C141 LineBot=16.
-- A new grant defaults to 7 (query + Excel + print). Rebuild requires an explicit grant.
-- VCHSTAT NULL means active; DC means deleted.

CREATE TABLE OpdAccrReportPermissionTbl (
    NPERMISSIONID       NUMBER NOT NULL,
    NMENUCODE           VARCHAR2(4 CHAR) NOT NULL,
    NPERMISSIONTYPE     NUMBER(1) NOT NULL,
    VCHPERMISSIONTARGET VARCHAR2(10 CHAR) NOT NULL,
    VCHSTAT             VARCHAR2(2 CHAR),
    DCDATE              DATE DEFAULT SYSDATE NOT NULL,
    VCHCUSRID           VARCHAR2(10 CHAR) NOT NULL,
    DRECDATE            DATE,
    VCHRECID            VARCHAR2(10 CHAR),
    DDCDATE             DATE,
    VCHDCUSERID         VARCHAR2(10 CHAR),
    NBTNPERMISSION      NUMBER(2) DEFAULT 7 NOT NULL,
    CONSTRAINT PK_OpdAccrReportPermissionTbl PRIMARY KEY (NPERMISSIONID)
        USING INDEX TABLESPACE TBS_IDX_GUID,
    CONSTRAINT UQ_OpdAccrReportPermissionTbl UNIQUE
        (NMENUCODE, NPERMISSIONTYPE, VCHPERMISSIONTARGET)
        USING INDEX TABLESPACE TBS_IDX_GUID,
    CONSTRAINT CK_OpdAccrReportPermType CHECK (NPERMISSIONTYPE IN (0, 1, 2)),
    CONSTRAINT CK_OpdAccrReportPermStat CHECK (VCHSTAT IS NULL OR VCHSTAT = 'DC'),
    CONSTRAINT CK_OpdAccrReportPermMask CHECK (NBTNPERMISSION BETWEEN 0 AND 31)
) TABLESPACE TBS_GUID;

CREATE INDEX IX_OpdAccrReportPermTarget
    ON OpdAccrReportPermissionTbl (NPERMISSIONTYPE, VCHPERMISSIONTARGET, NMENUCODE)
    TABLESPACE TBS_IDX_GUID;

COMMENT ON TABLE OpdAccrReportPermissionTbl IS 'Report action grants by menu and target';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.NPERMISSIONID IS 'Permission row ID';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.NMENUCODE IS 'Report code from ReportCatalogService, such as C21 or C214';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.NPERMISSIONTYPE IS '0=administrator user, 1=department, 2=individual user';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.VCHPERMISSIONTARGET IS 'GenUserProfile1.chUserID for types 0/2; chUserSector for type 1';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.VCHSTAT IS 'NULL=active; DC=deleted';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.DCDATE IS 'Created at';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.VCHCUSRID IS 'Created by GenUserProfile1.chUserID';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.DRECDATE IS 'Updated at';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.VCHRECID IS 'Updated by GenUserProfile1.chUserID';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.DDCDATE IS 'Deleted at';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.VCHDCUSERID IS 'Deleted by GenUserProfile1.chUserID';
COMMENT ON COLUMN OpdAccrReportPermissionTbl.NBTNPERMISSION IS 'Action mask: query=1, Excel=2, print=4, rebuild=8, C141 LineBot=16';


-- Seed the first permission manager before enforcing report permissions.
-- The type 0 row also grants C1 query, Excel and print; rebuild remains denied.
INSERT INTO OpdAccrReportPermissionTbl
    (NPERMISSIONID, NMENUCODE, NPERMISSIONTYPE, VCHPERMISSIONTARGET, VCHSTAT,
     DCDATE, VCHCUSRID, NBTNPERMISSION)
VALUES
    (1, 'C1', 0, 'F1482', NULL, SYSDATE, 'F1482', 7);

COMMIT;
