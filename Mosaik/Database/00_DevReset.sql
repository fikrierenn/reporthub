-- Mosaik Dev/Staging Reset — MASTER SCRIPT
--
-- KULLANIM (SSMS):
--   1) "master" DB'ye bağlan (Available Databases dropdown)
--   2) Query menüsü → "SQLCMD Mode" aktive et (önemli — :r komutları için zorunlu)
--   3) Bu dosyayı aç (Mosaik/Database/00_DevReset.sql)
--   4) F5 / Execute
--
-- NE YAPAR:
--   - Eski PortalHUB DB'sini DROP eder (varsa)
--   - Yeni Mosaik DB'sini oluşturur
--   - 26 migration sırayla çalıştırır
--   - Seed data + dashboard SP'lerini deploy eder
--
-- DİKKAT: PortalHUB'daki tüm data SİLİNECEK. Dev/staging için. Prod için kullanma.

:setvar __MosaikScriptDir "D:\Dev\reporthub\Mosaik\Database"

-- ====================================================
-- 0. Master cleanup (önceki başarısız reset master DB'ye kirlilik bıraktı)
-- ====================================================
USE master;
GO

-- FK bağımlı tablolar önce
IF OBJECT_ID('master.dbo.UserDataFilters', 'U') IS NOT NULL DROP TABLE master.dbo.UserDataFilters;
IF OBJECT_ID('master.dbo.ReportFavorites', 'U') IS NOT NULL DROP TABLE master.dbo.ReportFavorites;
IF OBJECT_ID('master.dbo.ReportAllowedRoles', 'U') IS NOT NULL DROP TABLE master.dbo.ReportAllowedRoles;
IF OBJECT_ID('master.dbo.ReportGroupLinks', 'U') IS NOT NULL DROP TABLE master.dbo.ReportGroupLinks;
IF OBJECT_ID('master.dbo.ReportCategoryLinks', 'U') IS NOT NULL DROP TABLE master.dbo.ReportCategoryLinks;
IF OBJECT_ID('master.dbo.ReportGroups', 'U') IS NOT NULL DROP TABLE master.dbo.ReportGroups;
IF OBJECT_ID('master.dbo.ReportCategories', 'U') IS NOT NULL DROP TABLE master.dbo.ReportCategories;
IF OBJECT_ID('master.dbo.UserRoles', 'U') IS NOT NULL DROP TABLE master.dbo.UserRoles;
IF OBJECT_ID('master.dbo.AuditLog', 'U') IS NOT NULL DROP TABLE master.dbo.AuditLog;
IF OBJECT_ID('master.dbo.ReportCatalog', 'U') IS NOT NULL DROP TABLE master.dbo.ReportCatalog;
IF OBJECT_ID('master.dbo.FilterDefinition', 'U') IS NOT NULL DROP TABLE master.dbo.FilterDefinition;
IF OBJECT_ID('master.dbo.SubeMapping', 'U') IS NOT NULL DROP TABLE master.dbo.SubeMapping;
IF OBJECT_ID('master.dbo.Sube', 'U') IS NOT NULL DROP TABLE master.dbo.Sube;
IF OBJECT_ID('master.dbo.Users', 'U') IS NOT NULL DROP TABLE master.dbo.Users;
IF OBJECT_ID('master.dbo.Roles', 'U') IS NOT NULL DROP TABLE master.dbo.Roles;
IF OBJECT_ID('master.dbo.DataSources', 'U') IS NOT NULL DROP TABLE master.dbo.DataSources;
IF OBJECT_ID('master.dbo.ReportRunLog', 'U') IS NOT NULL DROP TABLE master.dbo.ReportRunLog;

PRINT 'Master cleanup OK';
GO

-- ====================================================
-- Migration zinciri
-- ====================================================
:r $(__MosaikScriptDir)\01_CreateDatabase.sql
:r $(__MosaikScriptDir)\02_CreateTables.sql
:r $(__MosaikScriptDir)\03_SeedData.sql
:r $(__MosaikScriptDir)\04_CreateAuditLog.sql
:r $(__MosaikScriptDir)\05_DropReportRunLog.sql
:r $(__MosaikScriptDir)\06_CreateReportFavorites.sql
:r $(__MosaikScriptDir)\07_AddReportCategory.sql
:r $(__MosaikScriptDir)\08_MigrateRolesAndCategories.sql
:r $(__MosaikScriptDir)\09_AddIsAdUser.sql
:r $(__MosaikScriptDir)\10_AddDashboardColumns.sql
:r $(__MosaikScriptDir)\11_AddDashboardConfigJson.sql
:r $(__MosaikScriptDir)\12_SeedPDKSDashboard.sql
:r $(__MosaikScriptDir)\13_CreateUserDataFilter.sql
:r $(__MosaikScriptDir)\14_SeedSatisDashboard.sql
:r $(__MosaikScriptDir)\15_NullableUserRolesCsv.sql
:r $(__MosaikScriptDir)\16_DeprecateDashboardHtml.sql
:r $(__MosaikScriptDir)\17_DropDashboardHtml.sql
:r $(__MosaikScriptDir)\18_MigrateDashboardSchemaV2.sql
:r $(__MosaikScriptDir)\19_DropUserRolesCsv.sql
:r $(__MosaikScriptDir)\20_AddFilterDefinition.sql
:r $(__MosaikScriptDir)\21_AddSubeCanonical.sql
:r $(__MosaikScriptDir)\22_DropSubeCanonical_AddDataSourceFilterUnique.sql
:r $(__MosaikScriptDir)\23_RenameRaporKategoriToRaporGrubu.sql
:r $(__MosaikScriptDir)\24_RenameCategoryToGroup.sql
:r $(__MosaikScriptDir)\25_DefaultGetUtcDate.sql
:r $(__MosaikScriptDir)\26_AddResultContract.sql
:r $(__MosaikScriptDir)\27_MigrateWidgetBinding.sql

-- NOT: sp_PdksPano (PDKS linked server) ve sp_SatisPano (DerinSISBkm)
-- external DB'lerde duruyor, Mosaik'e deploy edilmez. Kapsam dışı.

PRINT '';
PRINT '====================================================';
PRINT '  Mosaik baseline kuruldu.';
PRINT '  Connection string: appsettings.Development.json';
PRINT '  -> Initial Catalog=Mosaik';
PRINT '====================================================';
GO
