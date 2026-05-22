-- Migration 68: AuditLog ConnString temizliği (HIGH-2 fix)
-- datasource_update event'lerinde NewValuesJson içindeki ConnString alanını kaldır.
-- Plan 41 güvenlik fix — AdminController.DataSources edit artık ConnStringChanged:true yazıyor.

UPDATE dbo.AuditLog
SET NewValuesJson = JSON_MODIFY(NewValuesJson, '$.ConnString', NULL)
WHERE EventType = 'datasource_update'
  AND JSON_VALUE(NewValuesJson, '$.ConnString') IS NOT NULL;
