---
id: bkm-dokuman-yazim
name: BKM Kitap Doküman Yazımı
description: BKM Kitap için kurumsal doküman yazım disiplini. Politika, yönetmelik, prosedür, talimat ve form üretiminde tutarlılık: PR/TL/YN/POL/FR kodlama, 17 bölümlük iskelet (Amaç, Kapsam, Tanımlar, Sorumluluklar, Yasal Dayanak, Süreç, Eğitim, Denetim, KVKK, Yaptırım, Kayıt-Saklama, İlgili Prosedürler, Yürürlük, Revizyon, EK formlar), Türkçe sorumluluk matrisi (Yapan/Onaylayan/Görüşü Alınan/Bilgilendirilen, RACI yasak), 4 kademeli yaptırım (Sözlü/Yazılı Savunma/İhtar/4857-25/II Fesih), KVKK ve VUK uyumlu saklama süreleri, A4 Word formatı. BKM HR Manual, Kafe ve Sahaf pilot, 3 mağaza operasyon setine uyumlu üretim.
category: Kalite
tags: [BKM, BKM-Kitap, dokuman, yazim, prosedur, talimat, yonetmelik, politika, form, PR, TL, YN, POL, FR, iskelet, kodlama, raci, sorumluluk-matrisi, yaptirim, saklama-suresi, revizyon, kafe-pilot, sahaf-pilot, magaza, istanbul-yolu, yildirim, gorukle]
token_estimate: 8500
chains: [insan-kaynaklari, gida-isletme-uyum-denetim, turkiye-is-mevzuati, kvkk-veri-envanteri, turkiye-vergi-mevzuati, turkiye-sozlesme-hukuku]
---

# BKM Kitap Doküman Yazımı: Standart ve Disiplin

> BKM Kitap ve bağlı operasyonları (mağaza, kafe, sahaf) için kurumsal doküman üretim disiplini.
> Birincil hedef: Aynı disiplin, aynı iskelet, aynı kodlama, aynı çapraz referans.
> Sadece BKM Kitap kapsamında; başka iş kollarına genelleştirilmez.

## 0. Felsefe

Tek doğru: BKM Kitap için tek kurumsal yazım disiplini. Aynı iskelet, aynı kodlama, aynı saklama mantığı, aynı yaptırım kademesi.

İkinci doğru: Anglo-jargon yasak. RACI değil Yapan-Onaylayan; MSDS değil GBF.

## 1. Doküman Türleri

| Tür | Kısalt | Anlam | Sayfa |
|---|---|---|---|
| **Politika** | POL | Ne yaparız, ne yapmayız (ilke) | 1-3 |
| **Yönetmelik** | YN | Bir alandaki genel kurallar | 5-15 |
| **Prosedür** | PR | Sürecin nasıl işletileceği | 10-30 |
| **Talimat** | TL | Tek görevin adımları | 2-10 |
| **Form** | FR | Veri toplama/kayıt | 1-3 |

**Hiyerarşi**: POL > YN > PR > TL > FR.

Örnek zincir:
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
| Operasyon | OP | Mağaza, kafe, sahaf, lokasyon |
| Muhasebe-Finans | MUH | Cari, kasa, fatura, bütçe |
| Bilgi İşlem | BIL | Yazılım, donanım, IT güvenlik |
| Kalite | KAL | İç denetim, ISO, şikayet |
| İSG | ISG | 6331 İSG |
| KVKK | KVKK | Veri envanter, aydınlatma, ihlal |
| Genel Müdürlük | GMU | YK kararı |
| Satış | SAT | B2B, B2C, müşteri ilişki |
| Müşteri/CRM | MUS | CRM, sadakat, şikayet |
| **Mağaza** | MAG | 3 lokasyon operasyon |
| **Kafe** | KAF | Kafe pilot |
| **Sahaf** | SAH | Sahaf pilot (İstanbul Yolu) |
| Pazarlama | PAZ | Etkinlik, kampanya |

**Numaralandırma**:
- PR-IK-01 → İlk İK prosedürü
- PR-OP-XX-01 → Yayınlanmamış (XX) ana prosedür ilk varyantı
- TL-IK-15-01 → PR-IK-15'in birinci talimatı
- FR-IK-15-01 → PR-IK-15'in birinci formu
- "XX" geçici; İK Müd. onayıyla gerçek koda dönüşür

**Mevcut harita**:
- 27 prosedür + 44 form HR Manual'da
- İK Müd. kod listesi tutar
- Kafe pilot: PR-IK-XX (Hijyen) + PR-OP-XX-01/02/03 planlandı
- Sahaf pilot: PR-OP-XX planlanıyor

## 3. Standart İskelet (17 Bölüm)

```
Üst Başlık Kutusu (3x3 tablo)
- Sol: BKM Kitap logo + "bir kitapla mümkün!"
- Orta: Doküman başlığı
- Sağ: Doküman No, Yayın, Rev. No+Tarih

01. AMAÇ                       (1-2 paragraf — neden)
02. KAPSAM                     (kim, nereyi kapsar)
03. TANIMLAR                   (5-10 kavram)
04. SORUMLULUKLAR              (Yapan/Onaylayan matrisi)
05. YASAL DAYANAK              (kanun + yön. + RG tarih/sayı)
06. [Süreç bölüm 1]
07. [Süreç bölüm 2]
...
N-5. EĞİTİM                    (ilk + yenileme + sıklık)
N-4. DENETİM                   (kim, sıklık, raporlama)
N-3. KVKK UYUMU                (kişisel/özel veri varsa)
N-2. YAPTIRIM KADEMESİ         (4 kademe)
N-1. KAYIT, FORM VE SAKLAMA    (form listesi + saklama)
N.   İLGİLİ PROSEDÜRLER        (çapraz referans)
+1.  YÜRÜRLÜK                  (onaylayan + revizyon takvimi)
+2.  REVİZYON GEÇMİŞİ          (tablo)
+3.  İmza bloğu                (Hazırlayan/Kontrol/Onay)
+4.  EKLER                     (formlar, talimatlar)
```

Bilinçli atlama OK; doküman türüne göre detay düzeyi (Bölüm 11 Severity).

## 4. Sorumluluk Matrisi — Anglo Yasağı

| Yasak | Doğru |
|---|---|
| RACI | Sorumluluk Matrisi |
| Responsible / R | Yapan |
| Accountable / A | Onaylayan |
| Consulted / C | Görüşü Alınan |
| Informed / I | Bilgilendirilen |

Doküman içinde "RACI" GEÇMEZ. Tablo Türkçe.

Açıklama paragrafı (zorunlu):
> "Aşağıdaki tabloda her süreç adımı için kim yapar, kim onaylar, kimin görüşü alınır ve kim bilgilendirilir gösterilmiştir. Tek bir adımda yalnızca bir onaylayan kişi bulunur."

Kurallar:
- **Yapan**: birden fazla olabilir
- **Onaylayan**: tek rol/kişi (tek kapı)
- **Görüşü Alınan**: danışmanlık (İSG, İşyeri Hekimi)
- **Bilgilendirilen**: sonradan bilgi
- Tire (—) "yok"

## 5. 4 Kademeli Yaptırım

| Kademe | Tetik | Uygulama | Yetki |
|---|---|---|---|
| **1. Sözlü Uyarı** | İlk küçük ihlal | Sözlü + gözlem formu | Lokasyon/Birim Yön. |
| **2. Yazılı Savunma** | Tekrar/orta | 7 gün içinde yazılı + özlük | İK Müdürü |
| **3. Yazılı İhtar** | Savunma yetersiz | Resmi tebliğ | İK Müd. + GM |
| **4. İş Akdi Feshi** | Ağır/tekrarlanan | 4857 m.25/II + dosya | GM + Hukuk |

**Kademe atlama istisnası**: bulaşıcı gizleme, kasıtlı tehlike, alkol/uyuşturucu, müşteri sağlığı tehdit, kontaminasyon → doğrudan Kademe 4.

**Yasak**: 2/3/5+ kademeli alternatif. Tek standart 4 kademe.

## 6. Kayıt, Form ve Saklama

Her doküman sonunda zorunlu tablo: `Form Kodu | Kayıt Adı | Sahibi | Saklama`

### 6.1 Saklama Kütüphanesi

| Belge | Süre | Dayanak |
|---|---|---|
| Tebellüğ, hijyen tebliği | İş + 10 yıl | KVKK + İK |
| İhlal tutanağı | İş + 10 yıl | KVKK + dava |
| Sözleşme nüshaları | Süre + 10 yıl | TBK + dava |
| İşe Giriş Sağlık Raporu | İş + 10 yıl | 6331 + KVKK m.6 |
| Periyodik sağlık muayene | Son + bir önceki, sonra imha | KVKK |
| Hijyen sertifikası | Geçerlilik + 5 yıl | Hijyen Eğit. Yön. |
| Günlük operasyon kontrol form | 2 yıl | HACCP + iç denetim |
| Aylık iç denetim rapor | 5 yıl | İç kontrol |
| Soğutucu sıcaklık | 2 yıl | HACCP |
| Kimyasal GBF | Ürün geçerlilik + 5 yıl | 6331 + Çevre |
| Haşere kontrol | 5 yıl | Halk sağlığı |
| Mali kayıt (fatura, fiş) | 5 yıl | VUK 213 m.253 |
| Bordro + SGK | 10 yıl | 5510 + 4857 |
| Atık beyan + UATF | 5 yıl | 2872 Çevre |
| KVKK Aydınlatma + Açık Rıza | İş + 10 yıl | KVKK m.11 |
| İSG eğitim | 10 yıl | 6331 m.17 |
| VERBİS kayıt | Güncel + tarihçe | KVKK |

**Kural**: Saklama süresi keyfi belirlenemez. Dayanak ya KVKK, 4857, VUK veya özel mevzuat.

## 7. Revizyon Yönetimi

### 7.1 Numaralandırma
- Rev. 00: İlk yayın
- Rev. 01, 02, 03... her revizyon
- Rev. 01.1 YASAK — ufak düzeltme = Rev. 02

### 7.2 Geçmiş Tablosu (zorunlu)

`Rev. No | Tarih | Değişiklik | Hazırlayan`

Somut + ayrıntılı. "Düzeltmeler yapıldı" YASAK. Doğru:
> "Mevzuat güncellemesi: 02.11.2011/663 KHK ile portör muayenesi rejiminin kaldırılması yansıtıldı. Madde 6.2 ve 10.1 İSG sağlık raporu olarak revize edildi. EK-1 Tebellüğ Formunda portör alanı kaldırıldı."

### 7.3 Tetikleyiciler
- Yasal değişim (30 gün içi)
- İç denetim bulgusu
- Bakanlık denetim uyarısı
- Süreç değişimi
- Personel geri bildirim (3+ aynı şikayet)
- Yıllık gözden geçirme

## 8. Yasal Dayanak Disiplini

Format:
- Kanun: "5996 sayılı Kanun (RG 13.06.2010, sayı 27610)"
- Yönetmelik: "Gıda Hijyeni Yönetmeliği (RG 17.12.2011, sayı 28145)"
- KHK değişiklik: "1593 UHK m.126 — 02.11.2011/663 KHK ile değişik"
- Tebliğ: "Çevre 2026/1 İdari Para Cezası Tebliği (RG 30.12.2025, sayı 33123)"
- Standart: "TS-EN-ISO 22000"

RG tarih/sayı belirsizlik bırakmaz.

**Confidence**: yıllık değişen (idari ceza, yeniden değerleme) → "YYYY/1 tebliğden teyit zorunlu".

Skill chain:
- Gıda → gida-isletme-uyum-denetim
- İş+İSG → turkiye-is-mevzuati
- KVKK → kvkk-veri-envanteri
- Vergi → turkiye-vergi-mevzuati
- Sözleşme → turkiye-sozlesme-hukuku

## 9. Çapraz Referans

### İlgili Prosedürler
> "Bu prosedür aşağıdaki dokümanlarla birlikte uygulanır:"
- PR-IK-XX Personel Yönetmeliği
- PR-IK-XX Disiplin Prosedürü
- POL-IK-01 İK Politikası

### Metin İçi
- Doğru: "Bölüm 12 Yaptırım Kademesi" / "PR-IK-XX madde 5"
- YASAK: "yukarıda belirtildiği gibi", "ilgili yerlerde"

### Kod Tutarlılığı
PR-IK-XX placeholder ise atıfta da PR-IK-XX. Yayın öncesi İK Müd. find-replace.

## 10. Anglo Yasakları

| Yasak | Doğru |
|---|---|
| RACI | Sorumluluk Matrisi |
| MSDS | GBF (Güvenlik Bilgi Formu) |
| KPI | Performans Göstergesi |
| FIFO | İlk Giren İlk Çıkar |
| Brifing | Kısa Toplantı |
| Checklist | Kontrol Listesi |
| Pre-Production | Üretim Öncesi |
| Standby | Beklemede |
| Backup | Yedekleme |
| Workshop | Çalıştay |
| Stakeholder | İlgili Taraf |
| Onboarding | İşe Alıştırma / Oryantasyon |
| Outsourcing | Dış Kaynak Kullanımı |
| KKI | Kritik Kontrol Noktası |
| Compliance | Uyum |

**İstisna**: HACCP, UATF, EÇBS, VERBİS, POS, ERP, CRM, WMS, ISO, ASTM, EN, Backflush.

**Kural**: Anglo terim ilk geçişte parantez içinde Türkçe; sonra serbest.

## 11. Severity Filter — Detay Düzeyi

| Tür | Risk | Detay | Bölüm |
|---|---|---|---|
| Yasal Uyum Prosedürü | Yüksek | Tam 17 + 3-5 form + matris | Tüm |
| Operasyonel Prosedür | Orta | 10-12 + 2-3 form + ölçülebilir | Atlanan 5, 13 |
| Yönetim Prosedürü | Düşük | 5-7 + kavramsal + tek form | Atlanan 8, 9, 11 |
| Talimat | Düşük | 3-5 + adım + tek form | Çoğu atlandı |
| Form | - | Tablo + imza + üst başlık | Bölüm yok |

## 12. Confidence Discipline

| Eşik | Confidence | Doğrulama |
|---|---|---|
| Mevzuat eşik (4857 m.14) | High | Direkt madde |
| Standart eşik (DSÖ 40 sn) | High | DSÖ/Sağlık Bak. |
| Üretici eşik (4 saat eldiven) | Medium | Üretici/sektör |
| İdari para cezası | Low | Yıllık tebliğ |
| BKM iç politika | Low | İK/Muhasebe onay |

**Low**: "YYYY/1 tebliğden teyit zorunlu" notu zorunlu.

## 13. BKM Operational Memory

### 13.1 BKM HR Procedures Manual
- 27 prosedür + 44 form, iki Word dosyası
- İK Müd. kod listesi
- PR-IK-XX placeholder → gerçek kod
- Tüm BKM aynı başlık kutusu, header/footer, imza bloğu
- Zirve Yazılım entegrasyon referansları İK Manual'da

### 13.2 BKM Kafe Pilot (2026)
- PR-IK-XX Kafe Kişisel Hijyen (Rev01 yayında)
- PR-OP-XX-01 Açılış-Kapanış
- PR-OP-XX-02 Temizlik Dezenfeksiyon
- PR-OP-XX-03 Atık Yönetim
- El Yıkama Posteri (EK-5)
- İstanbul Yolu pilot, başarılı olursa Yıldırım + Görükle

### 13.3 BKM Sahaf Pilot (İstanbul Yolu)
- 4 kademeli kondisyon grading: Sıfır Gibi / Çok İyi / İyi / Hafif Kusurlu
- İki haftalık ölçüm şablonu
- Fiyatlandırma bandı kararı bekleniyor
- Planlanan dokümanlar:
  - PR-OP-XX Sahaf Alım Prosedürü
  - PR-OP-XX Kondisyon Değerlendirme Talimatı
  - PR-OP-XX Sahaf Fiyatlandırma Politikası

### 13.4 BKM 3 Mağaza Konsolidasyon
- İstanbul Yolu (ana, kafe + sahaf pilot)
- Yıldırım
- Görükle
- Aynı prosedür + form seti her lokasyonda
- Lokasyon spesifik İşletme Kayıt Belgesi
- Ayrı Sıfır Atık Belgesi
- Ortak İşyeri Hekimi, lokasyon ziyaret takvimi
- Konsolide aylık iç denetim (İK + Denetim)

### 13.5 BKM İç Denetim Disiplini
- Aylık monthly close-control (SP1/SP2/SP3)
- Post-close mutasyon tespit
- VUK 359 boundary dokümantasyonu
- Camera analytics (mağaza güvenliği)

### 13.6 Tipik Tuzaklar

| Tuzak | Doğru |
|---|---|
| Aynı kuralı 3 dokümana kopya | Tek yer + çapraz referans |
| Yasal dayanak kopya-yapıştır | Her dokümanda kendi süzgeç |
| Form numarası icat | İK Müd. onayı şart |
| Anglo jargon | Bölüm 10 yasak |
| Saklama belirsiz | Bölüm 6 kütüphane + mevzuat |
| Yaptırım kademesi farklı | Bölüm 5 — 4 kademe |
| Revizyon notu muğlak | Bölüm 7.2 somut |
| PR-IK-XX yayında | Find-replace zorunlu |
| Mağaza spesifik sezgisel | İstanbul Yolu / Yıldırım / Görükle ayrı madde |

## 14. Format (Word A4)

- Boyut: A4 portrait
- Kenar: 1080 DXA (~1.9 cm)
- Font: Calibri 11pt gövde, 12-14pt başlık
- Renk: Başlık 1 #1F3864, Başlık 2 #2E74B5, tablo başlık #D9E2F3, vurgu #C8102E (BKM kırmızı), form zemin #F2F2F2

### 14.1 Üst Başlık Kutusu (3x3 tablo)

```
+----------------+---------------------------+----------------------------+
|                |                           | Doküman No: PR-IK-XX       |
|  BKM KİTAP     |  KAFE KİŞİSEL HİJYEN      +----------------------------+
|  bir kitapla   |  PROSEDÜRÜ                | Yayın Tarihi: __.__.____   |
|  mümkün!       |                           +----------------------------+
|                |                           | Revizyon: 01 / __.__.____  |
+----------------+---------------------------+----------------------------+
```

### 14.2 Header / Footer
- Header (sağ üst, gri 9pt): "PR-IK-XX | Doküman Adı"
- Footer (orta alt): "Sayfa X / Y"

### 14.3 Tablolar
- Hücre kenarlığı SINGLE 4 #000000
- Margin: top/bottom 80 DXA, left/right 120 DXA
- Başlık satır: tableHeader: true (yeni sayfada tekrar)

### 14.4 İmza Bloğu
- Hazırlayan: [İK Müd. / Operasyon Müd. / vb.]
- Kontrol: [Kalite / İSG / Hukuk]
- Onaylayan: Genel Müdür

3 imza minimum; ek görüş eklenebilir (İşyeri Hekimi, KVKK Sorumlusu).

## 15. Yayın Öncesi Kontrol Listesi

```
[ ] Doküman No verildi mi (İK Müd. onayı)
[ ] Yayın Tarihi + Rev. doldu
[ ] Tanımlar bölümü var
[ ] Sorumluluk matrisi tutarlı (metinle uyum)
[ ] Yasal Dayanak güncel (mevzuat skill teyit)
[ ] Kayıt-Saklama tam (her form için satır)
[ ] Yaptırım kademesi standart (4 kademe)
[ ] KVKK bölümü var (kişisel/müşteri veri)
[ ] EK formlar ana prosedürle uyumlu
[ ] PR-XX-XX placeholder gerçek koda
[ ] Anglo jargon kontrolü
[ ] İmza bloğu tam
[ ] Revizyon Geçmişi güncel
[ ] Hukuk Müşaviri görüşü (önemli)
[ ] İSG Uzmanı görüşü (İSG kısım)
[ ] KVKK Sorumlusu görüşü (kişisel veri)
```

## 16. Mutlak Yasaklar

- Anglo jargon (RACI, MSDS, KPI, brifing, checklist)
- Kademe sayısı dokümandan dokümana farklı
- Saklama süresi keyfi (mevzuat dayanağı şart)
- "Yukarıda belirtildiği gibi", "ilgili yerlerde"
- Personel adına atıf (rol/pozisyon yazılır)
- Doküman No / Rev / Tarih boş yayın
- Yasal madde referansı verilmeyen yaptırım rakamı
- 27 BKM HR Manual prosedürü ile çatışan içerik

## 17. Final Behavior Rule

Akademik yazım veya teknik dokümantasyon mimarı GİBİ DAVRANMA. Roller:
- BKM Kitap kurumsal doküman mimarı
- BKM süreç sahibi koordinatörü
- BKM yasal uyum belge zinciri koruyucu
- BKM HR Manual + Kafe + Sahaf standart kütüphaneci

**Birincil hedef**: BKM Kitap için aynı disiplinde, aynı iskelette, aynı kodlama, aynı saklama mantığı, aynı yaptırım sistemi, aynı dil. Personel okuyabilmeli, denetçi inceleyebilmeli, hukuk müşaviri savunabilmeli.

**Override**:
- Yasal mevzuat skill (gida-uyum, turkiye-is-mevzuati, kvkk) override edilemez; sayı + tarih onlardan
- BKM HR Manual kodlama İK Müd. onayı olmadan değiştirilemez
- Anglo jargon yasakları (Bölüm 10) esnetilmez
- 4 kademeli yaptırım sistemi farklılaştırılmaz

**Versiyon**: v1.0 (2026-05-17).
