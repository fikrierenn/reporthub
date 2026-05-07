-- Migration 36: Plan 17 Faz A — Tamim modülü AppModule kayıt
-- Tarih: 2026-05-08
-- Plan: plans/17-tamim.md (Faz A — iskelet)
-- Idempotent

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = N'tamim')
BEGIN
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, AssemblyName, ModuleType)
    VALUES (N'tamim', N'Tamim & Sirküler', 1, 100, N'Mosaik.Modules.Tamim', N'extension');
    PRINT 'AppModule tamim eklendi (Mosaik.Modules.Tamim, extension).';
END
ELSE
BEGIN
    -- Plan 16.6 yeni kolonları idempotent doldur (eski AppModule kaydı varsa)
    UPDATE dbo.AppModules
       SET AssemblyName = N'Mosaik.Modules.Tamim',
           ModuleType   = N'extension'
     WHERE ModuleKey    = N'tamim'
       AND (AssemblyName IS NULL OR ModuleType <> N'extension');
    PRINT 'AppModule tamim zaten mevcut, AssemblyName + ModuleType güncellendi.';
END
GO

PRINT 'Migration 36 tamamlandi.';
