# ReportHub - Windows Kurulum (IIS disi)

Bu dokuman, ReportHub'i Windows sunucuda IIS kullanmadan Kestrel + Windows Service ile ayaga kaldirmak icin hazirlanmistir. SQL veritabani merkezi bir sunucudadir.

Bu rehberde her adimin ne yaptigi aciklanmistir.

## 1) On kosullar (neden gerekli?)
- Windows Server: Uygulama burada calisacak.
- .NET 10 Hosting Bundle (preview): .NET runtime + ASP.NET Core runtime icerir; uygulamanin calismasi icin gereklidir.
- .NET 10 SDK (preview): publish almak icin gelistirme makinesinde gerekir.
- NSSM: Uygulamayi Windows Service olarak kaydeder (sunucu yeniden baslasa bile otomatik calisir).

Indirme linkleri:
- .NET 10 Hosting Bundle (preview): https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- .NET 10 SDK (preview): https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- NSSM: https://nssm.cc/download

## 2) Yayina alma (publish) - uygulamayi paketleme
Bu adim, uygulamanin calisabilir dosyalarini tek klasore toplar.

Yerel makinede:
```
cd D:\Dev\reporthub\ReportPanel
dotnet publish -c Release -o C:\Deploy\ReportHub
```

Sunucuda uygulamanin duracagi klasor:
```
C:\Apps\ReportHub
```

`C:\Deploy\ReportHub` icerigini sunucuda `C:\Apps\ReportHub` altina kopyalayin.

## 3) Connection string ayari (SQL ile baglanti)
Uygulamanin merkezi SQL sunucuya baglanmasi icin `appsettings.json` dosyasi ayarlanir.

Sunucuda `C:\Apps\ReportHub\appsettings.json` icinde:
```
"DefaultConnection": "Server=MERKEZ_SQL;Database=PortalHUB;User Id=XXX;Password=YYY;TrustServerCertificate=true;"
```

## 4) Port ayari (uygulama hangi porttan acilacak?)
Uygulamanin dinleyecegi portu belirleyin (ornegin 5197).

### `--urls` ne demek?
`--urls` parametresi, uygulamanin hangi adres/porttan hizmet verecegini belirtir.
Ornek:
- `http://0.0.0.0:5197` -> tum network kartlarindan 5197 portunu dinler.
Bu sayede baska cihazlardan IP uzerinden erisilebilir.

Isterseniz `appsettings.json` icine ekleyin:
```
"Urls": "http://0.0.0.0:5197"
```

Alternatif: servis parametresinde `--urls` kullanilabilir (asagida).

## 5) Windows Service (NSSM ile)
Bu adim, uygulamayi servis yapar; sunucu acilinca otomatik calisir.

NSSM kurulduktan sonra Komut Satiri (Admin) ile:
```
nssm install ReportHub
```

NSSM penceresinde:
- Path: `C:\Program Files\dotnet\dotnet.exe`
- Arguments: `C:\Apps\ReportHub\ReportPanel.dll --urls http://0.0.0.0:5197`
- Startup directory: `C:\Apps\ReportHub`

Servisi baslatmak icin:
```
nssm start ReportHub
```

## 6) Firewall
Disaridan erisim icin portu acmak gerekir.

Portu acmak icin:
```
netsh advfirewall firewall add rule name="ReportHub" dir=in action=allow protocol=TCP localport=5197
```

## 7) Kontrol
Tarayicidan:
```
http://SUNUCU_IP:5197
```

## 8) Ilk kurulum adimlari
1) Admin kullanici ile giris yapin.
2) Admin panelinden veri kaynaklarini tanimlayin.
3) Rapor kataloglarini ekleyin ve rollerini belirleyin.

## 9) Guncelleme / deploy adimlari
1) Yerelde publish alin:
```
cd D:\Dev\reporthub\ReportPanel
dotnet publish -c Release -o C:\Deploy\ReportHub
```
2) `C:\Deploy\ReportHub` icerigini sunucudaki `C:\Apps\ReportHub` klasorune kopyalayin (ustune yazabilirsiniz).
3) Windows Service'i yeniden baslatin:
```
nssm restart ReportHub
```
4) Yeni bir veritabani degisikligi varsa ilgili SQL scriptini calistirin.
   - Favoriler tablosu icin: `ReportPanel/Database/06_CreateReportFavorites.sql`

## 10) IP ile erisim ornegi
Eger domain/DNS tanimlamadan dogrudan IP ile baglanmak istersen:
```
http://192.168.40.201:5197
```
Bu calisabilmesi icin uygulama `--urls http://0.0.0.0:5197` ile dinlemeli ve firewall portu acik olmali.

## Notlar
- Gercek sifreleri dokumanlara koymayin.
- Loglar icin bir klasor belirlemek isterseniz `C:\Apps\ReportHub\logs` kullanabilirsiniz.
