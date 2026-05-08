-- Migration 40: Plan 20 Faz B — Organizasyon şeması default seed
-- Tarih: 2026-05-09
-- Kaynak: Zirve BKM_GENEL.dbo.vw_PersonelDepartman DISTINCT Unvan (2026-05-09 keşfi, plan §10)
-- 61 unvan / 271 aktif personel / 6 katmanlı hiyerarşi taslağı
-- Idempotent: aynı Code varsa INSERT atlanır, mevcut hiyerarşi ezilmez.
-- Admin GUI'de drag-drop ile ParentPositionId ve DisplayOrder güncellenir.
--
-- Hiyerarşi mantığı:
--   1. Üst Yönetim (Yön. Kurulu / Direktör)
--   2. Orta Yönetim (Müdür / Yönetici)
--   3. Şef / Sorumlu
--   4. Operasyonel (mağaza/kafe/depo personeli)
--   5. Uzman / Bireysel katkıcı
--   6. Stajyer / Aday

SET NOCOUNT ON;
GO

-- Helper: tek satırlık idempotent insert. Aynı Code varsa atla.
-- ParentPositionId NULL ise root, değilse Code üzerinden lookup et.
-- DisplayOrder kardeş arası sıralama (1=ilk).

-- ============================================================
-- KATMAN 1 — Üst Yönetim (root)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'YÖNETİM KURULU BAŞKANI')
    INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
    VALUES (N'YÖNETİM KURULU BAŞKANI', N'Yönetim Kurulu Başkanı', NULL, 1, 1, N'system');

DECLARE @ykbId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'YÖNETİM KURULU BAŞKANI');

INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @ykbId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'YÖNETİM KURULU BAŞKAN YARD.', N'Yönetim Kurulu Başkan Yardımcısı', 1),
    (N'İŞVEREN VEKİLİ', N'İşveren Vekili', 2),
    (N'SAHA OPERASYON DİREKTÖRÜ', N'Saha Operasyon Direktörü', 3),
    (N'MALİ İŞLER DİREKTÖRÜ', N'Mali İşler Direktörü', 4),
    (N'FİNANS DİREKTÖRÜ', N'Finans Direktörü', 5),
    (N'MALİ İŞLER YÖN.SİS. DİREKTÖRÜ', N'Mali İşler Yön. Sis. Direktörü', 6),
    (N'İÇ DENETİM VE İK MÜDÜRÜ', N'İç Denetim ve İK Müdürü', 7)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);
GO

-- ============================================================
-- KATMAN 2 — Orta Yönetim
-- ============================================================
DECLARE @sahaId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'SAHA OPERASYON DİREKTÖRÜ');
DECLARE @maliId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'MALİ İŞLER DİREKTÖRÜ');
DECLARE @icDenId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'İÇ DENETİM VE İK MÜDÜRÜ');

-- Saha Operasyon altı (mağaza/kafe yönetimi)
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @sahaId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'KAFE BÖLGE MÜDÜRÜ', N'Kafe Bölge Müdürü', 1),
    (N'MAĞAZA MÜDÜRÜ', N'Mağaza Müdürü', 2),
    (N'BİLGİ TEKNOLOJİLERİ YÖNETİCİSİ', N'Bilgi Teknolojileri Yöneticisi', 3),
    (N'İDARİ İŞLER YÖNETİCİSİ', N'İdari İşler Yöneticisi', 4),
    (N'SATIN ALMA YÖNETİCİSİ', N'Satın Alma Yöneticisi', 5)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);

-- Kafe alt zinciri
DECLARE @kafeBolgeId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'KAFE BÖLGE MÜDÜRÜ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'KAFE MÜDÜRÜ', N'Kafe Müdürü', @kafeBolgeId, 1, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'KAFE MÜDÜRÜ');

DECLARE @kafeMudurId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'KAFE MÜDÜRÜ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'KAFE YÖNETİCİSİ', N'Kafe Yöneticisi', @kafeMudurId, 1, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'KAFE YÖNETİCİSİ');

-- İK / Denetim altı (uzman + asistan)
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @icDenId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'İNSAN KAYNAKLARI UZMANI', N'İnsan Kaynakları Uzmanı', 1),
    (N'İŞ ANALİSTİ', N'İş Analisti', 2),
    (N'ÖLÇME VE DEĞERLENDİRME UZMANI', N'Ölçme ve Değerlendirme Uzmanı', 3),
    (N'RİSK YÖNETİM VE DENETİM PERS.', N'Risk Yönetim ve Denetim Personeli', 4)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);

-- Mali İşler altı
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @maliId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'FİNANS SORUMLUSU', N'Finans Sorumlusu', 1),
    (N'MUHASEBE ŞEFİ', N'Muhasebe Şefi', 2)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);
GO

-- ============================================================
-- KATMAN 3 — Mağaza altı (Müdür Yrd / Şef seviyesi)
-- ============================================================
DECLARE @magazaId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'MAĞAZA MÜDÜRÜ');

INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @magazaId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'MAĞAZA MÜDÜR YRD.', N'Mağaza Müdür Yardımcısı', 1),
    (N'REYON ŞEFİ', N'Reyon Şefi', 2),
    (N'KASA ŞEFİ', N'Kasa Şefi', 3),
    (N'MUTFAK ŞEFİ', N'Mutfak Şefi', 4),
    (N'DEPO ŞEFİ', N'Depo Şefi', 5),
    (N'MAL KABUL SORUMLUSU', N'Mal Kabul Sorumlusu', 6)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);

-- Mağaza Müdür Yrd → Aday
DECLARE @magazaYrdId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'MAĞAZA MÜDÜR YRD.');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'MAĞAZA MÜDÜR YRD. ADAYI', N'Mağaza Müdür Yrd. Adayı', @magazaYrdId, 1, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'MAĞAZA MÜDÜR YRD. ADAYI');

-- Reyon Şefi → Aday + Satış Danışmanı
DECLARE @reyonId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'REYON ŞEFİ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @reyonId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'REYON ŞEFİ ADAYI', N'Reyon Şefi Adayı', 1),
    (N'SATIŞ DANIŞMANI', N'Satış Danışmanı', 2),
    (N'SATIŞ DESTEK PERSONELİ', N'Satış Destek Personeli', 3)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);

-- Kasa Şefi → Kasiyer
DECLARE @kasaId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'KASA ŞEFİ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'KASİYER', N'Kasiyer', @kasaId, 1, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'KASİYER');

-- Mutfak Şefi → Şef Garson + Aşçı + Bulaşıkçı + Yemekhane
DECLARE @mutfakId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'MUTFAK ŞEFİ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @mutfakId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'ŞEF GARSON', N'Şef Garson', 1),
    (N'AŞÇI YARDIMCISI', N'Aşçı Yardımcısı', 2),
    (N'BARİSTA', N'Barista', 3),
    (N'BULAŞIKÇI', N'Bulaşıkçı', 4),
    (N'YEMEKHANE PERSONELİ', N'Yemekhane Personeli', 5)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);

-- Şef Garson → Garson
DECLARE @sefGarsonId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'ŞEF GARSON');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'GARSON', N'Garson', @sefGarsonId, 1, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'GARSON');

-- Depo Şefi → Sorumlu → Personel + Forklift
DECLARE @depoSefId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'DEPO ŞEFİ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'DEPO SORUMLUSU', N'Depo Sorumlusu', @depoSefId, 1, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'DEPO SORUMLUSU');

DECLARE @depoSorumluId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'DEPO SORUMLUSU');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @depoSorumluId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'DEPO PERSONELİ', N'Depo Personeli', 1),
    (N'FORKLİFT OPERATÖRÜ', N'Forklift Operatörü', 2),
    (N'STOK KONTROL GÖREVLİSİ', N'Stok Kontrol Görevlisi', 3)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);

-- Mal Kabul Sorumlusu → Personel
DECLARE @malKabulId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'MAL KABUL SORUMLUSU');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'MAL KABUL PERSONELİ', N'Mal Kabul Personeli', @malKabulId, 1, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'MAL KABUL PERSONELİ');

-- Muhasebe Şefi → Görevli + Ön Muhasebe
DECLARE @muhasebeId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'MUHASEBE ŞEFİ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @muhasebeId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'MUHASEBE GÖREVLİSİ', N'Muhasebe Görevlisi', 1),
    (N'ÖN MUHASEBE GÖREVLİSİ', N'Ön Muhasebe Görevlisi', 2)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);
GO

-- ============================================================
-- KATMAN 4 — Satın Alma alt zinciri
-- ============================================================
DECLARE @satinAlmaYonId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'SATIN ALMA YÖNETİCİSİ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @satinAlmaYonId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'SATIN ALMA SORUMLUSU', N'Satın Alma Sorumlusu', 1),
    (N'SATIN ALMA UZMANI', N'Satın Alma Uzmanı', 2),
    (N'SATIN ALMA GÖREVLİSİ', N'Satın Alma Görevlisi', 3)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);

-- Çağrı Merkezi (root altı, BT/İdari ile aynı seviyede — Saha Operasyon altı)
DECLARE @sahaId2 INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'SAHA OPERASYON DİREKTÖRÜ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'ÇAĞRI MERKEZİ TEMSİLCİSİ', N'Çağrı Merkezi Temsilcisi', @sahaId2, 6, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'ÇAĞRI MERKEZİ TEMSİLCİSİ');

-- Teknik İşler Uzmanı (BT altı)
DECLARE @btId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'BİLGİ TEKNOLOJİLERİ YÖNETİCİSİ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'TEKNİK İŞLER UZMANI', N'Teknik İşler Uzmanı', @btId, 1, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'TEKNİK İŞLER UZMANI');

-- İdari İşler altı (temizlik, bekçi, şoför)
DECLARE @idariId INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'İDARİ İŞLER YÖNETİCİSİ');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @idariId, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'TEMİZLİK PERSONELİ', N'Temizlik Personeli', 1),
    (N'BEKÇİ', N'Bekçi', 2),
    (N'ŞOFÖR', N'Şoför', 3),
    (N'MAKAM ŞOFÖRÜ', N'Makam Şoförü', 4),
    (N'YÖNETİCİ ASİSTANI', N'Yönetici Asistanı', 5)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);
GO

-- ============================================================
-- KATMAN 5 — Pazarlama / Yaratıcı uzmanlar (Yön. Kurulu altı)
-- ============================================================
DECLARE @ykbId2 INT = (SELECT Id FROM dbo.OrgPositions WHERE Code = N'YÖNETİM KURULU BAŞKANI');
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT v.Code, v.Title, @ykbId2, v.DisplayOrder, 1, N'system'
FROM (VALUES
    (N'GRAFİK TASARIM UZMANI', N'Grafik Tasarım Uzmanı', 100),
    (N'SOSYAL MEDYA UZMANI', N'Sosyal Medya Uzmanı', 101),
    (N'METİN YAZARI', N'Metin Yazarı', 102),
    (N'VİDEOGRAPHER UZMANI', N'Videographer Uzmanı', 103),
    (N'ETKİNLİK ORGANİZASYON SORUML.', N'Etkinlik Organizasyon Sorumlusu', 104)
) AS v(Code, Title, DisplayOrder)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions p WHERE p.Code = v.Code);
GO

-- ============================================================
-- KATMAN 6 — Stajyer (root, geçici)
-- ============================================================
INSERT dbo.OrgPositions (Code, Title, ParentPositionId, DisplayOrder, IsActive, CreatedBy)
SELECT N'STAJYER', N'Stajyer', NULL, 999, 1, N'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrgPositions WHERE Code = N'STAJYER');
GO

-- Doğrulama sayımı
DECLARE @inserted INT = (SELECT COUNT(*) FROM dbo.OrgPositions WHERE CreatedBy = N'system');
PRINT CONCAT('Migration 40 tamamlandi: ', @inserted, ' OrgPosition seed (sistem). Beklenen 61 (Zirve distinct Unvan tamami).');
GO
