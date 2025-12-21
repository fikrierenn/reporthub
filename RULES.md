# Kodlama Standartlari

Bu dokuman, ReportHub reposu icin kodlama kurallarini tanimlar.
Butun degisiklikler bu kurallara uygun yapilmalidir.

## Genel
- Acik ve anlasilir kod yazin; gereksiz karmasikligin onune gecin.
- Fonksiyonlar tek bir isi yapsin; buyuyen yapilari bolun.
- Onay alinmadan yeni bagimlilik eklemeyin.
- Degisiklikleri gorev kapsaminda ve kucuk tutun.

## C# ve ASP.NET Core
- Adlandirmada C# standartlarini izleyin: tip/metotlar PascalCase, degiskenler camelCase.
- Tipi acik olan durumlarda `var` kullanin.
- Gercekten async olan metotlarda async kullanin; gerekiyorsa `Async` soneki ekleyin.
- Veri kaydinda yerel zaman gerekmiyorsa `DateTime.UtcNow` kullanin.
- Girdileri erken dogrulayin, acik hata mesajlari verin.
- Karmaşık veri akisi icin `ViewBag` yerine ViewModel/DTO tercih edin.
- Controller'lari ince tutun; buyuyen mantigi servislere alin.

## Veritabani
- Tum sema degisiklikleri `ReportPanel/Database` altinda SQL script ile gelmelidir.
- Sifreleri duz metin saklamayin; yalnizca PBKDF2 hash kullanin.
- Mümkünse seed data icinde gercek baglanti bilgisi kullanmayin.
- SQL her zaman parametreli olsun; string birlestirmeden kacinin.

## Razor View
- Tum sayfalar ortak layout kullansin (header/footer/nav tutarliligi).
- Yerel olmayan JavaScript icin sayfa ici script yazmayin.
- Formlarda `@Html.AntiForgeryToken()` kullanin.
- Anlasilir etiketler ve erisilebilir HTML yazin.

## CSS / UI
- Once mevcut Tailwind siniflarini ve `assets/css/style.css` kullanimini tercih edin.
- Gerekmedikce yeni global CSS eklemeyin.
- Sayfalar arasi tipografi ve bosluklarda tutarlilik koruyun.

## Hata Yonetimi
- Sunucu tarafinda anlamli hata kaydi tutun; kullaniciya temiz mesaj gosterin.
- Hatalari sessizce yutmayin.

## Test
- Onemli mantik degisikliklerinde test ekleyin/guncelleyin.
- Test yazilmadiysa nedeni not edin.

