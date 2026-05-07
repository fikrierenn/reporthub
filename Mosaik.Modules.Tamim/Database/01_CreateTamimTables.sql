-- Migration: Plan 17 Faz B — Tamim modül tabloları
-- Modül: Mosaik.Modules.Tamim
-- Tarih: 2026-05-08
-- İdempotent

SET NOCOUNT ON;
GO

-- 1. Tamim ana tablo
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tamim' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Tamim
    (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tamim PRIMARY KEY,
        Title         NVARCHAR(200) NOT NULL,
        Body          NVARCHAR(MAX) NOT NULL,
        Status        INT           NOT NULL CONSTRAINT DF_Tamim_Status DEFAULT 0,  -- Draft
        PublishDate   DATETIME2(0)  NULL,
        ExpiresAt     DATETIME2(0)  NULL,
        CreatedById   INT           NOT NULL,
        ApprovedById  INT           NULL,
        CreatedAt     DATETIME2(0)  NOT NULL CONSTRAINT DF_Tamim_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy     NVARCHAR(100) NULL,
        UpdatedAt     DATETIME2(0)  NULL,
        UpdatedBy     NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.Tamim olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.Tamim zaten mevcut, atlandi.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tamim_Status')
    CREATE NONCLUSTERED INDEX IX_Tamim_Status ON dbo.Tamim(Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tamim_StatusPublish')
    CREATE NONCLUSTERED INDEX IX_Tamim_StatusPublish ON dbo.Tamim(Status, PublishDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tamim_CreatedById')
    CREATE NONCLUSTERED INDEX IX_Tamim_CreatedById ON dbo.Tamim(CreatedById);
GO

-- 2. TamimReadLog — okundu kaydı (Faz D'de aktif kullanılır, Faz B'de tablo hazır)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimReadLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TamimReadLog
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TamimReadLog PRIMARY KEY,
        TamimId         INT          NOT NULL,
        UserId          INT          NOT NULL,
        ReadAt          DATETIME2(0) NOT NULL CONSTRAINT DF_TamimReadLog_ReadAt DEFAULT GETUTCDATE(),
        IsAcknowledged  BIT          NOT NULL CONSTRAINT DF_TamimReadLog_Ack DEFAULT 0,
        CreatedAt       DATETIME2(0) NOT NULL CONSTRAINT DF_TamimReadLog_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0) NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT FK_TamimReadLog_Tamim FOREIGN KEY (TamimId)
            REFERENCES dbo.Tamim(Id) ON DELETE CASCADE
    );
    PRINT 'Tablo dbo.TamimReadLog olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.TamimReadLog zaten mevcut, atlandi.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TamimReadLog_TamimUser')
    CREATE UNIQUE NONCLUSTERED INDEX UX_TamimReadLog_TamimUser
        ON dbo.TamimReadLog (TamimId, UserId);
GO

PRINT 'Tamim modul migration 01 tamamlandi.';
