-- Mosaik.Modules.Tamim Migration 02: Plan 17 v2 sadeleştirme
-- Tarih: 2026-05-08
-- Plan: plans/17-tamim.md (kullanıcı: "gereksiz fazla olanları kaldırabiliriz")
-- ⚠️ DROP içeriyor — TamimBlok ve TamimOkudu tabloları boş (Faz B'de yaratıldı, kullanıcı yok)
-- TamimOkudu yerine AuditLog (EventType='tamim_okundu') kullanılacak
-- TamimBlok yerine GunlukBlok.TamimId FK doğrudan bağ

SET NOCOUNT ON;
GO

-- 1. TamimBlok junction kaldır (1:N için gereksiz)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimBlok' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.TamimBlok;
    PRINT 'Tablo dbo.TamimBlok silindi (junction gereksiz, GunlukBlok.TamimId FK ile bag).';
END
GO

-- 2. TamimOkudu kaldır (AuditLog yeter)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimOkudu' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.TamimOkudu;
    PRINT 'Tablo dbo.TamimOkudu silindi (AuditLog EventType=tamim_okundu kullanilacak).';
END
GO

-- 3. GunlukBlok.TamimId FK ekle (junction yerine doğrudan)
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.GunlukBlok') AND name = 'TamimId')
BEGIN
    ALTER TABLE dbo.GunlukBlok ADD TamimId INT NULL;
    PRINT 'Kolon dbo.GunlukBlok.TamimId eklendi (NULLable).';
END
GO

-- FK constraint
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GunlukBlok_Tamim')
BEGIN
    ALTER TABLE dbo.GunlukBlok
        ADD CONSTRAINT FK_GunlukBlok_Tamim FOREIGN KEY (TamimId)
            REFERENCES dbo.Tamim(Id) ON DELETE SET NULL;
    PRINT 'FK_GunlukBlok_Tamim eklendi.';
END
GO

-- Index TamimId
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_TamimId')
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_TamimId ON dbo.GunlukBlok (TamimId);
GO

-- 4. GunlukBlok'tan onay akışı kolonlarını kaldır (cron otomatik, manual onay yok)
-- Önce Durum'a bağlı index'i drop et
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_TarihDurum'
           AND object_id = OBJECT_ID('dbo.GunlukBlok'))
BEGIN
    DROP INDEX IX_GunlukBlok_TarihDurum ON dbo.GunlukBlok;
    PRINT 'IX_GunlukBlok_TarihDurum silindi (Durum kolonu kaldirilacak).';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.GunlukBlok') AND name = 'Durum')
BEGIN
    -- DEFAULT constraint önce drop
    DECLARE @df NVARCHAR(128);
    SELECT @df = dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE c.object_id = OBJECT_ID('dbo.GunlukBlok') AND c.name = 'Durum';
    IF @df IS NOT NULL EXEC('ALTER TABLE dbo.GunlukBlok DROP CONSTRAINT ' + @df);
    ALTER TABLE dbo.GunlukBlok DROP COLUMN Durum;
    PRINT 'GunlukBlok.Durum kolonu silindi (TamimId NULL/NOT NULL ile durum bilgisi).';
END
GO

-- Yeni index TamimId+BlokTarihi (Durum yerine TamimId IS NULL/NOT NULL kullanılır)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GunlukBlok_TarihTamim'
               AND object_id = OBJECT_ID('dbo.GunlukBlok'))
    CREATE NONCLUSTERED INDEX IX_GunlukBlok_TarihTamim ON dbo.GunlukBlok (BlokTarihi, TamimId);
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GunlukBlok') AND name = 'RedSebebi')
BEGIN
    ALTER TABLE dbo.GunlukBlok DROP COLUMN RedSebebi;
    PRINT 'GunlukBlok.RedSebebi silindi (onay akisi yok).';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GunlukBlok') AND name = 'OnaylayanId')
BEGIN
    ALTER TABLE dbo.GunlukBlok DROP COLUMN OnaylayanId;
    PRINT 'GunlukBlok.OnaylayanId silindi.';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GunlukBlok') AND name = 'OnayTarihi')
BEGIN
    ALTER TABLE dbo.GunlukBlok DROP COLUMN OnayTarihi;
    PRINT 'GunlukBlok.OnayTarihi silindi.';
END
GO

-- 5. GunlukBlok.IsActive ekle (soft-delete)
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.GunlukBlok') AND name = 'IsActive')
BEGIN
    ALTER TABLE dbo.GunlukBlok
        ADD IsActive BIT NOT NULL CONSTRAINT DF_GunlukBlok_IsActive DEFAULT 1;
    PRINT 'GunlukBlok.IsActive eklendi (soft-delete).';
END
GO

-- 6. Tamim'den denormalized kolonları kaldır (COUNT/EXISTS ile hesaplanır)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tamim') AND name = 'BlokSayisi')
BEGIN
    DECLARE @df2 NVARCHAR(128);
    SELECT @df2 = dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE c.object_id = OBJECT_ID('dbo.Tamim') AND c.name = 'BlokSayisi';
    IF @df2 IS NOT NULL EXEC('ALTER TABLE dbo.Tamim DROP CONSTRAINT ' + @df2);
    ALTER TABLE dbo.Tamim DROP COLUMN BlokSayisi;
    PRINT 'Tamim.BlokSayisi silindi (denormalized — COUNT(*) ile).';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tamim') AND name = 'Acil')
BEGIN
    DECLARE @df3 NVARCHAR(128);
    SELECT @df3 = dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE c.object_id = OBJECT_ID('dbo.Tamim') AND c.name = 'Acil';
    IF @df3 IS NOT NULL EXEC('ALTER TABLE dbo.Tamim DROP CONSTRAINT ' + @df3);
    ALTER TABLE dbo.Tamim DROP COLUMN Acil;
    PRINT 'Tamim.Acil silindi (denormalized — EXISTS WHERE Acil=1 ile).';
END
GO

PRINT 'Migration 02 tamamlandi: Tamim modul schema sadelestirildi (4 tablo -> 2 tablo).';
