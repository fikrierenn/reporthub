-- Mosaik.Modules.Circular Migration 02: Plan 17 v2 sadeleştirme
-- Tarih: 2026-05-08
-- Plan: plans/17-tamim.md (kullanıcı: "gereksiz fazla olanları kaldırabiliriz")
-- ⚠️ DROP içeriyor — TamimBlok ve TamimOkudu tabloları boş (Faz B'de yaratıldı, kullanıcı yok)
-- TamimOkudu yerine AuditLog (EventType='tamim_okundu') kullanılacak
-- TamimBlok yerine DailyBlock.CircularId FK doğrudan bağ

SET NOCOUNT ON;
GO

-- 1. TamimBlok junction kaldır (1:N için gereksiz)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimBlok' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.TamimBlok;
    PRINT 'Tablo dbo.TamimBlok silindi (junction gereksiz, DailyBlock.CircularId FK ile bag).';
END
GO

-- 2. TamimOkudu kaldır (AuditLog yeter)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimOkudu' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.TamimOkudu;
    PRINT 'Tablo dbo.TamimOkudu silindi (AuditLog EventType=tamim_okundu kullanilacak).';
END
GO

-- 3. DailyBlock.CircularId FK ekle (junction yerine doğrudan)
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.DailyBlock') AND name = 'CircularId')
BEGIN
    ALTER TABLE dbo.DailyBlock ADD CircularId INT NULL;
    PRINT 'Kolon dbo.DailyBlock.CircularId eklendi (NULLable).';
END
GO

-- FK constraint
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GunlukBlok_Tamim')
BEGIN
    ALTER TABLE dbo.DailyBlock
        ADD CONSTRAINT FK_GunlukBlok_Tamim FOREIGN KEY (CircularId)
            REFERENCES dbo.Circular(Id) ON DELETE SET NULL;
    PRINT 'FK_GunlukBlok_Tamim eklendi.';
END
GO

-- Index CircularId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_TamimId')
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_TamimId ON dbo.DailyBlock (CircularId);
GO

-- 4. DailyBlock'tan onay akışı kolonlarını kaldır (cron otomatik, manual onay yok)
-- Önce Durum'a bağlı index'i drop et
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_TarihDurum'
           AND object_id = OBJECT_ID('dbo.DailyBlock'))
BEGIN
    DROP INDEX IX_GunlukBlok_TarihDurum ON dbo.DailyBlock;
    PRINT 'IX_GunlukBlok_TarihDurum silindi (Durum kolonu kaldirilacak).';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.DailyBlock') AND name = 'Durum')
BEGIN
    -- DEFAULT constraint önce drop
    DECLARE @df NVARCHAR(128);
    SELECT @df = dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE c.object_id = OBJECT_ID('dbo.DailyBlock') AND c.name = 'Durum';
    IF @df IS NOT NULL EXEC('ALTER TABLE dbo.DailyBlock DROP CONSTRAINT ' + @df);
    ALTER TABLE dbo.DailyBlock DROP COLUMN Durum;
    PRINT 'DailyBlock.Durum kolonu silindi (CircularId NULL/NOT NULL ile durum bilgisi).';
END
GO

-- Yeni index CircularId+BlockDate (Durum yerine CircularId IS NULL/NOT NULL kullanılır)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_TarihTamim'
               AND object_id = OBJECT_ID('dbo.DailyBlock'))
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_TarihTamim ON dbo.DailyBlock (BlockDate, CircularId);
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DailyBlock') AND name = 'RedSebebi')
BEGIN
    ALTER TABLE dbo.DailyBlock DROP COLUMN RedSebebi;
    PRINT 'DailyBlock.RedSebebi silindi (onay akisi yok).';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DailyBlock') AND name = 'OnaylayanId')
BEGIN
    ALTER TABLE dbo.DailyBlock DROP COLUMN OnaylayanId;
    PRINT 'DailyBlock.OnaylayanId silindi.';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DailyBlock') AND name = 'OnayTarihi')
BEGIN
    ALTER TABLE dbo.DailyBlock DROP COLUMN OnayTarihi;
    PRINT 'DailyBlock.OnayTarihi silindi.';
END
GO

-- 5. DailyBlock.IsActive ekle (soft-delete)
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.DailyBlock') AND name = 'IsActive')
BEGIN
    ALTER TABLE dbo.DailyBlock
        ADD IsActive BIT NOT NULL CONSTRAINT DF_GunlukBlok_IsActive DEFAULT 1;
    PRINT 'DailyBlock.IsActive eklendi (soft-delete).';
END
GO

-- 6. Circular'den denormalized kolonları kaldır (COUNT/EXISTS ile hesaplanır)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Circular') AND name = 'BlokSayisi')
BEGIN
    DECLARE @df2 NVARCHAR(128);
    SELECT @df2 = dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE c.object_id = OBJECT_ID('dbo.Circular') AND c.name = 'BlokSayisi';
    IF @df2 IS NOT NULL EXEC('ALTER TABLE dbo.Circular DROP CONSTRAINT ' + @df2);
    ALTER TABLE dbo.Circular DROP COLUMN BlokSayisi;
    PRINT 'Circular.BlokSayisi silindi (denormalized — COUNT(*) ile).';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Circular') AND name = 'IsUrgent')
BEGIN
    DECLARE @df3 NVARCHAR(128);
    SELECT @df3 = dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE c.object_id = OBJECT_ID('dbo.Circular') AND c.name = 'IsUrgent';
    IF @df3 IS NOT NULL EXEC('ALTER TABLE dbo.Circular DROP CONSTRAINT ' + @df3);
    ALTER TABLE dbo.Circular DROP COLUMN IsUrgent;
    PRINT 'Circular.IsUrgent silindi (denormalized — EXISTS WHERE IsUrgent=1 ile).';
END
GO

PRINT 'Migration 02 tamamlandi: Circular modul schema sadelestirildi (4 tablo -> 2 tablo).';
