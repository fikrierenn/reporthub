# ReportHub

ReportHub, rol tabanli erisim kontrolu, merkezi audit log ve admin paneli uzerinden rapor/kurulum yonetimi saglayan ASP.NET Core MVC tabanli bir rapor portalidir. Hedef; kurum ici rapor ihtiyacini hizli, izlenebilir ve guvenli sekilde karsilamaktir.

## Neler sunar?
- Rol tabanli yetkilendirme (admin, ik, mali, user) ve rapor bazli izinler.
- Parametreli stored procedure calistirma ve Excel export.
- Merkezi audit log: kullanici aksiyonlari ve sistem olaylari kayit altinda.
- Admin paneli: veri kaynaklari, rapor katalugu, kullanici/rol yonetimi.
- Ortak layout, responsive arayuz ve server-side filtreleme.

## Teknoloji yiginı
- .NET 8 SDK
- ASP.NET Core MVC
- EF Core
- SQL Server
- Razor Views

## Proje yapisi
- `ReportPanel/` - Ana uygulama.
- `ReportPanel/Controllers` - Auth, admin, reports, profile, logs, dashboard.
- `ReportPanel/Views` - Razor arayuzleri.
- `ReportPanel/Models` - EF Core entity ve DbContext.
- `ReportPanel/Services` - Audit log ve sifreleme servisleri.
- `ReportPanel/Database` - Schema ve seed scriptleri.
- `ReportPanel.Tests/` - Otomatik testler.

## Kurulum (lokal)
1) `ReportPanel/appsettings.json` icindeki connection string'i kendi ortamina gore guncelle.
2) `ReportPanel/Database` altindaki scriptlerle veritabanini olustur.
3) Uygulamayi calistir:

```
cd ReportPanel
dotnet run
```

## Ortamlar ve connection string mantigi
Bu projede ortamlar ayridir. Her ortam kendi connection string'ini kullanir:
- `appsettings.json` -> varsayilan / genel ayarlar
- `appsettings.Development.json` -> sadece lokal gelistirme (git ignore)
- `appsettings.Staging.json` -> staging ortam ayarlari
- `docker-compose.staging.yml` -> docker staging calistirmasi icin env override

Amac: Herkesin ayni veritabanini kullanmasi degil, ortam bazli ayrim yapmaktir.

## Admin ve roller
- `Admin` alani: rapor, kullanici, veri kaynagi CRUD islemleri.
- Rapor gorunurlugu: rapor bazinda role listesi (csv).
- Loglar sadece admin rolune gorunur.

## Audit log
Audit log tek tabloda tutulur ve su aksiyonlari kapsar:
- Login/logout
- Profil guncelleme ve sifre degisikligi
- Kullanici/rapor/veri kaynagi CRUD
- Rapor calistirma ve export
- Veri kaynagi testleri

## Testler
```
cd ..
dotnet test reporthub.sln
```

## Notlar
- Gercek sifreleri ya da production connection string'lerini commit etmeyin.
- Gizli bilgiler `appsettings.Development.json` icinde tutulur ve git'e eklenmez.
- Build ciktilari `.gitignore` ile dislanir.
