-- 26_AddResultContract.sql
-- M-10 Faz 5: PDKS sp_PdksPano (7 RS) + Satış bkm.sp_SatisPano (7 RS) için
-- DashboardConfigJson'a `resultContract` idempotent ekle.
--
-- Naming: camelCase (Faz 1 kuralı).
-- Required: SP'nin "her zaman dolu döndüğü" RS'ler true (KPI satırı, ana detay/özet).
-- Optional: TOP N veya kategori dağılımı gibi (boş dönebilir) RS'ler false.
--
-- Plan: TODO M-10 Faz 5; ADR-007.
-- Idempotent: resultContract zaten varsa atla (WHERE JSON_QUERY ... IS NULL).

USE [PortalHUB];
GO

-- 1. PDKS sp_PdksPano (RS0..RS6)
UPDATE dbo.ReportCatalog
SET DashboardConfigJson = JSON_MODIFY(
    DashboardConfigJson,
    '$.resultContract',
    JSON_QUERY(N'{
      "planFiili":     { "resultSet": 0, "required": true,  "shape": "table" },
      "subeOzet":      { "resultSet": 1, "required": true,  "shape": "table" },
      "kpi":           { "resultSet": 2, "required": true,  "shape": "row"   },
      "fmTopN":        { "resultSet": 3, "required": false, "shape": "table" },
      "gecTopN":       { "resultSet": 4, "required": false, "shape": "table" },
      "bolumDoluluk":  { "resultSet": 5, "required": false, "shape": "table" },
      "eksikOkutma":   { "resultSet": 6, "required": false, "shape": "table" }
    }')
)
WHERE ProcName = N'sp_PdksPano'
  AND JSON_QUERY(DashboardConfigJson, '$.resultContract') IS NULL;

DECLARE @PdksRows INT = @@ROWCOUNT;
PRINT 'PDKS resultContract: ' + CAST(@PdksRows AS varchar(10)) + ' rapor guncellendi.';
GO

-- 2. Satış bkm.sp_SatisPano (RS0..RS6)
UPDATE dbo.ReportCatalog
SET DashboardConfigJson = JSON_MODIFY(
    DashboardConfigJson,
    '$.resultContract',
    JSON_QUERY(N'{
      "kpi":           { "resultSet": 0, "required": true,  "shape": "row"   },
      "magazaOzet":    { "resultSet": 1, "required": true,  "shape": "table" },
      "ciroTrend":     { "resultSet": 2, "required": false, "shape": "table" },
      "kategoriSatis": { "resultSet": 3, "required": false, "shape": "table" },
      "topUrun":       { "resultSet": 4, "required": false, "shape": "table" },
      "saatlikSatis":  { "resultSet": 5, "required": false, "shape": "table" },
      "odemeTip":      { "resultSet": 6, "required": false, "shape": "table" }
    }')
)
WHERE ProcName = N'bkm.sp_SatisPano'
  AND JSON_QUERY(DashboardConfigJson, '$.resultContract') IS NULL;

DECLARE @SatisRows INT = @@ROWCOUNT;
PRINT 'Satis resultContract: ' + CAST(@SatisRows AS varchar(10)) + ' rapor guncellendi.';
GO

-- 3. Doğrulama — resultContract eklendi mi?
SELECT ReportId, Title, ProcName,
       CASE WHEN JSON_QUERY(DashboardConfigJson, '$.resultContract') IS NOT NULL
            THEN 'VAR' ELSE 'YOK' END AS ResultContract,
       LEN(DashboardConfigJson) AS Len
FROM dbo.ReportCatalog
WHERE IsActive = 1 AND ProcName IN (N'sp_PdksPano', N'bkm.sp_SatisPano');
GO
