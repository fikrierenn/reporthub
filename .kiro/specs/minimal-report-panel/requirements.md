# Gereksinimler Dokümanı

## Giriş

Personelin SQL şifresi gerektirmeden ve SSMS kurulumu yapmadan SQL Server raporlarını çalıştırabilmesini sağlayan minimal web tabanlı rapor panel sistemi. Sistem, stored procedure'lar aracılığıyla güvenli rapor çalıştırma, parametre doğrulama, Excel export yetenekleri ve çoklu sunucu desteği sağlarken sıkı güvenlik kontrollerini korur.

## Sözlük

- **Report_Panel**: Raporları çalıştırmak için web tabanlı uygulama sistemi
- **User**: Web arayüzü üzerinden rapor çalıştıran personel
- **Admin**: Veri kaynaklarını ve rapor konfigürasyonlarını yöneten IT personeli
- **DataSource**: İlişkili metadata ile yapılandırılmış SQL Server bağlantısı
- **Report_Catalog**: Mevcut raporların ve konfigürasyonlarının merkezi kayıt sistemi
- **Stored_Procedure**: Rapor çalıştırma için kullanılan "rpt_" önekli SQL Server stored procedure'ları
- **ParamSchema**: Rapor parametrelerini, doğrulama kurallarını ve form oluşturmayı tanımlayan JSON konfigürasyonu
- **RunLog**: Tüm rapor çalıştırmalarının metadata ve sonuçlarıyla denetim kaydı
- **Service_Account**: Uygulama tarafından tüm veritabanı bağlantıları için kullanılan özel veritabanı hesabı

## Gereksinimler

### Gereksinim 1

**Kullanıcı Hikayesi:** Bir personel üyesi olarak, güvenliği korurken ihtiyacım olan verileri alabilmek için SQL erişimi olmadan raporları çalıştırmak istiyorum.

#### Kabul Kriterleri

1. WHEN bir kullanıcı sisteme eriştiğinde THEN Report_Panel SQL Server kimlik bilgileri gerektirmeden onları doğrulayacak
2. WHEN bir kullanıcı rapor seçtiğinde THEN Report_Panel sadece rollerine göre erişim yetkisi olan raporları gösterecek
3. WHEN bir kullanıcı rapor çalıştırdığında THEN Report_Panel veritabanına sadece Service_Account kimlik bilgileri ile bağlanacak
4. WHEN bir kullanıcı SQL'e doğrudan erişmeye çalıştığında THEN Report_Panel herhangi bir doğrudan veritabanı erişimini engelleyecek
5. WHEN bir kullanıcı rapor sonuçlarını görüntülediğinde THEN Report_Panel altta yatan SQL sorgularını açığa çıkarmadan verileri gösterecek

### Gereksinim 2

**Kullanıcı Hikayesi:** Bir kullanıcı olarak, SQL yazmadan rapor çıktısını özelleştirebilmek için rapor parametrelerini web formu aracılığıyla sağlamak istiyorum.

#### Kabul Kriterleri

1. WHEN bir kullanıcı rapor seçtiğinde THEN Report_Panel ParamSchema konfigürasyonuna dayalı bir form oluşturacak
2. WHEN bir kullanıcı parametreleri gönderdiğinde THEN Report_Panel tüm gerekli alanların sağlandığını doğrulayacak
3. WHEN bir kullanıcı parametreleri gönderdiğinde THEN Report_Panel parametre değerlerini doğru veri tiplerine dönüştürecek
4. WHEN parametre doğrulaması başarısız olduğunda THEN Report_Panel açık hata mesajları gösterecek ve çalıştırmayı engelleyecek
5. WHEN parametreler geçerli olduğunda THEN Report_Panel ilişkili Stored_Procedure'ı parametre bağlama ile çalıştıracak

### Gereksinim 3

**Kullanıcı Hikayesi:** Bir kullanıcı olarak, verileri çevrimdışı analiz edebilmek ve paylaşabilmek için rapor sonuçlarını Excel formatına aktarmak istiyorum.

#### Kabul Kriterleri

1. WHEN bir rapor çalıştırması tamamlandığında THEN Report_Panel bir Excel indirme seçeneği sunacak
2. WHEN sonuç seti 200.000 satır veya daha az içerdiğinde THEN Report_Panel XLSX format dosyaları oluşturacak
3. WHEN sonuç seti 200.000 satırı aştığında THEN Report_Panel yedek olarak CSV format dosyaları oluşturacak
4. WHEN export oluşturulurken THEN Report_Panel rapor adı ve zaman damgası ile uygun dosya adlandırması yapacak
5. WHEN export talep edildiğinde THEN Report_Panel geçici dosyalar saklamadan indirmeyi akış halinde yapacak

### Gereksinim 4

**Kullanıcı Hikayesi:** Bir yönetici olarak, raporların farklı veritabanlarına ve sunuculara erişebilmesi için birden fazla SQL Server bağlantısını yönetmek istiyorum.

#### Kabul Kriterleri

1. WHEN veri kaynaklarını yapılandırırken THEN Report_Panel birden fazla SQL Server örneği için bağlantı dizelerini saklayacak
2. WHEN bir rapor tanımlandığında THEN Report_Panel onu tam olarak bir DataSource ile ilişkilendirecek
3. WHEN bir rapor çalıştırılırken THEN Report_Panel raporun DataSource konfigürasyonuna göre doğru sunucuya bağlanacak
4. WHEN bir DataSource aktif değilse THEN Report_Panel ilişkili raporların çalıştırılmasını engelleyecek
5. WHEN bağlantıları yönetirken THEN Report_Panel aktivasyondan önce DataSource bağlantısını doğrulayacak

### Gereksinim 5

**Kullanıcı Hikayesi:** Bir sistem yöneticisi olarak, sistem kullanımını denetleyebilmek ve sorunları giderebilmek için tüm rapor çalıştırmalarının loglanmasını istiyorum.

#### Kabul Kriterleri

1. WHEN bir rapor çalıştırıldığında THEN Report_Panel çalıştırma metadata'sı ile bir RunLog kaydı oluşturacak
2. WHEN çalıştırmayı loglarken THEN Report_Panel kullanıcı adı, rapor ID'si, parametreler, zaman damgası ve süreyi kaydedecek
3. WHEN çalıştırma tamamlandığında THEN Report_Panel satır sayısını ve başarı durumunu loglayacak
4. WHEN çalıştırma başarısız olduğunda THEN Report_Panel hata mesajlarını ve başarısızlık detaylarını loglayacak
5. WHEN export işlevselliğine erişirken THEN Report_Panel RunLog kayıtlarını doğrulayacak ve süre sınırlarını uygulayacak

### Gereksinim 6

**Kullanıcı Hikayesi:** Bir veritabanı yöneticisi olarak, veri erişimini kontrol edebilmek ve SQL injection'ı önleyebilmek için raporların sadece stored procedure kullanmasını istiyorum.

#### Kabul Kriterleri

1. WHEN raporları tanımlarken THEN Report_Panel tüm raporların "rpt_" önekli Stored_Procedure'lara referans vermesini gerektirecek
2. WHEN raporları çalıştırırken THEN Report_Panel SQL injection'ı önlemek için parametre bağlama kullanacak
3. WHEN stored procedure'ları çağırırken THEN Report_Panel sadece doğrulanmış ve tip dönüştürülmüş parametreleri geçecek
4. WHEN stored procedure'lar çalıştığında THEN Report_Panel sadece okuma işlemlerini (SELECT only) zorunlu kılacak
5. WHEN parametre şemaları tanımlandığında THEN Report_Panel date, text, number, select ve checkbox parametre tiplerini destekleyecek

### Gereksinim 7

**Kullanıcı Hikayesi:** Bir kullanıcı olarak, indirmeden önce verileri doğrulayabilmek için rapor sonuçlarını export öncesi önizlemek istiyorum.

#### Kabul Kriterleri

1. WHEN bir rapor başarıyla çalıştığında THEN Report_Panel 500 satırla sınırlı sonuçların önizlemesini gösterecek
2. WHEN önizleme gösterilirken THEN Report_Panel verileri okunabilir tablo formatında gösterecek
3. WHEN önizleme gösterildiğinde THEN Report_Panel tam sonuç seti için export seçenekleri sunacak
4. WHEN sorgu çalıştırması zaman aşımını aştığında THEN Report_Panel 60 saniye sonra çalıştırmayı sonlandıracak
5. WHEN sonuçlar gösterildiğinde THEN Report_Panel toplam satır sayısı bilgisini gösterecek

### Gereksinim 8

**Kullanıcı Hikayesi:** Bir yönetici olarak, yeni raporları dinamik olarak ekleyebilmek için uygulama güncellemeleri olmadan rapor kataloğunu yönetmek istiyorum.

#### Kabul Kriterleri

1. WHEN yeni raporlar eklenirken THEN Report_Panel rapor tanımlarını Report_Catalog veritabanı tablosunda saklayacak
2. WHEN raporları yapılandırırken THEN Report_Panel başlık, açıklama, stored procedure ve parametre şeması belirtilmesine izin verecek
3. WHEN izinleri ayarlarken THEN Report_Panel AllowedRoles konfigürasyonu aracılığıyla rol tabanlı erişim kontrolünü destekleyecek
4. WHEN raporlar aktif değilse THEN Report_Panel onları kullanıcı arayüzlerinden gizleyecek
5. WHEN parametre şemaları select alanları içerdiğinde THEN Report_Panel seçenekleri doldurmak için lookup procedure'larını çalıştıracak