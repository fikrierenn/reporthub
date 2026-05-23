---
id: turkiye-vergi-mevzuati
name: Türkiye Vergi Mevzuatı
description: Türkiye fatura, gider belgeleri ve vergi beyanname mevzuatı disiplini. BKM Kitap ve Belinza için VUK 229-242 (fatura), e-Fatura/e-Arşiv/e-İrsaliye/e-SMM zorunlulukları, gider belgeleri (gider pusulası, perakende satış vesikası, ÖKC fişi, müstahsil), KDV 3065, GVK 193, KVK 5520, MUHSGK aylık, Ba-Bs, Damga 488, geçici vergi, ay kapanış disiplini. "Bu fatura geçerli mi", "e-Fatura zorunlu", "KDV oranı", "beyan tarihi", "Ba-Bs eşik", "ÖKC fişi yeterli", "period lock", "gider pusulası ne zaman" ifadelerinde tetikle. Severity Filter (kriminal kayıt/idari ceza/operasyonel/hijyen), Confidence Discipline (VUK madde High, 2026 oran/limit Low YMM teyit), Operational Memory (BKM period lock ihlali, post-close mutasyon, belge silme vakası) içerir. finans-butce-muhasebe, turkiye-is-mevzuati, turkiye-sozlesme-hukuku ile zincirleme.
category: Finans
tags: [vergi, VUK, fatura, e-fatura, e-arsiv, KDV, GVK, KVK, MUHSGK, Ba-Bs, damga, gecici-vergi, period-lock, OKC, gider-pusulasi, mustahsil, SMM, BKM, Belinza]
token_estimate: 8500
chains: [finans-butce-muhasebe, turkiye-is-mevzuati, turkiye-sozlesme-hukuku]
---

# Türkiye Vergi Mevzuatı: Fatura, Gider Evrakları, Beyanname Disiplini

> Bu skill bir YMM/SMMM yerine geçen sistem, vergi danışmanı veya muhasebe yazılımı DEĞİLDİR.
> Birincil hedef: Belge (fatura/gider) ve beyan (KDV/GV/KV/MUHSGK/Ba-Bs) konularında **kriminal kayıt** veya **idari para cezası** riski yaratan ihlallerin önceden tespit edilmesi; somut vakada YMM/SMMM görüşüne yönlendirme.

## 0. Felsefe

Vergi mevzuatı **hesap mantığı değil belge düzenidir**. Hesap doğru olsa bile belge yanlışsa ceza var; belge doğru olsa bile hesap eksikse vergi var.

Üç tehlike:
1. "Hesap doğru, belge önemsiz" yanılgısı: VUK belge düzenini muhasebeden ayrı sayar; eksik fatura veya geç kayıt = kayıt nizamı ihlali = ayrı ceza.
2. Period lock disiplini zayıflığı: Kapanmış ay üzerine kayıt yapmak = sahte fatura disiplini araştırması. BKM 10.04.2026 belge silme vakası bu kategoride.
3. 2026 oran/limit ezberle değil teyitle: Asgari ücret, KDV oran, Ba-Bs eşik (5K TL), MUHSGK takvimi yıllık değişebilir.

**Kural:** Her vergi sorusu VUK/KDV/GVK/KVK madde referansı + 2026 oran search teyit + YMM/SMMM somut görüş zinciriyle cevaplanır.

## 1. Yasal Çerçeve

### 1.1 Vergi Usul Kanunu (VUK 213)
- Madde 175-217: Defter ve belge düzeni
- Madde 229-242: Fatura ve fatura yerine geçen belgeler
- Madde 232: Fatura içeriği zorunlu unsurları
- Madde 234: Sevk irsaliyesi
- Madde 237: Perakende satış vesikası, ÖKC fişi
- Madde 238: Gider pusulası
- Madde 240: Müstahsil makbuzu
- Madde 241: Serbest meslek makbuzu
- **Madde 353: İdari para cezaları** (kayıt nizamı)
- **Madde 359: Kriminal cezalar** (sahte fatura, vergi kaçakçılığı)
- Mük. Madde 257: e-Belge zorunluluğu

### 1.2 KDV Kanunu (3065)
- Madde 10: Vergi doğuşu (mal teslimi/hizmet ifası)
- Madde 13-17: İstisnalar
- Madde 28-29: KDV oranları
- Madde 9, 11: KDV tevkifatı
- Madde 32: İade

### 1.3 GVK (193) ve KVK (5520)
- GVK 37-44: Ticari kazançlar
- GVK 80-83: Beyanname, geçici vergi
- GVK 94: Stopaj (kira, ücret, serbest meslek)
- KVK 25-32: Beyan, oran, taksit

## 2. Fatura Mevzuatı (VUK 229-242)

### 2.1 Fatura Düzenleme Zorunluluğu

| Durum | Zorunlu? |
|---|---|
| 1.000 TL+ mal/hizmet (B2B) | EVET |
| Toptan satış (her tutar) | EVET |
| Eşik altı tüketici | Perakende vesikası / ÖKC yeterli |
| Müşteri talep ederse | EVET (talep üzerine) |

### 2.2 Zorunlu İçerik (VUK 232)

1. Tarih · 2. Seri/sıra no · 3. Düzenleyen (ünvan/adres/vergi dairesi/vergi no)
4. Müşteri (ünvan/adres/vergi dairesi/vergi no) · 5. Mal/hizmet (nev'i/miktar/birim fiyat/toplam)
6. KDV (oran bazlı ayrı + toplam) · 7. Genel toplam

Eksik unsur = idari ceza (VUK 353).

### 2.3 Düzenleme Süresi
Mal teslimi/hizmet ifasından itibaren **7 gün içinde** (VUK 231). Geç = idari ceza.

### 2.4 Müteselsil Sorumluluk
Sahte/yanıltıcı belge düzenleyen + **bilen/bilmesi gereken alıcı** müteselsil sorumlu (VUK 359/b). Şüpheli faturayı kabul eden alıcı aynı cezayı alabilir.

**BKM kritik**: tedarikçi seçiminde vergi sicil temizliği kontrolü.

## 3. e-Belge Zorunlulukları

### 3.1 e-Fatura
- Yıllık ciro eşiği üstü mükellefler (2026 eşik search teyit)
- EFKS / Özel Entegratör → tüm fatura e-Fatura
- İhracat zorunlu
- BKM Kitap muhtemelen e-Fatura zorunlu; aksi → VUK Mük.257 + 353

### 3.2 e-Arşiv
B2C satışlar için elektronik belge. 5.000 TL+ tüketici satışlarında zorunlu (search teyit).

### 3.3 e-İrsaliye / e-SMM / e-Müstahsil
Yıllık ciro eşiği üstü; sektör bazlı zorunluluk.

### 3.4 Saklama
e-Belgeler **10 yıl** saklama (VUK). Hash chain, GİB onaylı yöntem. Yedekleme + erişilebilirlik.

**Belge silme = VUK 359 kriminal**. BKM 10.04.2026 belge silme vakası kritik referans.

## 4. Gider Belgeleri

### 4.1 Gider Pusulası (VUK 238)
Vergi mükellefi olmayan kişiden alım. Ev hizmetlisi, mevsimlik dışı işçi.

### 4.2 Perakende Satış Vesikası (VUK 237)
Eşik altı tüketici satışında. ÖKC çıktısı veya manuel.

### 4.3 ÖKC Fişi
Perakende sektörü (market, kafe, restoran). GİB onaylı cihaz + yazılım. Günlük Z raporu.

### 4.4 Müstahsil Makbuzu (VUK 240)
Çiftçi alımı. e-Müstahsil belirli ciro üstü.

### 4.5 Serbest Meslek Makbuzu (VUK 241)
Avukat, doktor, mali müşavir. e-SMM zorunlu (2020+). Stopaj GVK 94.

## 5. KDV (3065)

### 5.1 Oranlar (Confidence Low — 2026 search teyit)
- Genel %20 — çoğu mal/hizmet
- İndirimli %10 — bazı temel gıda, sağlık, eğitim, kitap (kontrol)
- İndirimli %1 — ekmek, süt, gazete, bazı tarım

### 5.2 İstisnalar
İhracat, diplomatik, eğitim, sağlık, banka/sigorta, konut teslimi (ilk 150m² altı), BIST işlemleri.

### 5.3 Tevkifat
| Sektör | Oran |
|---|---|
| İnşaat | 5/10 veya 9/10 |
| Temizlik | 9/10 |
| Yemek (toplu) | 5/10 |
| Tekstil/konfeksiyon | 9/10 |
| Hurda | Tam |
| Kıymetli maden/taş | Tam |

Belinza: hammadde alım, fason kaplama tevkifat altı olabilir; YMM teyit.

### 5.4 Beyan
Aylık KDV → bir sonraki ayın 26'sına kadar. Beyan + ödeme tek tarih. e-Beyan GİB.

## 6. GVK / KVK

### 6.1 GVK Dilimleri (Confidence Low — 2026 search)
%15 / %20 / %27 / %35 / %40 (ücret değil) — yıllık Cumhurbaşkanlığı kararnamesi.

### 6.2 Stopaj (GVK 94)
| Ödeme | Oran |
|---|---|
| Kira | %20 |
| Serbest meslek | %20 |
| Ücret | Dilimli |
| Telif | %17 |
| Kâr payı (A.Ş.) | %15 |
| Mevduat faizi | %15 |

### 6.3 Beyan
- GV yıllık: Mart
- KV yıllık: Nisan
- Geçici vergi: 3 ayda 1 (Mart/Mayıs/Ağustos/Kasım search teyit)
- MUHSGK: aylık 26

## 7. MUHSGK (Muhtasar + Prim)

2020+ birleşik beyan. Aylık 26. e-Beyan + e-Bildirge tek sistem. Geç beyan = idari ceza; geç ödeme = gecikme faizi + zammı.

## 8. Ba-Bs Formları

Aylık 5.000 TL+ alım (Ba) ve satım (Bs) bildirimi (2026 eşik search teyit). Beyan: bir sonraki ayın son günü.

GİB çapraz kontrol: Ba ↔ Bs eşleşmeli; eşleşmiyorsa **denetim flag**. Sahte fatura tespiti çoğunlukla bu çapraz kontrol ile.

## 9. Geçici Vergi
3 ayda bir. Dönem sonu Mart/Mayıs/Ağustos/Kasım (search teyit). 3 aylık kâr × geçici vergi oranı (%25 genel). Yıllık beyanda mahsup.

## 10. Ay Kapanış + Period Lock

### 10.1 Kapanış Adımları
1. Tüm faturalar girilmiş mi
2. Banka mutabakatı
3. Cari mutabakat (yaşlandırma)
4. Stok sayım
5. Maaş bordro
6. Stopaj hesapları
7. KDV beyan hazırlık
8. Dönem sonu reklasse (avans, prepaid, accrual)
9. **Period lock devreye**
10. Yedekleme

### 10.2 Period Lock Mantığı
Kapanmış dönem üzerinde **kayıt yapılamaz**; düzeltme yeni belge ile (iade fatura, tashih fatura).

**BKM Memory**: Period lock zayıf; post-period değişiklikler; belge silme bulgusu (10.04.2026, Resul Bey) kritik.

### 10.3 BKM Audit Findings (Nisan 2026)
| # | Bulgu | Severity |
|---|---|---|
| 1 | **Belge silme (10.04.2026)** | Kriminal — YMM + avukat derhal |
| 2 | Post-period close fatura değişikliği | İdari + lock zayıf |
| 3 | Wrong cost center | Operasyonel |
| 4 | Çift kayıt | Operasyonel |
| 5 | Eksik kayıt | Vergi matrahı azaltma |
| 6 | KDV oranı hatası | Operasyonel |
| 7 | Mutabakat farkı görmezden | Kontrol eksik |
| 8 | Tarih sırası bozuk | Lock zayıf |

### 10.4 Period Lock Önerileri
- Kapanış sonrası edit yetki kapanır
- Sadece yetkili (GM + Mali Müdür) açabilir, log + yazılı sebep + bildirim
- Açılan kayıtta değişiklik log + original korunur

## 11. Kriminal vs İdari Ceza

### 11.1 İdari (VUK 353)
Eksik fatura içeriği, defter ibrazsızlık, e-Fatura ihlal, Ba-Bs eksik, geç beyan. Ödeme + düzeltme ile kapanır.

### 11.2 Kriminal (VUK 359)
| İhlal | Ceza |
|---|---|
| Sahte fatura düzenleme/kullanma | 3-5 yıl hapis |
| Sahte muhteviyatlı belge | 1-3 yıl |
| Defter/belge yok etme | 1.5-3 yıl |
| Hesap/muhasebe hilesi | 1.5-3 yıl |

Adli sicile geçer; banka kredi keser; ticaret ihraç riski.

### 11.3 Müteselsil
Sahte zincirde her halka birbirinin cezasından sorumlu. **BKM**: ucuz tedarikçi = sahte fatura zinciri riski.

## 12. Sık Hatalar

| Pattern | Çözüm |
|---|---|
| Geç fatura kayıt | 7 gün içi; yoksa pişmanlık beyan |
| Tedarikçi vergi sicili kontrolsüz | GİB sicil sorgu; müteselsil |
| e-Fatura zorunlu ama kağıt | Otomatik e-Fatura sistemi |
| Period lock yok | Lock + yetki yönetimi |
| Belge silme | Adli sicil + iç soruşturma |
| Ba-Bs eşik yanlış | 2026 teyit + sistem oto |
| MUHSGK son ödeme kaçırma | Takvim + alarm |
| Stopaj eksik kesim | Otomatik kesim |
| KDV tevkifat görmezden | YMM sektör review |
| Geç beyan | İlk hafta beyan + 26 ödeme |

## 13. Final Behavior Rule

YMM/SMMM, vergi avukatı veya muhasebe yazılımı GİBİ DAVRANMA. Roller:
- VUK/KDV/GVK/KVK madde referansı bilgi tabanı
- 2026 oran/limit Confidence Low + search yönlendiren teyit bekçisi
- Kriminal vs İdari sınır tarayıcı (VUK 353 vs 359)
- Period lock + ay kapanış bekçisi
- BKM audit findings memory referans
- Kritik vakada **YMM + ceza avukatı** zorunlu yönlendirme

**Yasak**:
- Kriminal sınır iç sessizlik
- 2026 sezgisel rakam (search teyit zorunlu)
- "Esnek olalım" period lock gevşetme
- Belge silme "hata oldu" savunması
- Sahte zincir tedarikçi seçim ihmali

**Versiyon**: v1.0 (2026-05-17).
