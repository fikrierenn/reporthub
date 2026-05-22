-- Migration 04: BlockFile.FilePath wwwroot → App_Data (HIGH-1 fix)
-- Mevcut satırlar uploads/blocks/... şeklinde (WebRoot-relative) kaydedildi.
-- Yeni upload'lar App_Data/blocks/... (ContentRoot-relative).
-- Hem DB hem disk tutarlı olsun: DB satırları App_Data/ prefix'i ile güncelleniyor.
--
-- NOT: Fiziksel dosyalar hâlâ wwwroot\uploads\blocks\ altında.
-- Uygulama deploy'u öncesi şu komutu çalıştır (PowerShell):
--   Move-Item "Mosaik\wwwroot\uploads\blocks" "Mosaik\App_Data\blocks" -Force
-- Sonra wwwroot\uploads\blocks klasörünü sil.

UPDATE dbo.BlockFiles
SET FilePath = 'App_Data/' + FilePath
WHERE FilePath LIKE 'uploads/blocks/%';
