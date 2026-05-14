# Mimari Kurallar ve Bilinen Tutarsızlıklar

_Kapsam: Projenin genel mimarisi, karar kayıtları (ADR'lere dönüşene kadar özet burada)._

## Veri Erişim

- **Rapor/dashboard verisi** → SQL Server **stored procedure**. `CommandType.StoredProcedure` + `SqlParameter` zorunlu. String concat YOK.
- **Metadata CRUD** (User, Role, ReportCatalog, AuditLog, Category, Favorite, UserDataFilter) → **EF Core 10**, `MosaikContext`.
- **Dapper yok**, eklenmeyecek (tek geliştirici için gereksiz katman).
- **SP execution helper:** `ReportsController.ExecuteStoredProcedureMultiResultSets` — ileride `Services/IStoredProcedureExecutor.cs`'e taşınacak (TODO M-01 service extraction ile birlikte).

## Rapor Render Mimarisi (TEK PATH — ADR-009)

**`ReportType` kolonu `[Obsolete]`** — kod bu alana **bakmıyor**. Her rapor aynı path'ten geçer:

```
GET /Reports/Run/{id}  →  parametresiz ise otomatik POST
POST /Reports/Run
  → ReportParamValidator.ValidateAndBuild()   [param parse + SqlParameter]
  → UserDataFilterInjector.InjectAsync()       [multi-tenant filtre]
  → IStoredProcedureExecutor.ExecuteMultipleAsync()  [multi-RS ADO.NET]
  → DashboardConfig deserialize (NULL → boş config fallback + audit)
  → DashboardRenderer.Render(config, resultSets)
      kpi → KpiRenderer | chart → ChartRenderer | table → TableRenderer
  → Run.cshtml: <iframe sandbox="allow-scripts" srcdoc="...">
```

- **`ReportType="table"` DB'de görünse bile davranış değişmez** — Migration 18B tam çalışmadı, DB'deki `table` değerleri stale. Migration 19 DROP edecek.
- **`DashboardConfigJson` boşsa:** `dashboard_config_missing` audit + boş config → boş iframe (veri var ama görünmez).
- **Table widget `columns:[]`:** SP'nin döndürdüğü tüm kolonları otomatik gösterir.
- **Result binding:** `result:"rs0"` / `result:"rs1"` (rsN regex) veya `ResultContract` named lookup. `ResolveResultSet()` → `DashboardConfig.cs:41`.
- **Export:** Yalnızca first result set, dashboard config'e bakılmaz.

## Dashboard Config Yapısı

- **Config-driven JSON.** `ReportCatalog.DashboardConfigJson` tek source-of-truth. `DashboardHtml` DROP edildi (ADR-005, migration 17).
- **Renderer:** `Services/DashboardRenderer.cs` — statik, StringBuilder ile HTML + inline JS emits.
- **Güvenlik:** iframe sandbox (`allow-scripts` only, `allow-same-origin` yok) — XSS büyük ölçüde izole.
- **XSS önlemleri:** DOM API `createElement`+`textContent`, `eval()` yasak, `onclick` attribute yasak, `addEventListener`+closure, `</script>` regex kaçırma.

## Auth + Yetkilendirme

- **Cookie auth** (ASP.NET Core default). JWT ekleme yok (multi-client değiliz).
- **Rol modeli:** `UserRole` junction tablosu **birincil**. `User.Roles` CSV kolonu **kaldırılıyor** (TODO M-03, migration 15). AuthController CSV fallback **kaldırılacak**.
- **`[Authorize(Roles="admin")]`** tüm admin controller'larda class-level.
- **`[ValidateAntiForgeryToken]`** POST action'larda zorunlu. (TestController exception — TODO G-06.)

## Bilinen Tutarsızlıklar (YÜKSEK risk)

1. **`AllowedRoles` CSV — DashboardController hâlâ kullanıyor** — `DashboardController.cs:299-313` CSV-bazlı `AllowedForUser`. `ReportsController` ise junction tablosunu doğru kullanıyor. İki paralel sistem. TODO M-03 kapsamı. `DashboardController` junction'a geçmeli.
2. **User.Roles CSV + UserRole tablo ikili sistem** — TODO M-03, bu hafta.
3. **AdminController çok büyük** — service layer'a bölünmüş ama partial split yeterli değil. TODO M-01.

## ORTA risk

- **`ReportType [Obsolete]` hâlâ yazılıyor** — `ReportManagementService.cs` + `AdminController.cs:273` `"dashboard"` sabit yazıyor. Migration 19 çalıştırıldıktan sonra `#pragma` + DTO temizle.
- **`DateTime.Now` view'da** — `Dashboard/Index.cshtml:9,16`. Server UTC değilse yanlış saat. Comment ekle veya timezone config.
- **ViewModel entity direkt mapping** — mass assignment riski. TODO M-07.
- **`.AsNoTracking()` eksik** — okuma sorgularının büyük kısmı tracked. ✅ `AdminController.Brand.cs` düzeltildi (7 Mayıs 2026).

## DÜŞÜK risk

- **Form syntax: raw `<form>` standart** — 21/24 view raw kullanıyor, 3'ü `Html.BeginForm`. Standart: raw `<form method="post">` + `@Html.AntiForgeryToken()` (CSRF eşdeğer, tutarlılık için).
- **AuditLog selektif** — datasource/category delete log'lanmıyor. TODO G-04.
- **Test coverage <%10** — öncelik: DashboardRenderer, UserDataFilter, UserRole sync.

## Düzeltilen Aykırılıklar (7 Mayıs 2026 taraması)

- ✅ `ex.Message` → user'a JSON dönme — `AdminController.Filters.cs:158` güvenli mesaja çevrildi.
- ✅ `IsDashboard` ölü property — `ReportRunViewModel.cs` + 2 controller set satırı silindi.
- ✅ CSS eski class — `form-card`/`btn-brand` vb. tüm view'larda temiz (M-13 Plan 03 R2 sonrası).
- ✅ `[ValidateAntiForgeryToken]` — tüm POST action'larda mevcut.
- ✅ `async void` — hiç yok.
- ✅ `AdminController.Brand.cs` GET → `.AsNoTracking()` eklendi.

## Kararlar (kronolojik — küçük notlar, büyük kararlar ADR'lere)

**22 Nisan 2026:**

- **Dev launch config `Development` zorunlu.** `dotnet run --no-launch-profile` Production default'a düşüyor → `appsettings.Development.json` (gitignored local SA şifresi) yüklenmez → DB connection kırılır. Çözüm: `.claude/launch.json` → `--launch-profile http` geç (launchSettings'teki profil `ASPNETCORE_ENVIRONMENT=Development` ayarlar).
- **Dev-only şifreler için rotate şart değil** (G-01 kapsamı). Kullanıcı kararı: "dev ortamı zaten lokal, git history'deki eski SA şifresi önemsiz". Fix sadece leak'i durdurur, history'yi dokunmaz. Prod/staging şifreleri için aynı karar **geçerli değil** — rotation zorunlu.
- **AdminController 1736 satır anti-pattern kabul.** M-01 (Faz 2, 2 gün) service extraction planlandı ama commit-split sırasında `feat(admin): consolidated admin panel` bundled commit (`07f4b91`) olarak kabul edildi. "Known technical debt" commit mesajında açıkça belirtildi. Refactor ayrı ADR konusu.
- **User.Roles CSV deprecate 3 faz**: Faz A (kod-düzeyi temizlik ✅ `2d0c3fd`), Faz B (kolon nullable + `[Obsolete]`), Faz C (kolon drop + field sil). Detay: [ADR-003](../../docs/ADR/003-role-model.md).
- **Skill commit davranışı 3 kademeli.** session-handoff (tam otomatik, tek path), plan-tracker (kod commit'iyle birlikte), commit-splitter (her bucket için onay). Detay: [ADR-004](../../docs/ADR/004-skill-design-principles.md).
- **Pragmatik commit-split** (65 dosya → 16 commit): yeni dosyalar feature-başına, modified controller/view dosyaları scope-based consolidated. Hunk-level split 2-3 saat maliyet, solo-dev için yatırım değil. Pattern kaydı: `claude-context-template/docs/PATTERNS.md` P-1.
- **Pre-commit antipattern hook** commit-split süresince disable edildi (mevcut M-02 ihlalleri blok ediyordu), sonda re-enable ayrı commit (`59888db`). Pattern: `claude-context-template/docs/PATTERNS.md` P-2.
- **Deprecated dosya silme > banner.** `Views/Auth/AGENT.md` banner yerine silindi (`7a7b81d`). Banner kafa karıştırır.
- **Koşulsuz SessionStart hook kuralı.** `.claude/rules/session-protocol.md` — context'te hook çıktısı görünse bile `bash` elle tekrar çalıştırılır. Aksi varsayım iki kez hata üretti.

## CSS Pattern Kuralı (ZORUNLU)

`form-card` / `form-group` / `form-label` / `form-input-brand` / `btn-brand` / `btn-brand-outline` → **M-13 Plan 03 R2'de SİLİNDİ.** Yeni view yazarken kullanma.

**Modern pattern:** `.field` / `.lab` / `.inp` / `.btn` / `.btn.primary` / inline `var(--paper)` card (CreateUser.cshtml referans al).

## Controller → Sorumluluk Hızlı Referans

`AdminController` (8 partial) → admin CRUD hub | `AuthController` → login/cookie | `DashboardController` → ana sayfa | `LogsController` → audit viewer | `ProfileController` → profil+şifre | `ReportsController` (3 partial) → rapor index+run+export+preview | `TestController` → dev-only DB test

## Service → Sorumluluk Hızlı Referans

`IBrandService` / `IModuleService` → **Singleton**, DB cache, `Invalidate()` sonrası yeniler | `StoredProcedureExecutor` → multi-RS ADO.NET | `UserDataFilterInjector` → multi-tenant, deny-by-default | `DashboardRenderer` → static orchestrator | `AuditLogService` → tüm kritik aksiyonlar | `PasswordHasher` → PBKDF2 100k iter

**Tam mimari harita:** `memory/project_architecture_map.md`

## Proje Durumu (snapshot — 2026-05-14 VISION.md uyumu)

- **Mevcut özellik olgunluğu:** ~%75. Reports + Dashboard + Contracts/Obligations + AI extraction + Tamim + OrgChart canlı kullanıma hazır.
- **vNext "iç portal" vaadi:** ~%40-50. 10 vNext modülünden 3 tam (Reports, Dashboard, Tamim), 1 yarım (Documents), 6 yok (SOP, Comment/Mention, Form/Anket, Workflow Designer, Duyuru, KPI/OKR).
- **İki olgunluğu karıştırma** — "%75 hazır" mevcut iddiası için doğru, vNext için yanıltıcı. Detay: [`docs/VISION.md`](../../docs/VISION.md).
- **Uncommitted:** tipik olarak 0.
- **Aktif controller (16):** Admin, Ai, Auth, Calendar, Compliance, Contracts, Dashboard, Documents, Home, Logs, Notifications, Obligations, OrgChart, Profile, Reports, Test.
- **Modül ayrımı:** Sadece `Mosaik.Modules.Circular` ayrı csproj. Diğer 4 modül (Documents, Contracts, OrgChart, Calendar) ana projede — [ADR-015](../../docs/ADR/015-new-modules-separate-assembly.md) ile 1/ay tempoda çıkarılacak.

## Module ayrımı disiplini (ADR-002 + ADR-015)

- **Yeni modüller:** `Mosaik.Modules.<X>` ayrı csproj **zorunlu** (istisna yok). [ADR-015](../../docs/ADR/015-new-modules-separate-assembly.md).
- **Mevcut 4 modül çıkarma sırası:** OrgChart (Haz 2026) → Calendar (Tem) → Contracts (Ağu) → Documents (Eyl).
- **Core kalır:** Reports + Dashboard cross-modül kullanım merkezi, ana proje.
- **Cross-modül iletişim:** Doğrudan API çağrısı **yasak**. Sadece `Mosaik.Core` abstraction'ları üzerinden (`IUserDataScope`, `IApprovalService`, `ILookupService`, `INotificationService`, `IEmailService`).
- **Şablon:** [`Mosaik.Modules.Circular`](../../Mosaik.Modules.Circular/) (Plan 17 referans implementation).
- **Skill:** [`vnext-entity-port`](../../.claude/skills/vnext-entity-port/SKILL.md).

## Frontend stack disiplini (ADR-014)

- **Vanilla IIFE** — birincil, karmaşık client logic (drag-drop, canvas, 250+ satır)
- **Alpine.js** — UI state default (`x-data` ≤20 satır inline; toggle/modal/drawer/filter)
- **htmx** — **ertelenmiş** (canlı kullanım 0). Yeni view'da kullanma. 3 spesifik use case (server partial swap, multi-stage form, sidebar live update) için **ADR ek + plan onayı** ile geri açılır.

Detay: [ADR-014](../../docs/ADR/014-frontend-stack-layering.md).

## Referanslar

- **Yön belgesi:** [`docs/VISION.md`](../../docs/VISION.md) (vNext kalbi SOP+Comment+Workflow Designer ~6-9 hafta).
- **ADR'ler (13 toplam):** ADR-001 (data-access), ADR-002 (modular-monolith), ADR-003 (role-model), ADR-004 (skill-design), ADR-005 (dashboard-architecture), ADR-006 (datetime-utc), ADR-007 (named-result-contract), ADR-008 (dashboard-builder-v2), ADR-009 (report-type), ADR-010 (plan-first-tier), ADR-011 (sidebar-shell), ADR-012 (pk-scheduler-firma-filter), ADR-013 (multi-db-topology), ADR-014 (frontend-stack), ADR-015 (new-modules-separate-assembly).
- Bağlam yönetimi: `docs/CONTEXT_MANAGEMENT.md`.
- Kapsamlı TODO: `TODO.md` → "BIRLESIK ONCELIK SIRASI".
- Real-world pattern'ler: `claude-context-template/docs/PATTERNS.md` (P-1..P-10).
