---
name: kvkk-veri-envanteri
description: KVKK 6698 kişisel veri işleme envanteri hazırlama, gözden geçirme ve VERBİS uyum disiplini. BKM Kitap, Belinza için madde 5-6 hukuki sebepleri, VERBİS 22 standart veri kategorisi, kişi grupları, saklama süresi yasal dayanakları, KVKK Mart 2025 envanter rehberi formatı, yurt dışı aktarım rejimi (Eylül 2024 sonrası), VERBİS muafiyet (2025/1572 Kurul Kararı) içerir. "Veri envanteri hazırla", "VERBİS güncelle", "bu süreçte hangi veriler işleniyor", "saklama süresi ne olmalı", "hangi hukuki sebep", "yurt dışı aktarım izni gerekli mi", "envanter eksik mi", "aydınlatma metni uyumlu mu", "kopyala-yapıştır işleme amacı" ifadelerinde tetikle. Severity Filter (kriminal/idari ceza/hijyen), Confidence Discipline (kanun maddesi High, 2026 limit Low), Operational Memory (kopyala-yapıştır amaç, CCTV süresi, açık rıza yanlış kullanımı pattern kütüphanesi) içerir. turkiye-is-mevzuati, turkiye-vergi-mevzuati, insan-kaynaklari, risk-tarama, abd-sozlesme-hukuku ile zincirleme.
id: kvkk-veri-envanteri
category: Etik
tags: [kvkk, 6698, veri-envanteri, VERBİS, kisisel-veri, aydinlatma, acik-riza, yurt-disi-aktarim, ozel-nitelikli, biyometrik, CCTV, saklama-suresi, BKM, Belinza]
token_estimate: 7000
chains: [turkiye-is-mevzuati, turkiye-vergi-mevzuati, insan-kaynaklari, risk-tarama, abd-sozlesme-hukuku]
---

# KVKK Kişisel Veri İşleme Envanteri Disiplini

Bu skill, KVKK 6698 sayılı Kanun ve ikincil mevzuat çerçevesinde kişisel veri işleme envanteri (KVİE) hazırlamak, gözden geçirmek ve güncellemek için sistematik disiplini içerir. Amaç: jenerik bir Excel doldurmak değil, kurumun veri işleme gerçekliğini mahkemede ve Kurul denetiminde savunabilir biçimde haritalamak.

## 1. Yasal Çerçeve ve Otorite Hiyerarşisi

**Birincil mevzuat:**
- 6698 sayılı Kişisel Verilerin Korunması Kanunu (24 Mart 2016)
- Madde 5: Kişisel verilerin işlenme şartları (6 hukuki sebep)
- Madde 6: Özel nitelikli kişisel verilerin işlenmesi (Mart 2024 değişiklikleri)
- Madde 9: Yurt dışına aktarım (Eylül 2024 yeni rejim)
- Madde 10: Aydınlatma yükümlülüğü
- Madde 13: İlgili kişi başvurusu
- Madde 16: VERBİS kayıt yükümlülüğü

**İkincil mevzuat:**
- Veri Sorumluları Sicili Hakkında Yönetmelik (30.12.2017)
- Kişisel Verilerin Silinmesi, Yok Edilmesi veya Anonim Hale Getirilmesi Hakkında Yönetmelik (28.10.2017)

**KVKK kararları ve rehberler:**
- Kişisel Veri İşleme Envanteri Hazırlama Rehberi (Mart 2025 güncel, KVKK Yayın No: 61)
- Kurul Kararı 2018/10 — Özel nitelikli verilerde yeterli önlemler
- Kurul Kararı 2025/1572 (04.09.2025) — VERBİS muafiyet eşikleri

## 2. VERBİS Standart Veri Kategorileri (22 Kategori)

Envanterde **mutlaka bu standart adları** kullan.

**Genel nitelikli (KVKK m.5):**
1. Kimlik · 2. İletişim · 3. Lokasyon · 4. Özlük · 5. Hukuki İşlem · 6. Müşteri İşlem
7. Fiziksel Mekân Güvenliği (CCTV) · 8. İşlem Güvenliği · 9. Risk Yönetimi · 10. Finans
11. Mesleki Deneyim · 12. Pazarlama · 13. Görsel ve İşitsel Kayıtlar

**Özel nitelikli (KVKK m.6, Mart 2024 yeni rejim):**
14. Irk/Etnik · 15. Siyasi · 16. Felsefi/Din · 17. Kılık · 18. Dernek/Vakıf/Sendika
19. Sağlık · 20. Cinsel Hayat · 21. Ceza Mahkûmiyeti · 22. Biyometrik · 23. Genetik

## 3. KVKK Madde 5 Hukuki Sebepleri

**m.5/2 (açık rıza gerekmeyen):**
- a) Kanunlarda öngörülme — VUK, SGK, İş K., MASAK
- b) Fiili imkânsızlık
- c) Sözleşmenin kurulması/ifası
- ç) Hukuki yükümlülük
- d) Alenileştirme
- e) Hakkın tesisi/kullanılması/korunması
- f) Meşru menfaat (en tartışmalı, denetim hedefi)

**m.5/1 (açık rıza)**: Son çare. Kanuni yükümlülük varsa açık rıza istemek dürüstlüğe aykırı.

## 4. KVKK Madde 6 — Özel Nitelikli (Mart 2024 Yeni Rejim)

**m.6/3 sebepleri:**
- a) Açık rıza · b) Kanun · c) Fiili imkânsızlık · ç) Hukuki yük · d) Alenileştirme
- e) Hakkın tesisi · f) Kamu sağlığı/tıbbî · g) İstihdam/İSG/sosyal güvenlik · ğ) Vakıf/dernek/sendika

| Süreç | Veri | Hukuki Sebep |
|---|---|---|
| İSG sağlık raporu | Sağlık | m.6/3/g |
| PDKS parmak izi | Biyometrik | m.6/3/a + alternatif yöntem |
| Ceza sicili işe alım | Ceza | m.6/3/a |
| Müşteri din verisi | Din | İŞLENEMEZ |

**Kurul 2018/10**: Şifreleme zorunlu, yetki matrisi, erişim loglama, eğitim, 72 saat ihlal bildirim.

## 5. Standart Kişi Grupları

Çalışan · Çalışan Adayı · Eski Çalışan · Stajyer · Müşteri · Potansiyel Müşteri · Tedarikçi/Yetkilisi · İş Ortağı · Ziyaretçi · Üçüncü Kişi · Aile Üyesi · Hissedar · Hizmet Alan
(Genişletilmiş: Online kullanıcı, abone, yarışma katılımcısı, ihbarcı, soruşturma tarafları)

## 6. Envanter Zorunlu Alanları (KVKK Mart 2025 Rehberi)

1. Süreç adı (spesifik) · 2. Departman · 3. Veri Kategorisi (22 standart) · 4. Veri Türü
5. Kişi Grubu · 6. İşleme Amacı (jenerik kopya YASAK) · 7. Hukuki Sebep (somut bend)
8. Alıcı · 9. Yurt Dışı Aktarım · 10. Saklama Süresi · 11. İmha Yöntemi
12. İdari Tedbirler · 13. Teknik Tedbirler

## 7. Saklama Süresi — Yasal Dayanak Matrisi

| Veri | Süre | Dayanak |
|---|---|---|
| Bordro, ücret | 10 yıl | İş K. m.32, TBK m.146 |
| SGK işe giriş | 10 yıl + emekli | SGK 5510 m.86 |
| Özlük | 10 yıl | İş K. m.75 |
| Aday CV (alınmayan) | 1 yıl | Meşru menfaat |
| Aday CV açık rıza | 2 yıl max | Periyodik yenileme |
| Vergi belgeleri | 5 yıl | VUK m.253 |
| Ticari defter | 10 yıl | TTK m.82 |
| Müşteri sözleşmesi | 10 yıl | TBK m.146 |
| Pazarlama verisi | Açık rıza süresi (max 2 yıl) | Açık rıza |
| Log (IP, login) | 2 yıl Yer Sağ., 1 yıl İçerik | 5651 m.5 |
| **CCTV** | **30-60 gün max** | **Kurul 90 günü eleştiriyor** |
| Çağrı kayıt | 1-3 yıl | Sözleşme + meşru menfaat |
| Ziyaretçi giriş | 2 yıl | Meşru menfaat |
| İş kazası | 10 yıl + dava ZA | İş K., SGK |
| KVKK başvuru | 3 yıl | KVKK m.13 |
| KVKK ihlal bildirimi | 5 yıl | Kurul standart |
| Açık rıza kayıt | Veri saklama + 10 yıl | İspat, TBK m.146 |
| İYS pazarlama izin | İYS geçerli olduğu sürece | 6563 |
| Aydınlatma metin versiyon | 10 yıl | KVKK m.10 ispat |

**Severity**: Yasal süresi BİTMİŞ veriyi silmemek → Kriminal. DOLMAMIŞ veriyi erkenden silmek → Kriminal.

## 8. Yurt Dışı Aktarım — Eylül 2024 Yeni Rejim

**m.9 mekanizması:**
1. Yeterlilik kararlı ülkeye → serbest (şu an liste boş)
2. Uygun güvence: a) Standart sözleşme b) BCR c) Taahhütname (Kurul onayı)
3. Arızi (m.9/6): açık rıza, sözleşme ifası, kamu yararı, hak tesisi, hayati tehlike

**ATLASCOREUS / Atlas LLC (ABD)**: yeterlilik kararı yok → standart sözleşme veya açık rıza
**BKM Kitap**: Microsoft 365, Google Workspace, AWS, GitHub, Slack → yurt dışı aktarım VAR; sözleşme ifası + meşru menfaat + standart sözleşme öneri

## 9. VERBİS Muafiyet (Kurul 2025/1572, 04.09.2025)

Yıllık çalışan < 50 **VE** mali bilanço < 100M TL **VE** ana faaliyet özel nitelikli veri DEĞİL → VERBİS muaf.
**Muafiyet sadece VERBİS kayıt**; envanter zorunluluğu kalkmaz.
BKM Kitap muhtemelen muafiyetten çıkar (50+ çalışan, 100M+ bilanço).

## 10. Operational Memory — Sık Hatalar

### Pattern 1: Kopyala-Yapıştır Amaç
5 farklı süreçte aynı "Toplantı bilgi sunum" amacı. **Çözüm**: her süreç özgün amaç. **Severity**: idari ceza + aydınlatma uyumsuzluğu.

### Pattern 2: Açık Rıza ile Kanuni Yükümlülük Karıştırma
Bordro için "açık rıza". **Çözüm**: m.5/2/a + m.5/2/ç (İş K. + SGK). Açık rıza geri çekildiğinde işleme durmak zorunda gibi yanlış izlenim.

### Pattern 3: CCTV 10 Yıl Saklama
Kameraya 10 yıl. **Çözüm**: 30-60 gün; Kurul 90 üstünü eleştiriyor. Olay tespit edildiyse o kayıt **olay dosyası** ayrı saklanır.

### Pattern 4: Envanter ↔ Aydınlatma Uyumsuzluğu
Aydınlatma metninde olmayan amaç envanterde. **Çözüm**: aydınlatma envanterden TÜRETİLİR; periyodik çapraz kontrol. **Kritik** — KVKK m.10 ihlali + 2026 denetim hedefi.

### Pattern 5: Aday CV 10 Yıl
İK envanterinde 10 yıl. **Çözüm**: 1 yıl meşru menfaat, açık rıza ile max 2 yıl.

### Pattern 6: Yurt Dışı "Yok" Yanılgısı
"Aktarmıyoruz" dediği halde Microsoft 365 + Slack + AWS kullanılıyor. **Çözüm**: her SaaS = aktarım. KVKK m.9 ihlali, ceza yüksek.

### Pattern 7: Whistleblowing Envanterde Yok
"Anonim aldığımız için kişisel veri yok" yanılgısı. **Çözüm**: ihbar edilen kişinin verisi mutlaka var; özel nitelikli olasılığı; özel önlemler.

### Pattern 8: İhlal Yönetim Süreci Yok
72 saat bildirim için süreç tanımsız. **Çözüm**: İhlal Yönetim Prosedürü + envanter + yıllık tatbikat.

## 11. Envanter Üretim Protokolü

1. Departman+birim haritası (org şema)
2. Her birim için süreç envanteri (ISO 9001 kalite haritası)
3. Her süreç 7 alan: Departman / Faaliyet / Veri Kategori / Kişi Grubu / Amaç / Saklama + Hukuki Sebep + Yurt Dışı
4. Aydınlatma çapraz kontrol
5. VERBİS çapraz kontrol
6. İmha Politikası çapraz kontrol
7. Periyodik review (yılda en az 1, ideal 6 ayda)

## 12. Karar Cümlesi Şablonu

- **A güvenilir**: "Envanter Mart 2025 rehberine uyumlu. VERBİS'e beyan edilebilir. N küçük eksiklik X tarihine kadar düzeltilebilir."
- **B eksik**: "Envanter denetime hazır değil. N kritik eksiklik [...]. VERBİS güncelleme yapılmamalı; beyan-gerçeklik uyumsuzluğu idari ceza riskini büyütür. 30-60 gün."
- **C yeniden yaz**: "Envanter yapısal sorunlar. Faz 1: haritalama (4-6h), Faz 2: amaç-sebep eşleştirme (2-3h), Faz 3: aydınlatma+imha (2h). Toplam 8-11 hafta."

## 13. Red Lines

1. Hukuki sebep uydurma (meşru menfaat jolly card değil; orantılılık)
2. Kopyala-yapıştır amaç
3. Açık rızayı varsayılan yapma
4. CCTV 90+ gün önerme
5. "KVKK uygulanmaz" cevap (Wyoming LLC bahanesi geçersiz)
6. Saklama süresine yasal dayanak yazmama
7. Özel nitelikli veriyi normal gibi işleme (şifreleme + erişim + log zorunlu)

**Versiyon**: v1.0 (Mayıs 2026). Dayanak: KVKK Envanter Rehberi Mart 2025, Kurul 2025/1572, m.9 yeni rejim Eylül 2024, m.6 yeni rejim Mart 2024.
