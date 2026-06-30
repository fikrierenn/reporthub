-- Migration 79: Sidebar IA yeniden gruplama (anlamlı menü yerleşimi).
-- Yeni gruplar: main(Ana) / reporting(Raporlama) / content(İçerik) / process(Süreçler) / org(Kurum) / system.
-- Eski: workspace / contracts / structure → kaldırıldı. forms+sop NULL → process/content'e ev verildi.
-- Unconditional SET (idempotent — değer-set). _AppLayout.cshtml groupOrder ile eşleşmeli.

-- Raporlama
UPDATE dbo.AppModules SET GroupKey = N'reporting', SortOrder = 10 WHERE ModuleKey = N'reports';
UPDATE dbo.AppModules SET GroupKey = N'reporting', SortOrder = 20 WHERE ModuleKey = N'dashboards';
UPDATE dbo.AppModules SET GroupKey = N'reporting', SortOrder = 30 WHERE ModuleKey = N'ai';

-- İçerik (bilgi/iletişim)
UPDATE dbo.AppModules SET GroupKey = N'content', SortOrder = 10 WHERE ModuleKey = N'circular';
UPDATE dbo.AppModules SET GroupKey = N'content', SortOrder = 20 WHERE ModuleKey = N'documents';
UPDATE dbo.AppModules SET GroupKey = N'content', SortOrder = 30 WHERE ModuleKey = N'sop';

-- Süreçler (sözleşme/uyum/form)
UPDATE dbo.AppModules SET GroupKey = N'process', SortOrder = 10 WHERE ModuleKey = N'contracts';
UPDATE dbo.AppModules SET GroupKey = N'process', SortOrder = 20 WHERE ModuleKey = N'obligations';
UPDATE dbo.AppModules SET GroupKey = N'process', SortOrder = 30 WHERE ModuleKey = N'compliance';
UPDATE dbo.AppModules SET GroupKey = N'process', SortOrder = 40 WHERE ModuleKey = N'forms';

-- Kurum (organizasyon/takvim)
UPDATE dbo.AppModules SET GroupKey = N'org', SortOrder = 10 WHERE ModuleKey = N'orgchart';
UPDATE dbo.AppModules SET GroupKey = N'org', SortOrder = 20 WHERE ModuleKey = N'calendar';
GO

PRINT 'Migration 79 tamamlandi: AppModules yeni IA gruplarina tasindi.';
