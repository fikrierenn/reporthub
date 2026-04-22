# Mimari Kurallar ve Bilinen Tutarsızlıklar

_Kapsam: Projenin genel mimarisi, karar kayıtları (ADR'lere dönüşene kadar özet burada)._

## Veri Erişim

- **Rapor/dashboard verisi** → SQL Server **stored procedure**. `CommandType.StoredProcedure` + `SqlParameter` zorunlu. String concat YOK.
- **Metadata CRUD** (User, Role, ReportCatalog, AuditLog, Category, Favorite, UserDataFilter) → **EF Core 10**, `ReportPanelContext`.
- **Dapper yok**, eklenmeyecek (tek geliştirici için gereksiz katman).
- **SP execution helper:** `ReportsController.ExecuteStoredProcedureMultiResultSets` — ileride `Services/IStoredProcedureExecutor.cs`'e taşınacak (TODO M-01 service extraction ile birlikte).

## Dashboard Mimarisi

- **Config-driven JSON.** `ReportCatalog.DashboardConfigJson` birincil. `DashboardHtml` (legacy) kaldırılacak (TODO M-05).
- **Renderer:** `Services/DashboardRenderer.cs` — statik, StringBuilder ile HTML + inline JS emits.
- **Güvenlik:** iframe sandbox (`allow-scripts` only, `allow-same-origin` yok) — XSS büyük ölçüde izole.
- **XSS önlemleri (hepsi uygulanmış):**
  - DOM API: `createElement` + `textContent` (asla `innerHTML`).
  - `eval()` **yasak** — `window.__RS[rs]` array indexing kullan.
  - `onclick` inline attribute yasak — `addEventListener` + closure.
  - `</script>` break-out case-insensitive regex ile, `<!--` de kaçırıldı.

## Auth + Yetkilendirme

- **Cookie auth** (ASP.NET Core default). JWT ekleme yok (multi-client değiliz).
- **Rol modeli:** `UserRole` junction tablosu **birincil**. `User.Roles` CSV kolonu **kaldırılıyor** (TODO M-03, migration 15). AuthController CSV fallback **kaldırılacak**.
- **`[Authorize(Roles="admin")]`** tüm admin controller'larda class-level.
- **`[ValidateAntiForgeryToken]`** POST action'larda zorunlu. (TestController exception — TODO G-06.)

## Bilinen Tutarsızlıklar (YÜKSEK risk)

1. **User.Roles CSV + UserRole tablo ikili sistem** — TODO M-03, bu hafta.
2. **ReportCatalog.DashboardHtml + DashboardConfigJson dual storage** — TODO M-05, bu ay.
3. **AdminController 1736 satır** — service layer yok. TODO M-01, bu ay.
4. **Exception handling stack trace sızdırıyor** (`ex.Message` user'a gösteriliyor) — TODO M-02, bu hafta.

## ORTA risk

- **Async/await tutarsız** — bazı GET action'larda `.ToList()` sync. TODO M-08.
- **`.AsNoTracking()` eksik** — 15+ read query. TODO M-09.
- **ViewModel entity direkt mapping** — mass assignment riski. TODO M-07.
- **Form syntax karışık** — `Html.BeginForm` vs raw `<form>`. Standart: `Html.BeginForm`.

## DÜŞÜK risk

- **AuditLog selektif** — datasource/category delete log'lanmıyor. TODO G-04.
- **CSS Tailwind utility + custom karışık** — PostCSS build pipeline ilerde.
- **DB script organizasyonu** — `Database/` alt klasörlere ayrılacak. TODO M-06.
- **Test coverage <%10** — öncelik: DashboardRenderer, UserDataFilter, UserRole sync.

## Proje Durumu (snapshot)

- **Olgunluk:** %75.
- **Uncommitted:** 32 dosya (TODO — 7 fazlı commit-split).
- **Aktif modüller:** rol sistemi, kategori, favori, AD user, user data filter, dashboard motoru, SP önizleme.

## Referanslar

- ADR'ler (yazılacak): `docs/ADR/001-data-access.md`, `002-dashboard-architecture.md`, `003-role-model.md`, `004-sp-modularization.md`.
- Bağlam yönetimi: `docs/CONTEXT_MANAGEMENT.md`.
- Kapsamlı TODO: `TODO.md` → "BIRLESIK ONCELIK SIRASI".
