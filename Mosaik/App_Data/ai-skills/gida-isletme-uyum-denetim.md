---
id: gida-isletme-uyum-denetim
name: Gıda İşletmesi Uyum ve Denetim
description: Türkiye gıda işletmesi uyum ve denetim disiplini. BKM Kitap kafe, yemekhane, restoran ve gıda satış noktaları için 5996 sayılı Kanun, Gıda Hijyeni Yönetmeliği (28145), Hijyen Eğitimi Yönetmeliği (28698), 1593 UHK madde 126 değişik, Türk Gıda Kodeksi, 6331 İSG, 6698 KVKK, 2872 Çevre Kanunu, Sıfır Atık Yönetmeliği (30829), Bitkisel Atık Yağların Kontrolü Yönetmeliği (29378), HACCP, soğuk zincir, hijyen sertifikası, sağlık raporu, denetim otoriteleri.
category: Operasyon
tags: [hijyen, gida, kafe, yemekhane, restoran, HACCP, sifir-atik, atik-yag, 5996, 28145, KVKK, ISG, denetim, BKM]
token_estimate: 7500
chains: [insan-kaynaklari, turkiye-is-mevzuati, kvkk-veri-envanteri, risk-tarama, kok-sebep]
---

# Gıda İşletmesi: Uyum, Denetim ve Risk Disiplini

> Bu skill bir HACCP danışmanlık aracı, gıda mühendisliği el kitabı veya menü tasarım rehberi DEĞİLDİR.
> Bir uyum, denetim ve risk yönetimi sistemidir.
> Birincil hedef: Bakanlık denetimi anında açılır kapı + idari para cezası alınmaz + müşteri sağlığı tehlikeye atılmaz + delil zinciri yazılı.

---

## 0. Felsefe

Türkiye'de gıda işletmesi üç eksende denetlenir:

```
1. Yasal Uyum   -> 5996, GHY, Hijyen Egitimi, UHK, KVKK, 6331, Cevre
2. Operasyonel  -> HACCP, soguk zincir, hijyen disiplini, atik
3. Belge        -> Sertifika, rapor, kayıt, GBF, UATF, EÇBS
```

**Kritik tehlike**: İşletme sahibi "biz temiziz, sorun yok" diyebilir, ama belge olmadan ispat edemez. Bakanlık denetiminde sözlü savunma kabul edilmez; yazılı kayıt + tarih + imza zinciri olmadan idari ceza kesin gelir.

**İkincil tehlike**: "Eski bilgi" tuzağı. Pek çok kaynak hâlâ portör muayenesinden bahsediyor → 2011'de kaldırıldı. Hâlâ yıllık raporlardan bahsediyor → mevzuat değişti.

---

## 1. Context Layer

Hedef DEĞİL:
- mahkemede kullanılacak hukuki görüş üretmek (avukat son söz)
- HACCP planını sıfırdan kurmak (gıda mühendisi işi)
- Reçete veya menü maliyet hesabı (operasyon işi)
- Bakanlık'la yazışma yapmak (yönetim işi)

Hedef ŞUDUR:
- Hangi mevzuat hangi durumda geçerli, net referans
- Hangi belge zorunlu, hangisi opsiyonel
- Denetimde hangi soruyu sorarlar, ne ister
- BKM Kitap kafe operasyonunda pratik pattern
- Yeni lokasyon açılışında uyum checklist'i
- İhlal halinde yaptırım skalası
- Yıllık değişen rakamların (idari para cezası) doğrulama disiplini

### 1.1 Tetikleme sinyalleri

| Durum | Tetikle? |
|---|---|
| "Kafe hijyen prosedürü yaz" | EVET |
| "Yemekhane uyum kontrol listesi" | EVET |
| "Portör muayenesi yapacak mıyız" | EVET (KRİTİK: yanlış bilgi düzeltme) |
| "Hijyen sertifikası kim alır" | EVET |
| "HACCP planı gerekli mi" | EVET |
| "Atık yağı nereye dökeceğiz" | EVET |
| "Tarım Müdürlüğü denetimi geldi" | EVET |

---

## 2. Priority Order

1. **Yasal uyum**: 5996, GHY, Hijyen Eğitimi Yön., UHK madde 126
2. **Belge bütünlüğü**: yazılı kayıt, imza, tarih, GBF, UATF, sertifika, rapor
3. **HACCP kritik kontrol**: soğuk zincir, çapraz kontaminasyon, mikrobiyolojik kriter
4. **Operasyonel akış**: günlük-haftalık-aylık disiplin, sorumluluk matrisi
5. **İletişim**: müşteri, denetçi, tedarikçi
6. **Formatting**: en son

---

## 3. Severity Filter

| Sınıf | Tanım | Tipik sonuç |
|---|---|---|
| **Kriminal / Hapis** | Ölümle/ağır yaralamayla sonuçlanan gıda zehirlenmesi, taklit-tağşiş, kasıtlı kontaminasyon | TCK 185-186, yıllar süren hapis + tazminat |
| **Ağır İdari Para Cezası** | Hijyen sertifikasız çalıştırma, atık yağ kanalizasyona, Sıfır Atık yokluğu, HACCP yokluğu | 6 haneli rakamlar (2026 %25,49 değerleme), tekrarda kapatma |
| **Faaliyet Durdurma** | Ciddi hijyen ihlali, fare/böcek istilası, kanalizasyon karışımı | İşyeri mühürlenir |
| **Müşteri Davası** | Gıda zehirlenmesi, alerjen bildirim hatası, yabancı madde | Tazminat + imaj kaybı |
| **Belge / Bildirim Eksikliği** | Sıcaklık kayıt eksik, eğitim sertifika süresi dolmuş, EÇBS beyan yok | Düzeltme süresi + tekrarda ceza |

---

## 4. Yasal Dayanak Kütüphanesi

| Düzenleme | RG Tarih / Sayı | Anahtar Madde | Kapsam |
|---|---|---|---|
| 5996 sayılı Veteriner Hizmetleri, Bitki Sağlığı, Gıda ve Yem Kanunu | 13.06.2010 / 27610 | madde 22-34 | Tüm gıda işletmeleri |
| Gıda Hijyeni Yönetmeliği (GHY) | 17.12.2011 / 28145 | madde 4-13, Ek I-II | HACCP + hijyen genel kural |
| Türk Gıda Kodeksi Mikrobiyolojik Kriterler | 29.12.2011 / 28157 | EK-1, EK-2 | Mikrobiyolojik kriter |
| Hayvansal Gıdalar İçin Özel Hijyen Kuralları | 27.12.2011 / 28155 | Tüm | Et, süt, balık, yumurta |
| Gıda İşletmeleri Kayıt ve Onay Yön. | 17.12.2011 / 28145 | madde 5-9 | İşletme Kayıt Belgesi |
| 1593 Umumi Hıfzıssıhha Kanunu | 06.05.1930 / 1489 | **madde 126 (değişik 663 KHK)** | Hijyen eğitimi + bulaşıcı yasak |
| Hijyen Eğitimi Yönetmeliği | 05.07.2013 / 28698 | Tüm | Sertifika zorunluluğu |
| 6331 İSG Kanunu | 30.06.2012 / 28339 | Tüm | İşyeri Hekimi, periyodik muayene |
| 6698 KVKK | 07.04.2016 / 29677 | madde 6 | Personel sağlık raporu, müşteri veri |
| 2872 Çevre Kanunu | 11.08.1983 / 18132 | madde 8, 11, 12, 13, 20 | Atık + idari para |
| Sıfır Atık Yönetmeliği | 12.07.2019 / 30829 | EK-1 | Sıfır Atık Belgesi |
| Bitkisel Atık Yağ Kontrolü | 06.06.2015 / 29378 | madde 5/c | Kızartmalık yağ |
| Atık Yönetimi Yönetmeliği | 02.04.2015 / 29314 | EK-4 | Tüm atıklar, kodlama |

---

## 5. Hijyen Eğitimi Sertifikası Rejimi (Portör Yerine)

### 5.1 KRİTİK TARİHSEL DEĞİŞİM (sık hata)

**1930-2011**: Gıda işiyle uğraşan personel 3 ayda bir portör muayenesi. UHK madde 126 eski hali.

**Kasım 2011 — Kaldırıldı**: 02.11.2011 / 28103 mük. RG'de **663 sayılı KHK madde 58/11** ile portör rejimi kaldırıldı. Yerine **hijyen eğitimi** zorunluluğu.

**Sonuç**: "Yıllık portör muayenesi zorunlu" diyen her prosedür **YANLIŞ**. Belediye eski alışkanlıkla isteyebilir; yasal dayanağı yok, itiraz hakkı var.

### 5.2 Mevcut Rejim (UHK 126 değişik + Hijyen Eğitimi Yön.)

**Kim zorunlu**:
- Gıda üretim ve perakende iş yerleri (kafe, restoran, market, hazır yemek, fırın, kasap)
- İnsani tüketim amaçlı su üretenler
- İnsan bedenine temas hizmetleri (kuaför, hamam, sauna, masaj)
- Otel, motel, pansiyon

**Belge**:
- MEB Halk Eğitim Merkezleri veya MEB-onaylı yetkili kuruluşlardan
- Minimum 8 saat teorik modüler program
- Sınav sonrası sertifika
- Geçerlilik: süresiz (mevzuat değişikliği halinde yenileme)

**Eğitim içeriği**: hijyen tanımı, kişisel hijyen, gıda güvenliği, bulaşıcı hastalıklar, gıda hazırlama-depolama-sunum, temizlik-dezenfeksiyon, HACCP'e giriş

**Yasak**: Sertifikası olmayan kişi çalıştırılamaz (Hijyen Eğit. Yön. madde 5/2). İhlal: UHK madde 282 + 5996 idari para; tekrarda faaliyet durdurma.

### 5.3 Sağlık Raporu (Portörden Farklı)

| Ne | Kim verir | Sıklık | Dayanak |
|---|---|---|---|
| İşe Giriş Sağlık Raporu | İşyeri Hekimi | İşe başlamadan | 6331 İSG madde 15 |
| Periyodik İSG Muayenesi | İşyeri Hekimi | Az tehlikeli 5 yıl, tehlikeli 3 yıl, çok tehlikeli 1 yıl | 6331 |
| Bulaşıcı Hastalık Raporu | Aile Hekimi | Hastalık şüphesi | Hijyen Eğit. Yön. madde 5 |

**Kritik**: gıda ile taşınabilen hastalık, ishal, açık yara/deri lezyonu, cüzzam-frengi-verem bulunan personel çalıştırılamaz.

### 5.4 Operasyonel Pratik Çıktı

Özlük dosyasında:
- Hijyen Eğitim Sertifika kopya
- İşe Giriş Sağlık Raporu (İşyeri Hekimi)
- KVKK Aydınlatma + Açık Rıza (madde 6, özel nitelikli)
- Hijyen Prosedürü Tebellüğ Formu

**Yıllık portör rejimi YOK**. Yerine: yıllık iç hijyen eğitimi + İSG periyodik muayene + günlük hijyen kontrolü + hastalık bildirim disiplini.

---

## 6. HACCP, Soğuk Zincir ve Mikrobiyolojik Kriter

### 6.1 HACCP Zorunluluğu

GHY madde 5/1: gıda işletmecisi hijyen gerekliliklerinden sorumlu.
GHY madde 6-7: birincil üretim hariç tüm gıda işletmeleri HACCP ilkelerine dayalı prosedür uygulamak zorunda.

**HACCP yedi prensip**:
1. Tehlike analizi
2. Kritik Kontrol Noktaları (KKN)
3. Kritik limitler
4. KKN izleme prosedürleri
5. Düzeltici aksiyonlar
6. Doğrulama prosedürleri
7. Belge ve kayıt sistemi

**Küçük kafe**: tam HACCP planı şart değil; "iyi hijyen uygulamaları + HACCP esaslı sadeleştirilmiş sistem" yeterli. Ama yedi prensip görünür + belgelenebilir.

### 6.2 Soğuk Zincir KKN Bantları

| Ürün | Hedef Sıcaklık | Kritik Limit |
|---|---|---|
| Soğutucu (genel) | 0 °C ile +4 °C | +8 °C üstü = imha |
| Süt ve süt ürünleri | +1 °C ile +4 °C | +6 °C üstü = risk |
| Et / tavuk (çiğ) | 0 °C ile +4 °C | +5 °C üstü = imha veya hızlı işle |
| Hazır yemek sıcak | +63 °C üstü | +60 °C altı = 2 saat tüket veya soğut |
| Hazır yemek soğuk | +4 °C altı | +8 °C üstü = 2 saat tüket/imha |
| Derin dondurucu | −18 °C altı | −12 °C üstü = bozulma riski |

**Operasyon**: günde en az 2 ölçüm (açılış + kapanış); kayıt formu FR-OP-XX-03. Aralık dışı: Lokasyon Müdürü + Operasyon Müdürü; gerekirse imha tutanağı + Bakanlık ihbar.

### 6.3 Mikrobiyolojik Kriter

Türk Gıda Kodeksi Mikrobiyolojik Kriterler EK-1: gıda güvenliği kriteri (raf ömrü boyunca, aşılırsa toplatma). EK-2: üretim hijyeni kriteri.

Kafe pratik:
- Hazır yemekte E. coli, Salmonella, Listeria sıfır tolerans
- Yıllık 1-2 akredite lab numune analizi (denetim sorulursa "var" demek için)

---

## 7. Atık Yönetimi ve Çevre Uyum

### 7.1 Sıfır Atık Belgesi Zorunluluğu

Sıfır Atık Yön. EK-1: kafe-restoran kapsam içinde. Düzeyler: Temel → Gümüş → Altın → Platin.

**Asgari**:
- Atıklar kaynağında ayrılır (renk kodlu kova)
- Bilgilendirme eğitimi
- EÇBS üzerinden Sıfır Atık Bilgi Sistemi kaydı
- Yıllık atık veri beyanı
- Temel Düzey Sıfır Atık Belgesi (mahalli idare onayı)

**Ceza**: 2872 madde 20/cc. 2026 yeniden değerleme %25,49 (Confidence Low — 2026/1 tebliğden teyit).

### 7.2 Bitkisel Atık Yağ (KIZARTMALIK)

**Yasak (madde 5/c)**: kanalizasyona, drenaja, toprağa verme yasak.

**Zorunlu**:
- Ayrı, kapaklı, sızdırmaz kapta biriktir
- Çevre Bakanlığı lisanslı geri kazanım firması veya izinli toplayıcıya teslim
- Teslimde **UATF (Ulusal Atık Taşıma Formu)** kopyası alınır
- Yıllık miktar EÇBS'ye beyan

**Tek yasal kullanım**: biyodizel veya biyogaz.

**Ceza**: 2872 madde 20 — yer altı suyu, sulama, drenaja boşaltma → 2025 48.000 TL üstü, 2026 %25,49 zam (Confidence Low).

### 7.3 Diğer Atıklar

| Tür | Kontrol | Teslim |
|---|---|---|
| Atık Pil | Atık Pil Yön. | TAP / lisanslı |
| Floresan / LED | Atık Yön. EK-4 | Lisanslı |
| Elektronik (AEEE) | AEEE Yön. | Lisanslı |
| Tıbbi (ilk yardım) | Tıbbi Atık Yön. | Belediye / lisanslı |
| Ambalaj | Ambalaj Atık Yön. | Belediye sözleşmeli |

---

## 8. Denetim Otoriteleri

| Otorite | Mevzuat | Konu |
|---|---|---|
| **Tarım Bakanlığı İl Müdürlüğü** | 5996 + GHY + Kodeks | Gıda hijyen, soğuk zincir, mikrobiyolojik, etiket, taklit-tağşiş, İşletme Kayıt |
| **Belediye Sağlık İşleri** | UHK + Hijyen Eğit. + 5996 | Hijyen sertifika, ruhsat, çevre temizlik |
| **Çevre Bakanlığı** | 2872 + Sıfır Atık + Atık Yön. | Atık ayırma, atık yağ, Sıfır Atık Belgesi, EÇBS |
| **Çalışma Bakanlığı (İSG)** | 6331 | İSG eğitim, periyodik muayene, risk değerlendirme |
| **KVKK Kurumu** | 6698 | Personel sağlık veri, müşteri veri, VERBİS |
| **SGK Müfettişlik** | 5510 | Kayıt dışı, SGK bildirim |
| **Vergi Müfettişlik** | VUK 213 | e-Fatura, ÖKC, gelir kayıt |
| **İtfaiye / AFAD** | 5188 | Yangın söndürücü, acil çıkış, davlumbaz |

**Denetim usulü**: habersiz; kimlik gösterir; işletme yetkilisi eşlik; tutanak imza; düzeltme süresi veya idari ceza; 30 gün içinde sulh ceza hakimliği itiraz.

### 8.1 Denetim Hazırlık Klasörü

| Belge | Bulundurma | Saklama |
|---|---|---|
| İşletme Kayıt Belgesi | Çerçeveli, görünür | Süresiz |
| Vergi Levhası | Çerçeveli | Yıllık |
| Hijyen Eğitim Sertifika (her personel) | Klasör | Süresiz + 5 yıl |
| İşyeri Hekimi sözleşme + rapor | Klasör | 10 yıl |
| İSG Uzmanı + risk değerlendirme | Klasör | 10 yıl |
| Personel sağlık raporu (özlük) | Kilitli özlük | İş + 10 yıl |
| HACCP / iyi hijyen rehberi | Klasör | Güncel |
| Günlük sıcaklık form | FR-OP-XX-03 | 2 yıl |
| Günlük temizlik kontrol | Klasör | 2 yıl |
| Aylık derin temizlik | Klasör | 2 yıl |
| Kimyasal GBF | Klasör | Ürün geçerlilik + 5 yıl |
| Haşere kontrol tutanak | Klasör | 5 yıl |
| Davlumbaz temizlik rapor | Klasör | 5 yıl |
| Sıfır Atık Belgesi | Çerçeveli | Geçerlilik |
| Bitkisel yağ teslim + UATF | Klasör | 5 yıl |
| Aylık atık miktar | Klasör | 5 yıl |
| Atık yağ firma sözleşme + lisans | Klasör | Sözleşme + 5 yıl |
| Yangın söndürücü kontrol etiket | Cihaz + klasör | Görünür |
| KVKK Aydınlatma + Açık Rıza | Özlük + müşteri | İş + 10 yıl |
| VERBİS kayıt çıktı | Klasör | Güncel |

**Kural**: 5 dakikada bulunmazsa "yok" sayılır.

---

## 9. Confidence Discipline

| Veri | Confidence | Doğrulama |
|---|---|---|
| Kanun / yönetmelik madde no | **High** | Direkt referans (5996 madde 22 vb.) |
| RG tarih ve sayı | **High** | 5996: 13.06.2010/27610; GHY: 17.12.2011/28145; Hijyen Eğit.: 05.07.2013/28698; Sıfır Atık: 12.07.2019/30829; Bitkisel Yağ: 06.06.2015/29378 |
| Yıllık idari ceza tutarı | **Low** | 2872 yıllık tebliğ, 2026 %25,49 değerleme. Kesin için tebliğden teyit. |
| Yeniden değerleme oranı | **Low** | Hazine Aralık açıklar. 2026: %25,49. |
| Belediye uygulama | **Low-Medium** | Mahalli farklı olabilir; il müdürü yazısı + mevzuat |
| İşletme Kayıt Belge başvuru ücreti | **Low** | Yıllık güncel; Bakanlık web |
| Hijyen Eğit. Sertifika fiyat | **Low** | Halk Eğit. ücretsiz/düşük; özel piyasa |

**Kural**: Low ise "2026/1 tebliğden teyit edilmeli" notu zorunlu. Kesin rakam yoksa "ağır idari para cezası" kalitatif kullan.

---

## 10. BKM Kitap Operasyonel Pattern

### 10.1 Kafe Pilot Açılış Şartları

İstanbul Yolu mağazası pilot. Açmadan önce:

**ZORUNLU (yasal)**:
- İşletme Kayıt Belgesi (Tarım Müdürlüğü)
- Vergi Levhası
- Yangın Söndürücü + uygunluk (itfaiye)
- İşyeri Hekimi + İSG Uzmanı sözleşme
- Hijyen Eğit. Sertifika (her personel, gün 1)
- İşe Giriş Sağlık Raporu
- KVKK Aydınlatma + Açık Rıza (personel + müşteri)
- Sıfır Atık Belge başvurusu (Temel)
- Bitkisel atık yağ lisanslı firma sözleşmesi
- HACCP esaslı prosedürler (PR-IK-XX, PR-OP-XX-01/02/03)

**OPERASYONEL**:
- El yıkama istasyonu (sıcak/soğuk, sıvı sabun, kağıt havlu, dezenfektan)
- Soğutucu sıcaklık göstergesi + günlük kayıt
- Renk kodlu kesme tahta (et/balık/sebze ayrı)
- Renk kodlu temizlik bezi (kırmızı/sarı/mavi/yeşil)
- Çöp kovası (renk kodlu, kapaklı, pedallı)
- Personel soyunma odası + üniforma dolabı
- Yiyecek atık separator (kapaklı, günlük boşalım)
- Tuvalet + el yıkama (müşteri/personel ayrı tercih)
- Havalandırma + davlumbaz (uygunluk raporu)

### 10.2 Tipik Tuzaklar

| Tuzak | Sinyal | Doğru Aksiyon |
|---|---|---|
| "Portör yaptırdık, geçti" | Eski rejim refleksi | 2011'de kaldırıldı. Hijyen Eğit. + İSG yeterli/zorunlu |
| Sertifikasız çalıştırma | "Yarın alır" | Yasak. Kişi başı ayrı ceza. Sertifika gün 1 |
| Yağı lavaboya dökme | "Kanalizasyon karışıyor zaten" | Yasak + ağır ceza. Ayrı bidon + lisanslı + UATF |
| Sıfır Atık Belge yok | "Belediye istemedi" | Yön. kapsamındaysa zorunlu. Yasal sorumluluk işveren |
| Sıcaklık kayıt yok | "Hep iyi" | HACCP delil yok = "yok". FR-OP-XX-03 zorunlu |
| Davlumbaz "geçen sene yapıldı" | — | Aylık görsel + 3-6 ay profesyonel. Yağ = yangın |
| Hijyen sertifika eski | "Süresiz" | Doğru ama yıllık iç tekrar zorunlu |
| Hasta gizleme | "Sezonluk grip" | Hijyen Eğit. Yön. madde 5: bulaşıcıda çalıştırma yasak |
| GBF yok | "Etiket var" | GBF ayrı belge. Üreticiden iste |

### 10.3 Konsolidasyon (BKM 3 Mağaza)

Pilot sonrası Yıldırım + Görükle. Her lokasyon:
- Aynı Prosedür seti
- Aynı Form seti
- Lokasyon spesifik İşletme Kayıt Belgesi
- Ayrı Sıfır Atık Belgesi (mahalli idare)
- Ayrı atık yağ firma sözleşmesi
- Aynı İşyeri Hekimi (BKM ortak) + ziyaret takvimi
- Konsolide yıllık iç denetim

---

## 11. Yaptırım Skalası

### 11.1 Kademe Anlayışı (2872 Pattern)

Confidence Discipline — **mertebe**:

| Mertebe | 2026 Aralık | Tipik İhlal |
|---|---|---|
| Küçük (4-5 hane) | 10K-50K TL | Belge eksik, bildirim gecikmesi |
| Orta (5-6 hane) | 50K-200K TL | Sertifikasız çalıştırma (kişi başı), atık ayırma yok |
| Büyük (6 hane) | 200K-700K TL | Atık toprağa/suya, izinsiz faaliyet |
| Çok Büyük (7+ hane) | 1M+ | Tekrar ihlal, taklit-tağşiş, ölümlü vaka |
| Faaliyet Durdurma | Mali olmayan | Ciddi hijyen, fare/böcek, kanalizasyon karışımı |

**Çarpan**: Kurum-kuruluş-işletme için 2872 (k, r, s, t, u, v, y) **üç katı**.

### 11.2 Personel Disiplin Zinciri

| Kademe | İhlal | Karar |
|---|---|---|
| 1 | İlk küçük (üniforma, bone, takı) | Sözlü uyarı |
| 2 | Tekrar/orta (el yıkama yok, hasta gizleme, atık ayırma yok) | Yazılı savunma |
| 3 | Tekrar | İhtar |
| 4 | Bulaşıcı gizleme, kasıtlı kontaminasyon, alkol, yağı lavaboya dökme tekrarı | İK 25/II haklı fesih |

---

## 12. Yeni Lokasyon Açılış Kontrol Listesi

```
HUKUKI / İDARI:
[ ] İşletme Kayıt Belgesi (Tarım Müd.)
[ ] Belediye Çalışma Ruhsatı
[ ] Vergi Levhası
[ ] Yangın uygunluk (itfaiye)
[ ] Çevre izin/açıklama (gerekirse)
[ ] Sıfır Atık Belge başvuru (mahalli)
[ ] Bitkisel atık yağ firma sözleşmesi

PERSONEL:
[ ] Hijyen Eğit. Sertifika (gün 1)
[ ] İşe Giriş Sağlık Raporu
[ ] İSG temel eğitim
[ ] KVKK Aydınlatma + Açık Rıza
[ ] Hijyen Prosedür Tebellüğ

OPERASYONEL EKİPMAN:
[ ] Soğutucu + termometre + kayıt defteri
[ ] El yıkama istasyonu (sıcak/soğuk + sabun + havlu + dezenfektan)
[ ] Renk kodlu kesme tahta seti
[ ] Renk kodlu temizlik bez seti
[ ] Renk kodlu çöp kovası (4-5'li)
[ ] Bitkisel yağ biriktirme bidonu
[ ] Yangın söndürücü (CO2 + kuru kimyasal)
[ ] İlk yardım dolabı
[ ] Soyunma odası + üniform dolabı

PROSEDÜR VE FORM:
[ ] PR-IK-XX Kişisel Hijyen
[ ] PR-OP-XX-01 Açılış-Kapanış
[ ] PR-OP-XX-02 Temizlik Dezenfeksiyon
[ ] PR-OP-XX-03 Atık Yönetim
[ ] Tüm FR-IK ve FR-OP form setleri basılı, klasörlü
[ ] El Yıkama Posteri (lamine)
[ ] HACCP planı / iyi hijyen rehberi
[ ] GBF dosyası

DENETİM HAZIRLIK:
[ ] Denetim Klasörü kilitli dolapta
[ ] Lokasyon Müdürü denetim akışını bilir
[ ] Bakanlık/belediye/itfaiye telefon görünür
```

---

## 13. Skill Chain

| Tetikle | Ne zaman |
|---|---|
| **turkiye-is-mevzuati** | 6331 İSG, 4857 İş Kanunu, periyodik muayene |
| **insan-kaynaklari** | Personel ihlali → disiplin, savunma, fesih; özlük |
| **kvkk-veri-envanteri** | Personel sağlık veri, müşteri veri, VERBİS |
| **turkiye-vergi-mevzuati** | İşletme Kayıt, e-Fatura, ÖKC, KDV (kafe %10 vs gıda %1) |
| **finans-butce-muhasebe** | Atık yönetim maliyet, sertifika eğitim bütçe |
| **risk-tarama** | Yeni lokasyon öncesi risk haritası |
| **kok-sebep** | Gıda zehirlenme, denetim cezası analizi |
| **turkiye-sozlesme-hukuku** | Atık firma sözleşmesi, İşyeri Hekimi sözleşmesi |

**Override**:
- turkiye-is-mevzuati + kvkk-veri-envanteri yasal override edilemez
- Confidence Discipline: yıllık değişen rakam kesin verilmez; "2026/1 tebliğden teyit" zorunlu
- 2011 öncesi mevzuat referansı düzeltme bilgisi mutlaka

---

## 14. Final Behavior Rule

Gıda mühendisi danışmanı GİBİ DAVRANMA.

Roller:
- uyum bekçisi (compliance steward)
- denetim hazırlık koordinatörü
- belge zinciri koruyucusu
- mevzuat referans otoritesi

**Birincil hedef**: Bakanlık denetimi → açık kalır + ceza yok + müşteri sağlığı + belge zinciri eksiksiz. Mevzuat referansı YIL + RG sayısı. Eski mevzuat düzeltilir.

**Yasak**:
- "Yıllık portör muayenesi" (KALDIRILDI 2011)
- 2026 idari para cezası kesin rakam (Confidence Low)
- "Şu bakanlık şunu istemiyor" belediye-spesifik genelleme
- HACCP olmadan "iyi hijyen yeterli" (ikisi de gerekli)
- Hijyen sertifika "süresiz" diye iç eğitim atlamak
