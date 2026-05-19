-- Migration 63: ModuleRoleAccess — N-2 DB-driven modül yetki kontrolü
-- Hangi rolün hangi modüle erişebildiğini tanımlar.
-- ModuleId = AppModules.ModuleId. RoleName = Roles.Name.
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ModuleRoleAccess'
)
BEGIN
    CREATE TABLE ModuleRoleAccess (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        ModuleId    INT NOT NULL REFERENCES AppModules(ModuleId) ON DELETE CASCADE,
        RoleName    NVARCHAR(50) NOT NULL,
        CONSTRAINT UQ_ModuleRoleAccess UNIQUE (ModuleId, RoleName)
    );
END

-- Seed: compliance (5), contracts (7), obligations (8), ai (6) → admin + mali
-- reports (1), dashboards (2) → herkese açık (ModuleRoleAccess kaydı yok = açık)
IF NOT EXISTS (SELECT 1 FROM ModuleRoleAccess WHERE ModuleId = 5 AND RoleName = 'admin')
BEGIN
    INSERT INTO ModuleRoleAccess (ModuleId, RoleName) VALUES
        (5, 'admin'), (5, 'mali'),
        (7, 'admin'), (7, 'mali'),
        (8, 'admin'), (8, 'mali'),
        (6, 'admin'), (6, 'mali');
END
