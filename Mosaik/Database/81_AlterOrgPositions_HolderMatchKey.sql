-- Migration 81: Plan 55 Faz 1 — OrgPositions holder + Zirve match-key kolonları
-- Tarih: 2026-07-03
-- Plan: plans/55-bkm-org-structure-import.md (Faz 1)
-- Amaç: org.json (BKM gerçek org yapısı) OrgPositions'a aktarılabilsin diye 3 kolon:
--   HolderName        — org.json `name` (pozisyonun resmi/dondurulmuş sahibi kişi)
--   HolderPersonelno  — ileride Zirve kişi (GorevPersonelMap) bağı için (şimdilik NULL)
--   ZirveMatchKey     — temiz ünvan; incumbent match Code yerine buna taşınır (Faz 2).
--                       Code Faz 4'te org.json id suffix'i alacağı için match kırılmasın.
-- İdempotent: çoklu çalıştırma güvenli.

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrgPositions') AND name = 'HolderName')
    ALTER TABLE dbo.OrgPositions ADD HolderName NVARCHAR(150) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrgPositions') AND name = 'HolderPersonelno')
    ALTER TABLE dbo.OrgPositions ADD HolderPersonelno NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.OrgPositions') AND name = 'ZirveMatchKey')
    ALTER TABLE dbo.OrgPositions ADD ZirveMatchKey NVARCHAR(150) NULL;
GO

-- Mevcut satırlar için match-key backfill = Title (eski davranış korunur; Faz 2'de service
-- Code yerine ZirveMatchKey'e bakınca incumbent eşleşmesi aynen sürer).
UPDATE dbo.OrgPositions
SET ZirveMatchKey = Title
WHERE ZirveMatchKey IS NULL;
GO

PRINT 'Migration 81 tamamlandi: OrgPositions + HolderName + HolderPersonelno + ZirveMatchKey (backfill=Title).';
GO
