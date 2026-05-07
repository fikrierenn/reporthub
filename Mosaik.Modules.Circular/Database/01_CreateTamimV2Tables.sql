-- Mosaik.Modules.Circular Migration 01: Plan 17 v2 — DailyBlock+Circular+TamimBlok+TamimOkudu
-- Tarih: 2026-05-08
-- Plan: plans/17-tamim.md (Faz B)
-- İdempotent. Circular v1 tabloları Mosaik/Database/37 ile silinmişti.

SET NOCOUNT ON;
GO

-- 1. DailyBlock — GM çalışanlarının yazdığı duyuru/karar/uyarı/hadise/bilgi
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DailyBlock' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.DailyBlock
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GunlukBlok PRIMARY KEY,
        BlockNumber          NVARCHAR(50)  NOT NULL,
        CreatedById     INT           NOT NULL,
        Department    NVARCHAR(100) NOT NULL,
        Subject            NVARCHAR(200) NOT NULL,
        Content        NVARCHAR(MAX) NOT NULL,
        BlockDate      DATETIME2(0)  NOT NULL CONSTRAINT DF_GunlukBlok_BlokTarihi DEFAULT GETUTCDATE(),
        BlockTypeId      INT           NOT NULL,    -- Mosaik.Core DictionaryValue.Id
        Durum           INT           NOT NULL CONSTRAINT DF_GunlukBlok_Durum DEFAULT 0,
        IsUrgent            BIT           NOT NULL CONSTRAINT DF_GunlukBlok_Acil DEFAULT 0,
        RedSebebi       NVARCHAR(500) NULL,
        OnaylayanId     INT           NULL,
        OnayTarihi      DATETIME2(0)  NULL,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_GunlukBlok_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.DailyBlock olusturuldu.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_GunlukBlok_BlokNo')
    CREATE UNIQUE NONCLUSTERED INDEX UX_GunlukBlok_BlokNo ON dbo.DailyBlock (BlockNumber);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_TarihDurum')
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_TarihDurum ON dbo.DailyBlock (BlockDate, Durum);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_Olusturan')
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_Olusturan ON dbo.DailyBlock (CreatedById);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_BlokTuru')
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_BlokTuru ON dbo.DailyBlock (BlockTypeId);
GO

-- 2. Circular — günlük zarf (Body yok)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Circular' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Circular
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tamim PRIMARY KEY,
        CircularNumber         NVARCHAR(50)  NOT NULL,
        Title          NVARCHAR(200) NOT NULL,
        CircularDate     DATETIME2(0)  NOT NULL,
        PublishedAt     DATETIME2(0)  NOT NULL,
        BlokSayisi      INT           NOT NULL CONSTRAINT DF_Tamim_BlokSayisi DEFAULT 0,
        IsUrgent            BIT           NOT NULL CONSTRAINT DF_Tamim_Acil DEFAULT 0,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Tamim_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.Circular olusturuldu (zarf, Body YOK).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tamim_TamimNo')
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tamim_TamimNo ON dbo.Circular (CircularNumber);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tamim_TamimTarihi')
    CREATE NONCLUSTERED INDEX IX_Tamim_TamimTarihi ON dbo.Circular (CircularDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tamim_YayinTarihi')
    CREATE NONCLUSTERED INDEX IX_Tamim_YayinTarihi ON dbo.Circular (PublishedAt);
GO

-- 3. TamimBlok junction
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimBlok' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TamimBlok
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TamimBlok PRIMARY KEY,
        CircularId         INT           NOT NULL,
        BlockId          INT           NOT NULL,
        SiraNo          INT           NOT NULL CONSTRAINT DF_TamimBlok_SiraNo DEFAULT 0,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_TamimBlok_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT FK_TamimBlok_Tamim FOREIGN KEY (CircularId)
            REFERENCES dbo.Circular(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TamimBlok_Blok FOREIGN KEY (BlockId)
            REFERENCES dbo.DailyBlock(Id) ON DELETE NO ACTION
    );
    PRINT 'Tablo dbo.TamimBlok olusturuldu (junction).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TamimBlok_TamimBlok')
    CREATE UNIQUE NONCLUSTERED INDEX UX_TamimBlok_TamimBlok ON dbo.TamimBlok (CircularId, BlockId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TamimBlok_Tamim')
    CREATE NONCLUSTERED INDEX IX_TamimBlok_Tamim ON dbo.TamimBlok (CircularId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TamimBlok_Blok')
    CREATE NONCLUSTERED INDEX IX_TamimBlok_Blok ON dbo.TamimBlok (BlockId);
GO

-- 4. TamimOkudu — okuma logu
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimOkudu' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TamimOkudu
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TamimOkudu PRIMARY KEY,
        CircularId         INT           NOT NULL,
        UserId          INT           NOT NULL,
        IlkGorulme      DATETIME2(0)  NOT NULL CONSTRAINT DF_TamimOkudu_IlkGorulme DEFAULT GETUTCDATE(),
        Okundu          BIT           NOT NULL CONSTRAINT DF_TamimOkudu_Okundu DEFAULT 0,
        OkumaZamani     DATETIME2(0)  NULL,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_TamimOkudu_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT FK_TamimOkudu_Tamim FOREIGN KEY (CircularId)
            REFERENCES dbo.Circular(Id) ON DELETE CASCADE
    );
    PRINT 'Tablo dbo.TamimOkudu olusturuldu (okuma logu).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TamimOkudu_TamimUser')
    CREATE UNIQUE NONCLUSTERED INDEX UX_TamimOkudu_TamimUser ON dbo.TamimOkudu (CircularId, UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TamimOkudu_User')
    CREATE NONCLUSTERED INDEX IX_TamimOkudu_User ON dbo.TamimOkudu (UserId);
GO

PRINT 'Circular modul v2 migration 01 tamamlandi: 4 tablo (DailyBlock+Circular+TamimBlok+TamimOkudu).';
