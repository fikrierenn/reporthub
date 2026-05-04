# Plan 03 — DashboardV2 Stand-alone Kapanış (Plan 02 Alt-2V Revizyon)

**Tarih:** 2026-04-29
**Yazan:** Fikri (revize sürümü) + Claude (dosyaya geçirildi)
**Durum:** Onaylandı (2026-04-29)
**Branch:** `feature/m-11-dashboard-builder-redesign`
**İlişkili:** Plan 02 alt-2V (V2 fork pivot)

---

## Özet

DashboardV2 ana akış olacak. Hedef: V2 üzerinden sıfırdan rapor oluşturma, SP seçme, parametre üretme, veri görme, widget bağlama, validate ederek kaydetme ve gerçek dashboard preview.

**Kabul kriteri:** Admin V2'de yeni rapor oluşturur, DataSource + SP seçer, parametreleri üretir, veriyi önizler, widget bağlar, config'i validate ederek kaydeder, reload sonrası aynı dashboard çalışır.

**Bağlam:** Kullanıcı kararı (29 Nisan 2026): "ana uygulamam DashboardV2 olmalı, onu olgunlaştırma çalışıyorum". vNext mimari programı (Plan 04-11) backlog'a alındı; önce V2 stand-alone hale gelir.

**Sıra:**
1. TagHelper fix commit
2. CreateReportV2 SP seçim UI
3. Server-side dashboard config validation
4. F-9 gerçek iframe preview

---

## Doğrulanmış Bağımlılıklar

- **`/Admin/ProcParams`** ✓ mevcut (`AdminController.cs:195`, `SpExplorerService.GetParametersAsync` çağırır, `{ fields }` döner)
- **`DashboardConfigValidator`** ✓ mevcut (`Services/DashboardConfigValidator.cs:13`, static class)
- **`DashboardRenderer.Render`** ✓ full `<html><head><body>` document emit eder (`DashboardShellRenderer.BeginHtml(sb)` ile başlar) — iframe `srcdoc` direkt kullanılabilir

---

## Key Changes

### 1. TagHelper Fix (mevcut uncommitted)
- 3 açık Razor diff ayrı küçük commit olur
- `x-data="{}"` ve `x-on:input` syntax düzeltmeleri korunur
- 3 dosya: `EditReportV2.cshtml`, `CreateReportV2.cshtml`, `_ReportFormBuilderTopActionsV2.cshtml`
- Commit: `fix(m-11): normalize Alpine attributes in builder v2 views`

### 2. CreateReportV2 SP Seçim UI
- Drawer **Ayarlar** tab'ına DataSource dropdown + ProcName autocomplete eklenir
- SP listesi için mevcut `/Admin/SpList` kullanılır
- SP parametreleri için mevcut `/Admin/ProcParams` kullanılır
- `SpPreview` param metadata için kullanılmaz; sadece veri preview tarafında kalır
- DataSourceKey, ProcName, ParamSchemaJson hidden input'ları V2 state ile senkron tutulur (Alpine `x-bind:value`)
- Bu adım tek bundled commit olur; kapsam tek iş: `CreateReportV2 stand-alone`

### 3. Server-side Validator
- Mevcut `DashboardConfigValidator` server-side save öncesi çağrılır
- Konum: `ReportManagementService` create/update öncesi
- Model attribute validation kullanılmaz; JSON içerik domain validator ile doğrulanır
- Invalid config DB'ye yazılmaz
- Hatalar V2 view'da Türkçe liste olarak gösterilir

### 4. F-9 Preview Iframe
- Endpoint:
  - `POST /Admin/Reports/PreviewDashboardV2`
  - Body: `{ reportId?, dataSourceKey, procName, configJson, paramsJson }`
  - Response: `text/html`
- Response tam HTML dokümanı olur; `DashboardRenderer` zaten full `<html><head><body>` wrap üretir
- iframe `srcdoc` kullanır
- Sandbox: `allow-scripts`; `allow-same-origin` eklenmez
- Endpoint admin-only ve antiforgery korumalı olur

---

## Runtime Smoke Kuralı

Her Razor/V2 view değişikliğinden sonra **sadece build yeterli sayılmaz**.

**Zorunlu smoke** (her view edit sonrası):
- `/Admin/CreateReportV2` reload
- `/Admin/EditReportV2/13` reload

**Amaç:** fresh Razor compile/runtime TagHelper hatalarını yakalamak (modülarize commit'inden öğrenildi — build OK ≠ runtime OK).

---

## Test Planı

- `dotnet build reporthub.sln --nologo`
- `dotnet test ReportPanel.Tests/ReportPanel.Tests.csproj --nologo`
- JS parse check (`node -e "new Function(...)"`)
- Browser smoke:
  - DataSource seç
  - SP seç
  - ParamSchema üret
  - Preview data çek
  - Widget ekle/bağla
  - Kaydet
  - Reload
  - `/Reports/Run/{id}` kontrol

---

## Ertelenenler
- Param-bar date picker popover (düşük öncelik)
- Calculated fields full AST (ileri faz)
- `sp_PdksPano` SQL fix (ayrı DB yazma izni gerektirir)
- V1 builder **silinmez**; mevcut route'larda paralel ama UI'da gizli fallback kalır

---

## Varsayımlar
- V1 silinmez; mevcut route'larda paralel ama UI'da gizli fallback kalır
- V2 ana akış olur
- Önce stand-alone çalışırlık, sonra genel mimari backlog (Plan 04-11)

---

## Done Kriterleri (Plan tamamlandığında)
- 4 commit'lendi (TagHelper fix + SP seçim UI + Validator + F-9 iframe)
- Browser smoke tüm akış üzerinden geçti (DataSource → SP → param → preview → widget → kaydet → reload → Run)
- Build + test yeşil
- Plan 02 alt-2V "tamamlandı" olarak işaretlenir
- Bu plan `plans/archive/` klasörüne taşınır
