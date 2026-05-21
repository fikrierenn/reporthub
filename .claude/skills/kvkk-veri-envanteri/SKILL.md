---
name: kvkk-veri-envanteri
description: KVKK 6698 kişisel veri işleme envanteri hazırlama, gözden geçirme ve VERBİS uyum disiplini. BKM Kitap, Belinza için madde 5-6 hukuki sebepleri, VERBİS 22 standart veri kategorisi, kişi grupları, saklama süresi yasal dayanakları, KVKK Mart 2025 envanter rehberi formatı, yurt dışı aktarım rejimi (Eylül 2024 sonrası), VERBİS muafiyet (2025/1572 Kurul Kararı) içerir. "Veri envanteri hazırla", "VERBİS güncelle", "bu süreçte hangi veriler işleniyor", "saklama süresi ne olmalı", "hangi hukuki sebep", "yurt dışı aktarım izni gerekli mi", "envanter eksik mi", "aydınlatma metni uyumlu mu", "kopyala-yapıştır işleme amacı" ifadelerinde tetikle. Severity Filter (kriminal/idari ceza/hijyen), Confidence Discipline (kanun maddesi High, 2026 limit Low), Operational Memory (kopyala-yapıştır amaç, CCTV süresi, açık rıza yanlış kullanımı pattern kütüphanesi) içerir. turkiye-is-mevzuati, turkiye-vergi-mevzuati, insan-kaynaklari, risk-tarama, abd-sozlesme-hukuku ile zincirleme.
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

**Confidence Discipline:**
- Kanun maddesi, yönetmelik maddesi → **High** (sabit)
- Kurul kararı sayısı/tarihi → **High** (sabit, ancak değişebilir, beyan ederken kontrol et)
- 2026 idari para cezası alt-üst sınır → **Low** (her yıl yeniden değerlendirme tebliği, web search ile teyit)
- VERBİS muafiyet eşiği (mali bilanço, çalışan sayısı) → **Medium** (Kurul kararı ile değişebilir, son kararı teyit)

## 2. VERBİS Standart Veri Kategorileri (22 Kategori)

Envanterde **mutlaka bu standart adları** kullan. Kafadan kategori uydurma. Kurum-özgü alanlar varsa standart kategori altına alt-kırılım olarak yaz.

**Genel nitelikli kişisel veri (KVKK m.5):**
1. **Kimlik** — Ad-soyad, anne-baba adı, doğum tarihi/yeri, medeni hal, TC kimlik no, nüfus cüzdanı seri-sıra no
2. **İletişim** — Adres, e-posta, telefon, KEP, fax
3. **Lokasyon** — GPS, seyahat verisi, araç takip
4. **Özlük** — Bordro, disiplin soruşturması, işe giriş-çıkış kayıtları, mal bildirimi, özgeçmiş, performans
5. **Hukuki İşlem** — Adli yazışmalar, dava dosyası, vekâletname
6. **Müşteri İşlem** — Çağrı merkezi kaydı, fatura, senet, çek, sipariş, talep
7. **Fiziksel Mekân Güvenliği** — Giriş-çıkış kayıt, kamera (CCTV) görüntü ve ses
8. **İşlem Güvenliği** — IP, login-logout, şifre, parola, MAC, log
9. **Risk Yönetimi** — Ticari, teknik, idari risk yönetimi için işlenen
10. **Finans** — Bilanço, finansal performans, kredi notu, mal varlığı, banka hesap, IBAN
11. **Mesleki Deneyim** — Diploma, sertifika, kurs, transkript, meslek içi eğitim
12. **Pazarlama** — Alışveriş geçmişi, anket, çerez, kampanya, segmentasyon
13. **Görsel ve İşitsel Kayıtlar** — Fotoğraf, ses kaydı (kamera kayıtları 7. kategoride; bu kategori CCTV dışı)

**Özel nitelikli kişisel veri (KVKK m.6 — Mart 2024 değişiklikleri ile yeni rejim):**
14. **Irk ve Etnik Köken**
15. **Siyasi Düşünce**
16. **Felsefi İnanç, Din, Mezhep veya Diğer İnançlar**
17. **Kılık Kıyafet**
18. **Dernek, Vakıf ve Sendika Üyeliği**
19. **Sağlık Bilgileri**
20. **Cinsel Hayat**
21. **Ceza Mahkûmiyeti ve Güvenlik Tedbirleri**
22. **Biyometrik Veri** (parmak izi, retina, yüz tanıma)
23. **Genetik Veri**

**Severity Filter:**
- Özel nitelikli verinin envanter dışında işlenmesi → **Kriminal** (KVKK m.18 — idari para cezası + Kurul tedbir kararı + olası TCK 135-138 cezai sorumluluk)
- Kategori adının yanlış yazılması (örneğin "kimlik bilgisi" yerine "kişisel bilgiler") → **Hijyen** (denetim eleştirisi, hızla düzeltilebilir)

## 3. KVKK Madde 5 Hukuki Sebepleri (Genel Nitelikli)

Her işleme faaliyeti için **EN AZ BİR** hukuki sebep gerekir. Birden fazla sebep aynı anda uygulanabilir; ama envanterde **birincil ve ikincil** ayrımı yapılmalı.

**KVKK m.5/2 (açık rıza gerekmeyen haller):**
- **a) Kanunlarda açıkça öngörülme** — VUK, SGK, İş K., MASAK, ÇGTM mevzuatı, Vergi Usul, Sermaye Piyasası
- **b) Fiili imkânsızlık** — İlgili kişi rıza beyanını veremeyecek durumda; hayatı/beden bütünlüğünün korunması
- **c) Sözleşmenin kurulması veya ifası** — Müşteri sözleşmesi, iş sözleşmesi, tedarikçi sözleşmesi
- **ç) Veri sorumlusunun hukuki yükümlülüğü** — Vergi beyanı, SGK bildirimi, MUHSGK, Ba-Bs, KAP
- **d) İlgili kişinin kendisi tarafından alenileştirme** — Sosyal medya profilinden alınan veri
- **e) Bir hakkın tesisi, kullanılması veya korunması** — Dava, icra, alacak takibi
- **f) Meşru menfaat** — Veri sorumlusunun, ilgili kişinin temel hak ve özgürlüklerine zarar vermeyen meşru menfaati (en geniş ve en tartışmalı sebep; denetim hedefi)

**KVKK m.5/1 (açık rıza)** — Yukarıdaki 6 sebepten hiçbiri uygulanamıyorsa son çare olarak açık rıza alınır. **Önemli:** Açık rıza gerçekten gerekiyorsa alınır; mevcut kanuni yükümlülüğü olan bir işleme için açık rıza istemek **dürüstlük kuralına aykırıdır** ve Kurul tarafından eleştirilir (2026 denetim hedefi).

## 4. KVKK Madde 6 — Özel Nitelikli Veri Hukuki Sebepleri (Mart 2024 Değişiklikleri)

Mart 2024 değişiklikleriyle özel nitelikli verilerin işleme rejimi değişti. **Eski "sağlık verisi sadece sağlık kurumlarınca işlenir" kuralı kaldırıldı.**

**KVKK m.6/3 sebepleri (her özel nitelikli veri için geçerli):**
- a) İlgili kişinin **açık rızası**
- b) Kanunlarda açıkça öngörülmesi
- c) Fiili imkânsızlık
- ç) Hukuki yükümlülüğün yerine getirilmesi (yalnızca m.5/2/ç'deki kadar)
- d) İlgili kişinin alenileştirmesi
- e) Bir hakkın tesisi, kullanılması, korunması
- f) Sır saklama yükümlülüğü altındaki kişi veya kurumlarca **kamu sağlığı, koruyucu hekimlik, tıbbî teşhis, tedavi ve bakım hizmetleri**, sağlık hizmetleri planlama ve yönetimi
- g) İstihdam, iş sağlığı ve güvenliği, sosyal güvenlik, sosyal hizmetler ve sosyal yardım alanındaki hukuki yükümlülükler
- ğ) Vakıf, dernek, sendika ve diğer kâr amacı gütmeyen yapıların mevcut/eski üyelerine yönelik işleme

**Pratik özel nitelikli veri işleme örnekleri ve hukuki sebep eşleşmesi:**

| Süreç | Veri | Birincil Hukuki Sebep |
|---|---|---|
| İSG sağlık raporu | Sağlık | m.6/3/g |
| İşe giriş kan grubu | Sağlık | m.6/3/g (İSG için) |
| Engelli istihdam belgesi | Sağlık | m.5/2/ç + m.6/3/g |
| PDKS parmak izi | Biyometrik | m.6/3/a (açık rıza) — alternatif yöntem sunulmalı |
| Ceza sicili (yönetici işe alım) | Ceza mahkûmiyeti | m.6/3/a (açık rıza) — orantılılık şart |
| Müşteri din/mezhep verisi | Din | İşlenemez — özel sebep yoksa toplama |

**Kurul Kararı 2018/10 — Özel nitelikli verilerde alınacak yeterli önlemler:**
- İşlenen verilerin sınırlandırılması (data minimization katı uygulanır)
- Şifreleme zorunlu (özellikle iletim ve saklama)
- Yetki matrisi katı uygulanır
- Erişim loglama detaylı
- Çalışan eğitimi belgelendirilmeli
- Veri ihlali bildirimi 72 saat (m.12/5)

## 5. Standart Kişi Grupları

Envanterde **kişi grubunu mutlaka belirt**. Bir süreçte birden fazla kişi grubu olabilir.

**Çekirdek gruplar:**
- **Çalışan** (mevcut iş ilişkisi)
- **Çalışan Adayı** (CV gönderen, mülakat aşamasındakiler)
- **Eski Çalışan** (iş ilişkisi sona ermiş)
- **Stajyer** (zorunlu/gönüllü staj)
- **Müşteri** (sözleşmeli, kayıtlı, satış yapılmış)
- **Potansiyel Müşteri** (newsletter abonesi, lead, anonim çerez kaydı)
- **Tedarikçi/Yetkilisi** (B2B partner, mal/hizmet sağlayan)
- **İş Ortağı/Yetkilisi** (komisyon, dağıtım, franchise)
- **Ziyaretçi** (binaya/mağazaya gelen, kamera kaydı alanına giren)
- **Üçüncü Kişi** (referans, kefil, acil durum kişisi)
- **Aile Üyesi/Yakını** (çalışan yakını, müşteri vekili)
- **Hissedar/Ortak** (gerçek kişi hissedar)
- **Hizmet Alan Kişi** (yardım masası, müşteri hizmetleri arayanı)

**Genişletilmiş gruplar (kurum-özgü):**
- Online kullanıcı / mobil uygulama kullanıcısı
- E-bülten abonesi
- Yarışma/çekiliş katılımcısı
- Etkinlik/eğitim katılımcısı
- İhbarcı (whistleblower)
- Soruşturma tarafları

## 6. KVKK Mart 2025 Envanter Rehberi — Zorunlu Alanlar

KVKK'nın resmi envanter formatı şu alanları içerir. Kurum bu alanlardan **hiçbirini eksik bırakamaz**. Görsel ortamı veya tablonun başlığında varsayılan olarak görseldeki gibi 6 alan görünse bile alt detayda eksiksiz olmalı.

**Zorunlu alanlar:**
1. **Süreç adı / Faaliyet** — Spesifik, jenerik değil (örnek: "Bordro hesaplama" değil "Stok Sayım Operasyonu")
2. **Departman / Birim** — Sahibi belirli
3. **Veri Kategorisi** — Yukarıdaki 22 standart kategoriden
4. **Veri Türü (alt-kırılım)** — Kategori altında somut alanlar
5. **Kişi Grubu** — Yukarıdaki standart gruplardan
6. **İşleme Amacı** — Spesifik, süreçle eşleşmiş (jenerik kopyala-yapıştır YASAK)
7. **Hukuki Sebep** — KVKK m.5/2 veya m.6/3'ten somut bend
8. **Alıcı / Alıcı Grupları** — Kim/hangi kurumla paylaşılıyor
9. **Yurt Dışı Aktarım** — Var/yok; varsa ülke ve hukuki mekanizma
10. **Saklama Süresi** — Yıl/ay olarak, yasal dayanakla
11. **İmha Yöntemi** — Silme/yok etme/anonimleştirme
12. **İdari Tedbirler** — Politika, eğitim, sözleşme, yetki matrisi
13. **Teknik Tedbirler** — Şifreleme, loglama, erişim kontrolü, yedekleme

## 7. Saklama Süresi — Yasal Dayanak Matrisi

Saklama süresi **uydurulamaz**. Her sürenin bir yasal dayanağı veya iç politika gerekçesi olmalı. En sık karıştırılanlar:

| Veri Tipi | Süre | Yasal Dayanak |
|---|---|---|
| Bordro, ücret hesabı | 10 yıl | İş K. m.32, TBK m.146 zamanaşımı |
| SGK işe giriş, hizmet dökümü | 10 yıl + emekli olunca | SGK 5510 m.86 |
| Özlük dosyası | 10 yıl (sonra anonim) | İş K. m.75 |
| Çalışan adayı CV (işe alınmayanlar) | 1 yıl | Meşru menfaat — sonraki pozisyon |
| Aday CV açık rıza ile uzun saklama | 2 yıl (azami) | Açık rıza, periyodik yenileme |
| Vergi belgeleri (fatura, defter) | 5 yıl | VUK m.253 |
| Ticari defter | 10 yıl | TTK m.82 |
| KDV beyannamesi delili | 5 yıl | VUK m.253 |
| Banka mutabakat, ekstre | 10 yıl | TTK m.82 |
| Müşteri sözleşmesi | 10 yıl (fesihten sonra) | TBK m.146 zamanaşımı |
| Müşteri pazarlama verisi | Açık rıza süresi (azami 2 yıl periyodik yenileme) | Açık rıza |
| E-ticaret sipariş verisi | 10 yıl | TTK m.82, TBK m.146 |
| Kullanıcı log (IP, login) | 2 yıl | 5651 sayılı Kanun m.5 (Yer Sağlayıcı), 1 yıl (İçerik Sağlayıcı) |
| **CCTV (kamera) görüntüsü** | **30-60 gün (azami)** | **Meşru menfaat, orantılılık — Kurul kararları 90 gün üzerini eleştiriyor** |
| Çağrı merkezi ses kaydı | 1-3 yıl (amaca göre) | Sözleşme ifası + meşru menfaat |
| Ziyaretçi giriş-çıkış | 2 yıl | Meşru menfaat |
| İş kazası belgesi | 10 yıl + dava zamanaşımı | İş K., 5510 SGK |
| KVKK ilgili kişi başvuru evrakı | 3 yıl | KVKK m.13, denetim için |
| KVKK veri ihlali bildirim evrakı | 5 yıl | Kurul kararı standardı |
| Veri ihlali olay kaydı | 5 yıl | Kurul iyi uygulama önerisi |
| Açık rıza kayıt evrakı | Veri saklama süresi + 10 yıl | İspat yükümlülüğü, TBK m.146 |
| Dijital pazarlama izni (İYS) | İYS kaydı geçerli olduğu sürece | 6563 Sayılı Kanun |
| Aydınlatma metni versiyon arşivi | 10 yıl | KVKK m.10, ispat |

**Severity Filter — Saklama süresi:**
- Yasal saklama süresi BİTMİŞ veriyi silmemek → **Kriminal** (Kurul'a şikayet → idari para cezası, 2026'da örnek vakalar artıyor)
- Yasal saklama süresi DOLMAMIŞ veriyi erkenden silmek → **Kriminal** (vergi, iş hukuku ihlali; cezai sorumluluk)
- Saklama süresinin envanterde yazılı olmaması → **İdari ceza riski** (denetimde kritik bulgu)

## 8. Yurt Dışı Aktarım — Eylül 2024 Sonrası Yeni Rejim

**Önceki rejim (Eylül 2024 öncesi):** Yurt dışına aktarım için açık rıza VEYA Kurul izni VEYA taahhütname zorunluydu. Pratikte kilit darboğazıydı.

**Yeni rejim (KVKK m.9, Eylül 2024 değişikliği):**

**1. Yeterlilik kararı bulunan ülkeye aktarım:** Aktarım serbest (Kurul listesinde yeterlilik kararlı ülke şu an yok — sadece teorik)

**2. Uygun güvence ile aktarım (yeterlilik kararı yoksa):**
- a) Standart sözleşme (Kurul tarafından yayımlanmış model)
- b) Bağlayıcı şirket kuralları (BCR — grup şirketleri için)
- c) Taahhütname (Kurul onayı şart)

**3. Arızi durumlarda aktarım (m.9/6):**
- Açık rıza
- Sözleşme ifası
- Üstün kamu yararı
- Hakkın tesisi/kullanılması
- Hayati tehlike

**Atlas Digital Commerce LLC / ATLASCOREUS özelinde:**
- Etsy, Stripe, Mercury Bank, Podbul, AWS — bunlar veri ABD'ye akıyor
- ABD'nin yeterlilik kararı yok → **standart sözleşme** veya **arızi olarak müşteri açık rızası** gerekli
- ATLASCOREUS müşterileri zaten ABD platformunda işlem yapıyor → m.9/6/b (sözleşme ifası) uygulanabilir, ancak aydınlatma metninde **açıkça belirtilmeli**

**BKM Kitap özelinde:**
- Microsoft 365, Google Workspace, AWS, Azure, GitHub, Slack — yurt dışı aktarım var
- Aktarım gerekçesi sözleşme ifası + meşru menfaat
- Standart sözleşme imzası önerilir (Kurul listesi takip)

## 9. VERBİS Kayıt Yükümlülüğü ve Muafiyet (2025/1572)

**Mevcut muafiyet eşiği (Kurul Kararı 2025/1572, 04.09.2025):**
- Yıllık çalışan sayısı 50'den AZ **VE**
- Yıllık mali bilanço toplamı **100 milyon TL'nin altında** olan veri sorumluları
- **VE** ana faaliyet konusu özel nitelikli kişisel veri işleme **OLMAYAN** kurumlar

→ VERBİS kayıt yükümlülüğünden **muaftır**.

**Önemli:** Muafiyet sadece VERBİS kayıttan; envanter hazırlama yükümlülüğü değildir. KVKK'ya tabi her veri sorumlusu envanter hazırlamak zorunda; muafiyet sadece VERBİS'e beyan zorunluluğunu kaldırır.

**BKM Kitap muafiyetten çıkar:** Çalışan sayısı 50+ ve mali bilanço 100M TL üstü olması yüksek olasılıkla → VERBİS kayıt zorunlu.
**Atlas Digital Commerce LLC:** Türkiye'de veri sorumlusu değil (Wyoming şirketi); KVKK kapsamı değil — Türk müşteriye satış yapsa bile GDPR ve Türkiye yasaları karmaşıklığı için ayrı incele.

## 10. Operational Memory — Sık Görülen Hatalar (Pattern Library)

Envanter denetimlerinde en sık karşılaşılan kalitatif hatalar. Yeni envanter üretirken bu listeyi gözden geçir.

### Pattern 1: Kopyala-Yapıştır İşleme Amacı
**Belirti:** Görseldeki gibi 5 farklı süreçte aynı "Toplantı İle İlgili Bilgi Sunulması ve Güvenliği" amacı.
**Sebep:** Departmandan formu doldurması istenen orta kademe çalışan örnek bir cümle kopyalamış, hepsine yapıştırmış.
**Çözüm:** Her sürecin amacı süreçle eşleşmeli. "Stok Sayım" için amaç "envanter doğruluğunun sağlanması ve fire takibi", "Süreç ve Performans Analizi" için amaç "iş süreçlerinin verimlilik açısından değerlendirilmesi".
**Severity:** İdari ceza riski (denetimde kritik bulgu) + aydınlatma metni uyumsuzluğu.

### Pattern 2: Açık Rıza ile Kanuni Yükümlülüğü Karıştırma
**Belirti:** Bordro işleme için "açık rıza" hukuki sebebi yazılı.
**Sebep:** "Hassas konu, açık rıza alalım emin olalım" zihniyeti.
**Çözüm:** Bordro için hukuki sebep m.5/2/a (kanunda öngörülme — İş K., 5510 SGK) ve m.5/2/ç (hukuki yükümlülük). Açık rıza yanlış ve sakıncalı (çünkü rıza geri çekildiğinde işlemeye devam edilemez gibi yanlış bir izlenim doğar — ki bordro için durmaya hakkımız yok).
**Severity:** İdari ceza riski + yanlış rıza yönetimi.

### Pattern 3: CCTV 10 Yıl Saklama
**Belirti:** Kamera kayıtları 10 yıl saklama gösterilmiş.
**Sebep:** Diğer veriler için 10 yıl yazıyor, kameraya da aynısı yazılmış.
**Çözüm:** CCTV için orantılılık ilkesi: 30-60 gün. Kurul 90 günden fazlasını eleştiriyor. Bir olay (hırsızlık, iş kazası, ihlal) tespit edildiyse o spesifik kayıt **olay dosyası** olarak ayrı saklanır (kanuni süre veya dava zamanaşımı).
**Severity:** İdari ceza riski + denetim eleştirisi.

### Pattern 4: Envanter ile Aydınlatma Metni Uyumsuzluğu
**Belirti:** Aydınlatma metninde "pazarlama amacı" yok ama envanterde var; veya tersi.
**Sebep:** İki belge farklı zamanlarda farklı kişilerce hazırlanmış.
**Çözüm:** Aydınlatma metni envanterden TÜRETİLİR; tersi değil. Envanter ana referans. Periyodik (yılda en az bir kez) iki belge çapraz kontrol edilir.
**Severity:** Kritik — KVKK m.10 ihlali + 2026 Kurul denetim hedefi.

### Pattern 5: Çalışan Adayı CV'sini 10 Yıl Saklama
**Belirti:** İK envanterinde aday CV'leri 10 yıl saklama.
**Sebep:** "Belki tekrar başvurur" mantığı.
**Çözüm:** İşe alınmayan aday CV'si: 1 yıl meşru menfaat, açık rıza ile en çok 2 yıl (periyodik yenileme). 10 yıl orantısız.
**Severity:** İdari ceza riski.

### Pattern 6: Yurt Dışı Aktarım "Yok" Yazılı, Halbuki Var
**Belirti:** Envanterde "yurt dışı aktarım yok"; gerçekte Microsoft 365 (Microsoft Ireland → ABD veri merkezleri), Google Workspace, Slack, AWS, GitHub kullanılıyor.
**Sebep:** "Şirket olarak yurt dışına aktarmıyoruz" varsayımı; SaaS aktarımları gözden kaçıyor.
**Çözüm:** Her SaaS tedarikçisi için aktarım var sayılır. Sözleşme ve veri işleme şartlarını kontrol et.
**Severity:** Kritik — KVKK m.9 ihlali, idari ceza yüksek.

### Pattern 7: Etik Hat/İhbar Süreci Envanterde Yok
**Belirti:** Whistleblowing hattı kurulmuş ama envanterde süreç olarak yer almıyor.
**Sebep:** "Anonim aldığımız için kişisel veri yok" yanılgısı.
**Çözüm:** İhbar edilen kişinin verisi mutlaka var; ihbar edenin kim olduğu bile metadata'dan çıkarılabilir. Süreç envanterde tam yer alır; özel nitelikli veri olasılığı yüksek; özel önlemler (şifreleme, sınırlı erişim, ayrı sistem) zorunlu.

### Pattern 8: KVKK İhlal Yönetim Süreci Yok
**Belirti:** Veri ihlali olduğunda 72 saatte Kurul'a bildirim için süreç tanımlı değil.
**Sebep:** "İhlal olmaz" varsayımı.
**Çözüm:** İhlal Yönetim Prosedürü zorunlu; envanterde KVKK/Hukuk-Uyum departmanı altında ayrı süreç olarak listelenmeli; yıllık tatbikat önerilir.

## 11. Envanter Üretim Protokolü (BKM, Belinza için)

Yeni bir envanter oluştururken veya mevcut envanteri review ederken bu sırayla ilerle:

**Adım 1: Departman ve birim haritası**
- Organizasyon şemasından her departman ve alt birim listelenir
- Veri işleme yapan her birim envantere dahil edilir

**Adım 2: Her birim için süreç envanteri**
- Birim sorumlusu ile birlikte (veya ISO 9001 kalite süreç haritasından) tüm operasyonel süreçler listelenir
- Hijyen kontrolü: "Bu süreçte hiçbir kişisel veri yok mu?" sorusu sorulur — çoğu zaman var

**Adım 3: Her süreç için 7 alanı doldur (görsel sütunlar + hukuki sebep)**
- Departman / Birim / Faaliyet / Veri Kategorisi / Kişi Grubu / İşleme Amacı / Saklama Süresi
- Plus: Hukuki Sebep (m.5/2/x veya m.6/3/x)
- Plus: Yurt Dışı Aktarım var/yok

**Adım 4: Aydınlatma metni çapraz kontrolü**
- Envanterdeki her işleme amacı aydınlatma metninde geçiyor mu?
- Aydınlatma metninde yer alan amaç envanterde tanımlı mı?

**Adım 5: VERBİS beyanı ile çapraz kontrolü**
- Envanter ve VERBİS beyanı aynı kategori-amaç-saklama matrisini içeriyor mu?

**Adım 6: Saklama ve İmha Politikası ile çapraz kontrolü**
- Envanter saklama süreleri imha politikasına yansımış mı?
- Periyodik imha takvimi var mı? (6 aylık veya yıllık)

**Adım 7: Periyodik review**
- Yılda en az bir kez (KVKK iyi uygulama: 6 ayda bir)
- Yeni süreç eklendiğinde anlık güncelleme
- Tedarikçi değişikliğinde aktarım kayıtları gözden geçirilir

## 12. Karar Cümlesi Şablonu

Envanter review sonunda Fikri'ye veya birim yöneticisine sunulan rapor şöyle bitmelidir:

**Senaryo A (envanter güvenilir):**
"Envanter Mart 2025 KVKK rehberine uyumlu. VERBİS'e beyan edilebilir. Tespit edilen X küçük eksikliği (saklama süresi hijyeni, kategori adlandırması) Y tarihine kadar düzeltilebilir."

**Senaryo B (envanter eksik ama düzeltilebilir):**
"Envanter denetime hazır değil. N kritik eksiklik var: [listele]. Bu eksiklikler giderilmeden VERBİS güncellemesi yapılmamalı, aksi takdirde beyan-gerçeklik uyumsuzluğu idari ceza riskini büyütür. Eksikliklerin giderilme süresi: 30-60 gün."

**Senaryo C (envanter yeniden yazılmalı):**
"Envanter yapısal sorunlar içeriyor: kopyala-yapıştır işleme amaçları, hukuki sebep yanlış eşleşmeleri, özel nitelikli verilerin envanter dışı işlemesi. Mevcut envanter VERBİS beyan tabanı olamaz. Faz 1: yeniden veri haritalama (4-6 hafta); Faz 2: süreç-amaç-hukuki sebep eşleştirme (2-3 hafta); Faz 3: aydınlatma metni ve imha politikası yenilenme (2 hafta). Toplam: 8-11 hafta."

## 13. Zincirleme Skiller

Bu skill aşağıdaki skill'lerle birlikte tetiklenir:

- **turkiye-is-mevzuati** — Özlük, bordro, SGK, kıdem saklama süreleri için yasal otorite
- **turkiye-vergi-mevzuati** — VUK 5 yıl, TTK 10 yıl, e-belgeler için
- **insan-kaynaklari** — İK süreçlerinin envantere dökülmesi için
- **risk-tarama** — Veri ihlali risk yönetimi
- **abd-sozlesme-hukuku** — Atlas Digital Commerce LLC yurt dışı aktarım sözleşmeleri (DPA, SCC)
- **kok-sebep** — Envanter neden tutarsız sorularında 5 Whys
- **sql-server-uzmani** — Veri saklama-imha SP'leri (period lock, kişisel veri masking)
- **finans-butce-muhasebe** — Mali veri saklama süreleri için
- **bi-dashboard** — KVKK uyum dashboard tasarımı

## 14. Red Lines (Kırmızı Çizgiler)

Bu skill çalışırken aşağıdakileri ASLA yapma:

1. **Hukuki sebep uydurma:** "Meşru menfaat" her şeyin altına yazılan jolly card değil. Orantılılık testi yapılır.
2. **Kopyala-yapıştır işleme amacı:** Görseldeki BKM Kitap envanteri hatası. Her süreç özgün amaç ister.
3. **Açık rızayı varsayılan yapma:** Açık rıza son çare; diğer sebepler tükenmeden başvurulmaz.
4. **CCTV 90 günden fazla saklama önerme:** Olay yoksa azami 60 gün.
5. **"KVKK uygulanmaz" cevabı:** Türkiye'de veri işliyorsan, çalışan/müşterin Türkiye'deyse, KVKK uygulanır. Wyoming LLC bahanesi BKM Kitap için geçersiz.
6. **Saklama süresine yasal dayanak yazmama:** Her süre bir maddeye veya politikaya dayanır; gerekçesiz süre denetimde sorgulanır.
7. **Özel nitelikli veriyi normal veri gibi işleme:** Şifreleme, sınırlı erişim, ayrı loglama zorunlu.

---

**Skill versiyonu:** v1.0 (Mayıs 2026)
**Son güncelleme dayanakları:**
- KVKK Envanter Hazırlama Rehberi Mart 2025 (KVKK Yayın No: 61)
- Kurul Kararı 2025/1572 (04.09.2025) VERBİS muafiyet
- KVKK m.9 yeni rejim (Eylül 2024 değişikliği)
- KVKK m.6 yeni rejim (Mart 2024 değişikliği)
