-- Plan 44 Faz 3 — SopDocuments permission kaynak kolonları.
-- ChunkPermissionSyncJob bu kolonları SopChunks'a senkronize eder.
-- SecurityLevel: 0=Public, 1=Internal(default), 2=Confidential, 3=Restricted
-- AllowedRoleIds/AllowedDepartmentIds/AllowedUserIds: CSV; NULL = herkes erişebilir.

ALTER TABLE dbo.SopDocuments ADD
    SecurityLevel        TINYINT       NOT NULL DEFAULT 1,
    AllowedRoleIds       NVARCHAR(500) NULL,
    AllowedDepartmentIds NVARCHAR(500) NULL,
    AllowedUserIds       NVARCHAR(500) NULL;
