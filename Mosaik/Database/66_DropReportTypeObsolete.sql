-- Migration 66 (21 Mayıs 2026)
-- ADR-009: ReportType ayrımı tamamen kaldırıldı. Tüm raporlar dashboard.
-- M-11 F-1.5 alt-commit 1 [Obsolete] (önceki commit'ler), alt-commit 3 kolon DROP (bu migration).
--
-- Kod tarafında ReportType property sileliyor (ReportCatalog + MosaikContext + ReportManagementService + AdminController).
-- Bu migration DB kolonunu kaldırır.
--
-- Migration 18 Adım B'de stale ReportType='table' raporları zaten dashboard config'e dönüştürülmüştü;
-- ReportType kolonu hâlâ default 'dashboard' tutuyor ama hiç okunmuyor. Drop güvenli.
--
-- ROLLBACK: Kolonu yeniden oluşturmak mümkün (NVARCHAR(20) NOT NULL DEFAULT 'dashboard');
-- veri kaybı yok (tüm değerler 'dashboard' veya stale 'table' — anlamlı bilgi içermiyor).

USE [Mosaik];
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ReportCatalog]')
      AND name = N'ReportType'
)
BEGIN
    -- Default constraint varsa önce kaldır
    DECLARE @ConstraintName NVARCHAR(200);
    SELECT @ConstraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[ReportCatalog]')
      AND c.name = N'ReportType';

    IF @ConstraintName IS NOT NULL
    BEGIN
        DECLARE @DropDefaultSql NVARCHAR(MAX) = N'ALTER TABLE [dbo].[ReportCatalog] DROP CONSTRAINT [' + @ConstraintName + N']';
        EXEC sp_executesql @DropDefaultSql;
    END

    ALTER TABLE [dbo].[ReportCatalog] DROP COLUMN [ReportType];

    -- AuditLog
    INSERT INTO [dbo].[AuditLog] ([Id], [Username], [EventType], [TargetType], [TargetKey], [Description], [OldValuesJson], [NewValuesJson], [Success], [Timestamp])
    VALUES (NEWID(), 'system', 'schema_migration', 'table', 'ReportCatalog',
            'Migration 66: ReportType column dropped (ADR-009 consolidation final step)',
            '{"ReportType":"NVARCHAR(20) NOT NULL DEFAULT dashboard"}',
            NULL, 1, GETUTCDATE());

    PRINT 'Migration 66: ReportCatalog.ReportType column dropped.';
END
ELSE
BEGIN
    PRINT 'Migration 66: ReportCatalog.ReportType already removed (idempotent skip).';
END
GO
