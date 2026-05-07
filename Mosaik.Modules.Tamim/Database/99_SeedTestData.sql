-- Tamim Modülü Test Seed Verisi (DEV ONLY — production'da çalıştırma)
-- Tarih: 2026-05-08
-- Plan: plans/17-tamim.md (Faz C smoke test için)
--
-- Üretir:
--   - 2 geçmiş Tamim (önceki 2 gün için yayınlanmış)
--   - Her Tamim'e 3-4 bağlı GunlukBlok
--   - Bugün için 4 bekleyen GunlukBlok (TamimId NULL — Faz D cron test için)
--   - 6 AuditLog "tamim_okundu" event (admin user, okuma raporu test için)
--
-- İdempotent: BlokNo UNIQUE + TamimNo UNIQUE — tekrar çalıştırılınca hata vermek
-- yerine atlar (mevcut seed varsa hiçbir şey eklemez).

SET NOCOUNT ON;
GO

-- DEMO USER ID'lerini değişkenlere al
DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin');
DECLARE @IkId INT    = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'ik');

-- BlokTuru ID'leri
DECLARE @Duyuru INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='duyuru');
DECLARE @Karar INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='karar');
DECLARE @Hadise INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='hadise');
DECLARE @Uyari INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='uyari');
DECLARE @Bilgi INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='bilgi');

-- Tarihler (UTC)
DECLARE @Bugun DATE = CAST(GETUTCDATE() AS DATE);
DECLARE @Dun DATE = DATEADD(day, -1, @Bugun);
DECLARE @Onceki DATE = DATEADD(day, -2, @Bugun);

------------------------------------------------------------
-- 1. TAMİM 1 — 2 gün önce yayınlanmış (3 blok)
------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Tamim WHERE TamimNo = N'TAM-' + FORMAT(@Onceki, 'yyyyMMdd'))
BEGIN
    DECLARE @Tamim1Id INT;
    INSERT INTO dbo.Tamim (TamimNo, Baslik, TamimTarihi, YayinTarihi, CreatedAt)
    VALUES (
        N'TAM-' + FORMAT(@Onceki, 'yyyyMMdd'),
        FORMAT(@Onceki, 'dd MMMM yyyy', 'tr-TR') + N' Günlük Tamim',
        @Onceki,
        DATEADD(hour, 14, CAST(@Onceki AS DATETIME2(0))),  -- 17:00 TR = 14:00 UTC
        DATEADD(hour, 14, CAST(@Onceki AS DATETIME2(0)))
    );
    SET @Tamim1Id = SCOPE_IDENTITY();

    INSERT INTO dbo.GunlukBlok
        (BlokNo, OlusturanId, DepartmanAdi, Konu, Aciklama, BlokTarihi, BlokTuruId, Acil, TamimId, IsActive, CreatedAt)
    VALUES
        (N'BLK-' + FORMAT(@Onceki, 'yyyyMMdd') + N'-001', @AdminId, N'Bilgi İşlem',
         N'Sistem Bakım Bildirimi',
         N'Yarın 22:00 - 23:00 arası ERP sisteminde planlı bakım yapılacaktır. Bu süre zarfında satış sistemi geçici olarak erişilemeyecektir.',
         @Onceki, @Duyuru, 0, @Tamim1Id, 1, DATEADD(hour, 10, CAST(@Onceki AS DATETIME2(0)))),
        (N'BLK-' + FORMAT(@Onceki, 'yyyyMMdd') + N'-002', @AdminId, N'İnsan Kaynakları',
         N'Yeni İşe Başlayan Personel',
         N'Bugün itibariyle 2 yeni satış danışmanı görevine başlamıştır. ÖZLÜCE mağazasında Mehmet B. ve FSM mağazasında Ayşe D.',
         @Onceki, @Bilgi, 0, @Tamim1Id, 1, DATEADD(hour, 11, CAST(@Onceki AS DATETIME2(0)))),
        (N'BLK-' + FORMAT(@Onceki, 'yyyyMMdd') + N'-003', @IkId, N'Operasyon',
         N'Stok Sayım Hazırlığı',
         N'Bu Cumartesi yıl sonu stok sayımı yapılacak. Tüm mağaza müdürlerinin pazartesi akşamına kadar kategori sorumlularına bilgi vermesi gerekmektedir.',
         @Onceki, @Karar, 1, @Tamim1Id, 1, DATEADD(hour, 13, CAST(@Onceki AS DATETIME2(0))));

    PRINT 'Tamim 1 (2 gun once) + 3 blok eklendi.';
END
ELSE
    PRINT 'Tamim 1 zaten mevcut, atlandi.';
GO

------------------------------------------------------------
-- 2. TAMİM 2 — Dün yayınlanmış (4 blok)
------------------------------------------------------------
DECLARE @Dun DATE = DATEADD(day, -1, CAST(GETUTCDATE() AS DATE));
DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin');
DECLARE @IkId INT    = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'ik');
DECLARE @Duyuru INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='duyuru');
DECLARE @Karar INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='karar');
DECLARE @Hadise INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='hadise');
DECLARE @Uyari INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='uyari');
DECLARE @Bilgi INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='bilgi');

IF NOT EXISTS (SELECT 1 FROM dbo.Tamim WHERE TamimNo = N'TAM-' + FORMAT(@Dun, 'yyyyMMdd'))
BEGIN
    DECLARE @Tamim2Id INT;
    INSERT INTO dbo.Tamim (TamimNo, Baslik, TamimTarihi, YayinTarihi, CreatedAt)
    VALUES (
        N'TAM-' + FORMAT(@Dun, 'yyyyMMdd'),
        FORMAT(@Dun, 'dd MMMM yyyy', 'tr-TR') + N' Günlük Tamim',
        @Dun,
        DATEADD(hour, 14, CAST(@Dun AS DATETIME2(0))),
        DATEADD(hour, 14, CAST(@Dun AS DATETIME2(0)))
    );
    SET @Tamim2Id = SCOPE_IDENTITY();

    INSERT INTO dbo.GunlukBlok
        (BlokNo, OlusturanId, DepartmanAdi, Konu, Aciklama, BlokTarihi, BlokTuruId, Acil, TamimId, IsActive, CreatedAt)
    VALUES
        (N'BLK-' + FORMAT(@Dun, 'yyyyMMdd') + N'-001', @AdminId, N'Muhasebe',
         N'Ay Sonu Kapanış Hatırlatma',
         N'Bu ayın muhtasar beyannameleri 15''ine kadar verilecek. Tüm departman sorumlularının fiş ve fatura girişlerini Cuma 17:00''a kadar tamamlamaları gerekmektedir.',
         @Dun, @Uyari, 1, @Tamim2Id, 1, DATEADD(hour, 9, CAST(@Dun AS DATETIME2(0)))),
        (N'BLK-' + FORMAT(@Dun, 'yyyyMMdd') + N'-002', @AdminId, N'Pazarlama',
         N'Anneler Günü Kampanyası',
         N'8 Mayıs - 12 Mayıs arası tüm hediyelik kategorisinde %20 indirim. Vitrin düzenleme bugün akşam 19:00''dan sonra mağaza müdürleri tarafından yapılacak.',
         @Dun, @Duyuru, 0, @Tamim2Id, 1, DATEADD(hour, 10, CAST(@Dun AS DATETIME2(0)))),
        (N'BLK-' + FORMAT(@Dun, 'yyyyMMdd') + N'-003', @AdminId, N'Operasyon',
         N'FSM Mağaza Asansör Arızası',
         N'FSM mağazasında müşteri asansörü 2 gün boyunca servisten kalkacak. Engelli müşteri yönlendirmesi için kasa personeli bilgilendirildi.',
         @Dun, @Hadise, 0, @Tamim2Id, 1, DATEADD(hour, 11, CAST(@Dun AS DATETIME2(0)))),
        (N'BLK-' + FORMAT(@Dun, 'yyyyMMdd') + N'-004', @IkId, N'İnsan Kaynakları',
         N'İzin Talebi Sürecinde Değişiklik',
         N'1 Haziran itibariyle yıllık izin talepleri Mosaik portal üzerinden yapılacaktır. Mevcut Excel formu kullanımdan kalkacak. Eğitim videosu yarın paylaşılacak.',
         @Dun, @Karar, 0, @Tamim2Id, 1, DATEADD(hour, 13, CAST(@Dun AS DATETIME2(0))));

    PRINT 'Tamim 2 (dun) + 4 blok eklendi.';
END
ELSE
    PRINT 'Tamim 2 zaten mevcut, atlandi.';
GO

------------------------------------------------------------
-- 3. BUGÜN BEKLEYEN BLOKLAR (TamimId NULL — Faz D cron test için)
------------------------------------------------------------
DECLARE @Bugun DATE = CAST(GETUTCDATE() AS DATE);
DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin');
DECLARE @IkId INT    = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'ik');
DECLARE @Duyuru INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='duyuru');
DECLARE @Karar INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='karar');
DECLARE @Hadise INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='hadise');
DECLARE @Uyari INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='uyari');
DECLARE @Bilgi INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blokTuru' AND dv.Code='bilgi');

INSERT INTO dbo.GunlukBlok
    (BlokNo, OlusturanId, DepartmanAdi, Konu, Aciklama, BlokTarihi, BlokTuruId, Acil, TamimId, IsActive, CreatedAt)
SELECT v.BlokNo, v.OlusturanId, v.DepartmanAdi, v.Konu, v.Aciklama, v.BlokTarihi, v.BlokTuruId, v.Acil, NULL, 1, v.CreatedAt
FROM (VALUES
    (N'BLK-' + FORMAT(@Bugun, 'yyyyMMdd') + N'-001', @AdminId, N'Bilgi İşlem',
     N'Yeni Sistem Modülü Aktive: Tamim',
     N'Bugün itibariyle Mosaik portalda Tamim & Sirküler modülü devreye alındı. Sol menüden erişebilirsiniz. Geri bildirimlerinizi BT departmanına iletin.',
     @Bugun, @Duyuru, 0, DATEADD(hour, 9, CAST(@Bugun AS DATETIME2(0)))),
    (N'BLK-' + FORMAT(@Bugun, 'yyyyMMdd') + N'-002', @AdminId, N'Operasyon',
     N'Heykel Mağaza Klima Arızası',
     N'Heykel mağazası klima sistemi servis bekliyor. Tahmini onarım süresi 2 saat. Müşteri ve personel için pencere açıldı. Hava sıcaklığı 22°C tutuluyor.',
     @Bugun, @Hadise, 1, DATEADD(hour, 10, CAST(@Bugun AS DATETIME2(0)))),
    (N'BLK-' + FORMAT(@Bugun, 'yyyyMMdd') + N'-003', @IkId, N'İnsan Kaynakları',
     N'Doğum Günü Kutlaması: Ayşe D.',
     N'Bugün ÖZLÜCE mağazası kasiyer Ayşe D.''nin doğum günü. Mağaza saat 16:00''da küçük bir kutlama düzenleyecek. Tüm departman çalışanlarına duyurulur.',
     @Bugun, @Bilgi, 0, DATEADD(hour, 11, CAST(@Bugun AS DATETIME2(0)))),
    (N'BLK-' + FORMAT(@Bugun, 'yyyyMMdd') + N'-004', @AdminId, N'Satın Alma',
     N'Tedarikçi Görüşme Notu',
     N'X yayınevi ile yıllık alım sözleşmesi yenilendi. %3 ek indirim alındı. Yeni fiyat listesi yarın MainDB''e yansıyacak. Stok güncel kalemler için sipariş verilebilir.',
     @Bugun, @Karar, 0, DATEADD(hour, 13, CAST(@Bugun AS DATETIME2(0))))
) v (BlokNo, OlusturanId, DepartmanAdi, Konu, Aciklama, BlokTarihi, BlokTuruId, Acil, CreatedAt)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.GunlukBlok b WHERE b.BlokNo = v.BlokNo
);

PRINT 'Bugun bekleyen 4 blok eklendi (TamimId NULL).';
GO

------------------------------------------------------------
-- 4. AuditLog "tamim_okundu" event'leri (okuma raporu test için)
------------------------------------------------------------
DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin');
DECLARE @IkId INT    = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'ik');

DECLARE @Tamim1Id INT = (SELECT TOP 1 Id FROM dbo.Tamim WHERE TamimNo LIKE 'TAM-%' ORDER BY TamimTarihi);
DECLARE @Tamim2Id INT = (SELECT TOP 1 Id FROM dbo.Tamim WHERE TamimNo LIKE 'TAM-%' ORDER BY TamimTarihi DESC);

-- admin Tamim 1 ve 2 okudu
IF NOT EXISTS (SELECT 1 FROM dbo.AuditLog
               WHERE EventType = 'tamim_okundu' AND TargetKey = CAST(@Tamim1Id AS NVARCHAR(20))
                 AND Username = 'admin')
BEGIN
    INSERT INTO dbo.AuditLog (AuditId, Username, EventType, TargetType, TargetKey, IsSuccess, CreatedAt)
    VALUES
        (NEWID(), 'admin', 'tamim_okundu', 'tamim', CAST(@Tamim1Id AS NVARCHAR(20)), 1, DATEADD(hour, -20, GETUTCDATE())),
        (NEWID(), 'admin', 'tamim_okundu', 'tamim', CAST(@Tamim2Id AS NVARCHAR(20)), 1, DATEADD(hour, -3, GETUTCDATE()));
    PRINT 'admin icin 2 tamim_okundu event eklendi.';
END

-- ik Tamim 2 okudu (Tamim 1 okumadi — okuma raporu test verisi)
IF NOT EXISTS (SELECT 1 FROM dbo.AuditLog
               WHERE EventType = 'tamim_okundu' AND TargetKey = CAST(@Tamim2Id AS NVARCHAR(20))
                 AND Username = 'ik')
BEGIN
    INSERT INTO dbo.AuditLog (AuditId, Username, EventType, TargetType, TargetKey, IsSuccess, CreatedAt)
    VALUES (NEWID(), 'ik', 'tamim_okundu', 'tamim', CAST(@Tamim2Id AS NVARCHAR(20)), 1, DATEADD(hour, -1, GETUTCDATE()));
    PRINT 'ik icin 1 tamim_okundu event eklendi (Tamim 1 OKUMADI — rapor test).';
END
GO

------------------------------------------------------------
-- ÖZET
------------------------------------------------------------
SELECT
    (SELECT COUNT(*) FROM dbo.Tamim) AS Tamim,
    (SELECT COUNT(*) FROM dbo.GunlukBlok) AS Blok,
    (SELECT COUNT(*) FROM dbo.GunlukBlok WHERE TamimId IS NULL AND IsActive=1) AS BekleyenBlok,
    (SELECT COUNT(*) FROM dbo.GunlukBlok WHERE TamimId IS NOT NULL) AS YayindaBlok,
    (SELECT COUNT(*) FROM dbo.AuditLog WHERE EventType='tamim_okundu') AS OkumaLogu;

PRINT 'Tamim test seed tamamlandi.';
