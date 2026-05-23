---
id: turkiye-sozlesme-hukuku
name: Türkiye Sözleşme Hukuku
description: Türkiye sözleşme hukuku otorite ve review disiplini. BKM Kitap, Belinza ve PivotEra için TBK 6098 (Borçlar Kanunu), TTK 6102 (Ticaret Kanunu), cezai şart (BK 179-182), geçersizlik halleri (hata/hile/korkutma/gabin), damga vergisi, KVKK kloz uyumu, notarization zorunlu vakalar, sözleşme türleri kütüphanesi (satış, hizmet, eser, kira, vekalet, NDA, distribütörlük, gayrimenkul satış vaadi) kapsar. "Bu sözleşmeyi incele", "kloz eksik mi", "sınırsız sorumluluk var", "fesih şartı", "damga vergisi", "gayrimenkul satış vaadi", "BK madde X" ifadelerinde tetikle. Severity Filter (red flag/orange/yellow/green/hijyen), Confidence Discipline (TBK/TTK madde sabit High, damga oranı Low), Operational Memory (BKM 7 mülk vakası, sahaf alımları, kira sözleşmeleri) içerir.
category: Etik
tags: [sozlesme, TBK, TTK, 6098, 6102, cezai-sart, fesih, damga-vergisi, KVKK-kloz, notarizasyon, NDA, satis, hizmet, eser, kira, vekalet, gayrimenkul, BKM, Belinza, PivotEra]
token_estimate: 7800
chains: [abd-sozlesme-hukuku, turkiye-is-mevzuati, insan-kaynaklari, finans-butce-muhasebe, risk-tarama]
---

# Türkiye Sözleşme Hukuku: Otorite + Review Disiplini

> Bu skill bir hukuk bürosu, dava strateji aracı veya avukat yerine geçen sistem DEĞİLDİR.
> Birincil hedef: Sözleşmedeki kabul edilemez risk klozlarını tespit edip işaretlemek; standart eksiklikleri yakalamak; gerçek hukuki karar avukata yönlendirmek.

## 0. Felsefe

Sözleşme bir "kabul ettim" kağıdı değil; **operasyonel risk dağıtım belgesidir**. Yanlış kloz = ileride dava + zaman + para. Doğru kloz = sessiz koruma.

Üç tehlike:
1. "Standart sözleşme" yanılgısı: karşı tarafın şablonu sana standart değil; tek taraflı eklemeler olabilir.
2. Tehlikeli kloz görmezden gelinmesi: sınırsız sorumluluk en sık atlanır.
3. Yargıtay tezi tahmin: avukat işi; bu skill sezgi vermez, **risk işaretler**.

**Kural:** Her sözleşme 5 lensli review (red flag, kloz eksiklik, taraf dengesi, belirsizlik, hijyen) çıkmadan imza yok. Kritik vakada **avukat görüşü** zorunlu.

## 1. Yasal Çerçeve

### 1.1 TBK 6098 (2012 yürürlük)
- Genel hükümler (1-206): borç, sözleşme kurulması, hata-hile-korkutma, temsil, ifa, tazminat
- İsimli sözleşmeler (207-619): satış (207-281), bağışlama (285-298), kira (299-378), iş (393-447 → insan-kaynaklari skill), hizmet (502-514), vekalet, eser (470-486), saklama (561-580), kefalet (581-603), garanti (128)

### 1.2 TTK 6102 (2012 yürürlük)
Tacir, ticari iş, ticari hüküm; şirketler A.Ş./L.Ş.; kıymetli evrak (çek, bono, poliçe); taşıma; sigorta.

### 1.3 İlişkili
- TKHK (Tüketici, B2C sözleşmeler)
- Damga Vergisi 488 (oran/tutar yıllık güncellenir)
- TMK 4721 (kişiler/aile/miras/eşya)
- İcra İflas 2004

## 2. Severity Filter

| Sınıf | Tanım | Aksiyon |
|---|---|---|
| **Red Flag** | Sınırsız sorumluluk; tek taraflı fesih hakkı; cezai şart hakim müdahale dışı; geriye etkili; KVKK yoksunluğu; sınırsız garanti; tek taraflı değişiklik; her türlü zarar talep | **İmza YOK**; revize zorunlu |
| **Orange** | Mücbir sebep yok; fesih belirsiz; gizlilik zayıf; tazminat sınırı belirsiz | Revize zorunlu |
| **Yellow** | Teslim süresi belirsiz; açık fiyat klozları; dispute resolution yok; ihtarname belirsiz | Revize önerilir |
| **Green** | Standart ticari yapı; klozlar yerinde; taraf dengesi makul | Onaya hazır |
| **Hijyen** | Typo, atıf hatası (BK 818 yerine TBK 6098), tarih, unvan eksik | Hızlı düzelt |

## 3. 12 Zorunlu Kloz Checklist

| # | Kloz | Eksik = |
|---|---|---|
| 1 | Tarafların kesin tanımı (ünvan, vergi no, adres, yetkili) | Hijyen |
| 2 | Sözleşmenin konusu | Orange |
| 3 | İfa süresi / takvim | Yellow |
| 4 | Fiyat / ödeme şartları | Orange |
| 5 | Teslim şekli / yeri (FOB/CIF/DDP) | Yellow |
| 6 | Garanti / ayıba karşı tekeffül | Orange |
| 7 | **Sorumluluk sınırı** | **Red Flag** |
| 8 | Mücbir sebep | Orange |
| 9 | Fesih şartları | Orange |
| 10 | Cezai şart (varsa, hakim müdahale kabul) | Orange / Red Flag |
| 11 | KVKK kişisel veri klozu | Orange |
| 12 | Yetki / tahkim / uygulanacak hukuk | Yellow |

## 4. Tehlike Kelime Taraması

Otomatik Red Flag:
- "sınırsız sorumluluk"
- "her türlü zarar"
- "tüm zararlar dahil"
- "doğrudan, dolaylı, özel, sonradan doğan zararlar"
- "şartsız feragat"
- "geri dönülemez"
- "tek taraflı değişiklik hakkı"
- "onaylı olmaksızın"
- "ileride doğabilecek tüm talepler"
- "hiçbir şekilde sorumlu değildir"
- "kabul edilmiştir, itiraz hakkı yoktur"
- "kayıtsız şartsız kabul"

## 5. Sözleşme Türleri Kütüphanesi

### 5.1 Satış (BK 207-281)
Risk geçişi teslimle (BK 208). Ayıba karşı tekeffül 6 ay/2 yıl (taşınmazda 5 yıl). Ayıp ihbar süresi (BK 223) atlanır. **BKM**: yayıncıdan alım, müşteriye satış, sahaf alım/satım.

### 5.2 Hizmet (BK 502-514)
Vekaletten fark: hizmet sonucu garanti edilir. **Belinza danışmanlık**: çıktılar net tanımlı (proje teslim listesi).

### 5.3 Eser (BK 470-486)
Ayıba karşı tekeffül 2-5 yıl. **BKM**: yazılım, web, dekorasyon. Kabul prosedürü atlanır.

### 5.4 Vekalet (BK 502-514)
Özen yükümlülüğü, sonuç değil. **PivotEra danışmanlık + mentorluk**.

### 5.5 Kira (BK 299-378)
Süreli vs süresiz. Dönem sonu fesih (10 gün önceden). Konut artış TÜFE 12 aylık ortalama. **BKM**: mağaza/depo/ofis.

### 5.6 NDA / Gizlilik
Karşılıklı tercih. Süre, gizli bilgi tanımı, istisna (kamuya açık) atlanır. **PivotEra**: müşteri ile karşılıklı.

### 5.7 Distribütörlük / Bayilik
Münhasırlık (bölge/ürün/süre). Performans hedefleri. Fesih sonrası stok eritme.

### 5.8 **Gayrimenkul Satış Vaadi**
**Noter onayı zorunlu (TMK 706)**. Notarsız geçersiz; tapuya tescil edilemez. **BKM 7 mülk vakası (Mart-Nisan 2026)** kritik referans.

### 5.9 Lisans (FSEK + 6769 SMK)
Münhasır vs basit. Coğrafi sınır, süre, alan. **Atlas Digital Commerce**: ATLASCOREUS → Podbul üretim lisansı (US tarafı abd-sozlesme-hukuku skill).

### 5.10 Acente (TTK 102-122)
TTK 122 **müşteri tazminatı**: acentelik fesih sonrası karşı taraf müşteri tabanını kazandırdıysa. Hesap atlanır.

### 5.11 Garanti (BK 128)
Sürekli vs şart. Tedarikçi garantisi devri.

## 6. Cezai Şart (BK 179-182)

### 6.1 Tipleri
- BK 179/1: asıl borçtan ayrı talep edilebilir (yüksek karşı taraf yükü)
- BK 179/2: asıl borç yerine
- BK 179/3: vazgeçilebilir ek

### 6.2 Hakim Müdahale (BK 182)
Aşırı yüksek cezai şart **hakim indirir**. "BK 182 indirimi kabul edilmez" YAZAMAZ; emredici. Bu kloz = **Red Flag**.

### 6.3 BKM Kullanım
- Tedarikçi geç teslim: günlük cezai şart (bedelin %0.1-0.5)
- Bayi performans: minimum alım garantisi
- NDA ihlali: tazminat + cezai şart

## 7. Damga Vergisi (488)

**Confidence Low** — 2026 oran/tutar Maliye yayını teyit. Nispi (yüzde) veya maktu. Damga ödenmemiş sözleşme delil olarak kabul edilir ama vergi cezası ayrı. Sözleşme geçerliliğini etkilemez.

## 8. KVKK Kloz Uyumu

Kişisel veri toplanan/işlenen sözleşmelerde **kloz zorunlu**:
- Aydınlatma metni (hangi veri, amaç, süre)
- Açık rıza (özel nitelikli)
- Veri sorumlusu/işleyen tanımı
- Veri güvenliği taahhüdü
- Yurtdışı aktarım (ülke + Kurul izni)
- İhlal bildirim 72 saat

KVKK klozu yok → **Red Flag** (idari para cezası 2024 1.0M+ TL).

## 9. Notarizasyon Zorunlu Vakalar

| Vaka | Yasal Şart | Notersiz |
|---|---|---|
| Gayrimenkul satış vaadi | TMK 706, BK 237 | Geçersiz |
| Vekaletname (gayrimenkul) | TMK 558 | Geçersiz |
| Bağış (taşınmaz) | BK 288 | Geçersiz |
| İhalede teklif | KİK 7 | Reddedilebilir |
| Şirket ana sözleşmesi değişikliği | TTK 333 | Geçersiz |
| Miras sözleşmesi | TMK 545 | Geçersiz |
| Boşanma protokolü | TMK 184 | Geçersiz |

### 9.1 BKM 7 Mülk Satış Vaadi Vakası (Mart-Nisan 2026)
Bursa'da 7 ticari gayrimenkul. Notarizasyon eksikliği. Aile içi taraflar = transfer pricing scrutiny. Sonuç: **notarsız = geçersiz** uyarısı; revize gerekti.

## 10. Geçersizlik Halleri (BK 30-39)

- **Hata** (30-35): esaslı hata = tam geçersiz; iptal 1 yıl (BK 39)
- **Hile** (36): kasten yanıltma; iptal 1 yıl
- **Korkutma** (37-38): tehdit/baskı; iptal 1 yıl
- **Gabin** (28): aşırı orantısız edim; iptal 1 yıl
- **Genel İşlem Şartları** (20-25): matbu sözleşmede aşırı sürpriz = yazılmamış sayılır

## 11. Taraf Perspektifi

| Soru | BKM kendi | Karşı |
|---|---|---|
| Bu klozla ne kaybediyorum? | Liste | Liste |
| Karşı taraf ne kazanıyor? | Bilinçli ödün mü? | Aşırı kazanım mı? |
| Risk simetri var mı? | İkisinde aynı yük? | Pazarlık |
| Aciliyet hangi tarafta? | Pazarlık gücü | Pazarlık gücü |

Asimetri → karşı taraf benzer klozu kendine eklemiyorsa pazarlık.

## 12. Sık Hatalar

| Pattern | Çözüm |
|---|---|
| Standart şablonu hızlı imza | Kendi şablon hazırla |
| Cezai şart "indirim yok" kabul | BK 182 emredici, geçersiz |
| Sınırsız sorumluluk imza | Bedel ile sınırlı + dolaylı zarar hariç |
| Notarsız gayrimenkul | Geçersiz; istisnasız |
| KVKK klozu unutma | 2018 sonrası tüm sözleşmelere |
| Mücbir sebep tanımsız | "Salgın, savaş, doğal afet, devlet kararı..." somut |
| Yetki tek tarafa | Bursa veya tahkim (İTOTAM) |
| Cezai şart asimetrik | Karşılıklı veya kaldır |
| Geç ihbar yükümü "hemen" | 30 gün veya makul |
| Eski mevzuat referansı (BK 818 vs TBK 6098) | Atıf güncelle |

## 13. BKM 7 Mülk Vakası Öğrenmeleri

- Notarsız satış vaadi → TMK 706 gereği geçersiz
- Aile içi taraflar → transfer pricing scrutiny + vergi
- Halk Bankası mortgage → banka onayı; kreditör hakları
- 7 mülk = 7 ayrı sözleşme = ayrı damga; toplam hesap
- 5 yıllık GVK istisnası → konut için 5 yıl elden çıkarmama; ticari gayrimenkulde geçersiz
- Bağımsız değerleme → vergi denetiminde fiyat realitesi kanıtı

## 14. Final Behavior Rule

Avukat, hukuk bürosu veya dava strateji aracı GİBİ DAVRANMA. Roller:
- 5 lensli review disiplini bekçisi
- 12 zorunlu kloz checklist hatırlatıcı
- Red Flag tespit işaretçisi
- Tehlike kelime tarayıcı
- TBK/TTK madde referans bilgi tabanı
- Damga/notarizasyon uyarı zinciri
- Avukat görüşüne yönlendirici

**Yasak**:
- Red Flag tespit edildi → "hızlı imza" geçersiz
- Notarsız gayrimenkul satış vaadi onay
- BK 182 cezai şart hakim indirim "geçersiz" kabul
- "Mahkeme bunu nasıl yorumlar" sezgisel cevap
- KVKK klozu eksiklik görmezden

**Versiyon**: v1.0 (2026-05-17).
