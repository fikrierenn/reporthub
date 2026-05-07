-- Mosaik.Modules.Tamim Migration 01: Plan 17 v2 — GunlukBlok+Tamim+TamimBlok+TamimOkudu
-- Tarih: 2026-05-08
-- Plan: plans/17-tamim.md (Faz B)
-- İdempotent. Tamim v1 tabloları Mosaik/Database/37 ile silinmişti.

SET NOCOUNT ON;
GO

-- 1. GunlukBlok — GM çalışanlarının yazdığı duyuru/karar/uyarı/hadise/bilgi
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GunlukBlok' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.GunlukBlok
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GunlukBlok PRIMARY KEY,
        BlokNo          NVARCHAR(50)  NOT NULL,
        OlusturanId     INT           NOT NULL,
        DepartmanAdi    NVARCHAR(100) NOT NULL,
        Konu            NVARCHAR(200) NOT NULL,
        Aciklama        NVARCHAR(MAX) NOT NULL,
        BlokTarihi      DATETIME2(0)  NOT NULL CONSTRAINT DF_GunlukBlok_BlokTarihi DEFAULT GETUTCDATE(),
        BlokTuruId      INT           NOT NULL,    -- Mosaik.Core DictionaryValue.Id
        Durum           INT           NOT NULL CONSTRAINT DF_GunlukBlok_Durum DEFAULT 0,
        Acil            BIT           NOT NULL CONSTRAINT DF_GunlukBlok_Acil DEFAULT 0,
        RedSebebi       NVARCHAR(500) NULL,
        OnaylayanId     INT           NULL,
        OnayTarihi      DATETIME2(0)  NULL,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_GunlukBlok_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.GunlukBlok olusturuldu.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_GunlukBlok_BlokNo')
    CREATE UNIQUE NONCLUSTERED INDEX UX_GunlukBlok_BlokNo ON dbo.GunlukBlok (BlokNo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_TarihDurum')
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_TarihDurum ON dbo.GunlukBlok (BlokTarihi, Durum);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_Olusturan')
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_Olusturan ON dbo.GunlukBlok (OlusturanId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_BlokTuru')
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_BlokTuru ON dbo.GunlukBlok (BlokTuruId);
GO

-- 2. Tamim — günlük zarf (Body yok)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tamim' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Tamim
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tamim PRIMARY KEY,
        TamimNo         NVARCHAR(50)  NOT NULL,
        Baslik          NVARCHAR(200) NOT NULL,
        TamimTarihi     DATETIME2(0)  NOT NULL,
        YayinTarihi     DATETIME2(0)  NOT NULL,
        BlokSayisi      INT           NOT NULL CONSTRAINT DF_Tamim_BlokSayisi DEFAULT 0,
        Acil            BIT           NOT NULL CONSTRAINT DF_Tamim_Acil DEFAULT 0,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Tamim_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.Tamim olusturuldu (zarf, Body YOK).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Tamim_TamimNo')
    CREATE UNIQUE NONCLUSTERED INDEX UX_Tamim_TamimNo ON dbo.Tamim (TamimNo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tamim_TamimTarihi')
    CREATE NONCLUSTERED INDEX IX_Tamim_TamimTarihi ON dbo.Tamim (TamimTarihi);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tamim_YayinTarihi')
    CREATE NONCLUSTERED INDEX IX_Tamim_YayinTarihi ON dbo.Tamim (YayinTarihi);
GO

-- 3. TamimBlok junction
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimBlok' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TamimBlok
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TamimBlok PRIMARY KEY,
        TamimId         INT           NOT NULL,
        BlokId          INT           NOT NULL,
        SiraNo          INT           NOT NULL CONSTRAINT DF_TamimBlok_SiraNo DEFAULT 0,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_TamimBlok_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT FK_TamimBlok_Tamim FOREIGN KEY (TamimId)
            REFERENCES dbo.Tamim(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TamimBlok_Blok FOREIGN KEY (BlokId)
            REFERENCES dbo.GunlukBlok(Id) ON DELETE NO ACTION
    );
    PRINT 'Tablo dbo.TamimBlok olusturuldu (junction).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TamimBlok_TamimBlok')
    CREATE UNIQUE NONCLUSTERED INDEX UX_TamimBlok_TamimBlok ON dbo.TamimBlok (TamimId, BlokId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TamimBlok_Tamim')
    CREATE NONCLUSTERED INDEX IX_TamimBlok_Tamim ON dbo.TamimBlok (TamimId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TamimBlok_Blok')
    CREATE NONCLUSTERED INDEX IX_TamimBlok_Blok ON dbo.TamimBlok (BlokId);
GO

-- 4. TamimOkudu — okuma logu
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimOkudu' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.TamimOkudu
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TamimOkudu PRIMARY KEY,
        TamimId         INT           NOT NULL,
        UserId          INT           NOT NULL,
        IlkGorulme      DATETIME2(0)  NOT NULL CONSTRAINT DF_TamimOkudu_IlkGorulme DEFAULT GETUTCDATE(),
        Okundu          BIT           NOT NULL CONSTRAINT DF_TamimOkudu_Okundu DEFAULT 0,
        OkumaZamani     DATETIME2(0)  NULL,
        CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_TamimOkudu_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2(0)  NULL,
        UpdatedBy       NVARCHAR(100) NULL,
        CONSTRAINT FK_TamimOkudu_Tamim FOREIGN KEY (TamimId)
            REFERENCES dbo.Tamim(Id) ON DELETE CASCADE
    );
    PRINT 'Tablo dbo.TamimOkudu olusturuldu (okuma logu).';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TamimOkudu_TamimUser')
    CREATE UNIQUE NONCLUSTERED INDEX UX_TamimOkudu_TamimUser ON dbo.TamimOkudu (TamimId, UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TamimOkudu_User')
    CREATE NONCLUSTERED INDEX IX_TamimOkudu_User ON dbo.TamimOkudu (UserId);
GO

PRINT 'Tamim modul v2 migration 01 tamamlandi: 4 tablo (GunlukBlok+Tamim+TamimBlok+TamimOkudu).';
