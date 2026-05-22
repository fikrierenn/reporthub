-- Migration 02 — Plan 34 Faz B: SOP Category lookup seed (2026-05-22)
-- DictionaryType: sopCategory, 6 default değer (İK/Operasyon/IT/Finans/Kalite/Etik).
-- SopDocument.Category NVARCHAR(80) — lookup Label'a serbest referans (FK yok, esnek).
-- Idempotent.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DictionaryTypes WHERE Code = N'sopCategory')
BEGIN
    INSERT INTO dbo.DictionaryTypes (Code, Name, Description, IsActive)
    VALUES (N'sopCategory', N'SOP Kategorisi', N'Plan 34: prosedür kategorileri (İK/Operasyon/IT/Finans/Kalite/Etik)', 1);
    PRINT 'DictionaryType sopCategory oluşturuldu.';
END
ELSE
    PRINT 'DictionaryType sopCategory zaten mevcut, atlandı.';
GO

DECLARE @SopCategoryId INT = (SELECT Id FROM dbo.DictionaryTypes WHERE Code = N'sopCategory');

INSERT INTO dbo.DictionaryValues (TypeId, Code, Label, DisplayOrder, IsActive)
SELECT v.TypeId, v.Code, v.Label, v.DisplayOrder, 1
FROM (VALUES
    (@SopCategoryId, N'ik',         N'İK',         10),
    (@SopCategoryId, N'operasyon',  N'Operasyon',  20),
    (@SopCategoryId, N'it',         N'IT',         30),
    (@SopCategoryId, N'finans',     N'Finans',     40),
    (@SopCategoryId, N'kalite',     N'Kalite',     50),
    (@SopCategoryId, N'etik',       N'Etik',       60)
) v (TypeId, Code, Label, DisplayOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.DictionaryValues dv
    WHERE dv.TypeId = v.TypeId AND dv.Code = v.Code
);
GO

PRINT 'Migration 02 tamamlandi: sopCategory lookup seed edildi.';
