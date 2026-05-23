---
id: isletme-dokuman-yazim
name: İşletme Doküman Yazımı
description: BKM Kitap, Belinza ve Atlas Digital Commerce için kurumsal doküman yazım disiplini. Politika, yönetmelik, prosedür, talimat ve form üretiminde tutarlılık standardı: PR/TL/YN/POL/FR kodlama, 17 bölümlük iskelet (Amaç, Kapsam, Tanımlar, Sorumluluklar, Yasal Dayanak, Süreç, Eğitim, Denetim, KVKK, Yaptırım, Kayıt-Saklama, İlgili Prosedürler, Yürürlük, Revizyon, EK formlar), Türkçe sorumluluk matrisi (Yapan/Onaylayan/Görüşü Alınan/Bilgilendirilen, RACI yasak), 4 kademeli yaptırım (Sözlü/Yazılı Savunma/İhtar/4857-25/II Fesih), KVKK ve VUK uyumlu saklama süreleri, A4 Word formatı. Prosedür/talimat/yönetmelik yaz, form üret, kod, saklama süresi, yaptırım kademesi ifadelerinde tetikle.
category: Kalite
tags: [dokuman, yazim, prosedur, talimat, yonetmelik, politika, form, PR, TL, YN, POL, FR, iskelet, kodlama, raci, sorumluluk-matrisi, yaptirim, saklama-suresi, revizyon, A4, word, BKM, Belinza, Atlas]
token_estimate: 8200
chains: [insan-kaynaklari, gida-isletme-uyum-denetim, turkiye-is-mevzuati, kvkk-veri-envanteri, turkiye-vergi-mevzuati, turkiye-sozlesme-hukuku]
---

# İşletme Doküman Yazımı: Standart ve Disiplin

> Bu skill bir akademik yazım rehberi, kullanıcı kılavuzu şablonu veya teknik dokümantasyon standardı DEĞİLDİR.
> Bir kurumsal işletme doküman üretim disiplinidir.
> Birincil hedef: BKM Kitap, Belinza ve Atlas Digital Commerce için aynı disiplin, aynı iskelet, aynı kodlama, aynı çapraz referans zinciri.

## 0. Felsefe

Bir işletmede aynı kurallar farklı dokümanlarda farklı yazılırsa:
- Personel hangisi geçerli bilemez
- Denetimde çelişen iki kural çelişkili savunmaya döner
- Revizyon yönetimi imkansızlaşır
- Tebellüğ-tebliğ ispat zinciri çöker

**Tek doğru**: Tek kurumsal yazım disiplini. Aynı iskelet, aynı kodlama, aynı saklama süresi mantığı, aynı yaptırım kademesi.

**İkinci doğru**: Anglo-jargon kurumsal dokümanda kabul edilmez. RACI değil Yapan-Onaylayan; MSDS değil GBF; brifing değil kısa toplantı.

## 1. Doküman Türleri (5 Tip)

| Tür | Kısaltma | Anlam | Detay | Sayfa |
|---|---|---|---|---|
| **Politika** | POL | Ne yaparız, ne yapmayız (ilke) | Stratejik | 1-3 |
| **Yönetmelik** | YN | Bir alandaki genel kurallar | Kavramsal | 5-15 |
| **Prosedür** | PR | Sürecin nasıl işletileceği | Akış | 10-30 |
| **Talimat** | TL | Tek görevin adımları | Adım adım | 2-10 |
| **Form** | FR | Veri toplama/kayıt aracı | Şablon | 1-3 |

**Hiyerarşi**: POL > YN > PR > TL > FR. Üst alt'ı yönlendirir.

**Örnek zincir**:
- POL-IK-01 İK Politikası → ilke
- YN-IK-01 Personel Yönetmeliği → kural
- PR-IK-15 İşe Alım Prosedürü → süreç
- TL-IK-15-01 Mülakat Soru Talimatı → adım
- FR-IK-15-01 Aday Değerlendirme Formu → kayıt

## 2. Kodlama Sistemi

Format: `TÜR-BİRİM-SIRA[-ALT]`

| Birim | Kod | Kapsam |
|---|---|---|
| İnsan Kaynakları | IK | Personel, özlük, eğitim, disiplin |
| Operasyon | OP | Üretim, mağaza, kafe, lokasyon |
| Muhasebe-Finans | MUH | Cari, kasa, fatura, bütçe |
| Bilgi İşlem | BIL | Yazılım, donanım, IT güvenlik |
| Kalite | KAL | İç denetim, ISO, şikayet |
| İSG | ISG | 6331 kapsamı |
| KVKK | KVKK | Veri envanteri, aydınlatma |
| Genel Müdürlük | GMU | Yönetim kurulu kararı |
| Satış | SAT | B2B, B2C, müşteri ilişkileri |
| Müşteri/CRM | MUS | CRM, sadakat, şikayet |
| Üretim (Belinza) | UR | Üretim hattı, kalite kontrol |
| Depo/Lojistik | WMS | Mal kabul, sevkiyat, sayım |
| Mağaza | MAG | BKM mağaza operasyonu |
| Etsy/E-ticaret | ETSY | Atlas Digital Commerce |

**Numaralandırma kuralları**:
- PR-IK-01 → İlk İK prosedürü
- PR-OP-XX-01 → Yayınlanmamış (XX) ana prosedürün ilk varyantı
- TL-IK-15-01 → PR-IK-15'in birinci talimatı
- FR-IK-15-01 → PR-IK-15'in birinci formu
- "XX" geçici; yayın öncesi gerçek koda dönüşür (İK Müd. onayı)

**BKM**: 27 prosedür + 44 form HR Manual'da. Yeni kod İK Müdürü'nden alınır.

## 3. Standart İskelet (17 Bölüm)

```
Üst Başlık Kutusu (3x3 tablo: logo, başlık, Doküman No+Yayın+Rev)

01. AMAÇ                       (1-2 paragraf — neden var)
02. KAPSAM                     (kim, nereyi kapsar)
03. TANIMLAR                   (5-10 kavram, belirsizlik bırakma)
04. SORUMLULUKLAR              (Yapan/Onaylayan matrisi)
05. YASAL DAYANAK              (kanun + yön. + RG tarih/sayı)
06. [Süreç bölüm 1]
07. [Süreç bölüm 2]
...
N-5. EĞİTİM                    (ilk + yenileme + sıklık)
N-4. DENETİM                   (kim, sıklık, raporlama)
N-3. KVKK UYUMU                (kişisel/özel veri varsa)
N-2. YAPTIRIM KADEMESİ         (4 kademe)
N-1. KAYIT, FORM VE SAKLAMA    (form listesi + saklama süresi)
N.   İLGİLİ PROSEDÜRLER        (çapraz referans)
+1.  YÜRÜRLÜK                  (onaylayan + revizyon takvimi)
+2.  REVİZYON GEÇMİŞİ          (tablo)
+3.  İmza bloğu                (Hazırlayan/Kontrol/Onay)
+4.  EKLER                     (formlar, talimatlar)
```

Bilinçli atlama OK; doküman türüne göre detay düzeyi (Bölüm 12 Severity).

## 4. Sorumluluk Matrisi — Anglo Yasağı

| Yanlış | Doğru |
|---|---|
| RACI | Sorumluluk Matrisi |
| Responsible / R | Yapan |
| Accountable / A | Onaylayan |
| Consulted / C | Görüşü Alınan |
| Informed / I | Bilgilendirilen |

Doküman içinde "RACI" geçmez. Tablo başlığı Türkçe.

Kurallar:
- **Yapan**: birden fazla olabilir
- **Onaylayan**: her zaman tek rol/kişi (tek kapı)
- **Görüşü Alınan**: danışmanlık (İSG, İşyeri Hekimi)
- **Bilgilendirilen**: sonradan bilgi alır
- Tire (—) "yok"

## 5. 4 Kademeli Yaptırım Standardı

| Kademe | Tetikleyen | Uygulama | Yetki |
|---|---|---|---|
| **1. Sözlü Uyarı** | İlk küçük ihlal | Sözlü + gözlem formu | Lokasyon/Birim Yön. |
| **2. Yazılı Savunma** | Tekrar/orta düzey | 7 gün içinde yazılı + özlük | İK Müdürü |
| **3. Yazılı İhtar** | Savunma yetersiz/tekrar | Resmi tebliğ | İK Müd. + GM |
| **4. İş Akdi Feshi** | Ağır/tekrarlanan | 4857 m.25/II + dosya | GM + Hukuk |

**Kademe atlama istisnası**: bulaşıcı gizleme, kasıtlı tehlike, alkol/uyuşturucu, müşteri sağlığı tehdidi, kontaminasyon → doğrudan Kademe 4.

**Yasak**: 2, 3 veya 5+ kademeli alternatifler. Tek standart 4 kademe.

## 6. Kayıt, Form ve Saklama Süresi

Her doküman sonunda zorunlu tablo: `Form Kodu | Kayıt Adı | Sahibi | Saklama Süresi`

### 6.1 Saklama Süresi Kütüphanesi

| Belge | Süre | Dayanak |
|---|---|---|
| Tebellüğ formu, hijyen tebliği | İş + 10 yıl | KVKK + İK |
| İhlal tutanağı | İş + 10 yıl | KVKK + dava |
| Sözleşme nüshaları | Süre + 10 yıl | TBK + dava |
| İşe Giriş Sağlık Raporu | İş + 10 yıl | 6331 + KVKK m.6 |
| Periyodik sağlık muayene | Son + bir önceki, sonra imha | KVKK saklama-imha |
| Hijyen sertifikası | Geçerlilik + 5 yıl | Hijyen Eğit. Yön. |
| Günlük operasyon form | 2 yıl | HACCP + iç denetim |
| Aylık iç denetim rapor | 5 yıl | İç kontrol |
| Soğutucu sıcaklık kayıt | 2 yıl | HACCP |
| Kimyasal GBF | Ürün geçerlilik + 5 yıl | 6331 + Çevre |
| Haşere kontrol tutanağı | 5 yıl | Halk sağlığı |
| Mali kayıt (fatura, fiş, defter) | 5 yıl | VUK 213 m.253 |
| Bordro ve SGK | 10 yıl | 5510 + 4857 |
| Atık beyan + UATF | 5 yıl | 2872 Çevre |
| KVKK Aydınlatma + Açık Rıza | İş + 10 yıl | KVKK m.11 |
| İSG eğitim kaydı | 10 yıl | 6331 m.17 |
| VERBİS kaydı | Güncel + tarihçe | KVKK |

**Kural**: Saklama süresi keyfi belirlenemez. Dayanak ya KVKK, 4857, VUK veya özel mevzuata atıf.

## 7. Revizyon Yönetimi

### 7.1 Numaralandırma
- Rev. 00: İlk yayın
- Rev. 01, 02, 03... her revizyonda artar
- Rev. 01.1 **YASAK** — ufak düzeltme de Rev. 02

### 7.2 Revizyon Geçmişi Tablosu (zorunlu)

`Rev. No | Tarih | Değişiklik Açıklaması | Hazırlayan`

Açıklama somut + ayrıntılı. "Düzeltmeler yapıldı" YASAK. Doğru:
> "Mevzuat güncellemesi: 02.11.2011/663 KHK ile portör muayenesi rejiminin kaldırılması yansıtıldı. Madde 6.2 ve 10.1 İSG sağlık raporu olarak revize edildi. EK-1 Tebellüğ Formunda portör alanı kaldırıldı."

### 7.3 Tetikleyiciler
- Yasal değişim (30 gün içinde)
- İç denetim bulgusu
- Bakanlık denetim uyarısı
- Süreç değişimi (yeni ekipman, lokasyon)
- Personel geri bildirim (3+ aynı şikayet)
- Yıllık gözden geçirme

## 8. Yasal Dayanak Disiplini

Format:
- Kanun: "5996 sayılı Kanun (RG 13.06.2010, sayı 27610)"
- Yönetmelik: "Gıda Hijyeni Yönetmeliği (RG 17.12.2011, sayı 28145)"
- KHK ile değişiklik: "1593 UHK madde 126 — 02.11.2011/663 KHK ile değişik"
- Tebliğ: "Çevre Kanunu 2026/1 İdari Para Cezası Tebliği (RG 30.12.2025, sayı 33123)"
- Standart: "TS-EN-ISO 22000"

RG tarih/sayı belirsizlik bırakmaz.

**Confidence Discipline**: yıllık değişen (idari para cezası, yeniden değerleme) → "YYYY/1 tebliğden teyit zorunlu" notu.

Skill chain:
- Gıda → gida-isletme-uyum-denetim
- İş+İSG → turkiye-is-mevzuati
- KVKK → kvkk-veri-envanteri
- Vergi → turkiye-vergi-mevzuati
- Sözleşme → turkiye-sozlesme-hukuku

## 9. Çapraz Referans

### İlgili Prosedürler Bölümü
> "Bu prosedür aşağıdaki dokümanlarla birlikte uygulanır:"
- PR-IK-XX Personel Yönetmeliği
- PR-IK-XX Disiplin Prosedürü
- POL-IK-01 İK Politikası

### Metin İçi Atıflar
- Doğru: "Bölüm 12 Yaptırım Kademesi" veya "PR-IK-XX madde 5"
- YASAK: "yukarıda belirtildiği gibi", "ilgili yerlerde"

### Kod Tutarlılığı
PR-IK-XX placeholder ise atıfta da PR-IK-XX. Yayın öncesi İK Müd. find-replace.

## 10. Dil Disiplini — Anglo Yasakları

| Yasak | Doğru |
|---|---|
| RACI | Sorumluluk Matrisi |
| MSDS | GBF (Güvenlik Bilgi Formu) |
| KPI | Performans Göstergesi (parantez içinde KPI) |
| FIFO | İlk Giren İlk Çıkar (parantez içinde FIFO) |
| Brifing | Kısa Toplantı |
| Checklist | Kontrol Listesi |
| Pre-Production | Üretim Öncesi |
| Standby | Beklemede |
| Backup | Yedekleme |
| Workshop | Çalıştay |
| Stakeholder | İlgili Taraf |
| Onboarding | İşe Alıştırma / Oryantasyon |
| Outsourcing | Dış Kaynak Kullanımı |
| Compliance | Uyum |

**İstisna (kullanılabilir)**: HACCP, UATF, EÇBS, VERBİS, POS, ERP, CRM, WMS, ISO, ASTM, EN.

**Kural**: Anglo terim ilk geçişte parantez içinde Türkçe; sonra serbest. Türkçe karşılığı varsa Türkçe tercih.

## 11. Severity Filter — Detay Düzeyi

| Doküman Türü | Risk | Detay | Bölüm |
|---|---|---|---|
| Yasal Uyum Prosedürü | Yüksek | Tam 17 + 3-5 form + matris | Tüm |
| Operasyonel Prosedür | Orta | 10-12 + 2-3 form + ölçülebilir | Atlanan 5, 13 |
| Yönetim Prosedürü (politika) | Düşük | 5-7 + kavramsal + tek form | Atlanan 8, 9, 11 |
| Talimat (tek görev) | Düşük | 3-5 + adım + tek form | Çoğu atlandı |
| Form | - | Tablo + imza + üst başlık | Bölüm yok |

## 12. Confidence Discipline

| Eşik | Confidence | Doğrulama |
|---|---|---|
| Mevzuat eşiği (4857 m.14) | High | Direkt madde |
| Standart eşik (DSÖ 40 sn el yıkama) | High | DSÖ/Sağlık Bak. rehber |
| Üretici eşiği (4 saat eldiven) | Medium | Üretici/sektör |
| İdari para cezası tutarı | Low | Yıllık tebliğ |
| BKM iç politika | Low | İK/Muhasebe onayı |

**Low Confidence**: "YYYY/1 tebliğden teyit zorunlu" notu eklenir.

## 13. Operational Memory (BKM Pattern)

### BKM Kitap HR Procedures Manual
- 27 prosedür + 44 form
- İK Müdürü kod listesi tutar
- PR-IK-XX placeholder → gerçek kod (İK Müd. onayı)
- Tüm BKM aynı başlık kutusu, header/footer, imza bloğu

### BKM Kafe Pilot (2026)
- PR-IK-XX Kafe Kişisel Hijyen Prosedürü (Rev01 yayında)
- PR-OP-XX-01/02/03 Açılış-Kapanış / Temizlik / Atık
- El Yıkama Posteri (EK-5)
- İskelet bu skill standardı

### Belinza
- PR-UR-XX üretim (banyo aksesuar, batarya, mobilya, duşakabin)
- PR-WMS-XX depo
- PR-SAT-XX B2B
- Maliyet gizlilik politikası

### Atlas Digital Commerce
- POL-ETSY-01 ATLASCOREUS Marka Politikası
- PR-ETSY-XX listing üretim
- ABD hukuku (abd-sozlesme-hukuku skill)

### Tipik Tuzaklar

| Tuzak | Doğru |
|---|---|
| Aynı kuralı 3 dokümana kopya | Tek yer + çapraz referans |
| Yasal dayanak kopya-yapıştır | Her dokümanda kendi süzgeci |
| Form numarası icat | İK Müd. onayı şart |
| Anglo jargon | Bölüm 10 yasak listesi |
| Saklama süresi belirsiz | Bölüm 6 kütüphane + mevzuat |
| Yaptırım kademesi farklı | Bölüm 5 — 4 kademe |
| Revizyon notu muğlak | Bölüm 7.2 — somut |
| PR-IK-XX yayında | Find-replace zorunlu |

## 14. Format (Word A4)

- Boyut: A4 portrait
- Kenar: 1080 DXA (~1.9 cm)
- Font: Calibri 11pt gövde, 12-14pt başlık
- Renk: Başlık 1 #1F3864, Başlık 2 #2E74B5, tablo başlık #D9E2F3, vurgu #C8102E, form zemin #F2F2F2

### Üst Başlık Kutusu (3x3 tablo)
- Sol: Logo + slogan
- Orta: Doküman başlığı
- Sağ: Doküman No / Yayın Tarihi / Revizyon No+Tarih

### Header / Footer
- Header (sağ üst, gri 9pt): "PR-IK-XX | Doküman Adı"
- Footer (orta alt): "Sayfa X / Y"

### İmza Bloğu (3 imza zinciri)
- Hazırlayan: [İK Müd. / Operasyon Müd. / vb.]
- Kontrol: [Kalite / İSG / Hukuk]
- Onaylayan: Genel Müdür

## 15. Yayın Öncesi Kontrol Listesi

```
[ ] Doküman No verildi mi (İK Müd. onayı)
[ ] Yayın Tarihi + Rev. doldu mu
[ ] Tanımlar bölümü var mı
[ ] Sorumluluk matrisi tutarlı mı (metinle uyum)
[ ] Yasal Dayanak güncel mi (mevzuat skill teyit)
[ ] Kayıt-Saklama tablosu tam mı (her form için satır)
[ ] Yaptırım kademesi standart mı (4 kademe)
[ ] KVKK bölümü var mı (kişisel/müşteri veri varsa)
[ ] EK formlar ana prosedürle uyumlu mu
[ ] PR-XX-XX placeholder'lar gerçek koda dönüştü mü
[ ] Anglo jargon kontrolü yapıldı mı
[ ] İmza bloğu tam mı
[ ] Revizyon Geçmişi güncel mi
[ ] Hukuk Müşaviri görüşü (önemli prosedür)
[ ] İSG Uzmanı görüşü (İSG kısım)
[ ] KVKK Sorumlusu görüşü (kişisel veri)
```

## 16. Mutlak Yasaklar

- Anglo jargon (RACI, MSDS, KPI, brifing, checklist)
- Kademe sayısı dokümandan dokümana farklı
- Saklama süresi keyfi (mevzuat dayanağı şart)
- "Yukarıda belirtildiği gibi", "ilgili yerlerde", "gerekli görüldüğünde"
- Personel adına atıf (rol/pozisyon yazılır)
- Doküman No / Rev / Tarih boş yayın
- Yasal madde referansı verilmeyen yaptırım rakamı
- 27 BKM HR Manual prosedürü ile çatışan içerik

## 17. Final Behavior Rule

Akademik yazım rehberi veya teknik dokümantasyon mimarı GİBİ DAVRANMA. Roller:
- Kurumsal doküman mimarı
- Süreç sahibi koordinatörü
- Yasal uyum belge zinciri koruyucusu
- BKM/Belinza/Atlas standart kütüphanecisi

**Birincil hedef**: Aynı disiplinde, aynı iskelette, aynı kodlama, aynı saklama mantığı, aynı yaptırım sistemi, aynı dil. Personel okuyabilmeli, denetçi inceleyebilmeli, hukuk müşaviri savunabilmeli.

**Versiyon**: v1.0 (Mayıs 2026).
