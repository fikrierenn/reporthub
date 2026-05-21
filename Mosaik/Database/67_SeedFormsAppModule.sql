-- Migration 67 — Plan 41 Faz 0: Forms modülü AppModules kaydı (2026-05-21)
-- Idempotent

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = N'forms')
BEGIN
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, AssemblyName, ModuleType)
    VALUES (N'forms', N'Formlar', 1, 250, N'Mosaik.Modules.Forms', N'extension');
    PRINT 'AppModule forms eklendi (Mosaik.Modules.Forms, extension).';
END
ELSE
BEGIN
    UPDATE dbo.AppModules
       SET AssemblyName = N'Mosaik.Modules.Forms',
           ModuleType   = N'extension'
     WHERE ModuleKey    = N'forms'
       AND (AssemblyName IS NULL OR ModuleType <> N'extension');
    PRINT 'AppModule forms zaten mevcut, güncellendi.';
END
GO

PRINT 'Migration 67 tamamlandi.';
