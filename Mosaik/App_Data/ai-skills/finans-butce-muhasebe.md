---
id: finans-butce-muhasebe
name: Finans / Bütçe / Muhasebe
description: Bütçe, finansal yönetim, muhasebe, maliyet dağıtımı, nakit akışı, cari yönetimi ve kârlılık için hesap mantık kütüphanesi. Şirket bütçesi, bütçe-gerçekleşen sapma, proje kârlılığı, FIFO maliyet yansıtma, ay sonu kapanışı, KDV/vergi takibi, cari yaşlandırma, vade kontrolü ve finansal raporlamada kullan. Maliyet katmanları (primer/toplam/fiili), bütçe zinciri, proje P&L sapma analizi (fiyat/malzeme/miktar/verimlilik), nakit projeksiyonu ve Odoo finans modülü konfigürasyonu içerir. Severity Filter (regulatif/finansal/operasyonel/hijyen), Confidence Discipline (hesap kanıtı), BKM/Belinza pattern kütüphanesi (mutabakat farkı, FIFO eksikliği, period lock, MAX ile ortalama enflasyon) içerir. Hesap mantığı burada, T-SQL kodu sql-server-uzmani'nda.
category: Finans
tags: [butce, muhasebe, maliyet, FIFO, nakit-akisi, cari, yaslandirma, proje-karliligi, sapma-analizi, period-lock, ay-sonu-kapanis, KDV, MUHSGK, gecici-vergi, Odoo, BKM, Belinza]
token_estimate: 9500
chains: [sql-server-uzmani, turkiye-is-mevzuati, turkiye-vergi-mevzuati, kvkk-veri-envanteri, isletme-dokuman-yazim, risk-tarama, kok-sebep]
---

# Finans / Bütçe / Muhasebe: Hesap Mantık Kütüphanesi

> Muhasebe yazılımı, bordro motoru veya vergi danışmanı DEĞİL.
> Hesap mantık kütüphanesi + finansal kontrol disiplini.
> Birincil hedef: doğru maliyet, doğru kâr, doğru nakit projeksiyonu; reaktif değil proaktif.

## 0. Felsefe

Muhasebe geçmişi kaydeder. Finans geleceği şekillendirir.

3 tehlike:
1. Hesap sonucuna güvenip kaynağa bakmamak: "kâr 1.2M" raporu ama FIFO eksik = sahte kâr
2. Bütçeyi hedef değil duvar gibi sunmak: aşımı engellemek yerine "neden aşıldı" anlamak temel
3. Yasal takvimi pas geçmek: KDV/Muhtasar/SGK kaçırılırsa idari ceza

**Kural**: Her finansal rapor 3 soruya cevap verir: Doğru mu? (mutabakat) Anlamlı mı? (karşılaştırma) Karar üretiyor mu? (aksiyon)

## 1. Priority Order

1. Yasal uyum (KDV, Muhtasar, dönem kapanışı, period lock)
2. Hesap doğruluğu (mutabakat, FIFO katmanı, KDV oranı)
3. Veri bütünlüğü (fatura no gap, dönem kapanışı sonrası fiş yasak)
4. Maliyet doğruluğu (dağıtım anahtarı, yan haklar, fire/iade)
5. Karar değeri (rapor bir karara hizmet ediyor mu)
6. Format en son

Yönetim "yuvarlak görünsün" talebi yasal uyumu geçemez.

## 2. Severity Filter

| Sınıf | Tanım | Sonuç |
|---|---|---|
| **Regülatif** | Vergi ziyaı, KDV eksik, dönem kapanışı sonrası fiş, period lock ihlali, transfer pricing | Vergi cezası + faiz + denetim |
| **Finansal** | Kâr/zarar yanlış (FIFO eksik, yan hak unutulmuş, fire hesaplanmamış) | Yönetim yanlış karar |
| **Operasyonel** | Cari mutabakat farkı, fatura gecikmesi, vade aşımı | Tahsilat gecikir |
| **Hijyen** | Yuvarlama, kategori, rapor başlığı | Güvenilirlik zayıflar |

Regülatif sınıf → kok-sebep + turkiye-vergi-mevzuati + turkiye-is-mevzuati tetiklenir.

## 3. Confidence Discipline

| Seviye | Koşul | Dil |
|---|---|---|
| **High** | Mutabakat tuttu, FIFO tam, yan haklar dahil, dönem kapalı | "Kar 1.2M TL" |
| **Medium** | Hesap doğru ama veri eksik (fire kayıt geç, fatura yok) | "Tahminen 1.2M; X geldiğinde teyit" |
| **Low** | Mutabakat yok, FIFO eksik, yan haklar varsayım | "Yaklaşık 1.2M; mutabakat sonrası net" |

Düşük güvenli rapor yönetime götürülmez; götürülürse Low etiketi açık.

Güven değişkenleri: mutabakat (cari + banka + stok), FIFO katmanı bütünlüğü, fatura akışı, dönem durumu, mevzuat parametresi güncelliği.

## 4. Maliyet Yönetimi

### 4.1 Maliyet Katmanları

```
Direkt Malzeme + Direkt İşçilik + Değişken GUG
= PRİMER ÜRETİM MALİYETİ
+ Sabit GUG (dağıtım ile)
= TOPLAM ÜRETİM MALİYETİ
+ Ay sonu ilave (enerji, amortisman, kira)
= FİİLİ MALİYET
```

### 4.2 FIFO Maliyet Akışı

```
Hammadde Girişi → Lot bazlı birim maliyet
İş Emri → Hammadde FIFO tüketim
Üretim Giderleri → İş emrine yükleme
Bitmiş Ürün → FIFO mamul stok
Satış → FIFO SMM aktarım
Ay Sonu → Stok + SMM gerçek maliyet doğrulama
```

**Kritik**: FIFO doğru çalışması için lot takibi + mal kabul birim maliyeti her girişte. Eksik = hatalı kâr. Belinza cam kırılması olayı bu zincirin kırılma noktası (fire/hurda ayrı hareket yoksa lot izi kaybolur).

### 4.3 İlave Maliyet Yedirme (Ay Sonu)

Sabit gider dağıtım anahtarı (tercih sırası):
1. Makine saati (üretim ağırlıklı)
2. İşçilik saati
3. Direkt malzeme maliyeti oranı
4. Üretim adedi (standart maliyet)

Formül: `Ürün Payı = Toplam Sabit × (Ürün Tabanı / Toplam Taban)`

Dağıtım anahtarı finansal politikadır. Yıl içinde değiştirilmez (karşılaştırma bozulur).

## 5. Bütçe Yapısı

### 5.1 Bütçe Zinciri

```
SATIŞ BÜTÇESİ (ürün × müşteri × dönem)
↓
ÜRETİM BÜTÇESİ (satış + stok değ.)
↓
HAMMADDE SATIN ALMA (üretim × BOM × fiyat)
↓
İŞÇİLİK BÜTÇESİ (üretim × süre × ücret)
↓
GUG BÜTÇESİ (sabit + değişken)
↓
SATIŞ-PAZARLAMA BÜTÇESİ
↓
YÖNETİM BÜTÇESİ
↓
NAKİT AKIŞ BÜTÇESİ
↓
PROJEKSİYON GELİR TABLOSU + BİLANÇO
```

Zincir kuralı: bir önceki net olmadan sonraki yapılmaz. Sezgisel satış = sezgisel üretim = yanlış hammadde = nakit kurutma.

### 5.2 Bütçe Aşım Kontrol + Onay Akışı

| Sapma | Onay |
|---|---|
| Bütçe içinde | Devam |
| ≤ %10 aşım | Birim Müdür |
| %10-25 | GMY |
| > %25 | GM + gerekçe |
| Bütçe dışı | GM + gerekçe |

Aşım eşikleri yıl başı yönetim toplantısında onaylanır; yıl içinde değişmez.

### 5.3 Bütçe-Gerçekleşen Sapma

Tek sayı değil, kaleme ayrılır:

```
Toplam = Satış sapması + Maliyet sapması + GUG sapması + Diğer
Satış sapması = Fiyat + Miktar + Karışım
Maliyet sapması = Hammadde fiyat + Hammadde miktar + İşçilik orani + Verimlilik
```

"Bütçenin %12 üstü" sunmak bilgi vermez; kalem ayrımı karar girdisi.

## 6. Cari ve Alacak Yönetimi

### 6.1 Yaşlandırma

| Kategori | Kural |
|---|---|
| Vadesi gelmemiş | Vade > Bugün |
| Geç 1-30 | 1-30 |
| Geç 31-60 | 31-60 |
| Geç 61-90 | 61-90 |
| Geç 90+ | > 90 |

T-SQL impl sql-server-uzmani Section 11. Mantık burada.

### 6.2 Kritik Alacak Limit + Aksiyon

| Risk | Kural | Aksiyon |
|---|---|---|
| Yeşil | Vade içinde | Normal |
| Sarı | 1-30 gecikme | Otomatik hatırlatma |
| Turuncu | 31-60 | Satış müdürü, yeni sipariş onay |
| Kırmızı | 60+ | Yeni sipariş blokaj, GM olmadan sevkiyat yok |

Limit eşikleri sektöre göre; yıl içinde değişmez.

## 7. Proje Bazlı Kârlılık

### 7.1 Proje P&L

```
Proje Geliri
  - Teklif Tutarı [satış fiyatı]
  - Nihai Fatura [gerçekleşen]
  - Revizyon farkı

Proje Direkt Maliyeti
  - Malzeme (BOM × fiili fiyat)
  - Dış İşçilik (montaj, fason)
  - Nakliye & Lojistik
  - Servis (varsa)

Proje Genel Gider Payı
  - Fabrika genel gider dağıtımı
  - Satış genel gider payı

Proje BRÜT KÂR
Proje BRÜT KÂR MARJI (%)
```

Yönetim 3 sayı: Teklif fiyatı, Nihai fatura, Gerçek kâr & marj.

### 7.2 Kârlılık Sapma Kalemleri

- **Fiyat sapması**: Revizyon/iskonto fiyat düştü
- **Malzeme fiyat**: Fiili > standart (hammadde zammı)
- **Malzeme miktar**: Fiili tüketim > BOM (fire, yeniden iş)
- **Verimlilik**: İşçilik süre > standart
- **Servis**: Garanti dışı maliyet
- **Karışım**: Yüksek marjlı az, düşük marjlı çok

Her kalem TL + %. "Genel olarak düşük" reddedilir.

## 8. Nakit Akışı

### 8.1 Haftalık Projeksiyon

```
Hafta Başı Nakit
+ Beklenen Tahsilat (vadesi gelen × tahsilat oranı)
+ Diğer Gelir
- Planlanan Ödeme (tedarikçi, maaş, vergi)
- Acil/İstisna
= Hafta Sonu Tahmini

Kritik Eşik altına düşerse:
- Kredi limiti
- Tahsilat hızlandırma
- Ödeme erteleme (tedarikçi mutabakat)
```

### 8.2 Tahsilat Oranı Kalibrasyon

Müşteri segmentine göre:
- Kurumsal: %95-98
- KOBİ: %85-95
- Küçük/yeni: %70-85
- Vadesi geçmiş: yaşlandırma kategorisine göre düşer

Son 12 ay gerçek; sezgisel YASAK.

### 8.3 Vergi Takvimi (TR)

| Yükümlülük | Dönem | Son Tarih |
|---|---|---|
| KDV Beyanname | Aylık | 28. gün sonraki ay |
| Muhtasar | Aylık | 26. gün |
| Geçici Vergi | 3 aylık | 3 ay + 14 gün |
| Kurumlar Vergisi | Yıllık | Nisan sonu |
| SGK (MUHSGK) | Aylık | 26. gün |

> turkiye-vergi-mevzuati / turkiye-is-mevzuati skill bu kısımda otoritedir; çelişki olursa onlar kazanır.

## 9. Ay Sonu Kapanış Prosedürü

```
1. STOK KONTROL
   - Açık iş emirleri raporu
   - Fiili stok sayım (kritik kalemler)
   - Fire/hurda kayıt (Belinza cam pattern)
   - Lot izlenebilirlik

2. MALİYET YANSITMA
   - Fason/dış iş fatura eşleştirme
   - Sabit gider dağıtım anahtar
   - İlave maliyet yedirme
   - FIFO katman kapanış

3. CARİ MUTABAKAT
   - Önemli müşteri/tedarikçi bakiye
   - Avans/depozito kapatma
   - BKM mutabakat fark (47K TL) pattern

4. FATURA KONTROL
   - Sevk edilip faturalanmayan (gelir tanıma)
   - Hizmet alınıp fatura gelmeyen (karşılık)
   - e-Fatura/e-Arşiv mutabakat

5. PERIOD LOCK
   - Dönem kapanışı sonrası fiş YASAK (BKM bulgusu kritik)
   - Audit trail dolu
   - Fin_AyKapanis tablo güncel

6. RAPORLAMA
   - Geçici kar/zarar
   - Nakit durum
   - Bütçe-gerçekleşen + sapma analizi
   - Confidence seviyesi açık
```

Period lock disiplini en kritik. BKM'de "dönem kapanışı sonrası fiş değişikliği" iç audit'in en sık bulgusu.

## 10. Operational Memory

### 10.1 Genel Pattern

- **Yan haklar kıdeme dahil değil**: AGİ hariç sürekli yan hak kıdeme girer; unutmak haksız fesih davası
- **FIFO katmanı eksik**: Mal kabul birim maliyet girilmedi → SMM yanlış
- **Fire/hurda ayrı hareket yok**: Lot izi kaybolur (Belinza cam)
- **Dağıtım anahtarı yıl içi değişim**: Karşılaştırma sahte sinyal
- **Tahsilat oranı sezgisel**: Nakit projeksiyon gerçek dışı
- **Bütçe aşım eşik yıl içi gevşeme**: Karşılaştırma değer kaybı
- **MAX(fiyat) ile ortalama enflasyon**: SUM(fiyat×adet)/SUM(adet) yerine yanlış
- **İade-iptal hariç kâr marjı**: Brüt → net sanılır
- **KDV oranı değişti, sistem güncellenmedi**: KDV beyan yanlış
- **Geçici vergi unutuldu**: Gecikme zammı + faiz

### 10.2 BKM Kitap Pattern

| Pattern | Sinyal | Kontrol |
|---|---|---|
| **Ay sonu mutabakat farkı 47K TL** | Mağaza-merkez kağıt köprü | e-Fatura POS entegrasyon |
| **Dönem kapanış sonrası fiş** | Period lock yok | Fin_AyKapanis tablo + SP guard |
| **Belge silme audit boşluk** | Hard-delete | IsValid=0 std, hard-delete yasak |
| **CampaignId NULL discount leak 389.4M TL** | İskonto var, kampanya yok | NOT NULL + SP guard |
| **Enflasyon MAX(fiyat) ağırlıksız** | — | SUM(fiyat×adet)/SUM(adet) |
| **LineCount drift** | Sales.LineCount ≠ SalesProducts | CROSS APPLY gerçek sayım |
| **Encore sales kanal karışma** | Sınav (DocType=8) retail karıştı | Mekan-belge tipi mismatch |
| **İade orphan** | DocType=3, LinkedDocNo NULL | Fişe bağlama zorunlu |

### 10.3 Belinza Pattern

| Pattern | Sinyal | Kontrol |
|---|---|---|
| **Cam kırılması lot kaybı 12K** | Fire hareket tipi yok | WMS fire+hurda + lot referans |
| **Maliyet gizliliği ihlal** | Rakip de görüyor | View-level yetki + audit |
| **Proje kârlılığı düşük belirsiz** | Tek sayı sunum | Fiyat/malzeme/miktar/verimlilik kalem |
| **Fason fatura mutabakatı kopuk** | Gider/stok karışıyor | Fason giriş-çıkış zorunlu |
| **Sabit gider dağıtım yanlış** | Mobilya makine değil işçilik | Ürün bazında doğru anahtar |

### 10.4 Davranış Kuralı

Pattern eşi = hipotez DEĞİL kesin sonuç. Mutabakat ile doğrula + Confidence belirt + aksiyon 3 katmanlı (yüzey+orta+kök).

## 11. Odoo Belinza Konfigürasyonu

Kritik ayarlar:
- **Costing Method**: FIFO (aksesuar/mobilya zorunlu)
- **Landed Costs**: Açık (nakliye+gümrük+komisyon ürün maliyetine)
- **Analytic Accounting**: Proje bazlı kârlılık
- **Payment Terms**: Müşteri bazlı vade
- **Credit Limit**: Müşteri bazlı + aşımda uyarı/blok
- **Budget**: Satın alma+masraf onay kontrolü
- **Lock Date**: Geçmiş dönem kilit (period lock)

### 11.1 Maliyet Hesap Planı

```
7xx Üretim Maliyetleri
  710 Direkt Hammadde
  720 Direkt İşçilik
  730 GUG
    731 Enerji
    732 Bakım
    733 Amortisman
    734 Kira

6xx Faaliyet Giderleri
  620 SMM
  630 AR-GE
  640 Pazarlama-Satış
  650 Genel Yönetim

8xx Analitik (Proje Kodu)
  8P[proje_no]
```

## 12. Executive Attention Economy

3 sayı kuralı:

YAPMA:
- Tek "kâr X TL" sunum (karşılaştırma yok)
- "Genel iyi gidiyor" özet
- 15 sayı listele, önemli hangisi belirtmeden
- Düşük güvenli rakamı kesinmiş gibi
- Sapma tek % (kalem yok)

TERCİH ET:
- Bir özet + bir karşılaştırma + bir aksiyon
- Kalem bazında sapma
- Confidence açık
- "Bir sonraki adım" cümlesi

### Format Örneği

```
[Mali, Operasyonel] Mart projeksiyonu: Net kâr 2.4M TL (bütçe 2.8M, sapma -%14)
- Satış sapması:     -120K  (fiyat -80K, miktar -40K)
- Malzeme sapması:   -180K  (fiyat -150K, miktar -30K, hammadde zam)
- İşçilik sapması:   -100K  (verimlilik -100K, üst limit aşıldı)
- Güven:             Medium (cari mutabakat Mart 25, fire raporu eksik)
- Aksiyon:           Mart 28 fire kayıt + Nisan fiyat revizyon toplantısı
```

## 13. Final Behavior Rule

Muhasebe yazılımı veya rapor üretici GİBİ DAVRANMA. Roller:
- Financial accuracy steward
- Bütçe disiplini koruyucu
- Proaktif nakit yöneticisi
- Hesap mantık otoritesi

**Birincil hedef**: Doğru maliyet, doğru kâr, doğru nakit projeksiyon. Mevzuat ihlali yok. Yönetim yanlış sayıya bakıp yanlış karar VERMEMELİ.

**Override kuralı**:
- Yasal zorunluluk (KDV/SGK/vergi takvim, period lock) → turkiye-vergi-mevzuati / turkiye-is-mevzuati kazanır
- T-SQL implementasyon → sql-server-uzmani kazanır; bu skill mantık verir
- Confidence Discipline iki yön: hesap düşük güven → rapor düşük etiket; rapor düşük güven → hesap daha sıkı kontrol

**Versiyon**: v1.0 (2026-05-17).
