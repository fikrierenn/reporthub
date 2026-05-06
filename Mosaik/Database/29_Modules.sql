-- 29_Modules.sql
-- Plan 12 Faz 3a: Uygulama modülleri tablosu — sidebar toggle.
-- ModuleKey sabit kod sabiti, DisplayName UI'da gösterilir.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AppModules')
BEGIN
    CREATE TABLE dbo.AppModules (
        ModuleId    INT             NOT NULL IDENTITY(1,1) CONSTRAINT PK_AppModules PRIMARY KEY,
        ModuleKey   NVARCHAR(50)    NOT NULL CONSTRAINT UQ_AppModules_ModuleKey UNIQUE,
        DisplayName NVARCHAR(100)   NOT NULL,
        IsEnabled   BIT             NOT NULL DEFAULT 1,
        SortOrder   INT             NOT NULL DEFAULT 0
    );
END
GO

-- Varsayılan modüller — yoksa ekle
IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'reports')
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder)
    VALUES ('reports', N'Raporlar', 1, 10);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'dashboards')
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder)
    VALUES ('dashboards', N'Panolar', 1, 20);
GO
