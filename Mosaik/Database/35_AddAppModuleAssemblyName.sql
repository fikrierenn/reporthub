-- Migration 35: Plan 16.6 — AppModule.AssemblyName + ModuleType
-- Tarih: 2026-05-08
-- Plan: plans/16.6-module-extension-architecture.md (Faz C — AppModule schema)
-- Idempotent

SET NOCOUNT ON;
GO

-- AssemblyName kolonu (Mosaik.Modules.* DLL adı)
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.AppModules')
                 AND name = 'AssemblyName')
BEGIN
    ALTER TABLE dbo.AppModules ADD AssemblyName NVARCHAR(200) NULL;
    PRINT 'Kolon AppModules.AssemblyName eklendi.';
END
ELSE
    PRINT 'Kolon AppModules.AssemblyName zaten mevcut, atlandi.';
GO

-- ModuleType kolonu ('core' | 'extension')
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.AppModules')
                 AND name = 'ModuleType')
BEGIN
    ALTER TABLE dbo.AppModules ADD ModuleType NVARCHAR(20) NOT NULL
        CONSTRAINT DF_AppModules_ModuleType DEFAULT 'core';
    PRINT 'Kolon AppModules.ModuleType eklendi (default ''core'').';
END
ELSE
    PRINT 'Kolon AppModules.ModuleType zaten mevcut, atlandi.';
GO

PRINT 'Migration 35 tamamlandi.';
