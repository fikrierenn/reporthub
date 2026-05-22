-- Migration 71 — Plan 34 Faz A: SOP modülü AppModules kaydı (2026-05-22)
-- Idempotent

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = N'sop')
BEGIN
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, AssemblyName, ModuleType)
    VALUES (N'sop', N'Prosedürler', 1, 270, N'Mosaik.Modules.SOP', N'extension');
    PRINT 'AppModule sop eklendi (Mosaik.Modules.SOP, extension).';
END
ELSE
BEGIN
    UPDATE dbo.AppModules
       SET AssemblyName = N'Mosaik.Modules.SOP',
           ModuleType   = N'extension'
     WHERE ModuleKey    = N'sop'
       AND (AssemblyName IS NULL OR ModuleType <> N'extension');
    PRINT 'AppModule sop zaten mevcut, güncellendi.';
END
GO

PRINT 'Migration 71 tamamlandi.';
