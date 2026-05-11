-- 55_SeedPersonelKarsilastirmaRaporu.sql
-- Plan 18A ek: Personel Karşılaştırma Özeti
-- SP: dbo.sp_PersonelKarsilastirma_Ozet — seçili tarih vs geçen yıl lokasyon karşılaştırması
-- DataSource: IK, Roller: admin + ik

DECLARE @Config NVARCHAR(MAX) = N'{
  "schemaVersion": 2,
  "resultContract": {
    "ozet": { "resultSet": 0, "required": true, "shape": "table" }
  },
  "tabs": [{
    "title": "Karşılaştırma",
    "components": [
      {
        "type": "table",
        "result": "ozet",
        "span": 4,
        "tableOptions": { "stripe": true, "stickyHeader": true, "clientSearch": true, "pageSize": 50 }
      }
    ]
  }]
}';

IF NOT EXISTS (SELECT 1 FROM dbo.ReportCatalog WHERE ProcName = N'dbo.sp_PersonelKarsilastirma_Ozet')
    INSERT INTO dbo.ReportCatalog
        (Title, Description, DataSourceKey, ProcName, ParamSchemaJson, AllowedRoles, IsActive, ReportType, DashboardConfigJson)
    VALUES (
        N'Personel Karşılaştırma Özeti',
        N'Seçili tarihe göre geçen yıl ile bu yıl personel sayısı karşılaştırması — lokasyon ve şube bazlı',
        N'IK',
        N'dbo.sp_PersonelKarsilastirma_Ozet',
        N'{"fields":[{"name":"GirilenTarih","label":"Tarih","type":"date","required":true,"help":"Karşılaştırma tarihi — geçen yıl aynı tarihle kıyaslanır"}]}',
        N'admin',
        1,
        N'dashboard',
        @Config
    );
GO

-- Rol ataması: admin + ik
INSERT INTO dbo.ReportAllowedRoles (ReportId, RoleId, CreatedAt)
SELECT rc.ReportId, ro.RoleId, GETUTCDATE()
FROM dbo.ReportCatalog rc
CROSS JOIN dbo.Roles ro
WHERE rc.ProcName = N'dbo.sp_PersonelKarsilastirma_Ozet'
  AND ro.Name IN (N'admin', N'ik')
  AND NOT EXISTS (
    SELECT 1 FROM dbo.ReportAllowedRoles rar
    WHERE rar.ReportId = rc.ReportId AND rar.RoleId = ro.RoleId
  );
GO
