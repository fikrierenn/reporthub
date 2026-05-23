-- Migration 72: Tamim modül anahtarı + AssemblyName tutarsızlığı düzeltme
-- Tarih: 2026-05-25
-- Plan: kod ↔ DB hizalama (audit 2026-05-25)
--
-- Sebep:
--   - Migration 36 `tamim` ModuleKey seed etti (AssemblyName=Mosaik.Modules.Tamim).
--   - Plan 17 Faz J'de proje "Tamim" → "Circular" rename edildi (CircularModule.ModuleKey="circular").
--   - DB elle UPDATE'lendi (ModuleKey=tamim → circular) ama AssemblyName güncellenmedi.
--   - Fresh DB seed'inde Migration 36 yeniden `tamim` row ekleyebilir.
--
-- Bu migration:
--   1. `tamim` row varsa `circular`'a rename (duplicate ise tamim sil)
--   2. ModuleKey=circular row'unun AssemblyName + DisplayName + ModuleType doğrula
--   3. Idempotent — her koşumda tekrarlanabilir
--
-- Not: Migration 36 dokunulmuyor (geçmiş migration ezme yok); bu migration sonradan düzeltici.

SET NOCOUNT ON;
GO

DECLARE @tamimExists BIT = CASE WHEN EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = N'tamim') THEN 1 ELSE 0 END;
DECLARE @circularExists BIT = CASE WHEN EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = N'circular') THEN 1 ELSE 0 END;

IF @tamimExists = 1 AND @circularExists = 0
BEGIN
    -- Sadece tamim var → circular'a rename
    UPDATE dbo.AppModules
       SET ModuleKey    = N'circular',
           DisplayName  = N'Tamim & Sirküler',
           AssemblyName = N'Mosaik.Modules.Circular',
           ModuleType   = N'extension'
     WHERE ModuleKey    = N'tamim';
    PRINT 'AppModule tamim → circular rename + AssemblyName düzeltildi.';
END
ELSE IF @tamimExists = 1 AND @circularExists = 1
BEGIN
    -- İkisi de var (legacy + manuel insert) → tamim sil, circular doğrula
    DELETE FROM dbo.AppModules WHERE ModuleKey = N'tamim';
    PRINT 'AppModule tamim (duplicate) silindi.';
END
ELSE IF @tamimExists = 0 AND @circularExists = 0
BEGIN
    -- Hiçbiri yok → fresh seed
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, AssemblyName, ModuleType, GroupKey)
    VALUES (N'circular', N'Tamim & Sirküler', 1, 100, N'Mosaik.Modules.Circular', N'extension', N'workspace');
    PRINT 'AppModule circular fresh seed yapıldı.';
END
GO

-- AssemblyName + ModuleType + GroupKey doğrulama (her koşumda)
UPDATE dbo.AppModules
   SET AssemblyName = N'Mosaik.Modules.Circular'
 WHERE ModuleKey    = N'circular'
   AND AssemblyName <> N'Mosaik.Modules.Circular';
GO

UPDATE dbo.AppModules
   SET ModuleType = N'extension'
 WHERE ModuleKey  = N'circular'
   AND (ModuleType IS NULL OR ModuleType <> N'extension');
GO

UPDATE dbo.AppModules
   SET GroupKey = N'workspace'
 WHERE ModuleKey = N'circular'
   AND (GroupKey IS NULL OR GroupKey = '');
GO

PRINT 'Migration 72 tamamlandi.';
