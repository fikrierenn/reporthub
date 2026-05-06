# ADR-009 · Report/Dashboard tip ayrımını kaldır — tek render yolu

- **Durum:** Kabul edildi (24 Nisan 2026)
- **Etkilenen:** `ReportCatalog.ReportType` kolonu (SİLİNECEK), `ReportManagementService` (NormalizeReportType metodu + 2 dallanma), `ReportsController.Run` (:146, :216), `ReportManagementService.cs:41,92,107,111`, `BuildReportFormViewModel.ReportType`, `EditReport.cshtml` + `CreateReport.cshtml` (tip dropdown'u + toggleReportType JS), `Reports/Run.cshtml` (tablo render path'i SİLİNECEK), `ReportPanelContext.OnModelCreating` (fluent config), `AdminController.cs:1213`.
- **İlgili TODO:** M-11 Faz F-1 + F-1.5 (~7 saat). Plan: `C:/Users/fikri.eren/.claude/plans/imdi-planlama-yap-bu-optimized-hippo.md`.
- **İlgili ADR:** ADR-005 (config-driven dashboard), ADR-007 (named result contract), ADR-008 (dashboard builder v2 — bu karar onun F-1.5 fazıdır).

## Bağlam

`ReportCatalog.ReportType` (`NVARCHAR(20)`, default `"table"`) iki render yolunu işaret ediyor:

| Tip | Render path | UI |
|---|---|---|
| `"table"` | `ReportsController.Run` → SP exec → `Run.cshtml` düz HTML tablo | Tek bir tablo, kolon hizalaması statik |
| `"dashboard"` | `ReportsController.Run` → SP exec → `DashboardRenderer.Render()` → iframe | Multi-tab + KPI + chart + table kombine |

**Dallanma noktaları (grep ile bulundu):**
- `ReportsController.cs:146` — `var isDashboard = context.SelectedReport.ReportType == "dashboard";`
- `ReportsController.cs:216` — aynı dallanma, post action
- `ReportManagementService.cs:41, 92` — `if (reportType == "dashboard") { ... }` create/update validation
- `ReportManagementService.cs:107,111` — `report.ReportType = reportType`, `DashboardConfigJson = ... == "dashboard" ? ... : null`
- `AdminController.cs:1213` — `ReportType: Request.Form["ReportType"]`
- `EditReport.cshtml` — tip dropdown + `toggleReportType(type)` JS
- `CreateReport.cshtml` — tip dropdown
- `Reports/Run.cshtml` — `Model.IsDashboard` switch, iki tamamen farklı HTML bloğu

**Sorunlar:**

1. **Çift bakım maliyeti** — tablo-render path'i ve dashboard-render path'i iki ayrı yerde evrimleşiyor. Özellik (örn. pagination, conditional format, Excel export) hangisine eklenmeli?
2. **Builder yatırımı sadece dashboard'a gidiyor** — ADR-008 M-11 builder redesign'ı sadece `ReportType="dashboard"` raporları için değerli. Tablo raporları eski form'da kalır, kullanıcı "tablo" mu "dashboard" mu seçmek zorunda.
3. **Preview = Reports/Run vizyonu** (ADR-008) iki render path'i varken gerçekleşemez. Builder önizlemesi her iki tip için ayrı mı? Hayır — tek yol olmalı.
4. **ReportType = "table"** raporları dashboard config'i olmadan yaşıyor, `DashboardConfigJson IS NULL`. Bu durum `Reports/Run` içinde NULL kontrolü + fallback tablo render gerektiriyor — dead code gibi büyüyen complexity.

## Karar

**`ReportType` kolonu tamamen kaldırılır.** Her rapor = dashboard. Tablo-tipi raporlar tek-`table`-widget'lı dashboard'a migrate edilir; admin sonradan builder'da kolonları/tip ayarlarını düzenleyebilir.

### 3 fazlı plan

#### Faz F-1 (Migration 18 — idempotent, dev+prod uyumlu)

`Database/18_MigrateDashboardSchemaV2.sql` (~220 satır):

**Adım A:** Mevcut `schemaVersion < 2` dashboard configlerini v2'ye çevir (ADR-008 schema v2 alanları: `variant`, `numberFormat`, `axisOptions`, `tableOptions`, `calculatedFields`).

**Adım B:** `ReportType='table' AND (DashboardConfigJson IS NULL OR DashboardConfigJson='')` olan her rapor için auto-generated config yaz:

```jsonc
{
  "schemaVersion": 2,
  "tabs": [{
    "title": "Genel",
    "components": [{
      "id": "w_table_<hash>",
      "type": "table",
      "title": "<report.Title>",
      "span": 4,
      "resultSet": 0,
      "columns": []  // boş → renderer SP'nin ilk SELECT kolonlarını auto-map
    }]
  }]
}
```

**Adım C:** Her UPDATE için audit log entry:
- `dashboard_schema_migrated_v1_to_v2` (ESKİ config `OldValuesJson`'a)
- `report_type_consolidated_to_dashboard` (eski `ReportType='table'` `OldValuesJson`'a)

**İdempotency:** `WHERE ISNULL(JSON_VALUE(DashboardConfigJson, '$.schemaVersion'), 1) < 2 OR (ReportType = 'table' AND DashboardConfigJson IS NULL)`.

**Pre-migration zorunlu:** `SELECT ReportId, ReportType, DashboardConfigJson INTO Database/backup/YYYYMMDD_pre_m11.sql` (gitignored).

#### Faz F-1.5 (Code path consolidation + Migration 19)

**Tek commit bloku** (minimum 3 alt-commit'e bölünebilir — aşağıda):

**Alt-commit 1 — Backend (C#):**
- `ReportManagementService.NormalizeReportType` SİL
- `ReportManagementService.cs:41,92,107,111` dallanmalar temizle — `DashboardConfigJson` her zaman required
- `ReportsController.cs:146,216` `isDashboard` dallanması kaldır, her render `DashboardRenderer.Render()`
- `ReportCatalog.ReportType` property `[Obsolete("M-11 F-1.5, ADR-009")]` işaretle (ama hâlâ okunur/yazılır — migration 19'a kadar)
- `ReportPanelContext.OnModelCreating` ReportType config'i kaldır
- `BuildReportFormViewModel.ReportType` — sabit `"dashboard"` döndür veya sil
- `AdminController.cs:1213` `Request.Form["ReportType"]` kaldır

**Alt-commit 2 — Razor + JS:**
- `Reports/Run.cshtml` tamamen yeniden yaz — tablo render path'i SİL, sadece `DashboardRenderer` iframe output + param bar (ADR-008'deki tek renderer)
- `EditReport.cshtml` tip dropdown + `toggleReportType()` JS SİL. `dashboardConfigSection` hep görünür
- `CreateReport.cshtml` tip dropdown SİL, her yeni rapor dashboard
- Yeni rapor create sonrası otomatik `DashboardConfigJson` iskeletiyle geliyor (basic table widget)

**Alt-commit 3 — Migration 19 + tests:**
- `Database/19_DropReportTypeColumn.sql` (~40 satır):
  ```sql
  IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ReportCatalog') AND name = 'ReportType')
  BEGIN
      ALTER TABLE dbo.ReportCatalog DROP COLUMN ReportType;
  END
  ```
- `ReportCatalog.ReportType` C# property sil ([Obsolete] fazından sonra)
- `ReportPanel.Tests/DashboardMigrationTests.cs` idempotency + v1→v2 + table→dashboard auto-convert testleri
- Smoke: PDKS + Satış + varsayılan tablo raporları `/Reports/Run` erişimi test edilir (screenshot)

### Builder preview = Reports/Run (ADR-008 "tek renderer" kararının yansıması)

- Admin builder'da "Önizle" modu = `<iframe src="/Reports/Run/{id}?preview=1&configOverride=<draftBase64>">`.
- Taslak config (save öncesi) `configOverride` query param olarak geçer; `ReportsController.Run` admin ise override kabul eder (`[Authorize(Roles="admin")]` + anti-forgery).
- Non-admin kullanıcılar override parametresini geçemez — server-side kontrol: `User.IsInRole("admin")` yoksa override yok sayılır.
- **A path seçildi** (B reddedildi): B'nin DOM-level class toggle'ı param değişince AJAX + client-side re-render karmaşıklığı getiriyor, A daha temiz (iframe load + server-side render).

## Alternatifler (atılanlar)

- **`ReportType`'ı `enum` yapıp korumak** — çift render yolu devam eder, temel sorun çözülmez.
- **`ReportType="table"` yolunu deprecate ama kolonu tutmak** — ölü kolon, migration yazıp geri almak daha pahalı.
- **Dashboard'u optional yap, table hala default** — ADR-008 "preview = Reports/Run" vizyonuyla çelişir.
- **F-1 ve F-1.5'i ayrı commit blokları yap** — riskli: F-1 sonrası kod çalışmaz (migration config'ler v2 ama code v1). Hepsi aynı PR'da olmalı.

## Etki

### Kırılma riski

**YÜKSEK.** `Reports/Run` canlı endpoint, `ReportType="table"` raporları kullanıcılar her gün çalıştırıyor. Migration 18+19 + Run.cshtml rewrite atomik olmak zorunda.

### Risk azaltma

1. **Pre-migration backup zorunlu.** `SELECT INTO backup/*.sql`. Dev DB'de dry-run.
2. **Feature branch:** `feature/m-11-dashboard-builder-redesign`. F-1.5 tüm alt-commit'ler branch'te, main'e squash merge.
3. **Staged rollout yok (single-tenant app)** — main'e merge dev ortamında canlı deploy. Test ortamı 1 gün beklet, smoke OK ise prod deploy.
4. **Rollback plan:** Migration 19 öncesi yapılırsa kolon `[Obsolete]` haliyle geri dönüş mümkün. Migration 19 sonrası SSMS'te `ALTER TABLE ADD ReportType NVARCHAR(20) NULL` + backup'tan restore — manuel (solo-dev, dokümante).
5. **Smoke matrisi (F-12):**
   - PDKS `/Reports/Run/{id}` — eski görünüm piksel-yakın (auto-migrated v2 config)
   - Satış aynı
   - Eski `ReportType="table"` rapor (örn. "Ciro Raporu") — tek-table-widget dashboard render
   - Admin `/Admin/EditReport/{id}` — builder v2 açılır, tip dropdown yok

### Uncommitted etki

F-1 + F-1.5 birlikte ~20 dosya. **commit-discipline.md 15-eşik istisnası** — F-1.5 alt-commit'e bölünür (yukarıda 3 parça), her biri <15 dosya.

## Revizyon tarihçesi

- **2026-04-23** — M-11 planı yazıldı, F-1 + F-1.5 ayrı fazlar olarak taslaklandı ama scope net değildi.
- **2026-04-23 akşam** — "preview = Reports/Run" kararı (ADR-008) F-1.5'in Run.cshtml rewrite gerektirdiğini netledi.
- **2026-04-24 sabah** — bu ADR yazıldı, alt-commit stratejisi + rollback planı detaylandı.
