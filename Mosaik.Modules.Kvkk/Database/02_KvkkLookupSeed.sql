-- Plan 54 M6 / Plan 40 Faz 0 — KVKK REF lookup seed (idempotent, Code natural key).
-- Kaynak: kvkk-veri-envanteri skill + KVKK Mart 2025 rehber.
SET NOCOUNT ON;

-- ===== DataCategory (VERBİS 23 kategori; 14-23 özel nitelikli) =====
INSERT INTO dbo.KvkkDataCategories (Code, Name, IsSpecialCategory, SortOrder, IsActive)
SELECT v.Code, v.Name, v.Sp, v.So, 1 FROM (VALUES
 (N'kimlik',N'Kimlik',0,10),(N'iletisim',N'İletişim',0,20),(N'lokasyon',N'Lokasyon',0,30),
 (N'ozluk',N'Özlük',0,40),(N'hukukiIslem',N'Hukuki İşlem',0,50),(N'musteriIslem',N'Müşteri İşlem',0,60),
 (N'fizikselMekan',N'Fiziksel Mekan Güvenliği',0,70),(N'islemGuvenligi',N'İşlem Güvenliği',0,80),
 (N'riskYonetimi',N'Risk Yönetimi',0,90),(N'finans',N'Finans',0,100),(N'meslekiDeneyim',N'Mesleki Deneyim',0,110),
 (N'pazarlama',N'Pazarlama',0,120),(N'gorselIsitsel',N'Görsel ve İşitsel Kayıtlar',0,130),
 (N'irkEtnik',N'Irk ve Etnik Köken',1,140),(N'siyasiDusunce',N'Siyasi Düşünce',1,150),
 (N'inanc',N'Felsefi İnanç, Din, Mezhep',1,160),(N'kilikKiyafet',N'Kılık Kıyafet',1,170),
 (N'uyelik',N'Dernek, Vakıf, Sendika Üyeliği',1,180),(N'saglik',N'Sağlık Bilgileri',1,190),
 (N'cinselHayat',N'Cinsel Hayat',1,200),(N'cezaMahkumiyeti',N'Ceza Mahkûmiyeti ve Güvenlik Tedbirleri',1,210),
 (N'biyometrik',N'Biyometrik Veri',1,220),(N'genetik',N'Genetik Veri',1,230)
) v(Code,Name,Sp,So)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkDataCategories t WHERE t.Code=v.Code);

-- ===== LegalBasis (m.5/2, m.5/1, m.6/3) =====
INSERT INTO dbo.KvkkLegalBases (Code, Name, Article, IsSpecialCategoryBasis, SortOrder, IsActive)
SELECT v.Code, v.Name, v.Art, v.Sp, v.So, 1 FROM (VALUES
 (N'm5-2-a',N'Kanunlarda açıkça öngörülme',N'5/2-a',0,10),
 (N'm5-2-b',N'Fiili imkânsızlık',N'5/2-b',0,20),
 (N'm5-2-c',N'Sözleşmenin kurulması veya ifası',N'5/2-c',0,30),
 (N'm5-2-c2',N'Veri sorumlusunun hukuki yükümlülüğü',N'5/2-ç',0,40),
 (N'm5-2-d',N'İlgili kişinin alenileştirmesi',N'5/2-d',0,50),
 (N'm5-2-e',N'Bir hakkın tesisi, kullanılması veya korunması',N'5/2-e',0,60),
 (N'm5-2-f',N'Meşru menfaat',N'5/2-f',0,70),
 (N'm5-1',N'Açık rıza',N'5/1',0,80),
 (N'm6-3-a',N'Açık rıza (özel nitelikli)',N'6/3-a',1,90),
 (N'm6-3-b',N'Kanunlarda açıkça öngörülme (özel nitelikli)',N'6/3-b',1,100),
 (N'm6-3-c',N'Fiili imkânsızlık (özel nitelikli)',N'6/3-c',1,110),
 (N'm6-3-c2',N'Hukuki yükümlülük (özel nitelikli)',N'6/3-ç',1,120),
 (N'm6-3-d',N'Alenileştirme (özel nitelikli)',N'6/3-d',1,130),
 (N'm6-3-e',N'Bir hakkın tesisi (özel nitelikli)',N'6/3-e',1,140),
 (N'm6-3-f',N'Sağlık/koruyucu hekimlik (sır saklama yükümlüsü)',N'6/3-f',1,150),
 (N'm6-3-g',N'İstihdam, İSG, sosyal güvenlik yükümlülükleri',N'6/3-g',1,160),
 (N'm6-3-g2',N'Vakıf/dernek/sendika üyelerine yönelik işleme',N'6/3-ğ',1,170)
) v(Code,Name,Art,Sp,So)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkLegalBases t WHERE t.Code=v.Code);

-- ===== PersonGroup =====
INSERT INTO dbo.KvkkPersonGroups (Code, Name, SortOrder, IsActive)
SELECT v.Code, v.Name, v.So, 1 FROM (VALUES
 (N'calisan',N'Çalışan',10),(N'calisanAdayi',N'Çalışan Adayı',20),(N'eskiCalisan',N'Eski Çalışan',30),
 (N'stajyer',N'Stajyer',40),(N'musteri',N'Müşteri',50),(N'potansiyelMusteri',N'Potansiyel Müşteri',60),
 (N'tedarikci',N'Tedarikçi/Yetkilisi',70),(N'isOrtagi',N'İş Ortağı/Yetkilisi',80),(N'ziyaretci',N'Ziyaretçi',90),
 (N'ucuncuKisi',N'Üçüncü Kişi',100),(N'aileYakini',N'Aile Üyesi/Yakını',110),(N'hissedar',N'Hissedar/Ortak',120),
 (N'hizmetAlan',N'Hizmet Alan Kişi',130)
) v(Code,Name,So)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkPersonGroups t WHERE t.Code=v.Code);

-- ===== RetentionRule =====
INSERT INTO dbo.KvkkRetentionRules (Code, Name, DurationText, LegalReference, SortOrder, IsActive)
SELECT v.Code, v.Name, v.Dur, v.Ref, v.So, 1 FROM (VALUES
 (N'bordro',N'Bordro / ücret hesabı',N'10 yıl',N'İş K. m.32, TBK m.146',10),
 (N'sgkGiris',N'SGK işe giriş / hizmet dökümü',N'10 yıl + emeklilik',N'5510 m.86',20),
 (N'ozlukDosyasi',N'Özlük dosyası',N'10 yıl',N'İş K. m.75',30),
 (N'adayCv',N'İşe alınmayan aday CV',N'1 yıl',N'Meşru menfaat',40),
 (N'adayCvRiza',N'Aday CV (açık rıza)',N'2 yıl',N'Açık rıza',50),
 (N'vergiBelge',N'Vergi belgeleri (fatura/defter)',N'5 yıl',N'VUK m.253',60),
 (N'ticariDefter',N'Ticari defter',N'10 yıl',N'TTK m.82',70),
 (N'musteriSozlesme',N'Müşteri sözleşmesi',N'10 yıl',N'TBK m.146',80),
 (N'eticaretSiparis',N'E-ticaret sipariş verisi',N'10 yıl',N'TTK m.82',90),
 (N'kullaniciLog',N'Kullanıcı log (IP, login)',N'2 yıl',N'5651 m.5',100),
 (N'cctv',N'CCTV kamera görüntüsü',N'30-60 gün',N'Meşru menfaat, orantılılık',110),
 (N'cagriMerkezi',N'Çağrı merkezi ses kaydı',N'1-3 yıl',N'Sözleşme + meşru menfaat',120),
 (N'ziyaretciKayit',N'Ziyaretçi giriş-çıkış',N'2 yıl',N'Meşru menfaat',130),
 (N'isKazasi',N'İş kazası belgesi',N'10 yıl + zamanaşımı',N'İş K., 5510',140),
 (N'kvkkBasvuru',N'KVKK ilgili kişi başvuru evrakı',N'3 yıl',N'KVKK m.13',150),
 (N'ihlalBildirim',N'Veri ihlali bildirim evrakı',N'5 yıl',N'Kurul standardı',160),
 (N'acikRizaKayit',N'Açık rıza kayıt evrakı',N'Saklama + 10 yıl',N'TBK m.146 ispat',170),
 (N'aydinlatmaArsiv',N'Aydınlatma metni versiyon arşivi',N'10 yıl',N'KVKK m.10',180),
 (N'pazarlamaIzin',N'Dijital pazarlama izni (İYS)',N'İYS geçerli olduğu sürece',N'6563',190),
 (N'pazarlamaVeri',N'Müşteri pazarlama verisi',N'2 yıl',N'Açık rıza',200)
) v(Code,Name,Dur,Ref,So)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkRetentionRules t WHERE t.Code=v.Code);

-- ===== DisposalMethod =====
INSERT INTO dbo.KvkkDisposalMethods (Code, Name, SortOrder, IsActive)
SELECT v.Code, v.Name, v.So, 1 FROM (VALUES
 (N'silme',N'Silme',10),(N'yokEtme',N'Yok Etme',20),(N'anonimlestirme',N'Anonim Hale Getirme',30),
 (N'karartma',N'Karartma / Maskeleme',40),(N'fizikselImha',N'Fiziksel İmha (kağıt)',50),
 (N'guvenliSilme',N'Güvenli Dijital Silme',60)
) v(Code,Name,So)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkDisposalMethods t WHERE t.Code=v.Code);

-- ===== MeasureStandard (0 idari / 1 teknik) =====
INSERT INTO dbo.KvkkMeasureStandards (Code, Name, MeasureType, SortOrder, IsActive)
SELECT v.Code, v.Name, v.Mt, v.So, 1 FROM (VALUES
 (N'politika',N'Kişisel Veri Saklama ve İmha Politikası',0,10),
 (N'egitim',N'Çalışan KVKK Eğitimi',0,20),
 (N'gizlilikSozlesme',N'Gizlilik Sözleşmeleri',0,30),
 (N'yetkiMatrisi',N'Yetki Matrisi',0,40),
 (N'disiplinSureci',N'Disiplin Süreci',0,50),
 (N'denetim',N'Periyodik Denetim',0,60),
 (N'dpa',N'Veri İşleyen Sözleşmesi (DPA)',0,70),
 (N'sifrelemeIletim',N'Şifreleme (iletim - TLS)',1,80),
 (N'sifrelemeSaklama',N'Şifreleme (saklama)',1,90),
 (N'erisimKontrol',N'Erişim Kontrolü ve Loglama',1,100),
 (N'yedekleme',N'Yedekleme',1,110),
 (N'guvenlikDuvari',N'Güvenlik Duvarı / Antivirüs',1,120),
 (N'sizmaTesti',N'Sızma Testi',1,130),
 (N'logIzleme',N'Log İzleme / SIEM',1,140),
 (N'veriMaskeleme',N'Veri Maskeleme',1,150)
) v(Code,Name,Mt,So)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkMeasureStandards t WHERE t.Code=v.Code);

-- ===== ProcessingPurpose =====
INSERT INTO dbo.KvkkProcessingPurposes (Code, Name, SortOrder, IsActive)
SELECT v.Code, v.Name, v.So, 1 FROM (VALUES
 (N'insanKaynaklari',N'İnsan Kaynakları Süreçlerinin Yürütülmesi',10),
 (N'finansMuhasebe',N'Finans ve Muhasebe İşlerinin Yürütülmesi',20),
 (N'hukukiYukumluluk',N'Hukuki Yükümlülüklerin Yerine Getirilmesi',30),
 (N'sozlesmeYonetimi',N'Sözleşme Süreçlerinin Yürütülmesi',40),
 (N'musteriIliskileri',N'Müşteri İlişkileri Yönetimi',50),
 (N'pazarlamaSurec',N'Pazarlama ve Reklam Faaliyetlerinin Yürütülmesi',60),
 (N'bilgiGuvenligi',N'Bilgi Güvenliği Süreçlerinin Yürütülmesi',70),
 (N'fizikselGuvenlik',N'Fiziksel Mekan Güvenliğinin Temini',80),
 (N'talepSikayet',N'Talep ve Şikayetlerin Takibi',90),
 (N'denetimEtik',N'Denetim / Etik Faaliyetlerinin Yürütülmesi',100)
) v(Code,Name,So)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkProcessingPurposes t WHERE t.Code=v.Code);

-- ===== Recipient (IsCrossBorder=1 yurt dışı) =====
INSERT INTO dbo.KvkkRecipients (Code, Name, IsCrossBorder, SortOrder, IsActive)
SELECT v.Code, v.Name, v.Cb, v.So, 1 FROM (VALUES
 (N'sgk',N'SGK',0,10),(N'gib',N'Vergi Dairesi / GİB',0,20),(N'mahkeme',N'Mahkeme / İcra Daireleri',0,30),
 (N'bankalar',N'Bankalar',0,40),(N'tedarikciR',N'Tedarikçiler',0,50),(N'grupSirket',N'Grup Şirketleri',0,60),
 (N'microsoft',N'Microsoft 365 (yurt dışı)',1,70),(N'google',N'Google Workspace (yurt dışı)',1,80),
 (N'aws',N'AWS (yurt dışı)',1,90),(N'azure',N'Microsoft Azure (yurt dışı)',1,100)
) v(Code,Name,Cb,So)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkRecipients t WHERE t.Code=v.Code);

PRINT 'KVKK Faz 0 lookup seed (02) tamamlandi.';
