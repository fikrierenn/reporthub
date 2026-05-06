-- 22_DropSubeCanonical_AddDataSourceFilterUnique.sql
-- Plan 07 — Plan B (DataSource bazli filter scope).
-- Faz 5b'de eklenen canonical Sube + SubeMapping pattern'i geri saliniyor.
-- Yerine: FilterDefinition UNIQUE (FilterKey) → (DataSourceKey, FilterKey) composite.
-- Ayni 'sube' FilterKey 3 DataSource icin 3 satir; SP parametre adi sistem-bagimsiz `@sube_Filtre`.
--
-- Plan: plans/07-yetki-filter-revizyon.md (Plan B kararı 5 Mayis)
-- Onceki: 21_AddSubeCanonical.sql (geri saliniyor)
-- ROLLBACK: Migration 21 tekrar calistir (Sube + SubeMapping yeniden kurulur).

USE [PortalHUB];
GO

-- 1. SubeMapping DROP (FK once)
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SubeMapping')
BEGIN
    DROP TABLE [dbo].[SubeMapping];
    PRINT 'SubeMapping tablosu DROP edildi.';
END
ELSE
BEGIN
    PRINT 'SubeMapping zaten yok (idempotent).';
END
GO

-- 2. Sube DROP
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Sube')
BEGIN
    DROP TABLE [dbo].[Sube];
    PRINT 'Sube tablosu DROP edildi.';
END
ELSE
BEGIN
    PRINT 'Sube zaten yok (idempotent).';
END
GO

-- 3. FilterDefinition UNIQUE constraint guncelle: (FilterKey) → (DataSourceKey, FilterKey)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_FilterDefinition_Key' AND object_id = OBJECT_ID('dbo.FilterDefinition'))
BEGIN
    ALTER TABLE [dbo].[FilterDefinition] DROP CONSTRAINT UQ_FilterDefinition_Key;
    PRINT 'UQ_FilterDefinition_Key (eski tek kolon) DROP.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_FilterDefinition_DS_Key' AND object_id = OBJECT_ID('dbo.FilterDefinition'))
BEGIN
    -- DataSourceKey nullable; SQL Server'da NULL'lar UNIQUE icin tek deger gibi davranir.
    -- raporKategori (DataSourceKey=NULL) tek satir kalir, sorun yok.
    ALTER TABLE [dbo].[FilterDefinition]
        ADD CONSTRAINT UQ_FilterDefinition_DS_Key UNIQUE (DataSourceKey, FilterKey);
    PRINT 'UQ_FilterDefinition_DS_Key composite eklendi.';
END
ELSE
BEGIN
    PRINT 'UQ_FilterDefinition_DS_Key zaten var.';
END
GO

-- 4. FilterDefinition.sube canonical mode'dan PDKS'e geri donsun (Migration 21 NULL'a cevirmisti)
UPDATE dbo.FilterDefinition
SET DataSourceKey = N'PDKS',
    OptionsQuery = N'SELECT CAST(SubeNo AS varchar(10)) AS Value, SubeAd AS Label FROM vrd.SubeListe',
    UpdatedAt = GETDATE()
WHERE FilterKey = N'sube' AND DataSourceKey IS NULL;

PRINT 'FilterDefinition.sube PDKS modunda guncellendi.';
GO

-- 5. INSERT (DER, sube) — DerinSIS posMagaza, sadece magazalar (mekanTip=0)
IF NOT EXISTS (
    SELECT 1 FROM dbo.FilterDefinition
    WHERE FilterKey = N'sube' AND DataSourceKey = N'DER'
)
BEGIN
    INSERT INTO dbo.FilterDefinition (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder)
    VALUES (
        N'sube',
        N'Şube',
        N'spInjection',
        N'DER',
        N'SELECT CAST(mekanID AS varchar(10)) AS Value, mekanAd AS Label FROM dbo.posMagaza WHERE mekanTip=0',
        1,
        10
    );
    PRINT 'FilterDefinition (DER, sube) eklendi.';
END
GO

-- 6. INSERT (IK, sube) — Zirve henuz tablosu net degil; pasif olarak yer tutucu.
IF NOT EXISTS (
    SELECT 1 FROM dbo.FilterDefinition
    WHERE FilterKey = N'sube' AND DataSourceKey = N'IK'
)
BEGIN
    INSERT INTO dbo.FilterDefinition (FilterKey, Label, Scope, DataSourceKey, OptionsQuery, IsActive, DisplayOrder)
    VALUES (
        N'sube',
        N'Şube',
        N'spInjection',
        N'IK',
        NULL,
        0,  -- IsActive=0: Zirve sube tablosu netlesince OptionsQuery doldurulur ve aktiflesir.
        10
    );
    PRINT 'FilterDefinition (IK, sube) yer tutucu eklendi (IsActive=0).';
END
GO

-- 7. Backfill UserDataFilters: aktif user'lara DER ve IK sube icin '*' kayit (deny-by-default uyumlu)
INSERT INTO dbo.UserDataFilters (UserId, FilterKey, FilterValue, DataSourceKey, ReportId, CreatedAt)
SELECT u.UserId, fd.FilterKey, N'*', fd.DataSourceKey, NULL, GETDATE()
FROM dbo.Users u
CROSS JOIN dbo.FilterDefinition fd
WHERE u.IsActive = 1
  AND fd.IsActive = 1
  AND fd.FilterKey = N'sube'
  AND fd.DataSourceKey IN (N'DER', N'IK')  -- IK IsActive=0, dahil edilmiyor
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UserDataFilters x
      WHERE x.UserId = u.UserId
        AND x.FilterKey = fd.FilterKey
        AND x.DataSourceKey = fd.DataSourceKey
  );

PRINT 'UserDataFilters backfill: DER sube * eklendi (IK IsActive=0 oldugu icin atlandi).';
GO

-- 8. Dogrulama
SELECT
    'FilterDef_sube_PDKS' AS Item,
    COUNT(*) AS Adet
FROM dbo.FilterDefinition WHERE FilterKey = N'sube' AND DataSourceKey = N'PDKS'
UNION ALL
SELECT 'FilterDef_sube_DER', COUNT(*) FROM dbo.FilterDefinition WHERE FilterKey = N'sube' AND DataSourceKey = N'DER'
UNION ALL
SELECT 'FilterDef_sube_IK', COUNT(*) FROM dbo.FilterDefinition WHERE FilterKey = N'sube' AND DataSourceKey = N'IK'
UNION ALL
SELECT 'UserDataFilters_DER_sube', COUNT(*) FROM dbo.UserDataFilters WHERE FilterKey = N'sube' AND DataSourceKey = N'DER';
GO
