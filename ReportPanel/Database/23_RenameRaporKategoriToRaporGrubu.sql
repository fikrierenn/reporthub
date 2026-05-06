-- 23_RenameRaporKategoriToRaporGrubu.sql
-- Plan 07 Faz 7 ek: raporKategori → raporGrubu rename.
-- 'raporKategori' urunKategori (satış kategorisi) ile karışıyordu; raporGrubu daha net.
--
-- Plan: plans/07-yetki-filter-revizyon.md (5 Mayıs ek karar)

USE [PortalHUB];
GO

-- 1. FilterDefinition rename
UPDATE dbo.FilterDefinition
SET FilterKey = N'raporGrubu',
    Label = N'Rapor Grubu',
    UpdatedAt = GETDATE()
WHERE FilterKey = N'raporKategori';

PRINT 'FilterDefinition raporKategori → raporGrubu rename.';
GO

-- 2. UserDataFilters cascade rename
UPDATE dbo.UserDataFilters
SET FilterKey = N'raporGrubu'
WHERE FilterKey = N'raporKategori';

DECLARE @UDFRows INT = @@ROWCOUNT;
PRINT 'UserDataFilters: ' + CAST(@UDFRows AS varchar(10)) + ' satır FilterKey güncellendi.';
GO

-- 3. Doğrulama
SELECT 'FilterDefinition_raporGrubu' AS Item, COUNT(*) AS Adet
FROM dbo.FilterDefinition WHERE FilterKey = N'raporGrubu'
UNION ALL
SELECT 'FilterDefinition_raporKategori_kalintı', COUNT(*) FROM dbo.FilterDefinition WHERE FilterKey = N'raporKategori'
UNION ALL
SELECT 'UserDataFilters_raporGrubu', COUNT(*) FROM dbo.UserDataFilters WHERE FilterKey = N'raporGrubu'
UNION ALL
SELECT 'UserDataFilters_raporKategori_kalintı', COUNT(*) FROM dbo.UserDataFilters WHERE FilterKey = N'raporKategori';
GO
