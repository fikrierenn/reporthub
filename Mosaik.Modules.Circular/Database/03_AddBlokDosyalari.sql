-- Plan 17 Faz E — BlockFile tablosu (file ekleme)
-- Tarih: 2026-05-08
-- Bağımlılık: 01_CreateTamimV2Tables.sql (DailyBlock)

IF OBJECT_ID('dbo.BlockFile', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BlockFile (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        BlockId          INT NOT NULL,
        FileName        NVARCHAR(255) NOT NULL,
        FilePath       NVARCHAR(500) NOT NULL,
        Extension          NVARCHAR(10)  NOT NULL,
        MimeType        NVARCHAR(100) NULL,
        FileSize           BIGINT NOT NULL DEFAULT 0,
        UploadedById      INT NOT NULL,
        UploadedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy       NVARCHAR(100) NULL,
        UpdatedAt       DATETIME2 NULL,
        UpdatedBy       NVARCHAR(100) NULL
    );

    CREATE INDEX IX_BlokDosyasi_BlokId         ON dbo.BlockFile(BlockId);
    CREATE INDEX IX_BlokDosyasi_BlokId_Active  ON dbo.BlockFile(BlockId, IsActive);

    PRINT 'BlockFile tablosu oluşturuldu.';
END
ELSE
BEGIN
    PRINT 'BlockFile tablosu zaten mevcut, atlandı.';
END
GO

-- DosyaTuru lookup (Mosaik.Core DictionaryTypes + Values)
-- Whitelist: pdf/docx/xlsx/jpg/png/txt — service de aynı listeyi tutar
IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryTypes WHERE Code = 'fileType')
BEGIN
    INSERT INTO dbo.DictionaryTypes (Code, Name, IsActive, CreatedAt)
    VALUES ('fileType', 'Dosya Türü', 1, SYSUTCDATETIME());
    PRINT 'DictionaryType: fileType eklendi.';
END
GO

DECLARE @typeId INT = (SELECT Id FROM dbo.DictionaryTypes WHERE Code = 'fileType');

IF @typeId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryValues WHERE TypeId = @typeId AND Code = 'pdf')
        INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive, CreatedAt)
        VALUES (@typeId, 'pdf', 'PDF Belge', 1, 1, SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryValues WHERE TypeId = @typeId AND Code = 'docx')
        INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive, CreatedAt)
        VALUES (@typeId, 'docx', 'Word Belgesi', 2, 1, SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryValues WHERE TypeId = @typeId AND Code = 'xlsx')
        INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive, CreatedAt)
        VALUES (@typeId, 'xlsx', 'Excel Belgesi', 3, 1, SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryValues WHERE TypeId = @typeId AND Code = 'jpg')
        INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive, CreatedAt)
        VALUES (@typeId, 'jpg', 'JPG Görsel', 4, 1, SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryValues WHERE TypeId = @typeId AND Code = 'png')
        INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive, CreatedAt)
        VALUES (@typeId, 'png', 'PNG Görsel', 5, 1, SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryValues WHERE TypeId = @typeId AND Code = 'txt')
        INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive, CreatedAt)
        VALUES (@typeId, 'txt', 'Metin Belgesi', 6, 1, SYSUTCDATETIME());

    PRINT 'DosyaTuru lookup değerleri seed edildi.';
END
GO
