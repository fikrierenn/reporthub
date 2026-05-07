-- Circular Modülü Test Seed Verisi (DEV ONLY)
-- Tarih: 2026-05-08 (revize 2 — kullanıcı: "tamim bağlayıcı konular için, iç iletişim değil")
-- Plan: plans/17-tamim.md (Faz C smoke test için)
--
-- Circular = bağlayıcı, kalıcı, "haberim yoktu" diyemeyecek konular:
--   - Politika değişiklikleri (izin, mesai, kıyafet, fiyatlandırma)
--   - Yasal/uyumluluk hatırlatmaları (muhtasar beyan, vergi, KVKK)
--   - Operasyonel kurallar (kasiyer prosedür, satış akışı, stok sayım)
--   - Resmi kararlar (tedarikçi anlaşması, kampanya kuralları)
--
-- Circular DEĞİL:
--   - Doğum günü kutlamaları, yeni personel duyuruları
--   - Geçici hadiseler (klima arızası, asansör)
--   - Toplantı çağrıları
--   → Bu içerikler için Plan 27 İç İletişim / Duyuru Akışı (ayrı modül).
--
-- İdempotent: BlockNumber + CircularNumber UNIQUE.

SET NOCOUNT ON;
GO

-- Önce eski (yanlış) seed'i temizle (sadece DEV — CircularId silinince blok orphan kalır,
-- bunları tamamen yenisiyle değiştir)
DELETE FROM dbo.DailyBlock
WHERE BlockNumber LIKE 'BLK-%'
  AND BlockNumber IN (
    SELECT BlockNumber FROM dbo.DailyBlock
    WHERE Subject IN (
        N'Sistem Bakım Bildirimi',
        N'Yeni İşe Başlayan Personel',
        N'FSM Mağaza Asansör Arızası',
        N'İzin Talebi Sürecinde Değişiklik',
        N'Yeni Sistem Modülü Aktive: Circular',
        N'Heykel Mağaza Klima Arızası',
        N'Doğum Günü Kutlaması: Ayşe D.',
        N'Tedarikçi Görüşme Notu',
        N'Stok Sayım Hazırlığı',
        N'Ay Sonu Kapanış Hatırlatma',
        N'Anneler Günü Kampanyası'
    )
);
DELETE FROM dbo.Circular
WHERE CircularNumber LIKE 'TAM-%'
  AND NOT EXISTS (SELECT 1 FROM dbo.DailyBlock b WHERE b.CircularId = dbo.Circular.Id);
DELETE FROM dbo.AuditLog WHERE EventType = 'tamim_okundu';
PRINT 'Eski seed temizlendi.';
GO

DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin');
DECLARE @IkId INT    = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'ik');

DECLARE @Duyuru INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='duyuru');
DECLARE @Karar INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='karar');
DECLARE @Hadise INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='hadise');
DECLARE @Uyari INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='uyari');
DECLARE @Bilgi INT  = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='bilgi');

DECLARE @Bugun DATE = CAST(GETUTCDATE() AS DATE);
DECLARE @Dun DATE = DATEADD(day, -1, @Bugun);
DECLARE @Onceki DATE = DATEADD(day, -2, @Bugun);

------------------------------------------------------------
-- TAMİM 1 — 2 gün önce yayınlanmış (3 bağlayıcı blok)
------------------------------------------------------------
DECLARE @Tamim1Id INT;
INSERT INTO dbo.Circular (CircularNumber, Title, CircularDate, PublishedAt, CreatedAt)
VALUES (
    N'TAM-' + FORMAT(@Onceki, 'yyyyMMdd'),
    FORMAT(@Onceki, 'dd MMMM yyyy', 'tr-TR') + N' Günlük Circular',
    @Onceki,
    DATEADD(hour, 14, CAST(@Onceki AS DATETIME2(0))),
    DATEADD(hour, 14, CAST(@Onceki AS DATETIME2(0)))
);
SET @Tamim1Id = SCOPE_IDENTITY();

INSERT INTO dbo.DailyBlock
    (BlockNumber, CreatedById, Department, Subject, Content, BlockDate, BlockTypeId, IsUrgent, CircularId, IsActive, CreatedAt)
VALUES
    (N'BLK-' + FORMAT(@Onceki, 'yyyyMMdd') + N'-001', @AdminId, N'İnsan Kaynakları',
     N'İzin Talep Süreci 1 Haziran''dan İtibaren Portal Üzerinden',
     N'1 Haziran 2026 itibariyle yıllık izin talepleri yalnızca Mosaik portal üzerinden yapılacaktır. Mevcut Excel formu kullanımdan kalkacaktır.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Tüm çalışanların 1 Haziran öncesi sistemden bir kez giriş yaparak profil bilgilerini kontrol etmesi zorunludur. İzin tarihi onayları artık birim yöneticisi → İK departmanı zincirinde dijital olarak ilerleyecektir.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Sorularınız için ik@bkmkitap.com adresine yazabilirsiniz.',
     @Onceki, @Karar, 0, @Tamim1Id, 1, DATEADD(hour, 9, CAST(@Onceki AS DATETIME2(0)))),

    (N'BLK-' + FORMAT(@Onceki, 'yyyyMMdd') + N'-002', @AdminId, N'Muhasebe',
     N'Ay Sonu Kapanış Disiplini',
     N'Aylık muhtasar beyannamesi her ayın 15''ine kadar verildiğinden, tüm departman sorumlularının fiş ve fatura girişlerini ayın son iş gününün saat 17:00''a kadar tamamlamış olması zorunludur.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Geç giriş yapılan belgeler bir sonraki dönem beyanına kalır ve gecikme cezası riski doğurur. Lütfen kategori sorumluları takip için Cuma 16:00 hatırlatmasını kalendıarınıza ekleyin.',
     @Onceki, @Uyari, 1, @Tamim1Id, 1, DATEADD(hour, 11, CAST(@Onceki AS DATETIME2(0)))),

    (N'BLK-' + FORMAT(@Onceki, 'yyyyMMdd') + N'-003', @AdminId, N'Operasyon',
     N'Yıl Sonu Stok Sayımı Prosedürü',
     N'Bu yıl sayım 7 Haziran Cumartesi yapılacaktır. Tüm mağaza müdürleri sayım öncesi şu adımları tamamlar:' + CHAR(13) + CHAR(10) +
     N'1. Sayım listesi 5 Haziran sonuna kadar kategori sorumlularına dağıtılır.' + CHAR(13) + CHAR(10) +
     N'2. Sayım sırasında satış kapatılır (mağaza tüketici girişine kapalı).' + CHAR(13) + CHAR(10) +
     N'3. Fark raporları 9 Haziran 12:00''a kadar muhasebe departmanına teslim edilir.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Sayım protokolüne uymama disiplin ihlali kapsamında değerlendirilecektir.',
     @Onceki, @Karar, 0, @Tamim1Id, 1, DATEADD(hour, 13, CAST(@Onceki AS DATETIME2(0))));

PRINT 'Circular 1 (önceki gün) + 3 baglayici blok eklendi.';
GO

------------------------------------------------------------
-- TAMİM 2 — Dün yayınlanmış (4 bağlayıcı blok)
------------------------------------------------------------
DECLARE @Dun DATE = DATEADD(day, -1, CAST(GETUTCDATE() AS DATE));
DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin');
DECLARE @IkId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'ik');
DECLARE @Duyuru INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='duyuru');
DECLARE @Karar INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='karar');
DECLARE @Uyari INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='uyari');
DECLARE @Bilgi INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='bilgi');

DECLARE @Tamim2Id INT;
INSERT INTO dbo.Circular (CircularNumber, Title, CircularDate, PublishedAt, CreatedAt)
VALUES (
    N'TAM-' + FORMAT(@Dun, 'yyyyMMdd'),
    FORMAT(@Dun, 'dd MMMM yyyy', 'tr-TR') + N' Günlük Circular',
    @Dun,
    DATEADD(hour, 14, CAST(@Dun AS DATETIME2(0))),
    DATEADD(hour, 14, CAST(@Dun AS DATETIME2(0)))
);
SET @Tamim2Id = SCOPE_IDENTITY();

INSERT INTO dbo.DailyBlock
    (BlockNumber, CreatedById, Department, Subject, Content, BlockDate, BlockTypeId, IsUrgent, CircularId, IsActive, CreatedAt)
VALUES
    (N'BLK-' + FORMAT(@Dun, 'yyyyMMdd') + N'-001', @AdminId, N'Pazarlama',
     N'Anneler Günü Kampanya Uygulama Kuralları',
     N'8-12 Mayıs arası tüm hediyelik kategorisinde %20 indirim uygulanacaktır.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Kasiyer prosedürü:' + CHAR(13) + CHAR(10) +
     N'1. Hediye paketi seçeneği ÜCRETSİZ — sistemde ek tutar görünmez.' + CHAR(13) + CHAR(10) +
     N'2. Kampanya stikerli ürünlerde indirim otomatik düşer; manuel müdahale yasaktır.' + CHAR(13) + CHAR(10) +
     N'3. İade durumunda indirimli fiyat üzerinden işlem yapılır.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Vitrin düzenleme her mağaza müdürü tarafından 7 Mayıs 19:00 sonrası tamamlanmış olacaktır.',
     @Dun, @Karar, 0, @Tamim2Id, 1, DATEADD(hour, 9, CAST(@Dun AS DATETIME2(0)))),

    (N'BLK-' + FORMAT(@Dun, 'yyyyMMdd') + N'-002', @AdminId, N'Bilgi İşlem',
     N'KVKK Veri Sorumluluğu — Müşteri Bilgisi Paylaşım Yasağı',
     N'KVKK kapsamında müşteri telefon, e-posta, adres bilgileri başka kullanıcılara (mağaza personeli dahil) sözlü/yazılı paylaşılamaz.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'İhlal durumunda hem şirket hem ihlal yapan kişi yasal yaptırıma tabi tutulur. Müşteri bilgisi sadece sistem üzerinden ilgili siparişe ilişkin işlem amaçlı kullanılabilir.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Bu kuralın anlaşıldığı "Okudum" butonu ile tasdik edilmelidir.',
     @Dun, @Uyari, 1, @Tamim2Id, 1, DATEADD(hour, 10, CAST(@Dun AS DATETIME2(0)))),

    (N'BLK-' + FORMAT(@Dun, 'yyyyMMdd') + N'-003', @AdminId, N'Satın Alma',
     N'X Yayınevi Yıllık Sözleşme Yenilemesi — Yeni Fiyat Uygulaması',
     N'X Yayınevi ile yıllık alım sözleşmesi yenilendi. %3 ek liste indirimi alındı.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Yeni fiyat listesi yarın (gün sonu işlemi) MainDB''e yansıyacak. Kasiyerler güncel fiyatla satış başlatır. Eski etiketler tedarikçi tarafından değiştirilecek; satıcılar etiket-sistem uyumsuzluğu durumunda sistemdeki fiyatı esas alır.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Müşteri itirazında "yeni sözleşme dönemi" açıklaması yeterlidir.',
     @Dun, @Karar, 0, @Tamim2Id, 1, DATEADD(hour, 11, CAST(@Dun AS DATETIME2(0)))),

    (N'BLK-' + FORMAT(@Dun, 'yyyyMMdd') + N'-004', @IkId, N'İnsan Kaynakları',
     N'Kıyafet Politikası Hatırlatma',
     N'Mağaza personelinin BKM markalı yelek + siyah pantolon/etek ile çalışması zorunludur. Tabela kuralı:' + CHAR(13) + CHAR(10) +
     N'• Saat görünür, küçük takı serbest' + CHAR(13) + CHAR(10) +
     N'• Spor ayakkabı yalnız kapalı, BKM marka veya siyah/koyu' + CHAR(13) + CHAR(10) +
     N'• Tişört yelek altına uygun renkte (beyaz/açık tonlar)' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Mağaza müdürleri haftalık denetim yapar; uygunsuz kıyafet 1. uyarı sözlü, 2. yazılı, 3. disiplin kuruluna sevk.',
     @Dun, @Karar, 0, @Tamim2Id, 1, DATEADD(hour, 13, CAST(@Dun AS DATETIME2(0))));

PRINT 'Circular 2 (dun) + 4 baglayici blok eklendi.';
GO

------------------------------------------------------------
-- BUGÜN BEKLEYEN BLOKLAR (CircularId NULL — 17:00 cron test için)
------------------------------------------------------------
DECLARE @Bugun DATE = CAST(GETUTCDATE() AS DATE);
DECLARE @AdminId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'admin');
DECLARE @IkId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE Username = N'ik');
DECLARE @Duyuru INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='duyuru');
DECLARE @Karar INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='karar');
DECLARE @Uyari INT = (SELECT dv.Id FROM dbo.DictionaryValues dv JOIN dbo.DictionaryTypes dt ON dt.Id=dv.TypeId WHERE dt.Code='blockType' AND dv.Code='uyari');

INSERT INTO dbo.DailyBlock
    (BlockNumber, CreatedById, Department, Subject, Content, BlockDate, BlockTypeId, IsUrgent, CircularId, IsActive, CreatedAt)
VALUES
    (N'BLK-' + FORMAT(@Bugun, 'yyyyMMdd') + N'-001', @AdminId, N'Bilgi İşlem',
     N'Mosaik Portal — Circular Modülü Devreye Alındı',
     N'Bugün itibariyle Mosaik portala Circular & Sirküler modülü eklenmiştir. Tüm bağlayıcı duyurular bu modülde yayınlanacaktır.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Bundan sonra şirket içi e-posta gönderimi ile yapılan resmi tamim duyuruları **kabul edilmeyecektir**. Tüm çalışanların portal üzerinden tamimleri okuduklarını "Okudum" butonu ile teyit etmeleri zorunludur.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'İlk giriş için sol menüden "Circular & Sirküler" linkine tıklayın.',
     @Bugun, @Karar, 0, NULL, 1, DATEADD(hour, 9, CAST(@Bugun AS DATETIME2(0)))),

    (N'BLK-' + FORMAT(@Bugun, 'yyyyMMdd') + N'-002', @AdminId, N'Muhasebe',
     N'Yeni e-Fatura Eşik Tutarı — 1 Temmuz Uygulama',
     N'1 Temmuz 2026''dan itibaren e-Fatura zorunluluk eşiği yıllık 5 milyon TL''dan 3 milyon TL''ya düşmektedir. (GİB tebliği 31/2026)' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Şirketimiz bu eşiğin üzerinde olduğu için 1 Temmuz öncesi tüm kasiyer ekipmanlarının e-Fatura/e-Arşiv entegre edilmiş olması zorunludur. Mağaza müdürleri hafta sonu IT departmanı ile koordineli test sürecini başlatacaktır.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Test takvimi yarın paylaşılacak.',
     @Bugun, @Uyari, 1, NULL, 1, DATEADD(hour, 10, CAST(@Bugun AS DATETIME2(0)))),

    (N'BLK-' + FORMAT(@Bugun, 'yyyyMMdd') + N'-003', @IkId, N'İnsan Kaynakları',
     N'Yıllık İzin Devir Kuralları — 31 Aralık Son Tarih',
     N'2025 yılına ait kullanılmayan yıllık izinlerin 2026''ya devir tarihi 31 Aralık 2026''dır. Bu tarihe kadar kullanılmayan izinler yasal olarak yanar.' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'Tüm çalışanların kalan izin günlerini 1 Ekim''e kadar İK departmanı ile mutabık etmesi gerekmektedir. Mutabakat yapılmamış izin günleri için ek talep kabul edilmez.',
     @Bugun, @Karar, 0, NULL, 1, DATEADD(hour, 11, CAST(@Bugun AS DATETIME2(0)))),

    (N'BLK-' + FORMAT(@Bugun, 'yyyyMMdd') + N'-004', @AdminId, N'Operasyon',
     N'Mağaza Açılış-Kapanış Saat Standardı',
     N'1 Haziran''dan itibaren mağaza açılış 09:30 / kapanış 21:00 standardı uygulanacaktır.' + CHAR(13) + CHAR(10) +
     N'• Hafta içi: 09:30 - 21:00' + CHAR(13) + CHAR(10) +
     N'• Cumartesi: 09:30 - 21:30' + CHAR(13) + CHAR(10) +
     N'• Pazar: 10:00 - 20:00' + CHAR(13) + CHAR(10) + CHAR(13) + CHAR(10) +
     N'AVM lokasyonları için AVM saatleri esas alınır (sözleşmesel zorunluluk). Kasiyer vardiyaları bu saatlere göre yeniden düzenlenecek; vardiya planı 25 Mayıs öncesi ilan edilecek.',
     @Bugun, @Karar, 0, NULL, 1, DATEADD(hour, 13, CAST(@Bugun AS DATETIME2(0))));

PRINT 'Bugun bekleyen 4 baglayici blok eklendi.';
GO

------------------------------------------------------------
-- AuditLog "circular_read" — okuma raporu test verisi
------------------------------------------------------------
DECLARE @Tamim1Id INT = (SELECT TOP 1 Id FROM dbo.Circular ORDER BY CircularDate);
DECLARE @Tamim2Id INT = (SELECT TOP 1 Id FROM dbo.Circular ORDER BY CircularDate DESC);

INSERT INTO dbo.AuditLog (AuditId, Username, EventType, TargetType, TargetKey, IsSuccess, CreatedAt)
VALUES
    (NEWID(), 'admin', 'tamim_okundu', 'tamim', CAST(@Tamim1Id AS NVARCHAR(20)), 1, DATEADD(hour, -20, GETUTCDATE())),
    (NEWID(), 'admin', 'tamim_okundu', 'tamim', CAST(@Tamim2Id AS NVARCHAR(20)), 1, DATEADD(hour, -3, GETUTCDATE())),
    (NEWID(), 'ik',    'tamim_okundu', 'tamim', CAST(@Tamim2Id AS NVARCHAR(20)), 1, DATEADD(hour, -1, GETUTCDATE()));
PRINT 'admin: 2 tamim okudu, ik: 1 tamim okudu (Circular 1 OKUMADI — okuma raporu test verisi).';
GO

------------------------------------------------------------
-- ÖZET
------------------------------------------------------------
SELECT
    (SELECT COUNT(*) FROM dbo.Circular) AS Circular,
    (SELECT COUNT(*) FROM dbo.DailyBlock WHERE IsActive=1) AS Block,
    (SELECT COUNT(*) FROM dbo.DailyBlock WHERE CircularId IS NULL AND IsActive=1) AS BekleyenBlok,
    (SELECT COUNT(*) FROM dbo.DailyBlock WHERE CircularId IS NOT NULL) AS YayindaBlok,
    (SELECT COUNT(*) FROM dbo.AuditLog WHERE EventType='tamim_okundu') AS OkumaLogu;

PRINT 'Circular test seed tamamlandi (revize 2 — baglayici icerik).';
