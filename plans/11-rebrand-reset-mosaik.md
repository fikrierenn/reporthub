# Plan 11 — Mosaik Foundation (Rebrand + Brand Parametric + Module Infra + Reset + UTC)

**Tarih:** 2026-05-07
**Yazan:** Fikri / Claude
**Durum:** `Uygulamada`

---

## 1. Problem

Tek pakette beş bağımsız sorun çözülecek — hepsi aynı baseline kuruluyor, ayrı oturumlara bölmek redundant reset/replay üretir:

1. **Proje adı tutarsız + dar:** "ReportPanel" (kod) / "ReportHub" (brand) ikili. vNext "şirket içi portal" vizyonuyla (TODO.md:298) uyuşmuyor — rapor odaklı isim modüler portal'a dar gelir. Yeni isim **Mosaik** seçildi (7 May 2026): modüler brand metaforu, kurum-bağsız (BKM içi + dışarı satılabilir).

2. **DB adı tutarsız:** "PortalHUB" (büyük HUB) proje adıyla hizalanmıyor.

3. **Brand görünümü hardcode:** UI'da "BKM Rapor Paneli", logo, renkler kod-içi. Tenant-spesifik değil — başka müşteriye satıldığında elle source code edit gerekir. Hedef: **DB-driven brand parametric** (tek tenant white-label).

4. **Modülerlik kavramı yok:** vNext modüller (tamim, doküman, takvim) eklendiğinde her müşteri **istediği modülleri aktif** edebilmeli. Hedef: **DB-driven module switchable**.

5. **DateTime Faz E** (ADR-006): 22 Nisan öncesi naive-local satırlar. Dev + sıfırlanabilir data → reset baseline çözer (Plan 10'un karmaşık veri shift gerek bırakmaz, Plan 10 supersed olur).

## 2. Scope

### Üç Katman (Mosaik Foundation)

| Katman | Ne | Nereye gider | Hardcode? |
|---|---|---|---|
| **L1 Canonical** | `Mosaik.*` namespace, `Mosaik.csproj`, `Mosaik.sln` | Kod | ✅ Sabit (build-time identity) |
| **L2 Brand parametric** | UI marka adı, logo, favicon, renkler, tagline, footer, login welcome | DB `BrandSettings` | ❌ |
| **L3 Module switchable** | Aktif modüller (Reports + vNext: Documents/Announcements/...) | DB `Modules` | ❌ |

### Kapsam dahili

- **L1 Kod rebrand:**
  - `ReportPanel.csproj` → `Mosaik.csproj`
  - `ReportPanel.Tests.csproj` → `Mosaik.Tests.csproj`
  - `ReportPanel.sln` → `Mosaik.sln`
  - Namespace `ReportPanel.*` → `Mosaik.*` (~120 dosya)
  - csproj `<RootNamespace>` + `<AssemblyName>`

- **L2 Brand parametric:**
  - `BrandSettings` tablo (key/value pair veya tek satırlık table)
  - Alanlar: `BrandName`, `LogoUrl`, `Favicon`, `PrimaryColor`, `AccentColor`, `Tagline`, `LoginWelcomeText`, `FooterText`
  - `IBrandService` (cache'li okuma) + DI register
  - `_AppLayout.cshtml` config-driven (`@inject IBrandService Brand` → `@Brand.Name`, `@Brand.LogoUrl`)
  - CSS variables: `:root { --brand-primary: @Brand.PrimaryColor }` (inline `<style>` tag, brand değişince re-render)
  - Default seed: `BrandName="Mosaik"`, `LogoUrl=null` (placeholder), `PrimaryColor="#dc2626"` (mevcut kırmızı)

- **L3 Module switchable:**
  - `Modules` tablo: `{ Key (PK), DisplayName, Icon, RouteRoot, Enabled, SortOrder, MinRole }`
  - `IModuleService` (cache'li okuma)
  - `_AppLayout.cshtml` sidebar `@foreach (var mod in Module.GetEnabled())` → koşullu render
  - Default seed: tek satır `{ Key="reports", DisplayName="Raporlar", Icon="fas fa-chart-bar", RouteRoot="/Reports", Enabled=1, SortOrder=10, MinRole=null }`
  - vNext modülleri (Documents/Announcements/...) ileride INSERT ile eklenir
  - Mevcut admin sayfaları modül değil — infra; her zaman görünür (admin role'a koşullu)
  - Mevcut "Genel Bakış" (Dashboard/Index) modül değil — landing page

- **DB:**
  - PortalHUB DROP → Mosaik CREATE (yeni adla)
  - 02..27 migration zinciri replay (DB adı parametric — `USE` statement'ları kaldır, "selected DB"de çalış)
  - **Yeni Migration 28** (`28_BrandAndModules.sql`): BrandSettings + Modules tabloları + default seed
  - 03_SeedData.sql `GETDATE()` → `GETUTCDATE()` (3 yer, UTC baseline)
  - sp_PdksPano + sp_SatisPano deploy (body GETDATE() kalır — external DB, scope dışı)

- **Connection string:** `appsettings.Development.json` Mosaik'e

- **Doc update:** `CLAUDE.md` kimlik bölümü, `TODO.md` aktif kimlik referansları, ADR-006 Faz E closure notu, Plan 10 archive notu

- **Test 246/246 yeşil + login + Dashboard smoke**

### Kapsam dışı

- **Repo klasörü `D:\Dev\reporthub`** — IDE workspace + git remote bağlamayı kırar; ayrı oturum işi.
- **SP body `GETDATE()` → `GETUTCDATE()`** — external DB, SP refactor fazında (TODO madde 21).
- **Logo / brand asset** — yeni logo yok (default null/placeholder), kullanıcı Plan 12'de admin GUI'den yükler.
- **Admin GUI sayfaları** (`/Admin/BrandSettings`, `/Admin/Modules`) — Plan 12.
- **vNext modüller** (Documents/Announcements/Calendar/Forms/Messages/Approvals) — Plan 13+.
- **Multi-tenant** — bu plan tek-tenant white-label.
- **Per-user theme** — out of scope.
- **Journal'lar** — tarihsel, dokunulmaz.

### Etkilenen dosyalar (tahmin)

| Kategori | Sayı | Değişiklik |
|---|---|---|
| Namespace `ReportPanel.*` → `Mosaik.*` | ~120 | sed replace |
| csproj/sln rename | 3 | git mv + içerik |
| Migration `USE PortalHUB` kaldırma | 14 | Manuel/sed |
| Yeni Migration 28 (Brand+Modules) | 1 yeni | Yazma |
| Seed UTC | 1 | Manuel |
| `appsettings.Development.json` | 1 | Manuel |
| Yeni Models (`BrandSetting.cs`, `Module.cs`) | 2 yeni | Yazma |
| Yeni Services (`BrandService.cs`, `ModuleService.cs`) | 2 yeni | Yazma |
| `_AppLayout.cshtml` config-driven | 1 | Refactor |
| `Program.cs` DI register | 1 | Edit |
| `CLAUDE.md`, `TODO.md`, ADR | ~5 | Manuel |

**Tahmini boyut:** ~150 dosya değişiklik, 5 yeni dosya, ~7-8 saat.

## 3. Alternatifler

### A: Sadece kod rebrand + DB reset (brand/module bir sonraki plana)
**Reddetme:** Reset opportunity'sini brand/module foundation kurmadan harcamak — sonra ikinci reset'e gerek doğar. Tek seansta foundation kurmak mantıklı.

### B: Brand+Module admin GUI dahil tek mega plan
**Reddetme:** ~12h, scope creep, test maliyeti yüksek. Admin GUI Plan 12'de pragmatic ayrı tutulur.

### C (SEÇİLEN): Foundation tek paket — kod + DB infra + reset + seed
**Sebep:** Reset baseline'ı brand/module tabloları + service + Layout config-driven ile hizalanmış kurulur. Admin GUI ayrı plan'a (Plan 12) çıkarılır — runtime düzenleme tek başına ihtiyaç bittiği zaman gelir, foundation gecikmemeli.

### D: Tek tablo `Settings` (key/value), brand + module + diğer hepsini kapsa
**Reddetme:** İlerde feature-flag/setting türleri çoğalırsa karışır. İki ayrı tablo (`BrandSettings`, `Modules`) tip-güvenli ve domain ayrımı net.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Namespace replace 120 dosyada eksik | Yüksek (build patlar) | Orta | Faz 2 sonu `dotnet build` doğrulama; sed pattern test |
| sln/csproj rename git history kaybı | Orta | Düşük | `git mv` (rename detect %50+ similarity) — git history korunur |
| MCP write izni yok → SQL bloke | Yüksek (full block) | Yüksek | **Faz 1'de doğrula** — kullanıcı `.claude/settings.local.json` MCP `ALLOW_WRITE=true` açar |
| BrandService cache stale → değişiklik UI'a yansımaz | Düşük | Orta | İlk implement'te cache TTL kısa (5dk) veya InMemory cache invalidate |
| Module sidebar render `Reports` route'u kırar | Yüksek | Düşük | RouteRoot `/Reports` mevcut route'la birebir, dokunulmaz |
| Connection string güncel değil → dev başlatılamaz | Yüksek | Düşük | Faz 7 smoke yakalar |
| Eski cookie geçersiz → 401 | Düşük | Yüksek | Beklenen, kullanıcı yeniden login |
| Migration replay sırası bozuk | Yüksek | Düşük | Numeric sıra (02..27) deterministic, manual sıralama gerek yok |

## 5. Done Criteria

- [ ] Build temiz: 0 error 0 warning, namespace `Mosaik`
- [ ] 246/246 test yeşil
- [ ] DB `Mosaik` var, `PortalHUB` silinmiş
- [ ] Migration zinciri 02..27 + Migration 28 (Brand+Modules) replay başarılı
- [ ] `BrandSettings` tablo: 1 default satır (`BrandName="Mosaik"`)
- [ ] `Modules` tablo: 1 default satır (`Key="reports", Enabled=1`)
- [ ] `_AppLayout.cshtml` `@inject IBrandService` + `@inject IModuleService` config-driven
- [ ] CSS variables brand renklerinden (`:root { --brand-primary }`)
- [ ] Sidebar Reports modülü görünür, koşullu render aktif
- [ ] `appsettings.Development.json` Mosaik connection
- [ ] Browser smoke: admin login → Dashboard → Reports tıkla → bir rapor çalıştır → admin/sıradan iki kullanıcı
- [ ] `CLAUDE.md` kimlik bölümü Mosaik
- [ ] Journal entry yazıldı (`docs/journal/2026-05-07.md`)
- [ ] Plan 10 archive notu (Plan 11 kapsadı)

## 6. Rollback Planı

### Acil (build kırık veya DB hasar)
- **Kod:** `git reset --hard <pre-plan-11-commit>` (kullanıcı açık onay verirse)
- **DB:** Reset öncesi opsiyonel `BACKUP DATABASE PortalHUB` — Mosaik baseline çalışmazsa restore + connection string PortalHUB'a geri.

### Selective
- Faz başına commit → istenmeyen faz revert: `git revert <commit>`. Namespace partial state riski → tüm chain revert tercih.

## 7. Adımlar

### Faz 1 — Hazırlık (~15 dk)
1. [ ] Dev server kapalı doğrula
2. [ ] Branch temiz, mevcut working tree commit'lendi
3. [ ] **MCP write izni doğrula** — `.claude/settings.local.json` PortalHUB MCP `ALLOW_WRITE=true`
4. [ ] Opsiyonel: `BACKUP DATABASE PortalHUB` (kullanıcı SSMS, "ne olur ne olmaz")
5. [ ] `dotnet clean`

### Faz 2 — L1 Kod rebrand (~1.5h)
6. [ ] csproj rename: `git mv ReportPanel/ReportPanel.csproj ReportPanel/Mosaik.csproj`
7. [ ] Tests csproj rename: `git mv ReportPanel.Tests/ReportPanel.Tests.csproj ReportPanel.Tests/Mosaik.Tests.csproj`
8. [ ] sln rename: `git mv ReportPanel.sln Mosaik.sln`
9. [ ] sln + Tests csproj proje referans path'leri güncelle
10. [ ] Namespace replace `ReportPanel` → `Mosaik` (~120 dosya)
11. [ ] csproj `<RootNamespace>` + `<AssemblyName>` Mosaik
12. [ ] Brand metni "BKM Rapor Paneli" / "ReportHub" — **placeholder olarak `@Brand.Name` reference** (şu an view'larda `@inject` henüz yok, hardcode kalır → Faz 3'te değişir)
13. [ ] Build: `dotnet build` 0w 0e
14. [ ] Test: `dotnet test --no-build` 246/246
15. [ ] Commit: `chore(rebrand): ReportPanel → Mosaik canonical (namespace + csproj) (plan: 11)`

### Faz 3 — L2 Brand parametric infrastructure (~1h)
16. [ ] `Models/BrandSetting.cs` — key/value pair (Id, Key, Value, UpdatedAt)
17. [ ] `MosaikContext` DbSet ekle
18. [ ] `Services/BrandService.cs` + `IBrandService` (cache + GetAsync('BrandName')...)
19. [ ] `Program.cs` DI register
20. [ ] `_AppLayout.cshtml`: `@inject IBrandService Brand` + `@Brand.Name`, `@Brand.LogoUrl`, `<style>:root{--brand-primary:@Brand.PrimaryColor}</style>`
21. [ ] `Login.cshtml` welcome metni `@Brand.LoginWelcome`
22. [ ] Footer metni `@Brand.FooterText`
23. [ ] Build temiz
24. [ ] Commit: `feat(brand): BrandSettings DB-driven Layout (plan: 11)`

### Faz 4 — L3 Module infrastructure (~1.5h)
25. [ ] `Models/Module.cs` — { Key, DisplayName, Icon, RouteRoot, Enabled, SortOrder, MinRole }
26. [ ] `MosaikContext` DbSet
27. [ ] `Services/ModuleService.cs` + `IModuleService` (`GetEnabledAsync()`, cache)
28. [ ] `Program.cs` DI register
29. [ ] `_AppLayout.cshtml` sidebar `@foreach (var mod in await Module.GetEnabledAsync())` koşullu render
30. [ ] Mevcut sabit "Raporlar" link'i bu loop'a değişir
31. [ ] Build temiz
32. [ ] Commit: `feat(module): Modules DB-driven sidebar render (plan: 11)`

### Faz 5 — Migration script'lerden DB adı kaldır + seed UTC (~30 dk)
33. [ ] 14 migration dosyasında `USE PortalHUB; GO` blokları kaldır (selected DB'de çalışacak)
34. [ ] `02_CreateTables.sql` USE temiz
35. [ ] `03_SeedData.sql` `GETDATE()` → `GETUTCDATE()` (3 yer)
36. [ ] **Yeni `28_BrandAndModules.sql`:** BrandSettings + Modules tablo + default seed (`Mosaik` brand satırı, `reports` modül satırı)
37. [ ] Commit: `chore(db): migration scriptleri DB-name agnostic + seed UTC + brand/module migration (plan: 11)`

### Faz 6 — DB reset (~45 dk, MCP write)
38. [ ] master DB: `ALTER DATABASE PortalHUB SET SINGLE_USER WITH ROLLBACK IMMEDIATE`
39. [ ] `DROP DATABASE PortalHUB`
40. [ ] `CREATE DATABASE Mosaik`
41. [ ] `USE Mosaik` (selected DB context)
42. [ ] Migration 02..28 sırayla çalıştır (her birinin başarısını doğrula)
43. [ ] sp_PdksPano + sp_SatisPano deploy
44. [ ] Doğrulama SELECT: tüm tablolar var, BrandSettings 1 satır, Modules 1 satır, MIN(CreatedAt) UTC

### Faz 7 — Connection string + smoke (~30 dk)
45. [ ] `appsettings.Development.json` connection string Mosaik'e
46. [ ] Build + dev server (kullanıcı veya preview_start)
47. [ ] Browser smoke (kullanıcı):
    - admin login (eski cookie geçersiz, yeniden giriş)
    - Dashboard (KPI'lar dolu, Brand UI'da "Mosaik" görünüyor)
    - Sidebar'da sadece "Raporlar" modülü görünür
    - Bir rapor çalıştır → temiz
48. [ ] `dotnet test --no-build` 246/246
49. [ ] Commit: `chore(db): DB reset → Mosaik baseline (plan: 11)`

### Faz 8 — Closure (~15 dk)
50. [ ] `TODO.md` madde 28 (DateTime Faz E) [x] + Plan 10 superseded notu
51. [ ] `docs/ADR/006-datetime-utc-convention.md` Faz E closure ("Plan 11 reset baseline")
52. [ ] `CLAUDE.md` kimlik bölümü Mosaik (proje adı, klasör adı yorumu)
53. [ ] Journal entry `docs/journal/2026-05-07.md`
54. [ ] Plan 10 → `plans/archive/` (superseded notu)
55. [ ] Plan 11 → `plans/archive/` (closure notu)
56. [ ] Commit: `chore(plan-11): foundation closure + journal + archive (plan: 11)`

## 8. İlişkili

- ADR: [006-datetime-utc-convention](../docs/ADR/006-datetime-utc-convention.md), [010-plan-first-tier-system](../docs/ADR/010-plan-first-tier-system.md)
- Önceki plan: [10-datetime-faz-e](./10-datetime-faz-e.md) — Plan 11 supersed eder, archive
- Sonraki plan: **Plan 12 — Admin GUI** (`/Admin/BrandSettings` file upload + color picker; `/Admin/Modules` toggle list) ~3-4h
- Sonraki plan: **Plan 13+ — vNext modüller** (Documents, Announcements, Calendar, Forms, Messages, Approvals) — modül-modül ekleme
- TODO: madde 28 (DateTime Faz E), proje adı arama (TODO.md:290 — 28 Nis 2026), vNext vision (TODO.md:298)
- Faz C commit: `dba9dc4` (22 Nis 2026)
- Faz D commit: Migration 25 (5 May 2026)

## 9. Onay

- [x] Plan kullanıcıya gösterildi
- [x] Geri bildirim alındı (3 iterasyon: scope eklemeleri — brand parametric, modüler mimari, install script foundation notu)
- [x] Onay alındı: 2026-05-07, Fikri Eren
