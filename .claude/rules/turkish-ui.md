# Türkçe UI Kuralları

_Kapsam: UI metinleri, Türkçe karakter kullanımı._

## Dil Ayrımı

| Alan | Dil |
|---|---|
| C# kod (class, method, variable) | İngilizce |
| SQL table/column adları | İngilizce |
| UI metni (cshtml, label, button, toast) | **Türkçe** (UTF-8) |
| Razor view comment'leri | Türkçe olabilir |
| TODO.md, CLAUDE.md, docs/ | Türkçe |
| Git commit message | Türkçe veya İngilizce (tutarlı kal — konvansiyon: Türkçe) |
| SP parametreleri | İngilizce tercih et (`@startDate` değil `@Tarih`) |

## Türkçe Karakter Kuralı

- **UTF-8** kullan. `_AppLayout.cshtml` destekliyor.
- ASCII'ye sadeleştirme **yasak**: "Düzenle" ✓, "Duzenle" ✗.
- Mevcut sadeleştirilmiş metinler (cshtml/js'de `Duzenle`, `Bilesen`, `Islem`) yavaş yavaş UTF-8'e çevriliyor. TODO F-05.
- İşaretler: `ı`, `İ`, `ş`, `Ş`, `ğ`, `Ğ`, `ü`, `Ü`, `ö`, `Ö`, `ç`, `Ç`.
- Özellikle dikkat: **İ** (büyük ı) vs **I** (büyük i) — "İptal", "İşlem", "Kaydet Edildi" **değil** "Edildi".

## `<html lang="tr">`

`_AppLayout.cshtml` içinde `<html lang="tr">` olmalı. Ekran okuyucular ve Google Translate için kritik.

## Yaygın Çeviriler (sözlük)

| İngilizce | Türkçe |
|---|---|
| Edit | Düzenle |
| Delete | Sil |
| Save | Kaydet |
| Cancel | İptal |
| Create / Add | Ekle / Oluştur |
| Update | Güncelle |
| Login / Logout | Giriş Yap / Çıkış Yap |
| User | Kullanıcı |
| Role | Rol |
| Report | Rapor |
| Dashboard | Pano / Dashboard |
| Stored Procedure | Stored Procedure (kalıyor, kod terimi) |
| Component | Bileşen |
| Data Source | Veri Kaynağı |
| Category | Kategori |
| Favorite | Favori |
| Filter | Filtre |
| Preview | Önizleme |
| Operation / Action | İşlem |

## Hata Mesajları

- Kullanıcıya dostça: "Beklenmedik bir hata oluştu. Lütfen sistem yöneticisine bildirin."
- Teknik detay **logger'a**, kullanıcıya asla.
- Türkçe ve net: "Kullanıcı adı zaten mevcut." ✓, "User already exists." ✗.

## Tek Kural — Kod %100 EN, UI %100 TR (ZORUNLU)

Karar 2026-05-08 (kullanıcı): "arayüz komple türkçe teknik kodlama tarafı komple ingilizce, class domain isimleri de dahil". Hibrit pattern kaldırıldı — Core entity (User/Role/ReportCatalog) standardı **tüm modüllere** uygulanır.

| Yer | Kural | Doğru | Yanlış |
|---|---|---|---|
| Class adı (entity, service, controller, viewmodel — **domain dahil**) | EN | `Circular`, `DailyBlock`, `BlockFile`, `CircularService` | `Tamim`, `GunlukBlok`, `BlokDosyaService` |
| Method adı | EN + Async suffix | `CreateAsync`, `GetTodaysAsync` | `OlusturAsync`, `GetBugunkuAsync` |
| Controller action | EN | `Index`, `Details`, `UploadFile` | `Liste`, `UploadDosya` |
| Property / field adı (domain dahil) | EN | `Subject`, `Content`, `BlockNumber`, `FileName`, `IsUrgent` | `Konu`, `Aciklama`, `BlokNo`, `Acil` |
| DB tablo adı | EN (PascalCase, çoğul) | `Circulars`, `DailyBlocks`, `BlockFiles` | `Tamim`, `GunlukBlok`, `BlokDosyasi` |
| DB kolon adı | EN | `Subject`, `BlockNumber`, `IsUrgent`, `CreatedAt` | `Konu`, `BlokNo`, `Acil` |
| Lookup `Code` değeri | camelCase EN | `blockType`, `fileType` | `blokTuru`, `dosyaTuru` |
| Audit event type | lowercase_snake_EN | `circular_read`, `block_file_upload` | `tamim_okundu`, `blok_dosya_upload` |
| URL slug / Area / ModuleKey | EN lowercase | `/circulars/blocks/create` | `/tamim/blok/yeni` |
| **UI metni (cshtml label, button, alert, sidebar)** | TR (UTF-8) | `Düzenle`, `Yeni Tamim`, `Bölüm` | `Edit`, `New Circular` |
| **DisplayName (modül adı, sidebar etiketi)** | TR | `"Tamim & Sirküler"` | `"Circular & Bulletin"` |

### Net özet
- Kod = İngilizce. **"Domain Türkçe ise TR yazılır" kuralı YOK** — `Tamim` kavramı `Circular`'a, `Blok` `Block`'a çevrilir.
- DB/SQL = İngilizce. Tablo adları çoğul (Core pattern: `Users`, `Roles`).
- UI metni = Türkçe (label, buton, breadcrumb metni, alert mesajı, hero başlık).
- İstisna: ModuleKey ve Area gibi route slug'ları EN lowercase, ama UI'da gösterilen `DisplayName` TR.

## Otomasyon

- `turkish-ui-normalizer` skill'i (planlı) — ASCII'leştirilmiş metinleri UTF-8'e çevirir.
- Şimdilik elle düzeltme veya IDE find/replace.
