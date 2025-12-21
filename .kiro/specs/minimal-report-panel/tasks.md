# Implementasyon Planı

- [x] 1. Proje yapısını ve temel altyapıyı kurma



  - /rp klasör yapısını oluştur
  - Composer ile PhpSpreadsheet bağımlılığını ekle
  - Temel PHP konfigürasyonu (error reporting, session settings)
  - _Requirements: 1.1, 2.1, 3.1_


- [x] 2. Veritabanı şemasını oluşturma


  - DataSources tablosunu oluştur
  - ReportCatalog tablosunu oluştur  
  - ReportRunLog tablosunu oluştur
  - Foreign key kısıtlamalarını ekle
  - _Requirements: 4.1, 5.1, 8.1_



- [ ] 3. boot.php helper fonksiyonlarını implement etme
- [ ] 3.1 Kimlik doğrulama helper'larını yaz
  - require_login(), user(), roles() fonksiyonları
  - has_role(), roles_intersect() fonksiyonları
  - _Requirements: 1.1, 1.2_

- [ ]* 3.2 Kimlik doğrulama için property test yaz
  - **Property 1: Güvenli Kimlik Doğrulama**
  - **Validates: Requirements 1.1**


- [ ]* 3.3 Rol tabanlı erişim için property test yaz
  - **Property 2: Rol Tabanlı Erişim Kontrolü**
  - **Validates: Requirements 1.2**

- [ ] 3.4 Veritabanı bağlantı helper'larını yaz
  - db_conn(), get_datasource(), get_report() fonksiyonları
  - list_reports_for_user() fonksiyonu
  - _Requirements: 4.1, 4.2, 4.3_

- [ ]* 3.5 Veritabanı bağlantıları için property test yaz
  - **Property 3: Servis Hesabı Bağlantısı**
  - **Validates: Requirements 1.3**


- [ ]* 3.6 Çoklu DataSource desteği için property test yaz
  - **Property 14: Çoklu DataSource Desteği**
  - **Validates: Requirements 4.1**

- [x] 3.7 Parametre işleme helper'larını yaz

  - parse_schema(), validate_cast_params() fonksiyonları
  - _Requirements: 2.1, 2.2, 2.3_

- [ ]* 3.8 Parametre doğrulama için property testler yaz
  - **Property 6: Parametre Doğrulama**
  - **Property 7: Tip Dönüştürme**
  - **Validates: Requirements 2.2, 2.3**


- [ ] 3.9 Rapor çalıştırma helper'larını yaz
  - call_sp(), log_run(), guid() fonksiyonları
  - _Requirements: 2.5, 5.1, 6.2_

- [ ]* 3.10 SQL injection koruması için property test yaz
  - **Property 9: Parametre Bağlama Güvenliği**
  - **Validates: Requirements 2.5**




- [ ] 4. login.php kimlik doğrulama sayfasını implement etme
- [ ] 4.1 Login formu ve session yönetimini yaz
  - Basit kullanıcı/şifre kontrolü (hardcoded MVP için)
  - Session başlatma ($_SESSION['user'], $_SESSION['roles'])
  - Logout işlevselliği
  - _Requirements: 1.1_

- [ ]* 4.2 Login güvenliği için unit testler yaz
  - Geçerli/geçersiz kimlik bilgileri testleri

  - Session oluşturma testleri
  - _Requirements: 1.1_

- [ ] 5. reports.php rapor listesi ve form sayfasını implement etme
- [ ] 5.1 Rapor listesi görüntüleme işlevini yaz
  - Kullanıcı rollerine göre filtrelenmiş rapor listesi
  - Aktif raporları gösterme
  - DataSource aktiflik kontrolü
  - _Requirements: 1.2, 8.4_



- [ ]* 5.2 Rapor görünürlüğü için property test yaz
  - **Property 36: Aktif Rapor Görünürlüğü**
  - **Validates: Requirements 8.4**

- [ ] 5.3 Dinamik parametre formu oluşturma işlevini yaz
  - ParamSchema JSON parsing
  - Form elemanları oluşturma (date, text, number, select, checkbox)
  - Select alanları için optionsProc çalıştırma
  - _Requirements: 2.1, 6.5, 8.5_

- [ ]* 5.4 Form oluşturma için property testler yaz
  - **Property 5: Dinamik Form Oluşturma**
  - **Property 28: Parametre Tipi Desteği**
  - **Property 37: Dinamik Seçenek Yükleme**
  - **Validates: Requirements 2.1, 6.5, 8.5**

- [ ] 6. run.php rapor çalıştırma sayfasını implement etme
- [ ] 6.1 Parametre doğrulama ve işleme sistemini yaz
  - Required alan kontrolü
  - Veri tipi dönüştürme
  - Hata mesajları gösterme
  - _Requirements: 2.2, 2.3, 2.4_

- [ ]* 6.2 Parametre işleme için property testler yaz
  - **Property 8: Hata Mesajları**
  - **Validates: Requirements 2.4**

- [ ] 6.3 Stored procedure çalıştırma sistemini yaz
  - Doğru DataSource bağlantısı seçimi
  - Parametre binding ile SP çağrısı
  - Timeout kontrolü (60 saniye)
  - _Requirements: 4.3, 6.2, 7.4_

- [ ]* 6.4 SP çalıştırma için property testler yaz
  - **Property 16: Doğru Sunucu Bağlantısı**
  - **Property 31: Timeout Kontrolü**
  - **Validates: Requirements 4.3, 7.4**

- [ ] 6.5 Sonuç önizleme sistemini yaz
  - 500 satır sınırı ile önizleme
  - HTML tablo formatında görüntüleme
  - Toplam satır sayısı gösterme
  - _Requirements: 7.1, 7.2, 7.5_

- [ ]* 6.6 Önizleme için property testler yaz
  - **Property 29: Önizleme Sınırı**
  - **Property 30: Tablo Formatı**
  - **Property 32: Satır Sayısı Bilgisi**
  - **Validates: Requirements 7.1, 7.2, 7.5**

- [ ] 6.7 RunLog kayıt sistemini yaz
  - Çalıştırma metadata'sı kaydetme
  - Başarı/hata durumu loglama
  - Performance metrikleri (süre, satır sayısı)
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ]* 6.8 Loglama için property testler yaz
  - **Property 19: Çalıştırma Loglaması**
  - **Property 20: Log Detayları**
  - **Property 21: Başarı Loglaması**
  - **Property 22: Hata Loglaması**
  - **Validates: Requirements 5.1, 5.2, 5.3, 5.4**

- [ ] 7. Checkpoint - Temel rapor çalıştırma testleri
  - Tüm testlerin geçtiğinden emin ol, sorular çıkarsa kullanıcıya sor.

- [ ] 8. export.php Excel/CSV export sayfasını implement etme
- [ ] 8.1 Export yetkilendirme sistemini yaz
  - RunLog kaydı doğrulama
  - Süre sınırı kontrolü (10 dakika)
  - Kullanıcı yetki kontrolü
  - _Requirements: 5.5_

- [ ]* 8.2 Export yetkilendirme için property test yaz
  - **Property 23: Export Yetkilendirme**
  - **Validates: Requirements 5.5**

- [ ] 8.3 XLSX export sistemini yaz
  - PhpSpreadsheet ile Excel dosyası oluşturma
  - 200k satır sınırı kontrolü
  - Dosya adlandırma (rapor adı + timestamp)
  - _Requirements: 3.2, 3.4_

- [ ]* 8.4 XLSX export için property test yaz
  - **Property 10: Format Seçimi - XLSX**
  - **Property 12: Dosya Adlandırma**
  - **Validates: Requirements 3.2, 3.4**

- [ ] 8.5 CSV fallback sistemini yaz
  - 200k+ satır için CSV oluşturma
  - Memory efficient streaming
  - _Requirements: 3.3_

- [ ]* 8.6 CSV fallback için property test yaz
  - **Property 11: Format Seçimi - CSV Fallback**
  - **Validates: Requirements 3.3**

- [ ] 8.7 Stream download sistemini yaz
  - Output buffering kontrolü
  - Geçici dosya kullanmadan indirme
  - Proper HTTP headers
  - _Requirements: 3.5_

- [ ]* 8.8 Stream download için property test yaz
  - **Property 13: Stream Download**
  - **Validates: Requirements 3.5**

- [ ] 9. admin.php yönetim panelini implement etme
- [x] 9.1 DataSource yönetim arayüzünü yaz
  - DataSource listesi, ekleme, düzenleme
  - Aktif/pasif durumu değiştirme
  - Bağlantı testi işlevselliği
  - **🆕 Kullanıcı dostu form arayüzü**
  - **🆕 Sunucu adı manuel girişi (varsayılan: BT-FIKRI\SQLEXPRESS)**
  - **🆕 Veritabanı hızlı butonları (PortalHUB, BKM_GENEL, MainDB, MaliDB)**
  - **🆕 Kimlik doğrulama radio button seçimi (Windows Auth / SQL Auth)**
  - **🆕 Akıllı form davranışı (Windows Auth → şifre alanları gizlenir)**
  - **🆕 Bağlantı string otomatik oluşturma**
  - **🆕 Modern kart tasarımı ve hover efektleri**
  - **🆕 Form validation (client-side)**
  - _Requirements: 4.4, 4.5_

- [ ]* 9.2 DataSource yönetimi için property testler yaz
  - **Property 17: Aktiflik Kontrolü**
  - **Property 18: Bağlantı Doğrulama**
  - **Validates: Requirements 4.4, 4.5**



- [ ] 9.3 ReportCatalog yönetim arayüzünü yaz
  - Rapor ekleme, düzenleme, silme
  - ParamSchema JSON editörü
  - AllowedRoles konfigürasyonu
  - _Requirements: 8.1, 8.2, 8.3_

- [ ]* 9.4 Rapor yönetimi için property testler yaz
  - **Property 33: Veritabanı Tabanlı Rapor Yönetimi**
  - **Property 34: Rapor Konfigürasyonu**
  - **Property 35: Rol Tabanlı Konfigürasyon**
  - **Validates: Requirements 8.1, 8.2, 8.3**

- [ ] 9.5 Stored procedure adı doğrulama sistemini yaz
  - "rpt_" öneki kontrolü
  - Geçersiz procedure adlarını reddetme
  - _Requirements: 6.1_

- [ ]* 9.6 SP adı doğrulama için property test yaz
  - **Property 24: Stored Procedure Öneki**
  - **Validates: Requirements 6.1**

- [ ] 10. Güvenlik ve performans optimizasyonları
- [ ] 10.1 SQL injection koruması testlerini yaz
  - Kötü niyetli SQL girişleri ile test
  - Parametre binding doğruluğu
  - _Requirements: 6.2, 6.3_

- [ ]* 10.2 Güvenlik için property testler yaz
  - **Property 25: SQL Injection Koruması**
  - **Property 26: Parametre Güvenliği**
  - **Validates: Requirements 6.2, 6.3**

- [ ] 10.3 Sadece okuma işlemleri kontrolünü yaz
  - SELECT-only stored procedure kontrolü
  - Write işlemlerini engelleme
  - _Requirements: 6.4_

- [ ]* 10.4 Okuma işlemleri için property test yaz
  - **Property 27: Sadece Okuma İşlemleri**
  - **Validates: Requirements 6.4**

- [ ] 10.5 SQL sorgu gizliliği kontrolünü yaz
  - Çıktıda SQL sorgu metni bulunmaması
  - Debug bilgilerinin gizlenmesi
  - _Requirements: 1.5_

- [ ]* 10.6 Sorgu gizliliği için property test yaz
  - **Property 4: SQL Sorgu Gizliliği**
  - **Validates: Requirements 1.5**

- [ ] 11. ASP.NET Core Specific Implementations
- [ ] 11.1 ReportService implementasyonu
  - Stored procedure çalıştırma servisi
  - Parametre doğrulama ve tip dönüştürme
  - RunLog kaydı oluşturma
  - _Requirements: 2.5, 5.1, 6.2_

- [ ] 11.2 ExportService implementasyonu
  - Excel/CSV export servisi
  - PhpSpreadsheet yerine EPPlus/ClosedXML kullanımı
  - Stream download işlevselliği
  - _Requirements: 3.2, 3.3, 3.5_

- [ ] 11.3 Authentication & Authorization
  - ASP.NET Core Identity entegrasyonu
  - Rol tabanlı erişim kontrolü
  - Session yönetimi
  - _Requirements: 1.1, 1.2_

- [ ] 11.4 ReportsController implementasyonu
  - Rapor listesi görüntüleme
  - Dinamik parametre formu oluşturma
  - Rapor çalıştırma işlevselliği
  - _Requirements: 2.1, 7.1, 7.2_

- [ ] 11.5 Stored Procedure Validation
  - "rpt_" öneki kontrolü
  - Procedure existence validation
  - Parameter schema validation
  - _Requirements: 6.1, 6.4_

- [ ] 12. Final Checkpoint - Tüm testlerin geçmesi
  - Tüm testlerin geçtiğinden emin ol, sorular çıkarsa kullanıcıya sor.