# ReportHub Kullanıcı Kılavuzu

## İçindekiler

1. [Giriş](#giris)
2. [Sisteme Giriş](#sisteme-giris)
3. [Ana Sayfa (Dashboard)](#ana-sayfa-dashboard)
4. [Raporlar Listesi ve Filtreleme](#raporlar-listesi-ve-filtreleme)
5. [Rapor Çalıştırma](#rapor-calistirma)
6. [Excel'e Aktarma](#excele-aktarma)
7. [Profil Yönetimi](#profil-yonetimi)
8. [Sık Sorulan Sorular](#sik-sorulan-sorular)
9. [Sorun Giderme](#sorun-giderme)

---

## Giriş

ReportHub, BKM Kitap çalışanlarının SQL bilgisine ihtiyaç duymadan güvenli bir şekilde raporları çalıştırabilmesini sağlayan web tabanlı bir rapor portalıdır.

### Sistem Gereksinimleri

- **Web Tarayıcı:** Chrome, Firefox, Edge veya Safari (güncel versiyonlar)
- **İnternet Bağlantısı:** Şirket ağına bağlı olmalısınız
- **Ekran Çözünürlüğü:** Minimum 1024x768 (önerilen 1920x1080)

### Kimler Kullanabilir?

ReportHub'ı kullanabilmek için:
- BKM Kitap çalışanı olmanız
- IT departmanı tarafından size bir kullanıcı hesabı tanımlanmış olması
- En az bir rapora erişim yetkinizin bulunması gerekir

---

## Sisteme Giriş

### Adım 1: Giriş Sayfasına Erişim

1. Web tarayıcınızı açın
2. Adres çubuğuna şu adresi yazın: `https://reporthub.bkmkitap.com.tr`
3. Enter tuşuna basın

### Adım 2: Kullanıcı Bilgilerinizi Girin

![Giriş Ekranı](docs/images/login-screen.png)

1. **Kullanıcı Adı** alanına IT departmanı tarafından size verilen kullanıcı adınızı yazın
2. **Şifre** alanına şifrenizi girin
3. **Giriş Yap** butonuna tıklayın

> AD kullanicilari icin kullanici adi alani `DOMAIN\\kullanici` formatinda olmalidir.

> **💡 İpucu:** İlk girişte size verilen geçici şifreyi değiştirmeniz önerilir.

### Adım 3: Başarılı Giriş

Başarılı giriş yaptığınızda otomatik olarak **Dashboard** (Ana Sayfa) ekranına yönlendirileceksiniz.

### Sorun Yaşıyorsanız

- ❌ **"Kullanıcı adı veya şifre hatalı"** hatası alıyorsanız:
  - Kullanıcı adınızı ve şifrenizi kontrol edin
  - Caps Lock tuşunuzun kapalı olduğundan emin olun
  - Şifrenizi unuttuysanız IT departmanı ile iletişime geçin

---

## Ana Sayfa (Dashboard)

Dashboard, sistemdeki faaliyetlerinizi ve size özel bilgileri görüntüleyebileceğiniz ana sayfadır.

### Dashboard Bileşenleri

#### 1. Üst Menü (Navbar)

![Üst Menü](docs/images/navbar.png)

- **Dashboard:** Ana sayfaya dönüş
- **Raporlar:** Rapor listesi ve çalıştırma sayfası
- **Loglar:** Sistem kayıtları (sadece admin kullanıcılar için)
- **Yönetim:** Admin paneli (sadece admin kullanıcılar için)
- **Profil:** Kullanıcı adınıza tıklayarak profil sayfasına gidebilirsiniz
- **Çıkış:** Güvenli çıkış yapar

#### 2. İstatistik Kartları

Dashboard'da aşağıdaki bilgileri görebilirsiniz:

- **Erişilebilir Rapor Sayısı:** Yetkili olduğunuz rapor sayısı
- **Bu Ay Çalıştırılan Rapor:** Bu ay kaç rapor çalıştırdığınız
- **Son Çalıştırma:** En son rapor çalıştırma tarihiniz

#### 3. Son Raporlar

Yetkili olduğunuz son 5 rapor burada listelenir. Rapor üzerine tıklayarak doğrudan çalıştırma sayfasına gidebilirsiniz.

#### 4. Son Aktiviteler

Son 5 rapor çalıştırma kaydınız tarih, saat ve rapor adı ile listelenir.

---

## Raporlar Listesi ve Filtreleme

Raporlar sayfasında raporlarınızı arayabilir, kategoriye göre filtreleyebilir ve favorilerinizi yönetebilirsiniz.

### Arama ve Kategori Filtresi

1. **Arama** kutusuna rapor adı, açıklama veya kategori yazın.
2. **Kategori** açılır listesinden bir kategori seçin.
3. **Filtrele** butonuna basın.
4. Filtreleri kaldırmak için **Temizle** butonunu kullanın.

### Favorilere Ekleme

1. Rapor satırının sağındaki yıldız ikonuna tıklayın.
2. Dolu yıldız = favori, boş yıldız = favori değil.
3. Favoriye ekleme/çıkarma işlemi rapor listesini tekrar düzenler.

---

## Rapor Çalıştırma

### Adım 1: Rapor Seçimi

1. Üst menüden **Raporlar** linkine tıklayın
2. Karşınıza çıkan rapor listesinden çalıştırmak istediğiniz raporu bulun
3. Rapor kartının üzerine tıklayın

![Rapor Listesi](docs/images/report-list.png)

> **📌 Not:** Sadece yetkiniz olan raporları görebilirsiniz.

### Adım 2: Parametreleri Girin

Rapor çalıştırma sayfasında, raporun gerektirdiği parametreleri görürsünüz.

![Parametre Formu](docs/images/parameter-form.png)

#### Parametre Tipleri

1. **Metin Alanı**
   - Örnek: Müşteri Adı, Ürün Kodu
   - Doğrudan metin yazabilirsiniz

2. **Sayı Alanı**
   - Örnek: Yıl, Miktar
   - Sadece rakam girebilirsiniz

3. **Tarih Alanı**
   - Örnek: Başlangıç Tarihi, Bitiş Tarihi
   - Tarih seçici ile tarih seçin
   - İpucu metni "today" yazıyorsa, bugünün tarihi otomatik seçilir

4. **Seçim Kutusu (Dropdown)**
   - Örnek: Departman, Bölge
   - Listeden bir seçenek seçin

5. **Onay Kutusu (Checkbox)**
   - Örnek: Sadece Aktif Kayıtlar
   - İşaretleyerek aktif edebilirsiniz

#### Zorunlu Alanlar

- Kırmızı yıldız (\*) işareti olan alanlar **zorunludur**
- Bu alanları doldurmadan rapor çalıştıramazsınız

#### Yardım Metinleri

- Her parametrenin altında açıklayıcı yardım metni bulunur
- Bu metinler size parametrenin ne anlama geldiğini ve nasıl doldurulacağını söyler

### Adım 3: Raporu Çalıştırın

1. Tüm parametreleri doğru şekilde doldurun
2. **Raporu Çalıştır** butonuna tıklayın
3. Sistem raporunuzu çalıştıracak ve sonuçları gösterecektir

![Rapor Sonuçları](docs/images/report-results.png)

### Adım 4: Sonuçları İnceleyin

Rapor başarıyla çalıştığında:

- Sonuçlar tablo formatında gösterilir
- **Toplam satır sayısı** ve **çalışma süresi** bilgileri görüntülenir
- Tabloda **arama** yapabilirsiniz (Ara kutusuna yazarak)

#### Sonuç Tablosunda Arama

![Arama Özelliği](docs/images/search-feature.png)

1. "Sonuçlarda Ara..." kutusuna aranacak kelimeyi yazın
2. **Ara** butonuna tıklayın
3. Sonuçlar filtrelenir ve eşleşen kayıtlar gösterilir
4. Arama kutusunu temizleyip tekrar aramak için **Temizle** butonuna tıklayın

### Görünüm Modları

Rapor sonuçlarını farklı modlarda görüntüleyebilirsiniz:

1. **Normal Görünüm** (varsayılan)
   - Standart sayfa genişliği
   - Üst menü ve alt bilgi görünür

2. **Geniş Görünüm**
   - Sayfa tam genişlikte
   - Daha fazla sütunu aynı anda görebilirsiniz

3. **Tam Ekran**
   - Menü ve footer gizlenir
   - Maksimum veri görünürlüğü

4. **Tablo Odak**
   - Parametre formu gizlenir
   - Sadece sonuç tablosu görünür
   - Tablo başlıkları sabitlenir (kaydırma yaparken başlıklar görünür kalır)

> **💡 İpucu:** Görünüm modunu değiştirmek için rapor çalıştırma sayfasındaki butonları kullanın.

---

## Excel'e Aktarma

Rapor sonuçlarını Excel dosyası olarak bilgisayarınıza indirebilirsiniz.

### Adım 1: Raporu Çalıştırın

Önce yukarıdaki adımları takip ederek raporunuzu çalıştırın.

### Adım 2: Excel'e Aktar

![Excel Export Butonu](docs/images/export-button.png)

1. Sonuçlar gösterildikten sonra **Excel'e Aktar** butonuna tıklayın
2. Tarayıcınız otomatik olarak dosyayı indirecektir

### Adım 3: Dosyayı Açın

1. İndirilen dosya genellikle `report_XXX_YYYYMMDD_HHMMSS.xlsx` formatında olur
2. Dosyayı Microsoft Excel veya benzeri programda açın

### Excel Dosyasının İçeriği

Excel dosyası iki bölümden oluşur:

#### 1. Rapor Özeti (İlk Tablo)

- **Rapor Adı:** Çalıştırdığınız raporun adı
- **Kullanıcı:** Kullanıcı adınız
- **Tarih:** Raporun çalıştırıldığı tarih ve saat
- **Parametreler:** Girdiğiniz parametre değerleri

#### 2. Rapor Verileri (İkinci Tablo)

- Tüm satırlar ve sütunlar Excel tablosu olarak
- Sıralama, filtreleme ve analiz yapabilirsiniz

> **⚠️ Önemli:** Excel dosyası `.xlsx` formatındadır. Eğer çok büyük veri setleri varsa (100,000+ satır), dosya açılırken uyarı alabilirsiniz.

---

## Profil Yönetimi

### Profil Sayfasına Erişim

1. Sağ üst köşedeki **kullanıcı adınıza** tıklayın
2. Açılır menüden **Profil** seçeneğini seçin

### Profil Bilgilerinizi Görüntüleme

Profil sayfasında şunları görebilirsiniz:

- **Kullanıcı Adı:** Sistem kullanıcı adınız (değiştirilemez)
- **Ad Soyad:** Tam adınız
- **E-posta:** E-posta adresiniz
- **Roller:** Sahip olduğunuz yetkiler (örn: user, ik, mali)
- **Hesap Durumu:** Aktif/Pasif
- **Son Giriş:** En son giriş tarih ve saatiniz
- **Hesap Oluşturma:** Hesabınızın oluşturulma tarihi

### Profil Bilgilerinizi Güncelleme

1. **Ad Soyad** ve **E-posta** alanlarını düzenleyebilirsiniz
2. Değişikliklerinizi yaptıktan sonra **Güncelle** butonuna tıklayın
3. Başarılı bir şekilde güncellendiğine dair mesaj göreceksiniz

### Şifre Değiştirme

![Şifre Değiştirme](docs/images/change-password.png)

1. Profil sayfasında **Şifre Değiştir** bölümüne gidin
2. **Mevcut Şifre:** Şu anki şifrenizi girin
3. **Yeni Şifre:** Yeni şifrenizi girin
4. **Yeni Şifre (Tekrar):** Güvenlik için yeni şifrenizi tekrar girin
5. **Şifreyi Değiştir** butonuna tıklayın

#### Şifre Gereksinimleri

- En az **8 karakter** uzunluğunda olmalı
- Büyük harf, küçük harf, rakam ve özel karakter içermesi önerilir
- Kolay tahmin edilebilir şifreler kullanmayın (örn: 12345678, sifre123)

> **🔒 Güvenlik İpucu:** Şifrenizi düzenli olarak değiştirin ve kimseyle paylaşmayın.

---

## Sık Sorulan Sorular

### Genel Sorular

**S: Hangi raporlara erişebileceğimi nasıl öğrenebilirim?**

C: Dashboard veya Raporlar sayfasında sadece yetkiniz olan raporları görebilirsiniz. Daha fazla rapora erişmek için yöneticiniz veya IT departmanı ile iletişime geçin.

---

**S: Bir raporu kaç kez çalıştırabilirim?**

C: Rapor çalıştırma sayısında bir sınır yoktur. Ancak sistem performansı için gereksiz yere rapor çalıştırmaktan kaçının.

---

**S: Rapor sonuçları ne kadar süre saklanır?**

C: Rapor sonuçları ekranda gösterildikten sonra sunucuda saklanmaz. Her çalıştırmada veritabanından yeniden çekilir. Ancak rapor çalıştırma kayıtlarınız (log) sistemde süresiz olarak tutulur.

---

### Teknik Sorular

**S: Hangi Excel formatlarını destekliyor?**

C: Sistem `.xlsx` formatında dosya üretir. Bu format Excel 2007 ve sonrası tüm versiyonlar tarafından açılabilir.

---

**S: Çok büyük raporları nasıl indirebilirim?**

C: Büyük veri setleri (100,000+ satır) için Excel yerine CSV formatı önerilir. Bu özellik için IT departmanı ile iletişime geçin.

---

**S: Mobil cihazdan rapor çalıştırabilir miyim?**

C: Evet, sistem responsive tasarıma sahiptir ve mobil cihazlardan erişilebilir. Ancak büyük tablolar için masaüstü kullanımı önerilir.

---

**S: Raporları zamanlayabilir miyim?**

C: Şu anda otomatik zamanlama özelliği bulunmamaktadır. Gelecek versiyonlarda eklenecektir.

---

### Hata ve Sorun Giderme

**S: "Bağlantı hatası" mesajı alıyorum**

C: 
1. İnternet bağlantınızı kontrol edin
2. Şirket VPN'ine bağlı olduğunuzdan emin olun
3. Tarayıcınızı yenileyin (F5)
4. Sorun devam ederse IT departmanını arayın

---

**S: Rapor çok yavaş çalışıyor**

C:
1. Tarih aralığını daraltmayı deneyin
2. Gereksiz parametreler girmediğinizden emin olun
3. Sistem yoğunluğu nedeniyle yavaşlama olabilir
4. 2 dakikadan fazla beklemeyi gerektiren raporlar için IT ile iletişime geçin

---

**S: "Yetkisiz erişim" hatası alıyorum**

C:
1. Doğru kullanıcı ile giriş yaptığınızdan emin olun
2. Bu rapora erişim yetkiniz olmayabilir
3. Yetki talebi için yöneticinizle görüşün

---

## Sorun Giderme

### Yaygın Hatalar ve Çözümleri

#### 1. Giriş Yapamıyorum

**Belirtiler:** Kullanıcı adı ve şifre giriyorum ama giriş yapamıyorum

**Çözümler:**
- [ ] Kullanıcı adını ve şifreyi kontrol edin (Caps Lock kapalı mı?)
- [ ] Tarayıcı çerezlerini temizleyin
- [ ] Farklı bir tarayıcı deneyin
- [ ] IT departmanına başvurun

#### 2. Sayfa Yüklenmiyor

**Belirtiler:** Beyaz ekran veya "Sayfa yüklenemedi" hatası

**Çözümler:**
- [ ] İnternet bağlantınızı kontrol edin
- [ ] Sayfayı yenileyin (Ctrl+F5 veya Cmd+Shift+R)
- [ ] Tarayıcı önbelleğini temizleyin
- [ ] Farklı bir tarayıcı deneyin

#### 3. Rapor Sonuçları Görünmüyor

**Belirtiler:** Rapor çalıştırdım ama sonuç göremiyorum

**Çözümler:**
- [ ] Sayfayı aşağı kaydırın (sonuçlar formun altındadır)
- [ ] Parametrelerin doğru girildiğinden emin olun
- [ ] Rapor boş sonuç dönmüş olabilir (filtreleri gevşetin)
- [ ] Hata mesajı varsa okuyun ve düzeltin

#### 4. Excel İndirme Çalışmıyor

**Belirtiler:** Excel'e Aktar butonuna tıklıyorum ama dosya inmiyor

**Çözümler:**
- [ ] Tarayıcınızın indirme engelleyicisini kontrol edin
- [ ] Farklı bir tarayıcı deneyin
- [ ] Dosya indirme klasörünüzü kontrol edin (zaten inmiş olabilir)
- [ ] Popup engelleyicisini kapatın

#### 5. Şifre Değiştirme Hatası

**Belirtiler:** Şifre değiştirirken hata alıyorum

**Çözümler:**
- [ ] Mevcut şifrenizi doğru girdiğinizden emin olun
- [ ] Yeni şifre gereksinimlerini kontrol edin (min. 8 karakter)
- [ ] Her iki "Yeni Şifre" alanına aynı değeri yazdığınızdan emin olun
- [ ] Özel karakterlerden kaçının (Türkçe karakter kullanmayın)

---

## Ek Kaynaklar

### İletişim Bilgileri

**IT Destek Ekibi**
- 📧 E-posta: it@bkmkitap.com.tr
- ☎️ Telefon: +90 (262) XXX XX XX
- 📍 Lokasyon: Gebze Merkez Ofis - IT Departmanı

### İleri Düzey Eğitim

Daha detaylı eğitim almak için:
- IT departmanı tarafından düzenlenen workshop'lara katılabilirsiniz
- Yönetici eğitimleri için Admin Kılavuzuna bakın
- Video eğitimler için şirket içi portal'ı ziyaret edin

---

## Versiyon Geçmişi

| Versiyon | Tarih | Değişiklikler |
|----------|-------|---------------|
| 1.0.0    | 2024-12-21 | İlk kullanıcı kılavuzu yayınlandı |
| 1.1.0    | 2025-12-24 | Raporlar listesinde arama, kategori filtreleme, favoriler ve Excel .xlsx güncellemesi eklendi |
| 1.2.0    | 2025-12-24 | Windows AD (DOMAIN\\kullanici) ile giris notu eklendi |

---

## Geri Bildirim

Bu kılavuz hakkında geri bildiriminiz için: **it@bkmkitap.com.tr**

Son güncelleme: 24 Aralık 2025
