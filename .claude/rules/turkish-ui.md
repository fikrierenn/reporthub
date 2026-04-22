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

## Otomasyon

- `turkish-ui-normalizer` skill'i (planlı) — ASCII'leştirilmiş metinleri UTF-8'e çevirir.
- Şimdilik elle düzeltme veya IDE find/replace.
