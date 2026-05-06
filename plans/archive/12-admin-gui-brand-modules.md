# Plan 12 — Admin GUI + Brand + Module Parametric

**Tarih:** 2026-05-06  
**Yazan:** Fikri / Claude  
**Durum:** `Taslak`

---

## 1. Problem

Plan 11 ile Mosaik rebrand tamamlandı ama 2 önemli Faz ertelendi:

- **Faz 3 (BrandSettings):** Marka adı, logo, renkler şu an hardcode — Layout.cshtml'de statik "Mosaik" yazıyor, logo yok. Farklı kurumlara deploy için DB-driven olmalı.
- **Faz 4 (Modules):** Sidebar'da hangi modüller görünür (Raporlar, Panolar, vb.) hardcode — DB-driven toggle olmalı.
- **V2 save bug:** `EditReportV2` view'ının formu POST için `Admin/EditReportV2/{id}` route'una atıyor ama bu route sadece GET — 404 → "bulunmadı" hatası. `CreateReportV2` için benzer risk var.

---

## 2. Scope

### Kapsam dahili

- **Faz 1 — V2 save bug fix:** EditReportV2 + CreateReportV2 view'larındaki form action'ları doğru POST route'a (`Admin/EditReport/{id}` / `Admin/CreateReport`) çevrilir.
- **Faz 2 — BrandSettings:** DB tablosu + EF entity + `IBrandService` + `BrandSettingsController` (veya AdminController partial) + `/Admin/BrandSettings` admin sayfası (marka adı, slogan, logo upload, primary color) + `_AppLayout.cshtml`'de config-driven render.
- **Faz 3 — Modules:** DB tablosu + EF entity + `IModuleService` + `/Admin/Modules` admin sayfası (toggle list) + sidebar koşullu render.
- Migration scriptleri: `28_BrandSettings.sql`, `29_Modules.sql`.
- Audit log: BrandSettings + Module değişiklikleri loglanır.

### Kapsam dışı

- Logo dosyası `wwwroot/` dışına upload (Plan 14 install script konusu).
- SSO veya harici IdP brand entegrasyonu.
- Per-user veya per-role module erişimi (Plan 13 vNext kapsamı).
- ReportCatalog AllowedRoles CSV deprecate (TODO M-03 ayrı).
- Admin liste arama/filtre/son giriş (TODO FAZ 2 madde 23).

### Etkilenen dosyalar (tahmin)

- `Mosaik/Database/28_BrandSettings.sql` — yeni
- `Mosaik/Database/29_Modules.sql` — yeni
- `Mosaik/Models/BrandSettings.cs` — yeni
- `Mosaik/Models/AppModule.cs` — yeni
- `Mosaik/Models/MosaikContext.cs` — DbSet ekle
- `Mosaik/Services/BrandService.cs` + `IBrandService.cs` — yeni
- `Mosaik/Services/ModuleService.cs` + `IModuleService.cs` — yeni
- `Mosaik/Controllers/AdminController.Brand.cs` — yeni partial
- `Mosaik/Controllers/AdminController.Modules.cs` — yeni partial
- `Mosaik/Views/Admin/BrandSettings.cshtml` — yeni
- `Mosaik/Views/Admin/Modules.cshtml` — yeni
- `Mosaik/Views/Shared/_AppLayout.cshtml` — brand config-driven
- `Mosaik/Views/Admin/EditReportV2.cshtml` — form action fix
- `Mosaik/Views/Admin/CreateReportV2.cshtml` — form action fix (varsa)
- `Mosaik/Program.cs` — DI registration

**Tahmini boyut:** ~15 dosya / ~400 satır.

---

## 3. Alternatifler

### A: appsettings.json'da brand config
**Açıklama:** BrandSettings'i DB yerine `appsettings.json`'a koy, `IOptions<BrandSettings>` ile oku.  
**Reddetme sebebi:** Canlı ortamda değişiklik için deploy gerekir. Admin GUI'den anlık değişim mümkün olmaz. Çok-tenant senaryosunda her tenant için ayrı deploy gerekir.

### B: Sadece brand sayfası, module sistemi sonraya
**Açıklama:** Plan 12'yi ikiye böl — brand bu sprint, modules sonraki.  
**Reddetme sebebi:** Module ve brand altyapısı birbirine benzer (migration + service + admin page pattern). Aynı pattern'i iki kez kurmak yerine bir seferde yapmak daha verimli. Scope manageable.

### C: DB-driven BrandSettings + Modules (seçilen)
**Açıklama:** Her ikisi de DB tablosu + service + admin page. Singleton cache ile DB'ye her request'te gitme.  
**Sebep:** Canlı değişim, çok-tenant hazırlık, ve her ikisi de aynı pattern — birlikte tamamlanması mantıklı.

---

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| BrandService null dönerse layout crash | yüksek | düşük | Fallback default değerler (name="Mosaik", logo=null) |
| Logo upload path traversal | yüksek | orta | Sadece `wwwroot/assets/brand/` dizinine, `Path.GetFileName` + extension whitelist |
| Module toggle yanlış sidebar'ı saklarsa | orta | düşük | Default: tüm mevcut modüller aktif; boş tablo = hepsi açık |
| V2 form action fix — diğer alanlar bozulur | orta | düşük | Sadece form action tag helper'ı değiştir, model binding dokunma |
| Migration 28-29 Mosaik DB'de çalışmazsa | orta | düşük | 00_DevReset çalıştırınca migration chain hepsi tekrar çalışır |

---

## 5. Done Criteria

- [ ] `EditReportV2` kaydedince "bulunmadı" hatası alınmıyor, başarılı redirect çalışıyor
- [ ] `/Admin/BrandSettings` sayfası açılıyor, marka adı + slogan kaydediliyor
- [ ] Logo upload çalışıyor, `wwwroot/assets/brand/` dizinine kaydediliyor
- [ ] `_AppLayout.cshtml` marka adını DB'den okuyor (hardcode "Mosaik" yok)
- [ ] `/Admin/Modules` sayfası açılıyor, modül toggle çalışıyor
- [ ] Sidebar devre dışı bırakılan modülü göstermiyor
- [ ] BrandSettings + Module değişiklikleri audit log'a yazılıyor
- [ ] `dotnet build` temiz, 0 warning
- [ ] `dotnet test` yeşil (mevcut testler kırmıyor)
- [ ] Smoke: brand değiştir → layout anında yansıyor (cache invalidation)

---

## 6. Rollback Planı

- **V2 bug fix:** `git revert` — view değişikliği, sıfır DB etkisi.
- **Migration 28-29:** Down script `DROP TABLE BrandSettings; DROP TABLE AppModules;` — servis fallback default'a döner, layout statik "Mosaik"e döner.
- **Layout değişikliği:** `git revert` + migration rollback yeterli, başka etki yok.

---

## 7. Adımlar

1. [ ] **Faz 1** — V2 save bug: `EditReportV2.cshtml` form action `asp-route-id` + `asp-controller/action` fix; `CreateReportV2.cshtml` kontrol + fix. Smoke: kaydet → redirect çalışıyor.

2. [ ] **Faz 2a** — Migration `28_BrandSettings.sql`: tablo oluştur (Id, SiteTitle, Slogan, LogoPath, PrimaryColor, UpdatedAt). `BrandSettings.cs` model + `MosaikContext` DbSet.

3. [ ] **Faz 2b** — `IBrandService` + `BrandSettingsService` (singleton cache, 5dk TTL). `Program.cs`'e DI. `_AppLayout.cshtml` brand değerlerini servisten okusun.

4. [ ] **Faz 2c** — `AdminController.Brand.cs` partial: GET `/Admin/BrandSettings` + POST (save + logo upload). `Views/Admin/BrandSettings.cshtml` form (text inputs + file input + color picker). Audit log.

5. [ ] **Faz 3a** — Migration `29_Modules.sql`: tablo oluştur (Id, ModuleKey, DisplayName, IsEnabled, SortOrder). Seed: Reports + Dashboards aktif. `AppModule.cs` model + DbSet.

6. [ ] **Faz 3b** — `IModuleService` + `ModuleService` (cache). `_AppLayout.cshtml` sidebar koşullu render. `Program.cs` DI.

7. [ ] **Faz 3c** — `AdminController.Modules.cs` partial + `Views/Admin/Modules.cshtml` toggle list. Audit log.

8. [ ] **Faz 4** — Build + test + smoke. Commit'ler.

---

## 8. İlişkili

- Önceki plan: `plans/archive/11-rebrand-reset-mosaik.md` — Faz 3+4 buradan taşındı
- TODO: satır 123-128 (Plan 12 maddesi)
- Journal: `docs/journal/2026-05-06.md`
- Partial commit'ler zaten var: `eea3aa7` (V1 fallback link) + `04992d2` (Run route fix)

---

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Geri bildirim alındı
- [ ] Onay alındı: —
