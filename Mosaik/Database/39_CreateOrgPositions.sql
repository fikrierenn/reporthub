-- Migration 39: Plan 20 Faz A — Organizasyon şeması (görev hiyerarşisi) tablosu
-- Tarih: 2026-05-08
-- Plan: plans/20-org-chart.md (Faz A — Backend altyapı)
-- İdempotent: çoklu çalıştırma güvenli.
-- Self-ref FK: bir görev (OrgPosition) kendi parent'ına bağlanır.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OrgPositions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.OrgPositions
    (
        Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrgPositions PRIMARY KEY,
        Code              NVARCHAR(100) NOT NULL,
        Title             NVARCHAR(150) NOT NULL,
        ParentPositionId  INT           NULL,
        DisplayOrder      INT           NOT NULL CONSTRAINT DF_OrgPositions_DisplayOrder DEFAULT 0,
        IsActive          BIT           NOT NULL CONSTRAINT DF_OrgPositions_IsActive DEFAULT 1,
        Description       NVARCHAR(500) NULL,
        CreatedAt         DATETIME2(0)  NOT NULL CONSTRAINT DF_OrgPositions_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy         NVARCHAR(100) NULL,
        UpdatedAt         DATETIME2(0)  NULL,
        UpdatedBy         NVARCHAR(100) NULL,
        CONSTRAINT FK_OrgPositions_Parent FOREIGN KEY (ParentPositionId)
            REFERENCES dbo.OrgPositions (Id)
            -- ON DELETE NO ACTION (default) — cycle/self-ref nedeniyle CASCADE yasak
    );
    PRINT 'Tablo dbo.OrgPositions olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.OrgPositions zaten mevcut, atlandi.';
GO

-- Code unique (case-insensitive default collation) — Zirve Unvan match key.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_OrgPositions_Code')
    CREATE UNIQUE NONCLUSTERED INDEX UX_OrgPositions_Code
        ON dbo.OrgPositions (Code);
GO

-- Tree traversal performans için parent index.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrgPositions_Parent')
    CREATE NONCLUSTERED INDEX IX_OrgPositions_Parent
        ON dbo.OrgPositions (ParentPositionId)
        INCLUDE (DisplayOrder, IsActive);
GO

PRINT 'Migration 39 tamamlandi: OrgPositions tablosu + UX_Code + IX_Parent.';
GO
