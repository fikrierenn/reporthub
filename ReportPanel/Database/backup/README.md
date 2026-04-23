# Migration backup'ları

Her ortam (dev/staging/prod) kendi backup'ını üretir. Bu klasör `.gitignore` ile `*.sql` dosyalarını git'e almaz.

## Ritüel — Migration 18 öncesi

```sql
-- PortalHUB (veya ortam DB'si) üstünde SSMS'te çalıştır:
:SETVAR BackupDate "20260424"

-- 1) Mevcut tüm rapor config'lerini UPDATE script'i olarak üret:
SELECT
  '-- Rollback için: '
  + 'UPDATE dbo.ReportCatalog SET ReportType = ''' + ISNULL(ReportType, 'NULL') + ''', '
  + 'DashboardConfigJson = '
  + ISNULL('N''' + REPLACE(DashboardConfigJson, '''', '''''') + '''', 'NULL')
  + ' WHERE ReportId = ' + CAST(ReportId AS VARCHAR) + ';'
  AS RollbackStatement
FROM dbo.ReportCatalog
ORDER BY ReportId;
```

Sonuçları `20260424_pre_m11.sql` dosyasına kaydet (bu klasör altında).

## Rollback

Migration 18 sonrası regresyon çıkarsa:

```sql
-- Transaction içinde rollback betiğini çalıştır
BEGIN TRANSACTION;
  -- 20260424_pre_m11.sql içeriği
  -- UPDATE'ler...
  -- Doğrulama: SELECT ReportId, ReportType, LEFT(DashboardConfigJson, 50) FROM dbo.ReportCatalog;
COMMIT TRANSACTION;
```

## Migration 19 (ReportType DROP) sonrası

`ReportType` kolonu kalktıktan sonra rollback için `ALTER TABLE dbo.ReportCatalog ADD ReportType NVARCHAR(20) NULL;` + backup'tan UPDATE gerekir. Manuel (solo-dev).
