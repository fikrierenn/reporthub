-- Migration 08 — Plan 34.1: SOP status lookup seed (2026-05-23)
-- Mosaik kuralı: status değerleri DB lookup tablosunda tutulur (DictionaryType+Value),
-- C# enum yerine byte + UI label lookup'tan çekilir.
--
-- Types:
--   sopVersionStatus      — 0 Draft | 1 Pending | 2 Approved | 3 Archived
--   sopEnrichmentStatus   — 0 Pending | 1 Completed | 2 Failed | 3 Accepted
-- Idempotent.

SET NOCOUNT ON;
GO

-- sopVersionStatus ----------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryTypes WHERE Code = N'sopVersionStatus')
BEGIN
    INSERT INTO dbo.DictionaryTypes (Code, Name, Description, IsActive)
    VALUES (N'sopVersionStatus', N'SOP Versiyon Durumu',
            N'Plan 34: SopVersion.Status byte değerlerinin UI etiketleri', 1);
    PRINT 'DictionaryType sopVersionStatus oluşturuldu.';
END
ELSE
    PRINT 'DictionaryType sopVersionStatus zaten mevcut, atlandı.';
GO

DECLARE @VersionTypeId INT = (SELECT Id FROM dbo.DictionaryTypes WHERE Code = N'sopVersionStatus');

INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive)
SELECT v.TypeId, v.Code, v.Label, v.DisplayOrder, 1
FROM (VALUES
    (@VersionTypeId, N'0', N'Taslak',     10),
    (@VersionTypeId, N'1', N'Onayda',     20),
    (@VersionTypeId, N'2', N'Yürürlükte', 30),
    (@VersionTypeId, N'3', N'Arşiv',      40)
) v (TypeId, Code, Label, DisplayOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DictionaryValues dv
    WHERE dv.TypeId = v.TypeId AND dv.Code = v.Code
);
GO

-- sopEnrichmentStatus -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryTypes WHERE Code = N'sopEnrichmentStatus')
BEGIN
    INSERT INTO dbo.DictionaryTypes (Code, Name, Description, IsActive)
    VALUES (N'sopEnrichmentStatus', N'SOP AI İnceleme Durumu',
            N'Plan 34.1: SopEnrichmentReport.Status byte değerlerinin UI etiketleri', 1);
    PRINT 'DictionaryType sopEnrichmentStatus oluşturuldu.';
END
ELSE
    PRINT 'DictionaryType sopEnrichmentStatus zaten mevcut, atlandı.';
GO

DECLARE @EnrichmentTypeId INT = (SELECT Id FROM dbo.DictionaryTypes WHERE Code = N'sopEnrichmentStatus');

INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive)
SELECT v.TypeId, v.Code, v.Label, v.DisplayOrder, 1
FROM (VALUES
    (@EnrichmentTypeId, N'0', N'İşleniyor',     10),
    (@EnrichmentTypeId, N'1', N'Tamamlandı',    20),
    (@EnrichmentTypeId, N'2', N'Hata',          30),
    (@EnrichmentTypeId, N'3', N'Kabul edildi',  40)
) v (TypeId, Code, Label, DisplayOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DictionaryValues dv
    WHERE dv.TypeId = v.TypeId AND dv.Code = v.Code
);
GO

PRINT 'Migration 08 tamamlandi: sopVersionStatus + sopEnrichmentStatus seed edildi.';
