-- Run in the GUID_AP01 application schema before deploying the C141 LineBot permission change.
-- Existing grants retain their current mask; LineBot starts denied for every user.
ALTER TABLE OpdAccrReportPermissionTbl
    ADD CONSTRAINT CK_OpdAccrReportPermMaskV2
    CHECK (NBTNPERMISSION BETWEEN 0 AND 31);

ALTER TABLE OpdAccrReportPermissionTbl
    DROP CONSTRAINT CK_OpdAccrReportPermMask;

ALTER TABLE OpdAccrReportPermissionTbl
    RENAME CONSTRAINT CK_OpdAccrReportPermMaskV2 TO CK_OpdAccrReportPermMask;

COMMENT ON COLUMN OpdAccrReportPermissionTbl.NBTNPERMISSION IS
    'Action mask: query=1, Excel=2, print=4, rebuild=8, C141 LineBot=16';

-- Confirm the active constraint now accepts the LineBot bit (16).
SELECT CONSTRAINT_NAME, STATUS, VALIDATED, SEARCH_CONDITION
FROM USER_CONSTRAINTS
WHERE TABLE_NAME = 'OPDACCRREPORTPERMISSIONTBL'
  AND CONSTRAINT_NAME = 'CK_OPDACCRREPORTPERMMASK';
