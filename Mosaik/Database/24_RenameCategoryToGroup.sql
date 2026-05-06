-- 24_RenameCategoryToGroup.sql
-- Plan 07 son aşama: ReportCategories/ReportCategoryLinks tabloları ve CategoryId kolonları
-- Group semantiğine rename. raporKategori (Faz 7) raporGrubu olunca tablo/model/UI da
-- "Kategori" yerine "Grup" oldu — semantik tutarlilik.
--
-- urunKategori (satış/ürün kategorisi) farklı bir kavram, dokunulmaz.
--
-- Plan: plans/07-yetki-filter-revizyon.md (5 Mayıs ek karar)
-- NOT: 6 Mayıs sabahı kod refactor (Models/Context/Service/ViewModel/View/Controller) ile
--      birlikte çalıştırılacak. Sırayla: önce SQL, sonra kod build + test + commit.

USE [PortalHUB];
GO

-- 1. ReportCategoryLinks → ReportGroupLinks (junction; FK'ler önce, parent sonra)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ReportCategoryLinks')
   AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ReportGroupLinks')
BEGIN
    EXEC sp_rename N'dbo.ReportCategoryLinks', N'ReportGroupLinks';
    PRINT 'ReportCategoryLinks → ReportGroupLinks rename.';
END
GO

-- 2. ReportCategories → ReportGroups
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ReportCategories')
   AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ReportGroups')
BEGIN
    EXEC sp_rename N'dbo.ReportCategories', N'ReportGroups';
    PRINT 'ReportCategories → ReportGroups rename.';
END
GO

-- 3. Kolon: ReportGroups.CategoryId → GroupId
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.ReportGroups') AND name = 'CategoryId')
BEGIN
    EXEC sp_rename N'dbo.ReportGroups.CategoryId', N'GroupId', N'COLUMN';
    PRINT 'ReportGroups.CategoryId → GroupId.';
END
GO

-- 4. Kolon: ReportGroupLinks.CategoryId → GroupId
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.ReportGroupLinks') AND name = 'CategoryId')
BEGIN
    EXEC sp_rename N'dbo.ReportGroupLinks.CategoryId', N'GroupId', N'COLUMN';
    PRINT 'ReportGroupLinks.CategoryId → GroupId.';
END
GO

-- 5. PK/FK constraint adlari (cosmetic — rename baslayacak ad ile uyumlu)
-- (Constraint isim degisikligi schema diff'ten gelebilir; islevsel etki yok, atlanabilir.)

-- 6. Doğrulama
SELECT 'ReportGroups' AS Tablo, COUNT(*) AS Adet FROM dbo.ReportGroups
UNION ALL
SELECT 'ReportGroupLinks', COUNT(*) FROM dbo.ReportGroupLinks;
GO
