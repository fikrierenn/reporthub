# TODO

Bu dosya yapilanlari ve kalanlari detayli takip icin kullanilir.

## Yapilanlar (tamam)
- Kullanici tablosu ve PBKDF2 sifreleme eklendi.
- PortalHUB tablolarina uygun seed scriptler duzeltildi.
- Raporlar liste + calistirma sayfasi olarak ayrildi.
- Excel disari aktarim eklendi (HTML table ile .xls).
- Parametre olusturucu ve SP parametrelerini getirme araci eklendi.
- Parametre alanlarinda placeholder/help/default destekleri eklendi.
- Ortak navbar yapisi icin `_AppLayout.cshtml` olusturuldu.
- Reports liste ve run sayfalari layout'a alindi.
- Dashboard sayfasi layout'a alindi.
- Admin ana sayfasi layout'a alindi.
- Admin CreateReport ve EditReport sayfalari layout'a alindi.
- Admin CreateDataSource ve EditDataSource sayfalari layout'a alindi.
- Home Index ve Privacy sayfalari layout'a alindi.
- Login sayfasi layout'a alindi (navbar uyumlu).
- Tekrarlanan header/footer kaldirildi.
- EditReport form action duzeltildi.
- EditReport form tag helper sorunu icin BeginForm kullanildi.
- Test sayfasi layout'a alindi.
- Navbar ve footer sabit (sticky) hale getirildi.
- Login sayfasi layout disi yapildi (navbar kaldirildi).
- Dashboard verileri canli log ve rapor verilerinden alinmaya baslandi.
- Dashboard metinleri Turkce karakterlerle guncellendi.
- Login sayfasi ve ortak layout metinleri Turkce karakterlerle guncellendi.
- Rapor calistirma sayfasinda genis gorunum anahtari ve tablo rahatligi eklendi.
- Run sayfasinda tam ekran ve tablo odak modlari kontrol edildi.
- Sonuc tablosunda arama (client-side) eklendi.
- Login ve layoutta Giris yazimi duzeltildi.
- Tum sayfalarda Turkce karakter taramasi yapildi.
- Login sayfasi ve sticky navbar/footer uyumu kontrol edildi.
- Admin rolune kullanici yonetimi eklendi (ekle/duzenle/sil).
- Rapor rol secimi checkbox listesine cevrildi.
- Profil ekrani eklendi (ad soyad, email, sifre guncelleme).
- Rol listesi hardcode olarak AdminController icinde tutuluyor.
- Admin sayfalarinda kalan bozuk Turkce metinleri temizlendi.
- Rapor rol secimi checkbox UX iyilestirmeleri tamamlandi.
- Excel export icerigine rapor bilgileri ve bos sonuc uyari metni eklendi.
- Parametre UX iyilestirmeleri tamamlandi (required vurgusu, varsayilan tarih).
- Rapor calistirma sayfasinda parametre hatalari form ustune alindi.
- Rapor calistirma tablosu icin genis gorunum/sticky header iyilestirmesi tamamlandi.
- Run sayfasinda JS kaldirildi, gorunum ve arama server-side hale getirildi.
- Sistem loglari Admin'den ayrildi, /Logs sayfasina tasindi.
- Admin sayfasindan log sorgusu kaldirildi, navbar loglara tasindi.
- Sistem loglari icin filtreleme server-side hale getirildi (JS yok).
- ViewBag kullanimlarini kritik sayfalarda ViewModel'e tasima (tamamlandi).
- Gereksiz inline CSS/HTML temizligi (tarandi, inline style bloklari tasindi).
- Script bloklarini sayfa disina (wwwroot/js) tasima.
- AuditLog tablosu ve migration scriptleri eklendi, ReportRunLog drop edildi.
- Audit log: tek log tablosu uzerinden calisildi.
- Audit log: log kapsami (login/logout, sifre, profil, kullanici/rol, rapor, veri kaynagi, export, test) eklendi.
- Audit log: alanlar (olay tipi, hedef, eski/yeni, zaman, ip/user-agent) eklendi.
- Audit log: log ekrani sadece adminde, filtre/arama aktif.
- Audit log: log listeleme UX (combo/select, arama, sayfalama) tamamlandi.
- Audit log: log yazma mekanizmasi merkezi servis ile eklendi.
- Otomatik testler eklendi (PasswordHasher, AuditLogService).
- Login, rapor calistirma, export akisi manuel smoke test tamamlandi.
- Admin rapor ekleme ve parametre uretme akisi manuel test tamamlandi.

## Devam eden isler (aktif)

## Yapilacaklar (detayli)
### 8) Iyilestirme onerileri
- Performance: rapor sonuclari icin caching, buyuk sonuc setleri icin pagination.
- Performance: connection pooling/timeout ayarlari gozenlemesi.
- Guvenlik: rate limiting (brute force korumasi), HTTPS zorunlulugu, CSP, session timeout.
- Ozellikler: scheduled reports, email export, dashboard grafiklerini zenginlestirme, rapor favorileri.
- Test: integration testleri artirma, UI test otomasyonu (Selenium), load testing.
- DevOps: CI/CD pipeline, otomatik deploy, monitoring/alerting, backup stratejisi.
### 7) Test ve kontrol
