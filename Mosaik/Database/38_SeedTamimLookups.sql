-- Migration 38: Plan 17 v2 — BlokTuru + BildirimTuru lookup seed
-- Tarih: 2026-05-08
-- Plan: plans/17-tamim.md (v2 Faz B)
-- İdempotent

SET NOCOUNT ON;
GO

-- 1. BlokTuru DictionaryType + 5 değer
DECLARE @BlokTuruId INT;
SELECT @BlokTuruId = Id FROM dbo.DictionaryTypes WHERE Code = N'blokTuru';

IF @BlokTuruId IS NULL
BEGIN
    INSERT INTO dbo.DictionaryTypes (Code, Name, Description, IsActive)
    VALUES (N'blokTuru', N'Günlük Blok Türü', N'Tamim modülü: GunlukBlok kategorisi (Duyuru/Karar/Hadise/Uyarı/Bilgi)', 1);
    SET @BlokTuruId = SCOPE_IDENTITY();
    PRINT 'DictionaryType blokTuru olusturuldu, Id=' + CAST(@BlokTuruId AS VARCHAR(10));
END
GO

DECLARE @BlokTuruId INT = (SELECT Id FROM dbo.DictionaryTypes WHERE Code = N'blokTuru');

INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive)
SELECT v.TypeId, v.Code, v.Label, v.DisplayOrder, 1
FROM (VALUES
    (@BlokTuruId, N'duyuru', N'Duyuru',     10),
    (@BlokTuruId, N'karar',  N'Karar',      20),
    (@BlokTuruId, N'hadise', N'Hadise',     30),
    (@BlokTuruId, N'uyari',  N'Uyarı',      40),
    (@BlokTuruId, N'bilgi',  N'Bilgi Notu', 50)
) v (TypeId, Code, Label, DisplayOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DictionaryValues dv
    WHERE dv.TypeId = v.TypeId AND dv.Code = v.Code
);
GO

-- 2. BildirimTuru DictionaryType + 3 değer
IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryTypes WHERE Code = N'bildirimTuru')
BEGIN
    INSERT INTO dbo.DictionaryTypes (Code, Name, Description, IsActive)
    VALUES (N'bildirimTuru', N'Bildirim Türü', N'Mosaik bildirim sistemi kategorileri', 1);
    PRINT 'DictionaryType bildirimTuru olusturuldu.';
END
GO

DECLARE @BildirimTuruId INT = (SELECT Id FROM dbo.DictionaryTypes WHERE Code = N'bildirimTuru');

INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive)
SELECT v.TypeId, v.Code, v.Label, v.DisplayOrder, 1
FROM (VALUES
    (@BildirimTuruId, N'tamimYayinlandi',  N'Tamim Yayınlandı',     10),
    (@BildirimTuruId, N'blokOnayBekliyor', N'Blok Onay Bekliyor',   20),
    (@BildirimTuruId, N'blokReddedildi',   N'Blok Reddedildi',      30)
) v (TypeId, Code, Label, DisplayOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DictionaryValues dv
    WHERE dv.TypeId = v.TypeId AND dv.Code = v.Code
);
GO

PRINT 'Migration 38 tamamlandi: BlokTuru + BildirimTuru lookuplari seed edildi.';
