-- Plan 23 — AppModules.GroupKey kolonu + 4 yeni modül seed + mevcut 6 modül group ataması.
-- Group key'ler: 'main' (Ana), 'workspace' (Çalışma Alanı), 'contracts' (Sözleşmeler & Uyum),
--                'structure' (Yapı & Yapay Zeka), 'system' (Sistem — sadece admin).

-- 1. GroupKey kolonu ekle
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'GroupKey' AND Object_ID = OBJECT_ID(N'dbo.AppModules'))
BEGIN
    ALTER TABLE dbo.AppModules ADD GroupKey NVARCHAR(50) NULL;
END
GO

-- 2. Mevcut modülleri group'a ata (idempotent UPDATE)
UPDATE dbo.AppModules SET GroupKey = 'workspace' WHERE ModuleKey = 'reports'      AND (GroupKey IS NULL OR GroupKey = '');
UPDATE dbo.AppModules SET GroupKey = 'workspace' WHERE ModuleKey = 'dashboards'   AND (GroupKey IS NULL OR GroupKey = '');
UPDATE dbo.AppModules SET GroupKey = 'workspace' WHERE ModuleKey = 'tamim'        AND (GroupKey IS NULL OR GroupKey = '');
UPDATE dbo.AppModules SET GroupKey = 'workspace' WHERE ModuleKey = 'circular'     AND (GroupKey IS NULL OR GroupKey = '');
UPDATE dbo.AppModules SET GroupKey = 'workspace' WHERE ModuleKey = 'calendar'     AND (GroupKey IS NULL OR GroupKey = '');
UPDATE dbo.AppModules SET GroupKey = 'contracts' WHERE ModuleKey = 'compliance'   AND (GroupKey IS NULL OR GroupKey = '');
UPDATE dbo.AppModules SET GroupKey = 'structure' WHERE ModuleKey = 'ai'           AND (GroupKey IS NULL OR GroupKey = '');

-- 3. 4 yeni modül seed (idempotent — IF NOT EXISTS)
IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'contracts')
BEGIN
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType, GroupKey)
    VALUES (N'contracts', N'Sözleşmeler', 1, 40, N'core', N'contracts');
END

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'obligations')
BEGIN
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType, GroupKey)
    VALUES (N'obligations', N'Yükümlülükler', 1, 45, N'core', N'contracts');
END

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'orgchart')
BEGIN
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType, GroupKey)
    VALUES (N'orgchart', N'Organizasyon', 1, 50, N'core', N'structure');
END

IF NOT EXISTS (SELECT 1 FROM dbo.AppModules WHERE ModuleKey = 'documents')
BEGIN
    INSERT INTO dbo.AppModules (ModuleKey, DisplayName, IsEnabled, SortOrder, ModuleType, GroupKey)
    VALUES (N'documents', N'Dokümanlar', 1, 28, N'core', N'workspace');
END
GO
