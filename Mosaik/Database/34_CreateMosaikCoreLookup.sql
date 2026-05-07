-- Migration 34: Plan 16.5 Faz B — Mosaik.Core.Lookup tabloları
-- Tarih: 2026-05-08
-- Plan: plans/16.5-shared-kit.md (Faz B — Lookup + DataScope)
-- İdempotent: çoklu çalıştırma güvenli

SET NOCOUNT ON;
GO

-- 1. DictionaryTypes — kategori sözlüğü (departman/pozisyon/durum/öncelik vs.)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DictionaryTypes' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.DictionaryTypes
    (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DictionaryTypes PRIMARY KEY,
        Code         NVARCHAR(50)  NOT NULL,
        Name         NVARCHAR(100) NOT NULL,
        Description  NVARCHAR(500) NULL,
        IsActive     BIT           NOT NULL CONSTRAINT DF_DictionaryTypes_IsActive DEFAULT 1,
        CreatedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_DictionaryTypes_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy    NVARCHAR(100) NULL,
        UpdatedAt    DATETIME2(0)  NULL,
        UpdatedBy    NVARCHAR(100) NULL
    );
    PRINT 'Tablo dbo.DictionaryTypes olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.DictionaryTypes zaten mevcut, atlandi.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DictionaryTypes_Code')
    CREATE UNIQUE NONCLUSTERED INDEX UX_DictionaryTypes_Code
        ON dbo.DictionaryTypes (Code);
GO

-- 2. DictionaryValues — kategori altındaki seçenekler (FSM/ÖZLÜCE/HEYKEL vs.)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DictionaryValues' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.DictionaryValues
    (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DictionaryValues PRIMARY KEY,
        TypeId        INT           NOT NULL,
        Code          NVARCHAR(50)  NOT NULL,
        Label         NVARCHAR(200) NOT NULL,
        DisplayOrder  INT           NOT NULL CONSTRAINT DF_DictionaryValues_DisplayOrder DEFAULT 0,
        IsActive      BIT           NOT NULL CONSTRAINT DF_DictionaryValues_IsActive DEFAULT 1,
        CreatedAt     DATETIME2(0)  NOT NULL CONSTRAINT DF_DictionaryValues_CreatedAt DEFAULT GETUTCDATE(),
        CreatedBy     NVARCHAR(100) NULL,
        UpdatedAt     DATETIME2(0)  NULL,
        UpdatedBy     NVARCHAR(100) NULL,
        CONSTRAINT FK_DictionaryValues_Type FOREIGN KEY (TypeId)
            REFERENCES dbo.DictionaryTypes(Id) ON DELETE CASCADE
    );
    PRINT 'Tablo dbo.DictionaryValues olusturuldu.';
END
ELSE
    PRINT 'Tablo dbo.DictionaryValues zaten mevcut, atlandi.';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DictionaryValues_TypeCode')
    CREATE UNIQUE NONCLUSTERED INDEX UX_DictionaryValues_TypeCode
        ON dbo.DictionaryValues (TypeId, Code);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DictionaryValues_TypeActive')
    CREATE NONCLUSTERED INDEX IX_DictionaryValues_TypeActive
        ON dbo.DictionaryValues (TypeId, IsActive, DisplayOrder);
GO

PRINT 'Migration 34 tamamlandi: DictionaryTypes + DictionaryValues.';
