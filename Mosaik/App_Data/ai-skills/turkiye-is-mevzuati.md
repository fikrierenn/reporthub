---
id: turkiye-is-mevzuati
name: Türkiye İş Mevzuatı
description: Türkiye iş ve sosyal güvenlik mevzuatı kütüphanesi. 4857 İş Kanunu, 5510 SGK, 6331 İSG, 6698 KVKK, 1475/14 kıdem ve yönetmelikleri. Kıdem tazminatı, ihbar, fazla mesai ve gece çalışması zammı, yıllık izin, doğum izni, iş kazası bildirim takvimi, fesih türleri (haklı/haksız/geçerli/ikale), asgari ücret ve vergi parametreleri, SGK prim oranları, MUHSGK takvimi, İSG yükümlülükleri ve KVKK personel veri zorunluluğunda kullan. "Kıdem", "ihbar", "izin hakkı", "iş kazası bildirim", "SGK prim", "fazla mesai zammı", "yasal süre" geçen yerde tetikle. Yıllık değişen parametreler (asgari ücret, kıdem tavanı, KVKK cezası, gelir vergisi dilimleri) Confidence Low; kullanmadan önce web search ile teyit zorunlu.
category: İK
tags: [is-kanunu, 4857, SGK, 5510, ISG, 6331, KVKK, 6698, kidem, ihbar, fazla-mesai, yillik-izin, dogum-izni, is-kazasi, MUHSGK, fesih, ikale, asgari-ucret, AGI, BKM]
token_estimate: 10000
chains: [insan-kaynaklari, finans-butce-muhasebe, turkiye-vergi-mevzuati, kvkk-veri-envanteri, isletme-dokuman-yazim, risk-tarama, kok-sebep]
---

# Türkiye İş Mevzuatı: Regülatif Referans + Son Teyit Zorunluluğu

> Hukuki görüş aracı DEĞİL. Regülatif referans kütüphanesi.
> Birincil hedef: doğru yasal süre, doğru hesap formülü, doğru bildirim takvimi.
> Yıllık değişen parametreler (asgari ücret, kıdem tavanı, KVKK ceza, vergi dilimi) kullanmadan önce **web search ile teyit zorunlu**.

## 0. Felsefe

"Ortalama doğru" yetmez. Yanlış mevzuat = idari para cezası + çalışan davası + iş güvencesi tazminatı.

2 tehlike:
1. Eski bilgi ile karar: 2022'de AGİ kaldırıldı, 2017'de kıdem zamanaşımı 10→5 yıl, asgari ücret yılda 1-2 değişir
2. Yasal süre ↔ takvim süresi karıştırma: "3 iş günü" hafta sonu saymaz, "72 saat" saatlik takip, "6 gün" hak düşürücü

**Kural**: Yıllık değişen parametre ÖRNEK; kullanım anında **web search teyit zorunlu**.

## 1. Yasal Çerçeve

| Yasa | Konu | Kritik Madde |
|---|---|---|
| **4857 İş Kanunu** | İşçi-işveren | m.17 (ihbar), m.25 (fesih), m.41 (mesai), m.53 (izin), m.74 (analık) |
| **5510 SGK** | Sigorta, prim | m.4 (statü), m.13 (iş kazası), m.83 (prim) |
| **6331 İSG** | İş sağlığı/güvenlik | m.4 (eğitim), m.14 (kaza bildirim) |
| **6698 KVKK** | Kişisel veri | m.5 (rıza), m.7 (silme), m.12 (ihlal) |
| **1475/m.14** | Kıdem (eski) | Yürürlükte tek madde |
| **TTK m.82** | Defter saklama | 10 yıl |
| **BK m.146** | Genel zamanaşımı | 10 yıl |
| **2429** | Ulusal bayram | Resmi tatil |

## 2. Confidence Discipline (KRİTİK)

| Seviye | İçerik | Davranış |
|---|---|---|
| **High** | Yasal süreler (3 iş günü, 6 gün, 72 saat), ihbar haftası, izin günü, fesih türü | Direkt kullan |
| **Medium** | SGK prim oranı, KDV, fazla mesai limit (270 saat) | Yıllık SGK genelge teyit |
| **Low** | Asgari ücret, kıdem tavanı, KVKK ceza, gelir vergisi dilimi, damga oranı | **Web search ZORUNLU** |

Düşük güvenli parametre "kesin" diye sunulmaz.

## 3. İhbar Süreleri (4857 m.17)

```
0-6 ay       2 hafta
6 ay-1.5 yıl 4 hafta
1.5-3 yıl    6 hafta
3 yıl+       8 hafta
```

- Sözleşme ile uzatılabilir, kısaltılamaz
- Süre içinde günde 2 saat iş arama izni (toplu kullanılabilir)
- Uyulmazsa = ihbar tazminatı (süre × brüt ücret)
- Kötüniyet tazminatı = ihbar × 3 (m.17/6)

## 4. Kıdem Tazminatı (1475 m.14)

### 4.1 Hak Şartı
- Min 1 yıl çalışma
- İşveren feshi (haklı m.25/II HARİÇ)
- İşçi haklı feshi (m.24)
- Askerlik (erkek), Emeklilik, Evlenme (kadın, 1 yıl içi), Ölüm (mirasçı), Yaş hariç emeklilik

### 4.2 Hesap
```
Kıdem = (Brüt Aylık + Sürekli Yan Hak) × Hizmet Yılı

Yan Haklar:
+ Yemek (parasal)
+ Yol (parasal)
+ İkramiye (yıllık ort. aya pay)
+ Sürekli prim
+ Konut (kira değeri)
- AGİ DAHİL DEĞİL (2022 kaldırıldı)
- Fazla mesai (düzensiz) DAHİL DEĞİL
```

### 4.3 Kıdem Tavanı
**Confidence Low** — 6 ayda 1 güncellenir (memur maaş katsayısı). Web search teyit ZORUNLU.

### 4.4 Vergi
**Gelir vergisi istisna**. Sadece damga vergisi (binde 7,59).

## 5. Yıllık İzin (4857 m.53)

```
1-5 yıl       14 iş günü
5-15 yıl      20
15 yıl+       26
18 altı       Min 20
50 üstü       Min 20
Yeraltı maden +4 ek
```

- 1 yıl dolmadan kullanılmaz
- Min 10 gün kesintisiz, kalan bölünebilir
- **Para olarak ödenmez**; sadece sözleşme sona ererse kullanılmamış izin paraya
- Devir limiti yasada yok ama biriken = yasal risk
- Çalışılmış sayılan (m.55): hafta tatili, resmi tatil, rapor, evlilik (3 gün), ölüm (3 gün)

## 6. Doğum İzni (4857 m.74)

### Kadın
```
Tek bebek:    8 hafta önce + 8 hafta sonra = 16 hafta
Çoğul:        10 hafta önce + 8 hafta sonra = 18 hafta
Sağlıklı + doktor onayı: 3 hafta öncesine kadar çalışabilir
```

### Süt İzni
- 1.5 saat/gün, çocuk 1 yaşına kadar
- Bölünebilir, ücret kesilmez

### Ücretsiz İzin
- Doğum sonrası 6 aya kadar
- 3-6 ay: yarım çalışma + yarım maaş + ücretsiz

### Diğer
- Babalık: 5 gün ücretli (m.46 Ek 2)
- Evlat edinme: 8 hafta (kadın/tek)
- Hamile + süt veren: gece çalıştırılamaz, max 7.5 saat/gün

## 7. Fazla Mesai (4857 m.41)

```
Normal Fazla Mesai (45+ saat)    %50 zam
Fazla Süreli Çalışma (45'e kadar)%25 zam
Resmi Tatil                       %100 ek
Ulusal Bayram                     %100 ek
Hafta Tatili                      %50 (gündelik üzeri)
Gece Vardiyası                    Zam yok (max 7.5 saat)
```

- **Yıllık max 270 saat** (m.41/8)
- **Yıllık yazılı işçi onayı zorunlu** (m.41/7)
- Serbest zaman: her saat = 1.5 saat serbest, 6 ay içi
- Hafta tatili (m.46): 24 saat kesintisiz; çalışırsa 1 günlük + %50
- Resmi tatil: çalışılmasa da ücret; çalışırsa ek 1 gün

## 8. Gece Çalışması (m.69)

```
Tanım: 20:00-06:00
Max: 7.5 saat/gün (kesin)
Yasak:
- Gebe + süt veren (1 yaşına kadar)
- 18 yaş altı
- Sağlık raporu uygun olmayan
Periyodik sağlık: min 2 yılda 1 rapor
```

## 9. SGK Prim Oranları (4/A genel)

```
                  İşçi    İşveren  Toplam
Malullük-Yaşlılık  %9     %11      %20
GSS                %5     %7.5     %12.5
İşsizlik           %1     %2       %3 (devlet %1)
Kısa Vade İş Kazası -     %2       %2
TOPLAM             %15    %22.5    %37.5

İşveren teşvik 5510/81-i: -%5
Net işveren ~%20.5
```

**Tavan/Taban (Confidence Low)**:
- Prim tabanı = brüt asgari ücret
- Prim tavanı = brüt × 7.5

## 10. MUHSGK e-Bildirge

**Takip eden ayın 26'sına kadar**.

```
Ay sonu kapanış      1-5
Hata kontrol         6-10
İşveren onay         10-15
Bildirim             20'den önce ideal, 26 son
Ödeme                Takip eden ayın son günü
```

Geç bildirim:
- 1 ay içi: prim × gecikme zammı
- Süresinde değil: idari ceza (asgari ücretin %12+, Confidence Low)

**SGK işe giriş**: işe başlamadan 1 gün önce VEYA en geç başladığı gün.
**SGK işten çıkış**: 10 gün içi.

## 11. İş Kazası Bildirim (5510 m.13, 6331 m.14)

```
Olay → İlk Yardım → Bildirim Zinciri:
1. SGK             3 iş günü
2. Çalışma Bak.    İSG-KATIP
3. İşyeri kayıt    İSG defter + kaza tutanağı
4. Ölüm/uzun vade  Derhal kolluk + SGK

Tutanak:
- Tarih, saat, yer
- Olay seyri
- Tanık ifadesi
- Yaralanma tipi
- İlk müdahale
- KKD durumu
```

**Kritik**: Trafik kazası iş ile ilgiliyse (servis, görev) = iş kazası.

## 12. Fesih Türleri (4857)

### 12.1 İşveren Haklı (m.25)
```
I.   Sağlık sebep (kasıtlı değil → 1 hafta + ihbar sonra)
II.  Ahlak/iyiniyet → yalan, hırsızlık, sırı ifşa, hakaret
III. Zorlayıcı (1+ hafta)
IV.  Tutuklanma (ihbardan uzun)

→ İhbar tazminatı YOK
→ 25/II durumunda kıdem YOK
→ 6 gün hak düşürücü (öğrendiği gün)
→ Yazılı + savunma alınmış olmalı
```

### 12.2 İşçi Haklı (m.24)
```
I.   Sağlık
II.  Ahlak/iyiniyet (mobbing, hakaret, ücret ödenmemesi)
III. Zorlayıcı

→ İhbar süresi işlemez
→ Kıdem VAR
→ 6 gün süre
```

### 12.3 İşveren Geçerli (m.18)
```
30+ çalışan + 6 ay+ kıdem → işe iade davası

→ Geçerli sebep: yetersizlik, davranış, işyeri gereği
→ Yazılı + sebep + savunma
→ Kazanırsa: 4 ay ücret + işe iade
→ Başlamak istemezse: 4-8 ay iş güvencesi tazminatı
```

### 12.4 İkale (Karşılıklı Anlaşma)
- Makul yarar şartı (min ihbar + kıdem + ek ödeme)
- "Makul yarar yok" iddiası → mahkeme
- Tipik: ihbar+kıdem+izin+%15-25 ek

## 13. İSG (6331)

### 13.1 Tehlike Sınıfları
```
Az tehlikeli:   Ofis, mağaza, kitap satışı, e-ticaret
Tehlikeli:      Depo, üretim, lojistik (forklift, vinç)
Çok tehlikeli:  İnşaat, kimya, maden
```

### 13.2 Az Tehlikeli (BKM Profili) Yükümlülük
```
İSG hizmeti (10+ çalışan: OSGB)
İşyeri hekimi (>50 çalışan)
İSG uzmanı (her tehlike sınıfı)
Risk değerlendirmesi (yıllık güncelleme)
Acil durum + tatbikat (yılda 1)

İSG Eğitim:
- İşe başlangıç: 8 saat min
- Periyodik:
    Az tehlikeli: 3 yılda 1, 8 saat
    Tehlikeli: 2 yılda 1, 12 saat
    Çok tehlikeli: 1 yılda 1, 16 saat

Periyodik Sağlık:
    Az tehlikeli: 5 yılda 1
    Tehlikeli: 3 yılda 1
    Çok tehlikeli: 1 yılda 1

İSG defteri + kaza kayıtları
KKD (gerekli ise)
Çalışan temsilcisi seçimi (>2 çalışan)
```

### 13.3 İSG Kurulu
- 50+ çalışan + 6+ ay sürekli iş → zorunlu
- Aylık toplantı, karar defteri

## 14. KVKK Personel Veri (6698)

### 14.1 Aydınlatma (m.10)
İşe başlarken veya veri ilk işlendiğinde aydınlatma metni imzalı.

İçerik: veri sorumlusu, hangi veri, hangi amaç, kime aktarılıyor, hukuki sebep, hak listesi (m.11).

### 14.2 Açık Rıza
- **Genel**: işin gerektirdiği için rıza şart değil (sözleşme+kanun)
- **Özel nitelikli AÇIK RIZA ZORUNLU**: sağlık, adli sicil, biyometrik (PDKS parmak), etnik/din/sendika

### 14.3 Saklama-İmha (m.7)
Her veri tipi: saklama süresi, imha yöntemi, periyodik imha (6 ay 1).

### 14.4 İhlal Bildirim (m.12)
**72 saat içinde KVKK + ilgili kişi**.

### 14.5 VERBİS
50+ çalışan VEYA 25M+ ciro → zorunlu (Kurul 2025/1572 muafiyet eşik kontrol).

### 14.6 Ceza (Confidence Low — web search)
Aydınlatma yok / veri güvenlik eksik / yanıt vermeme = idari para. Yıllık değişir.

## 15. Asgari Ücret + Vergi (Confidence Low)

```
Brüt Asgari Ücret  → Resmi Gazete
Net Asgari Ücret   → ~Brüt × %85 (SGK işçi%15 - İşsizlik%1 - Damga)
Prim Tavanı        → Brüt × 7.5
Günlük Taban       → Brüt / 30
```

- **AGİ kaldırıldı (2022)**
- Net asgari üstü → gelir + damga vergisine TABİ
- Asgari ücret artışı ≠ otomatik kıdem hesabı revize (kıdem brüt üzerinden)

### Damga + Gelir Vergisi
```
Damga: %0.759 (binde 7.59) — tüm bordro
Gelir Vergisi (dilimli, asgari ücret istisna):
  %15 / %20 / %27 / %35 / %40
```

## 16. Zamanaşımı + Saklama

| Konu | Zamanaşımı |
|---|---|
| Ücret alacağı | 5 yıl |
| Yıllık izin parası | 5 yıl |
| İhbar tazminatı | 5 yıl |
| Kıdem tazminatı | 5 yıl (eski 10, 2017 değişti) |
| Genel (BK 146) | 10 yıl |
| Manevi tazminat | 2 yıl |
| İşe iade davası | 1 ay (fesih bildiriminden) |
| Fesih hak düşürücü (24/25) | 6 gün |

| Belge | Süre | Dayanak |
|---|---|---|
| Bordro, mesai | 10 yıl | TTK 82, BK 146 |
| İş sözleşmesi | 10 yıl | BK 146 |
| Özlük | 10 yıl | 4857 m.75 |
| SGK | 10 yıl | 5510 |
| İSG eğitim, sağlık | 15 yıl | 6331 |
| İş kazası | 15 yıl | 6331 |
| KVKK aydınlatma+rıza | İş + 10 yıl | KVKK + BK |
| Vardiya çizelge | 5 yıl | 4857 |

## 17. Hesap Şablonları

### Kıdem
```
Brüt Aylık + Sürekli Yan Hak = Kideme Esas
Hizmet: N yıl, M ay, K gün
Kıdem = Esas × N + (Esas/12 × M) + (Esas/365 × K)
Tavan kontrol (Confidence Low)
- Damga (%0.759)
= Net Kıdem
```

### İhbar
```
Brüt Aylık × (W hafta / 4) = İhbar
- Damga + Gelir Vergisi
```

### Yıllık İzin Parası
```
Brüt/30 = Günlük
Kullanılmamış İzin × Günlük = İzin Parası
- Damga + Gelir Vergisi
```

### Fazla Mesai
```
Saatlik Brüt = Aylık / 225
Fazla Mesai = Saat × Saatlik × 1.5
Hafta Tatili = 1.5 yevmiye
Resmi Tatil = 2 yevmiye
```

## 18. Hızlı Süre Referansı

| Konu | Süre |
|---|---|
| SGK işe giriş bildirim | 1 gün önce |
| SGK işten çıkış bildirim | 10 gün içi |
| MUHSGK e-bildirge | Ertesi ay 26 |
| İş kazası bildirim | 3 iş günü |
| Fesih hak düşürücü (24/25) | 6 gün |
| İşe iade davası | 1 ay |
| KVKK ihlal bildirim | 72 saat |
| Yıllık izin hak | 1 yıl |
| Vardiya değişiklik | 1 hafta önce yazılı |
| İSG eğitim (az tehl.) | 3 yıl |
| Periyodik sağlık (az tehl.) | 5 yıl |
| İhbar (3+ yıl) | 8 hafta |
| Yıllık fazla mesai limit | 270 saat |
| Doğum (tek bebek) | 16 hafta |
| Süt izni | 1 yaşına kadar, 1.5 saat/gün |
| Babalık | 5 gün |

## 19. Sık Tuzaklar

1. **AGİ kaldırıldı (2022)**: Eski bordroda var, yenide yok.
2. **Asgari ücret istisnası (2022+)**: Bu tutara kadar gelir+damga istisna. Üstü vergiye tabi.
3. **Kıdem zamanaşımı 5 yıl (2017+)**: Eski davalarda 10 yıl uygulanmış olabilir.
4. **İbraname Yargıtay**: Yazılı ibraname genelde GEÇERSİZ. Net ödeme + banka transferi güvenli.
5. **SGK işe giriş 1 gün önce**: Aynı gün = teftişte ceza, işçi sigortasız iddia.
6. **6 gün hak düşürücü**: Disiplin olayı öğrenildi, 6 gün sonra fesih → HAKSIZ FESİH'e döner.
7. **Yıllık izin para olarak ödenmez**: Sözleşme bitince paraya. "Para ver çalışsın" YASAK.
8. **Hamile koruması**: Fesih yasak değil ama haklı sebep şart + iş güvencesi davası riski.
9. **5+ aylık raporlu**: m.25/I dolunca fesih mümkün ama tazminat (ihbar+kıdem).
10. **Stajyer SGK**: Mesleki eğitim stajyeri 4/A %2.5 (sadece iş kazası).
11. **Trafik = iş kazası**: Servis/görev kapsamında 3 iş günü bildirim.
12. **Geçici iş göremezlik**: Sözleşmede deduction klozu + yazılı onay yoksa çift ödeme riski (m.22).

## 20. Final Behavior Rule

Hukuk bürosu veya bordro yazılımı GİBİ DAVRANMA. Roller:
- Regulatory reference librarian
- Son teyit zorunluluğu hatırlatıcı
- Yasal süre + formül + bildirim takvimi otoritesi

**Birincil hedef**: Doğru yasal süre + formül + takvim. Yıllık değişen parametre için "şu anki rakamı teyit et". Tartışmalı vakada avukat görüşüne yönlendir.

**Sorumluluk Reddi**: Bu skill operasyonel referans. Hukuki bağlayıcılığı yok. Kıdem tavanı, asgari ücret, KVKK ceza, gelir vergisi dilimi YILLIK DEĞİŞİR. Kullanmadan önce Resmi Gazete + SGK + KVKK duyuru teyit. Tartışmalı vakada iş hukuku avukatı.

**Override**:
- Yasal süre/formül/bildirim takvim diğer skill tarafından override edilemez
- Yıllık değişen parametre web search teyit BYPASS edilemez
- insan-kaynaklari: bu skill "ne" (yasa), o skill "nasıl" (operasyonel); çelişki = yasa kazanır
- finans-butce-muhasebe: vergi/SGK formül burası, kayıt orası

**Versiyon**: v1.0 (2026-05-17).
