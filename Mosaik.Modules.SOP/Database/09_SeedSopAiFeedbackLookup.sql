-- Migration 09 — Plan 34.1: SOP AI feedback lookup seed (2026-05-23)
-- Type: sopAiFeedback — 0 None | 1 ThumbsUp | 2 ThumbsDown
-- Mosaik kuralı: iş enum'u DictionaryType+Value'da. C# enum kaldırıldı.
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryTypes WHERE Code = N'sopAiFeedback')
BEGIN
    INSERT INTO dbo.DictionaryTypes (Code, Name, Description, IsActive)
    VALUES (N'sopAiFeedback', N'SOP AI Danışman Feedback',
            N'Plan 34.1: kullanıcının AI cevabına geri bildirimi (thumbs up/down)', 1);
    PRINT 'DictionaryType sopAiFeedback oluşturuldu.';
END
ELSE
    PRINT 'DictionaryType sopAiFeedback zaten mevcut, atlandı.';
GO

DECLARE @TypeId INT = (SELECT Id FROM dbo.DictionaryTypes WHERE Code = N'sopAiFeedback');

INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive)
SELECT v.TypeId, v.Code, v.Label, v.DisplayOrder, 1
FROM (VALUES
    (@TypeId, N'0', N'—',              10),
    (@TypeId, N'1', N'Beğenildi',      20),
    (@TypeId, N'2', N'Beğenilmedi',    30)
) v (TypeId, Code, Label, DisplayOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DictionaryValues dv
    WHERE dv.TypeId = v.TypeId AND dv.Code = v.Code
);
GO

PRINT 'Migration 09 tamamlandi: sopAiFeedback seed edildi.';
