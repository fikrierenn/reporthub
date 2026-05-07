-- Migration 37: Plan 17 Faz B yanlış pattern revert — Tamim v1 tablolarını sil
-- Tarih: 2026-05-08
-- Plan: plans/17-tamim.md (v1 yanlış pattern, v2'de doğru schema yazılacak)
-- ⚠️ DROP içeriyor. Tablolar BOŞ (yeni yaratıldı, hiç veri girilmedi).
-- yedek-almadan-silme prensibinin istisnası: zero data, 1 saat önce yaratıldı.
-- Plan 17 v2 (gelecek): GunlukBlok + Tamim (yeni schema) + TamimBlok + TamimOkudu

SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TamimReadLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.TamimReadLog;
    PRINT 'Tablo dbo.TamimReadLog silindi (FK CASCADE icin onceden).';
END
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tamim' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP TABLE dbo.Tamim;
    PRINT 'Tablo dbo.Tamim silindi.';
END
GO

PRINT 'Migration 37 tamamlandi: Plan 17 v1 tablolari temizlendi.';
