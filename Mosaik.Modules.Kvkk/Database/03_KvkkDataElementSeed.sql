-- Plan 54 M6 / Plan 40 Faz 0 — DataElement seed (~58 atomic öğe, idempotent).
-- IsSpecialCategory kategoriden devralınır. Aliases reverse-search (Türkçe-duyarsız) için.
SET NOCOUNT ON;

INSERT INTO dbo.KvkkDataElements (ElementCode, DisplayName, DataCategoryId, IsSpecialCategory, Aliases, IsActive)
SELECT v.Code, v.Name, c.Id, c.IsSpecialCategory, v.Aliases, 1
FROM (VALUES
 -- Kimlik
 (N'person.fullname',N'Ad-Soyad',N'kimlik',N'ad,soyad,isim,name,adsoyad'),
 (N'person.tckn',N'TC Kimlik No',N'kimlik',N'tc,tckn,kimlik no,tc kimlik'),
 (N'person.birthdate',N'Doğum Tarihi',N'kimlik',N'doğum,dogum tarihi,birthdate'),
 (N'person.birthplace',N'Doğum Yeri',N'kimlik',N'doğum yeri'),
 (N'person.gender',N'Cinsiyet',N'kimlik',N'cinsiyet,gender'),
 (N'person.maritalstatus',N'Medeni Hal',N'kimlik',N'medeni hal'),
 (N'person.nationality',N'Uyruk',N'kimlik',N'uyruk'),
 (N'person.parentname',N'Anne-Baba Adı',N'kimlik',N'anne adı,baba adı'),
 (N'person.idserial',N'Nüfus Cüzdanı Seri No',N'kimlik',N'seri no,cüzdan'),
 (N'person.signature',N'İmza',N'kimlik',N'imza,signature'),
 -- İletişim
 (N'contact.address',N'Adres',N'iletisim',N'adres,address'),
 (N'contact.email',N'E-posta',N'iletisim',N'email,e-posta,eposta,mail'),
 (N'contact.phone',N'Telefon',N'iletisim',N'telefon,phone,gsm,cep'),
 (N'contact.kep',N'KEP Adresi',N'iletisim',N'kep'),
 -- Lokasyon
 (N'location.gps',N'GPS / Konum',N'lokasyon',N'konum,gps,location'),
 (N'location.vehicle',N'Araç Takip',N'lokasyon',N'araç takip,plaka'),
 -- Özlük
 (N'hr.payroll',N'Bordro',N'ozluk',N'bordro,maaş,ücret,payroll'),
 (N'hr.disciplinary',N'Disiplin Kaydı',N'ozluk',N'disiplin'),
 (N'hr.attendance',N'İşe Giriş-Çıkış Kaydı',N'ozluk',N'puantaj,giriş çıkış,pdks'),
 (N'hr.cv',N'Özgeçmiş / CV',N'ozluk',N'cv,özgeçmiş,ozgecmis'),
 (N'hr.performance',N'Performans Değerlendirme',N'ozluk',N'performans'),
 -- Mesleki Deneyim
 (N'edu.diploma',N'Eğitim / Diploma',N'meslekiDeneyim',N'diploma,eğitim,transkript'),
 (N'edu.certificate',N'Sertifika',N'meslekiDeneyim',N'sertifika'),
 -- Finans
 (N'finance.iban',N'IBAN',N'finans',N'iban,hesap no,banka hesabı'),
 (N'finance.creditscore',N'Kredi Notu',N'finans',N'kredi notu'),
 (N'finance.assets',N'Mal Varlığı',N'finans',N'mal varlığı'),
 (N'finance.balance',N'Bilanço',N'finans',N'bilanço'),
 -- Müşteri İşlem
 (N'customer.order',N'Sipariş Bilgisi',N'musteriIslem',N'sipariş,order'),
 (N'customer.invoice',N'Fatura',N'musteriIslem',N'fatura,invoice'),
 (N'customer.callrecord',N'Çağrı Kaydı',N'musteriIslem',N'çağrı kaydı,call'),
 (N'customer.complaint',N'Talep / Şikayet',N'musteriIslem',N'talep,şikayet'),
 -- İşlem Güvenliği
 (N'security.ip',N'IP Adresi',N'islemGuvenligi',N'ip,ip adresi'),
 (N'security.loginlog',N'Login/Logout Log',N'islemGuvenligi',N'log,login,oturum'),
 (N'security.password',N'Şifre / Parola',N'islemGuvenligi',N'şifre,parola,password'),
 (N'security.mac',N'MAC Adresi',N'islemGuvenligi',N'mac'),
 -- Fiziksel Mekan Güvenliği
 (N'cctv.video',N'CCTV Görüntü',N'fizikselMekan',N'kamera,cctv,güvenlik kamerası,görüntü'),
 (N'cctv.audio',N'CCTV Ses',N'fizikselMekan',N'kamera ses'),
 (N'physical.entrylog',N'Bina Giriş-Çıkış Kaydı',N'fizikselMekan',N'kapı,bina giriş,turnike'),
 -- Pazarlama
 (N'marketing.history',N'Alışveriş Geçmişi',N'pazarlama',N'alışveriş geçmişi'),
 (N'marketing.cookie',N'Çerez',N'pazarlama',N'çerez,cookie'),
 (N'marketing.survey',N'Anket',N'pazarlama',N'anket,survey'),
 (N'marketing.consent',N'Pazarlama İzni',N'pazarlama',N'pazarlama izni,iys'),
 -- Görsel ve İşitsel
 (N'media.photo',N'Fotoğraf',N'gorselIsitsel',N'fotoğraf,foto,photo,resim'),
 (N'media.voice',N'Ses Kaydı',N'gorselIsitsel',N'ses kaydı,voice'),
 -- Hukuki İşlem
 (N'legal.case',N'Dava Dosyası',N'hukukiIslem',N'dava,dosya'),
 (N'legal.poa',N'Vekâletname',N'hukukiIslem',N'vekalet,vekâletname'),
 -- Sağlık (özel)
 (N'health.report',N'Sağlık Raporu',N'saglik',N'sağlık raporu,rapor'),
 (N'health.bloodtype',N'Kan Grubu',N'saglik',N'kan grubu'),
 (N'health.disability',N'Engellilik Durumu',N'saglik',N'engelli,engellilik'),
 -- Biyometrik (özel)
 (N'biometric.fingerprint',N'Parmak İzi',N'biyometrik',N'parmak izi,fingerprint,pdks parmak'),
 (N'biometric.face',N'Yüz Tanıma',N'biyometrik',N'yüz tanıma,face'),
 (N'biometric.retina',N'Retina',N'biyometrik',N'retina'),
 -- Ceza Mahkûmiyeti (özel)
 (N'criminal.record',N'Adli Sicil',N'cezaMahkumiyeti',N'adli sicil,sabıka,ceza sicili'),
 -- İnanç (özel)
 (N'belief.religion',N'Din / Mezhep',N'inanc',N'din,mezhep'),
 -- Üyelik (özel)
 (N'membership.union',N'Sendika Üyeliği',N'uyelik',N'sendika'),
 -- Etnik (özel)
 (N'ethnic.origin',N'Etnik Köken',N'irkEtnik',N'etnik,ırk'),
 -- Genetik (özel)
 (N'genetic.data',N'Genetik Veri',N'genetik',N'genetik,dna'),
 -- Cinsel Hayat (özel)
 (N'sexual.life',N'Cinsel Hayat',N'cinselHayat',N'cinsel hayat'),
 -- Siyasi Düşünce (özel)
 (N'political.opinion',N'Siyasi Düşünce',N'siyasiDusunce',N'siyasi'),
 -- Kılık Kıyafet (özel)
 (N'appearance.dress',N'Kılık Kıyafet',N'kilikKiyafet',N'kılık,kıyafet')
) v(Code,Name,CatCode,Aliases)
JOIN dbo.KvkkDataCategories c ON c.Code = v.CatCode
WHERE NOT EXISTS (SELECT 1 FROM dbo.KvkkDataElements t WHERE t.ElementCode = v.Code);

PRINT 'KVKK Faz 0 DataElement seed (03) tamamlandi.';
