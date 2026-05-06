-- 32_ActivateFilterDefinitions.sql
-- FilterDefinition aktive + UserDataFilter backfill (varsayılan '*' = tümü).
--
-- Bağlam:
--   Migration 31 ile 3 FilterDefinition kaydı eklendi (PDKS sube, DER sube, DER urunKategori),
--   IsActive=0 ile. Aktive etmeden önce her kullanıcıya her aktif (DataSourceKey, FilterKey)
--   için UserDataFilter kaydı şart — yoksa UserDataFilterInjector 403 atar.
--
-- Strateji:
--   1. Backfill: her user × her FilterDefinition → UserDataFilter ('*' = tümü).
--   2. IsActive=1 — injector zorunlu kontrolü etkinleşir.
--
-- Production geçişi:
--   Backfill sonrası admin GUI'den her kullanıcının erişim kapsamı düzenlenir
--   (örn. ik kullanıcısı sadece İstanbul mağazaları → '34,35,36').

-- =========================================================================
-- 1. UserDataFilter backfill — eksik (User × FilterDefinition) kombinasyonları
-- =========================================================================
INSERT INTO dbo.UserDataFilters (UserId, FilterKey, FilterValue, DataSourceKey, ReportId, CreatedAt)
SELECT u.UserId, fd.FilterKey, N'*', fd.DataSourceKey, NULL, GETUTCDATE()
FROM dbo.Users u
CROSS JOIN dbo.FilterDefinition fd
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.UserDataFilters udf
    WHERE udf.UserId = u.UserId
      AND udf.FilterKey = fd.FilterKey
      AND ISNULL(udf.DataSourceKey, N'') = ISNULL(fd.DataSourceKey, N'')
      AND udf.ReportId IS NULL
);
GO

-- =========================================================================
-- 2. FilterDefinition aktive et (idempotent: IsActive=0 olanları 1 yap)
-- =========================================================================
UPDATE dbo.FilterDefinition SET IsActive = 1 WHERE IsActive = 0;
GO
