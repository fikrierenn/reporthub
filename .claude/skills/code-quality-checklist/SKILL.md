# Code Quality Checklist — Mosaik

**Tetikleyici:** Kod yazarken / düzenlerken otomatik danış. "checklist", "kontrol", "kalite kontrol" komutlarıyla da çağrılabilir.

**Amaç:** Taramalarda tekrar tekrar çıkan hataları YAZIM SIRASINDA önle. Handoff taraması son doğrulama olsun, ilk savunma hattı bu skill olsun.

---

## 1. Exception Handling (EN KRİTİK)

### YASAK Pattern'ler

```csharp
// YASAK 1: Bare catch — sessiz yutma
catch { return defaultValue; }
catch { /* comment */ }

// YASAK 2: _ = ex discard — logger olmadan
catch (Exception ex) { _ = ex; TempData["Message"] = "Hata"; }

// YASAK 3: ex.Message kullanıcıya sızdırma
catch (Exception ex) { return $"Hata: {ex.Message}"; }
TempData["Message"] = $"İşlem başarısız: {ex.Message}";
result = new Result(false, null, $"Exception: {ex.Message}");

// YASAK 4: SaveChangesAsync try/catch'siz
_context.Add(entity);
await _context.SaveChangesAsync(); // catch yok = caller crash
```

### DOĞRU Pattern'ler

```csharp
// DOĞRU 1: Logger + generic user mesajı
catch (Exception ex)
{
    _logger.LogError(ex, "LookupService.CreateValueAsync typeId={TypeId}", typeId);
    return ServiceResult.Failure("Kayıt sırasında hata oluştu.");
}

// DOĞRU 2: Controller catch
catch (Exception ex)
{
    _logger.LogError(ex, "AdminController.Lookup GET failed");
    TempData["Message"] = "Beklenmedik bir hata oluştu.";
    TempData["MessageType"] = "error";
}

// DOĞRU 3: SaveChangesAsync korumalı
try { await _context.SaveChangesAsync(); }
catch (Exception ex)
{
    _logger.LogError(ex, "Save failed for {Entity}", entity.GetType().Name);
    return ServiceResult.Failure("Kayıt sırasında hata oluştu.");
}
```

### ILogger Inject Kuralı

Her servis ve controller'da `ILogger<T>` zorunlu:

```csharp
private readonly ILogger<MyService> _logger;

public MyService(MosaikContext context, ILogger<MyService> logger)
{
    _context = context;
    _logger = logger;
}
```

**Bilinen eksikler (tarama 2026-05-08):**
- `BrandSettingsService` — bare catch, logger yok
- `ModuleService` — bare catch, logger yok
- `SpExplorerService.BuildSpParametersAsync` — static method, logger erişemiyor
- `ReportParamValidator` — bare catch, boş liste dönüyor
- `ApprovalService` — SaveChanges korumasız, logger yok
- `UserRoleSyncService` — SaveChanges korumasız
- `StoredProcedureExecutor` — sıfır error handling
- `AuditLogService.LogAsync` — SaveChanges korumasız (SPoF: audit crash = işlem crash)
- `BlockFileService` (Circular) — ex.Message user'a sızıyor
- `CircularSummaryService` — ex.Message user'a sızıyor
- `CompileCircularJob` — bare catch
- `BlockService` (Circular) — logger yok
- `CircularService` (Circular) — logger yok

---

## 2. Güvenlik

### Mass Assignment

```csharp
// YASAK: Entity direkt model binding
[HttpPost]
public async Task<IActionResult> Create(DataSource dataSource)
{
    _context.DataSources.Add(dataSource); // tüm alanlar overwrite edilebilir
}

// DOĞRU: Route param + tek tek property atama
[HttpPost]
public async Task<IActionResult> Edit(int id)
{
    var entity = await _context.FindAsync(id);
    entity.Title = Request.Form["Title"];
    entity.IsActive = ReadFormBool("IsActive");
}

// DOĞRU: ViewModel/DTO + [BindNever] PK
public class UserFormInput
{
    [BindNever] public int UserId { get; set; }
    public string Username { get; set; }
}
```

**Bilinen eksikler:**
- `AdminController.DataSources.cs:26,103` — DataSource entity direkt binding
- `AdminController.RolesGroups.cs:33` — EditRole'da `role.RoleId` form'dan geliyor (IDOR riski), route `id` kullanılmalı

### CSRF

```csharp
// HER POST action'da zorunlu:
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(...)

// HER form'da zorunlu:
<form method="post">
    @Html.AntiForgeryToken()
```

### Authorize

```csharp
// Admin controller — class-level
[Authorize(Roles = "admin")]
public partial class AdminController : Controller

// Genel controller — class-level
[Authorize]
public class ReportsController : Controller
```

### Open Redirect

```csharp
// DOĞRU pattern (3 kontrol):
if (!string.IsNullOrEmpty(returnUrl)
    && Url.IsLocalUrl(returnUrl)
    && returnUrl.StartsWith("/")
    && !returnUrl.StartsWith("//")
    && !returnUrl.StartsWith("/\\"))
{
    return Redirect(returnUrl);
}
```

---

## 3. Türkçe UI (UTF-8 Zorunlu)

### YASAK: ASCII sadeleştirme

```
"bulunamadi"  → "bulunamadı"
"Deger"       → "Değer"
"Duzenle"     → "Düzenle"
"olusturuldu" → "oluşturuldu"
"guncellendi" → "güncellendi"
"Yayinlanmis" → "Yayınlanmış"
"silinemez"   → ✓ (zaten doğru)
"gecersiz"    → "geçersiz"
"secilmeli"   → "seçilmeli"
```

### Hızlı Kontrol

ServiceResult mesajı, TempData mesajı, veya UI'a giden herhangi bir string yazıyorsan:
1. `ı/İ` kullandın mı? (`i/I` değil)
2. `ş/Ş`, `ğ/Ğ`, `ü/Ü`, `ö/Ö`, `ç/Ç` doğru mu?
3. ASCII grep: `grep -P "[^\\x00-\\x7F]"` ile Türkçe karakter var mı kontrol et

**Bilinen eksikler (Circular modül):**
- `BlockService.cs` — 8+ satırda ASCII Türkçe
- `BlockController.cs:87,169` — "gecersiz" → "geçersiz"
- `LookupService.cs` — düzeltildi (bu oturumda)

---

## 4. Kod Kalitesi

### Console.WriteLine Yasak

```csharp
// YASAK: Production'da debug output
Console.WriteLine($"DataSource: {key}");

// DOĞRU: Conditional logging
_logger.LogDebug("DataSource: {Key}", key);
```

**Bilinen:** `AdminController.Reports.cs:45-51`, `AdminController.DataSources.cs:32-33,38`

### FilterOptionsService — Success on Failure

```csharp
// YASAK: Hata olduğunda success=true dönme
catch (Exception ex)
{
    _logger.LogWarning(ex, "Query failed");
    return new Result(true, null, Array.Empty<T>()); // success=true!
}

// DOĞRU: Hata flag'i + boş sonuç ayrımı
catch (Exception ex)
{
    _logger.LogWarning(ex, "Query failed for {Key}", key);
    return new Result(false, "Sorgu başarısız oldu.", Array.Empty<T>());
}
```

### AuditLogService — Single Point of Failure

```csharp
// SORUN: Audit log crash = tüm işlem crash
await _auditLog.LogAsync(entry); // SaveChanges atarsa caller da patlar

// ÖNERİ: Fire-and-forget veya try/catch ile audit izole et
try { await _auditLog.LogAsync(entry); }
catch (Exception ex) { _logger.LogError(ex, "Audit log failed"); }
```

### Partial-Save Riski

```csharp
// SORUN: İki ayrı SaveChanges — ilki başarılı, ikincisi crash
await _context.SaveChangesAsync(); // user kaydedildi
await _userRoleSync.SyncAsync(user.UserId, roleIds); // bu içinde SaveChanges → crash
// Sonuç: user var ama rolleri yok

// ÖNERİ: Transaction veya tek SaveChanges
using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // tüm değişiklikler
    await _context.SaveChangesAsync();
    await tx.CommitAsync();
}
catch { await tx.RollbackAsync(); throw; }
```

### Race Condition — AI Test

```csharp
// SORUN: DB state'i test sırasında geçici değiştirme
foreach (var p in others) p.IsEnabled = false;
await _context.SaveChangesAsync(); // başka request AI profil bulamaz!
// ... test ...
foreach (var p in others) p.IsEnabled = true;

// ÖNERİ: DB'yi değiştirmeden in-memory test
var provider = new AiSummaryProvider(testConfig);
var result = await provider.TestAsync(prompt);
```

---

## 5. UI / Tasarım / Erişilebilirlik (Design Skill'lerden)

### WCAG AA Zorunlu (accessibility-compliance)

```html
<!-- YASAK: Landmark eksik -->
<div class="crumbs">...</div>

<!-- DOĞRU: Semantic nav + aria-label -->
<nav class="crumbs" aria-label="Sayfa konumu">
  <a href="/">Ana Sayfa</a> / <span aria-current="page">Kullanıcılar</span>
</nav>
```

- `aria-current="page"` → breadcrumb'da aktif sayfa (şu an 59/61 view'da eksik)
- `aria-hidden="true"` → dekoratif ikonlar (`<i class="fas fa-plus" aria-hidden="true">`)
- `aria-label` → topbar search input, sidebar navigation, subnav
- `:focus-visible` → `.icon-btn`, `.subnav a`, `.nav-list a`'da **YOK** — eklenmeli
- Contrast → `var(--ink-3)` secondary text **3.8:1 FAIL** — `var(--ink-2)` kullan (4.8:1 ✓)
- Touch target → `.icon-btn` 34px, `.user-actions` 26px — mobilde 44px olmalı

### Renk ve Token (visual-design-foundations)

```css
/* YASAK: Hardcoded inline renk */
style="background: rgba(16,185,129,0.10); color: #065f46;"

/* DOĞRU: CSS variable */
style="background: var(--success-bg-soft); color: var(--success-ink);"
```

**Eksik token'lar** (tokens.css'e eklenmeli):
- `--empty-state-bg`, `--empty-state-border`
- `--danger-bg-soft`, `--success-bg-soft`

**19 view'da** hardcoded inline renk var — dokunduğunda token'a çevir.

### Responsive (responsive-design)

- Breakpoint tutarsızlığı: components.css 1100/600/1000/700px, app-shell 1023px, utilities 768px
- `max-width` card'larda 880px vs 720px karışık — standartlaştır
- Mobil override: `.icon-btn`, `.side-collapse` min-height 44px media query ekle

### Pattern Uyumu (ui-patterns.md)

| Kontrol | Durum |
|---|---|
| Breadcrumb `<nav>` + `aria-current` | 2/61 view ✓, 59 eksik |
| Hero `<div class="hero">` | Çoğu view ✓ |
| Empty state dashed+48px ikon | Tutarlı ama bazı view'larda eksik |
| Subnav `aria-current` aktif tab | 0 view ✓ |
| Form section card action row | Çoğu ✓, BrandSettings/Modules sapma |
| `<html lang="tr">` | ✓ |
| Skip-to-content link | **YOK** — eklenmeli |

**Bilinen sapmalar** (tarama 2026-05-08):
- `_AppLayout.cshtml` → sidebar `role="navigation"` eksik, breadcrumb `<div>` legacy
- `Auth/Login.cshtml:49` → inline `width:100%; height:38px` → `.btn.primary.block`
- `Admin/Index.cshtml:29` → inline subnav, breadcrumb section yok
- 19 view → hardcoded inline rgba renkleri

---

## 6. Dosya Yazarken Checklist (Kopyala-Yapıştır)

Yeni `.cs` dosyası yazıyorsan:

- [ ] `ILogger<T>` inject edildi mi?
- [ ] Her catch bloğunda `_logger.LogError/Warning` var mı?
- [ ] Bare `catch {}` veya `catch { return default; }` yok mu?
- [ ] `ex.Message` kullanıcıya (TempData/ServiceResult) sızmıyor mu?
- [ ] `SaveChangesAsync()` try/catch ile korunuyor mu?
- [ ] POST action'da `[ValidateAntiForgeryToken]` var mı?
- [ ] Entity direkt model binding yerine DTO/ViewModel mi kullanılıyor?
- [ ] Route `id` param mı kullanılıyor, form binding PK mi? (IDOR)
- [ ] `Console.WriteLine` yok mu? (`_logger.LogDebug` kullan)
- [ ] Türkçe mesajlar UTF-8 mi? (ı/İ/ş/ğ/ü/ö/ç kontrol)
- [ ] `[BindNever]` PK/sensitive alanlarda var mı?

Yeni `.cshtml` view yazıyorsan:

- [ ] `@Html.AntiForgeryToken()` her POST form'da var mı?
- [ ] `@Html.Raw` kullanıyorsan — user input değil, admin-controlled mı? (rich text → HTML sanitizer zorunlu)
- [ ] Inline JS'de user data interpolasyonu (XSS) yok mu?
- [ ] TempData null guard: `@if (TempData["Message"] != null)` var mı?
- [ ] Türkçe UTF-8 (Düzenle/Bölüm/İçerik, ASCII değil)
- [ ] iframe `sandbox` → `allow-same-origin` YOK mu? (XSS izolasyonu kırılır)
- [ ] `@Html.Raw(Model.SearchTerm)` gibi user input raw render yok mu?

Yeni `.js` dosyası yazıyorsan:

- [ ] IIFE `(() => { ... })()` sarmalı var mı? (global scope pollution)
- [ ] `innerHTML` + string concat yerine `createElement` + `textContent` mi?
- [ ] `eval()` yok mu?
- [ ] fetch/XHR'da `.catch()` error handler var mı? (sessiz failure yasak)
- [ ] 350 satır hard limit aşılmıyor mu?
- [ ] Inline `onclick` attribute yok mu? (`addEventListener` kullan)
- [ ] Ortak util fonksiyon (escHtml, fmtCell) varsa tekrar yazmak yerine import/paylaş
- [ ] CSRF token (`RequestVerificationToken`) POST fetch'lerde gönderiliyor mu?

Yeni `.cs` model/entity yazıyorsan:

- [ ] `BaseEntity` inherit ediyor mu? (CreatedAt/UpdatedAt/CreatedBy otomatik)
- [ ] ViewModel'de entity direkt expose edilmiyor mu? (ConnString gibi sensitive alanlar)
- [ ] `[BindNever]` PK + FK + sensitive property'lerde var mı?
- [ ] `IAuditable` interface (planlı) uygulanabilir mi?

---

## 7. Bilinen Sorunlu Dosyalar (Düzeltme Sırası)

### CRITICAL — İlk fırsatta düzelt

| Dosya | Sorun | Çözüm |
|---|---|---|
| `CreateReportV2.cshtml:879` | iframe `allow-same-origin` — XSS izolasyonu kırık | `sandbox="allow-scripts"` (allow-same-origin SİL) |
| `EditReportV2.cshtml:969` | iframe `allow-same-origin` — XSS izolasyonu kırık | `sandbox="allow-scripts"` (allow-same-origin SİL) |
| `Reports/Index.cshtml:44` | `@Html.Raw(Model.SearchTerm)` — reflected XSS | `@Model.SearchTerm` (Razor auto-encode) |
| `UserDataFilter.cs:9,11,26` | FilterId/UserId/CreatedAt `[BindNever]` eksik | `[BindNever]` ekle |
| `AdminController.DataSources.cs:26,103` | DataSource entity direkt binding (ConnString dahil!) | DTO/ViewModel veya [Bind] |
| `AdminController.RolesGroups.cs:33` | EditRole IDOR — RoleId form'dan | FindAsync(id) kullan |
| `builder-render.js` | 459 satır — hard limit 350 aşımı | KPI render'ı ayrı dosyaya split |
| `admin-simple.js` | IIFE yok — global scope pollution | `(() => { ... })()` sar |
| `dashboard-builder/*.js` | inline `onclick` attribute (3+ dosya) | `addEventListener` + closure |
| `sp-helper.js` | `innerHTML` ile SP sonuç render — XSS | `createElement` + `textContent` |
| `BrandSettingsService.cs:40` | bare catch, logger yok | ILogger inject + LogWarning |
| `ModuleService.cs:47` | bare catch, logger yok | ILogger inject + LogWarning |
| `SpExplorerService.cs:316` | bare catch, static method | instance method'a çevir + logger |
| `BlockFileService.cs:103` (Circular) | ex.Message leak | generic mesaj + logger |
| `CircularSummaryService` (Circular) | ex.Message leak | generic mesaj |

### HIGH — Bu sprint içinde düzelt

| Dosya | Sorun | Çözüm |
|---|---|---|
| 5 Circular view (Details/Print/Index/Edit/Create) | `@Html.Raw(Model.Content)` — stored XSS riski | HTML sanitizer (HtmlSanitizer NuGet veya whitelist) |
| Core Models (12+ entity) | `BaseEntity` inherit etmiyor — CreatedAt/UpdatedAt tutarsız | Mevcut entity'leri BaseEntity'ye migrate |
| 6+ ViewModel | `Message`/`MessageType` tekrar — TempData pattern | Shared base class veya TempData helper |
| `DashboardViewModel.cs` | Hard `Circular` dependency — modüler değil | Interface/abstraction ile decouple |
| `builder-v2/*.js` (3 dosya) | `escHtml` 4x tekrar tanımlanmış | Shared `util.js` modülüne çıkar |
| `builder-v2/*.js` (3 dosya) | `fmtCell`/`fmtChart` 3x tekrar | Shared formatter modülüne çıkar |
| `builder-v2/*.js` (5+ dosya) | fetch `.catch()` eksik — sessiz failure | Her fetch'e `.catch(err => ...)` |
| `dashboard-builder/builder-core.js` | `innerHTML` ile widget render | DOM API'ye geç |
| `ReportParamValidator.cs:76` | bare catch, boş liste | logger + hata flag |
| `FilterOptionsService.cs:70` | success on failure | success=false dön |
| `AuditLogService.cs:50` | SPoF, SaveChanges korumasız | try/catch izole |
| `StoredProcedureExecutor.cs:16-91` | sıfır error handling | try/catch + spesifik SqlException |
| `AdminController.Reports.cs:45` | Console.WriteLine | sil veya LogDebug |
| `AdminController.DataSources.cs:32` | Console.WriteLine | sil veya LogDebug |
| `ReportsController.Run.cs:142` | audit log'a ex.Message | generic description |
| `BlockService.cs` (Circular) | 8+ satır ASCII Türkçe | UTF-8 düzelt |
| `CompileCircularJob.cs:82` | bare catch | logger + LogWarning |

### MEDIUM — Dokunduğunda düzelt

| Dosya | Sorun |
|---|---|
| `UserRoleSyncService.cs:38` | SaveChanges korumasız, partial-save riski |
| `ApprovalService.cs:55,117` | SaveChanges korumasız, logger yok |
| `DashboardRenderer.cs:115` | Formula hata sessiz DBNull |
| `AiSummaryProvider.cs:63` | ex.Message result'ta |
| `AdminController.AiSettings.cs:166` | AI test race condition |
| `CircularController.cs:123` | bare catch JSON parse |
| `ModuleLoader.cs` | Silent exception swallow (modül yüklenemezse sessiz) |
| `builder-v2/*.js` | CSRF token POST fetch'lerde eksik bazı yerlerde |

---

## 8. Bu Skill Nasıl Kullanılır

1. **Kod yazarken:** §6 checklist'i kontrol et
2. **Yeni servis yazarken:** §1 ILogger + exception pattern'i kopyala
3. **View yazarken:** §5 UI/A11y + §3 Türkçe + §2 güvenlik kontrol
4. **Handoff öncesi:** 3 paralel agent (code-reviewer + silent-failure-hunter + security-review) çalıştır — bu skill yazım sırasındaki ilk savunma, agent'lar son doğrulama
5. **Bug fix yaparken:** §7 tablosunda dosya varsa, fix sırasında oradaki sorunu da düzelt
6. **UI değişikliğinde:** `accessibility-compliance` + `ui-ux-pro-max` skill otomatik tetikle

**Güncelleme:** Her taramada yeni pattern çıkarsa bu dosyaya ekle. Stale satır fark edersen (düzeltilmiş sorun) sil.
