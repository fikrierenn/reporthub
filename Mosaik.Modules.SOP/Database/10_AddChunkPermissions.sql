-- Migration 10 — Plan 44 Faz 1: SopChunks chunk-level permission guard (2026-05-26)
-- SecurityLevel: 0=public, 1=internal, 2=confidential, 3=restricted
-- AllowedRoleIds / AllowedDepartmentIds / AllowedUserIds: CSV whitelist (NULL = tüm kullanıcılar)
-- Default: Internal + NULL (geriye uyumluluk — mevcut chunk'lar erişilebilir kalır)
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.SopChunks') AND name = N'SecurityLevel'
)
BEGIN
    ALTER TABLE dbo.SopChunks
        ADD SecurityLevel        TINYINT       NOT NULL DEFAULT 1,  -- 1 = internal
            AllowedRoleIds       NVARCHAR(500) NULL,
            AllowedDepartmentIds NVARCHAR(500) NULL,
            AllowedUserIds       NVARCHAR(500) NULL;
    PRINT 'Migration 10 — SopChunks permission kolonları eklendi.';
END
ELSE
    PRINT 'SopChunks.SecurityLevel zaten mevcut, atlandı.';
GO

-- Index: FirmaId + SecurityLevel tabanlı retrieval hızlandırma.
-- FirmaId SopChunks'ta yok ama SopVersionId → SopDocument.FirmaId join ile;
-- direkt index için denormalize değil — SopVersionId üzerinden sorgulama devam eder.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SopChunks') AND name = N'IX_SopChunks_AccessScope'
)
BEGIN
    CREATE INDEX IX_SopChunks_AccessScope
        ON dbo.SopChunks (SopVersionId, SecurityLevel)
        INCLUDE (AllowedRoleIds, AllowedDepartmentIds, AllowedUserIds);
    PRINT 'Migration 10 — IX_SopChunks_AccessScope oluşturuldu.';
END
GO
