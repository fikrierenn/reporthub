-- 27_MigrateWidgetBinding.sql
-- M-10 Faz 6: PDKS sp_PdksPano + Satış bkm.sp_SatisPano widget'larında
-- legacy `resultSet: N` int index → named `result: "<contractName>"` migrate.
--
-- Plan: plans/06-m10-faz6-widget-migrate.md (Onaylandı 2026-05-06)
-- Bağlı: 26_AddResultContract.sql (resultContract isimleri)
--
-- Strateji:
--   1. PDKS contract: 0=planFiili, 1=subeOzet, 2=kpi, 3=fmTopN, 4=gecTopN,
--      5=bolumDoluluk, 6=eksikOkutma
--   2. Satış contract: 0=kpi, 1=magazaOzet, 2=ciroTrend, 3=kategoriSatis,
--      4=topUrun, 5=saatlikSatis, 6=odemeTip
--   3. Her widget için path '$.tabs[X].components[Y]' üret, JSON_MODIFY ile
--      result set + resultSet field'ını sil (NULL → lax mode field drop).
--
-- Idempotent:
--   WHERE clause `result` IS NULL AND `resultSet` IS NOT NULL — migrate edilmiş
--   widget'lar atlanır. Re-run güvenli.

USE [PortalHUB];
GO

SET NOCOUNT ON;

-- =========================================================================
-- Adım 1: Tüm legacy widget'ları topla — (ReportId, path, newName)
-- =========================================================================
DECLARE @widgets TABLE (
    ReportId int,
    Path nvarchar(200),
    NewName nvarchar(50)
);

-- PDKS: ProcName = 'sp_PdksPano'
INSERT INTO @widgets (ReportId, Path, NewName)
SELECT
    rc.ReportId,
    '$.tabs[' + t.[key] + '].components[' + c.[key] + ']',
    CASE JSON_VALUE(c.value, '$.resultSet')
        WHEN '0' THEN 'planFiili'
        WHEN '1' THEN 'subeOzet'
        WHEN '2' THEN 'kpi'
        WHEN '3' THEN 'fmTopN'
        WHEN '4' THEN 'gecTopN'
        WHEN '5' THEN 'bolumDoluluk'
        WHEN '6' THEN 'eksikOkutma'
        ELSE NULL
    END
FROM dbo.ReportCatalog rc
CROSS APPLY OPENJSON(rc.DashboardConfigJson, '$.tabs') t
CROSS APPLY OPENJSON(t.value, '$.components') c
WHERE rc.ProcName = N'sp_PdksPano'
  AND rc.IsActive = 1
  AND JSON_VALUE(c.value, '$.result') IS NULL
  AND JSON_VALUE(c.value, '$.resultSet') IS NOT NULL;

-- Satış: ProcName = 'bkm.sp_SatisPano'
INSERT INTO @widgets (ReportId, Path, NewName)
SELECT
    rc.ReportId,
    '$.tabs[' + t.[key] + '].components[' + c.[key] + ']',
    CASE JSON_VALUE(c.value, '$.resultSet')
        WHEN '0' THEN 'kpi'
        WHEN '1' THEN 'magazaOzet'
        WHEN '2' THEN 'ciroTrend'
        WHEN '3' THEN 'kategoriSatis'
        WHEN '4' THEN 'topUrun'
        WHEN '5' THEN 'saatlikSatis'
        WHEN '6' THEN 'odemeTip'
        ELSE NULL
    END
FROM dbo.ReportCatalog rc
CROSS APPLY OPENJSON(rc.DashboardConfigJson, '$.tabs') t
CROSS APPLY OPENJSON(t.value, '$.components') c
WHERE rc.ProcName = N'bkm.sp_SatisPano'
  AND rc.IsActive = 1
  AND JSON_VALUE(c.value, '$.result') IS NULL
  AND JSON_VALUE(c.value, '$.resultSet') IS NOT NULL;

-- Sanity: NULL NewName olmamalı (out-of-range resultSet sinyali)
IF EXISTS (SELECT 1 FROM @widgets WHERE NewName IS NULL)
BEGIN
    RAISERROR('Migration 27: out-of-range resultSet tespit edildi (contract 0-6 dışı). Iptal.', 16, 1);
    RETURN;
END

DECLARE @WidgetCount int = (SELECT COUNT(*) FROM @widgets);
PRINT 'Migrate edilecek widget sayisi: ' + CAST(@WidgetCount AS varchar(10));

-- =========================================================================
-- Adım 2: Cursor ile her widget'a JSON_MODIFY uygula
--   - result set
--   - resultSet sil (NULL → lax mode field drop)
-- Cursor sebebi: JSON_MODIFY array path'inde X+Y dinamik, set-based mümkün değil.
-- =========================================================================
DECLARE @reportId int, @path nvarchar(200), @newName nvarchar(50);

DECLARE widget_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT ReportId, Path, NewName FROM @widgets;

OPEN widget_cur;
FETCH NEXT FROM widget_cur INTO @reportId, @path, @newName;

WHILE @@FETCH_STATUS = 0
BEGIN
    UPDATE dbo.ReportCatalog
    SET DashboardConfigJson = JSON_MODIFY(
        JSON_MODIFY(DashboardConfigJson, @path + '.result', @newName),
        @path + '.resultSet',
        NULL
    )
    WHERE ReportId = @reportId;

    FETCH NEXT FROM widget_cur INTO @reportId, @path, @newName;
END

CLOSE widget_cur;
DEALLOCATE widget_cur;

PRINT 'Migration tamamlandi.';
GO

-- =========================================================================
-- Adım 3: Doğrulama — kaç widget named binding'e geçti, kaç legacy kaldı?
-- =========================================================================
SELECT
    rc.ReportId,
    rc.Title,
    COUNT(*) AS Total,
    SUM(CASE WHEN JSON_VALUE(c.value, '$.result') IS NOT NULL
                  AND JSON_VALUE(c.value, '$.result') NOT LIKE 'rs%' THEN 1 ELSE 0 END) AS NamedBinding,
    SUM(CASE WHEN JSON_VALUE(c.value, '$.result') LIKE 'rs%' THEN 1 ELSE 0 END) AS RsNPattern,
    SUM(CASE WHEN JSON_VALUE(c.value, '$.result') IS NULL
                  AND JSON_VALUE(c.value, '$.resultSet') IS NOT NULL THEN 1 ELSE 0 END) AS LegacyIndexOnly,
    SUM(CASE WHEN JSON_VALUE(c.value, '$.result') IS NULL
                  AND JSON_VALUE(c.value, '$.resultSet') IS NULL THEN 1 ELSE 0 END) AS NoBinding
FROM dbo.ReportCatalog rc
CROSS APPLY OPENJSON(rc.DashboardConfigJson, '$.tabs') t
CROSS APPLY OPENJSON(t.value, '$.components') c
WHERE rc.IsActive = 1
  AND rc.ProcName IN (N'sp_PdksPano', N'bkm.sp_SatisPano')
GROUP BY rc.ReportId, rc.Title;
GO
