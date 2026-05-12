---
name: mosaik-csharp-razor
description: Mosaik projesi C# (ASP.NET Core MVC + EF Core 10) ve Razor view standardı için uzman skill. Yeni controller action / view / ViewModel / service / migration yazılırken zorunlu tetiklenir. AntiForgery + EF AsNoTracking + SqlCommand SP pattern + raw form + form-section-card + Türkçe UI + güvenlik (ex.Message user'a gizleme, XSS, mass assignment) garanti eder.
---

# Mosaik C# + Razor Uzman Skill'i

## Ne zaman tetiklenir

- Yeni Controller action yazılırken (GET/POST)
- Yeni Razor view (.cshtml) yazılırken / refactor edilirken
- Yeni ViewModel oluştururken
- Yeni Service / partial controller eklerken
- EF query yazılırken (DbContext kullanımı)
- SQL Server stored procedure çağıran kod yazılırken
- Yeni migration ya da SQL script
- `code-quality-checklist` / `code-reviewer` öncesinde önleyici tarama

## Mosaik mimari kuralları (kısa)

- **Web:** ASP.NET Core MVC + Razor (NOT Pages). `Layout = "_AppLayout";`.
- **EF:** Mosaik metadata için (`MosaikContext`). Dapper yok.
- **SP execution:** ADO.NET `SqlCommand` + `CommandType.StoredProcedure` + `SqlParameter`. String concat yasak.
- **Auth:** Cookie authentication + `[Authorize(Roles = "admin")]` class-level admin controller.
- **DB:** SQL Server `Mosaik` (BT-FIKRI\SQLEXPRESS). Migration sqlcli `script` ile (EF Migrations YOK).

## Controller action zorunlulukları

```csharp
[Authorize(Roles = "admin")]
public partial class AdminController : Controller
{
    private readonly MosaikContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IAuditLog _auditLog;

    public AdminController(MosaikContext context, ILogger<AdminController> logger, IAuditLog auditLog)
    {
        _context = context; _logger = logger; _auditLog = auditLog;
    }

    [HttpGet]
    [Route("Admin/EditUser/{id:int}")]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _context.Users
            .AsNoTracking()                                  // okuma → tracking yok
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null) return NotFound();

        var vm = new EditUserViewModel { /* mapping */ };    // Entity'yi DİREKT View'a verme
        return View(vm);
    }

    [HttpPost]
    [Route("Admin/EditUser/{id:int}")]
    [ValidateAntiForgeryToken]                                // POST'ta ZORUNLU
    public async Task<IActionResult> EditUser(int id, EditUserViewModel input)
    {
        // input'tan id alma — [BindNever] route'tan
        if (!ModelState.IsValid) return View(input);

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            // explicit field mapping (mass assignment koruma)
            user.Username = input.Username;
            user.Email = input.Email;
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync("user_update", "user", id.ToString(),
                description: $"User {user.Username} updated");

            TempData["Message"] = "Kullanıcı güncellendi.";
            TempData["MessageType"] = "success";
            return RedirectToAction("Index", new { tab = "users" });
        }
        catch (SqlException sex)
        {
            _logger.LogError(sex, "EditUser DB error id={Id}", id);
            TempData["Message"] = "Veritabanı hatası oluştu.";  // user-dostu, ex.Message GÖSTERME
            TempData["MessageType"] = "error";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EditUser unexpected error id={Id}", id);
            TempData["Message"] = "Beklenmedik bir hata. Sistem yöneticisine bildirin.";
            TempData["MessageType"] = "error";
        }

        return View(input);
    }
}
```

**Zorunlu:**
- `async Task<IActionResult>` (async void asla)
- POST `[HttpPost]` + `[ValidateAntiForgeryToken]`
- `[Route("Admin/...")]` explicit (convention-based fallback varsa OK)
- `.AsNoTracking()` her okuma query
- Exception specific → generic sıralı, **`ex.Message` user'a göstermek YASAK** (SqlException → connection string sızar)
- `await _auditLog.LogAsync(...)` her kritik aksiyon (create/update/delete/login/export)
- `TempData["Message"]` + `TempData["MessageType"]` (success/warning/error/info)

## SP execution pattern

```csharp
public async Task<List<DataTable>> ExecuteSpAsync(string spName, IReadOnlyList<SqlParameter> parameters, CancellationToken ct)
{
    var results = new List<DataTable>();
    using var conn = new SqlConnection(_connString);
    await conn.OpenAsync(ct);
    using var cmd = new SqlCommand(spName, conn)
    {
        CommandType = CommandType.StoredProcedure,
        CommandTimeout = 120                                  // büyük rapor için
    };
    cmd.Parameters.AddRange(parameters.ToArray());            // SqlParameter — string concat YASAK
    using var reader = await cmd.ExecuteReaderAsync(ct);
    do { var dt = new DataTable(); dt.Load(reader); results.Add(dt); }
    while (!reader.IsClosed && reader.NextResult());
    return results;
}
```

`procName` ASLA user-input. `SqlDbType` explicit (varsayım yok). `CommandTimeout` 120 (büyük SP).

## ViewModel zorunlulukları

- Entity direkt View'a verme — DTO/VM ile flat properties
- `[BindNever]` kritik alanlar: `UserId`, `PasswordHash`, `ReportId` (route'tan gelir, body'den DEĞİL)
- `[Required]`, `[MaxLength]`, `[EmailAddress]` validation attribute'ları
- `UserMessageViewModel` base sınıfı (Message + MessageType) varsa onu extend et
- File: `Mosaik/ViewModels/<Domain><Action>ViewModel.cs`

## Razor view standardı

**İskelet:**

```cshtml
@model Mosaik.ViewModels.XyzViewModel
@{
    Layout = "_AppLayout";
    ViewData["Title"] = "Sayfa Adı - Mosaik";
    ViewData["PageTitle"] = "Sayfa Adı";
}

@section Breadcrumb {
    <nav class="crumbs" aria-label="Sayfa konumu">
        <a href="/Admin">Yönetim</a>
        <span class="sep" aria-hidden="true">/</span>
        <span aria-current="page">@ViewData["PageTitle"]</span>
    </nav>
}

@section TopActions {
    <a href="/Admin?tab=users" class="btn">
        <i class="fas fa-xmark icon-xs"></i> İptal
    </a>
}

<div class="hero">
    <h1>Sayfa Adı</h1>
    <div class="sub"><span>Kısa açıklama</span></div>
</div>

@if (!string.IsNullOrEmpty(Model.Message))
{
    @await Html.PartialAsync("_AlertMessage", (Model.MessageType ?? "info", Model.Message))
}

<form method="post" action="/Admin/EditUser/@Model.UserId" novalidate>
    @Html.AntiForgeryToken()

    <div class="form-section-card wide">
        <div class="form-section-head">
            <h2 class="form-section-head__title">Bilgiler</h2>
        </div>
        <div class="form-section-body stack">
            <div class="field">
                <label class="lab" for="Username">Kullanıcı Adı <span class="text-danger">*</span></label>
                <input id="Username" name="Username" value="@Model.Username" class="inp" required maxlength="50" />
                <div asp-validation-for="Username" class="text-danger"></div>
            </div>
        </div>
    </div>

    <div class="action-row wide">
        <a href="/Admin?tab=users" class="btn ghost">
            <i class="fas fa-arrow-left icon-xs"></i> Geri Dön
        </a>
        <button type="submit" class="btn primary">
            <i class="fas fa-save icon-xs"></i> Kaydet
        </button>
    </div>
</form>
```

**Zorunlu:**
- `Layout = "_AppLayout";` (auth dışı sayfa istisna)
- Raw `<form method="post">` + `@Html.AntiForgeryToken()` — **`Html.BeginForm` yasak** (21/24 view raw)
- POST'ta `@Html.AntiForgeryToken()` zorunlu
- `style="..."` inline attribute **sıfır tolerans yasak** (istisna: `display:contents`, `--w:@pct%`, `Print.cshtml` view-local `<style>` — Plan 25.1)
- Razor `style="@(cond ? "x" : "y")"` yerine class modifier `.pill.ok-tone / .pill.warn / .pill.danger`
- `@Html.Raw` minimum, user input ise yasak (XSS); admin-controlled için yorum: `@* Raw: admin-only, sanitize edilmedi *@`
- `<html lang="tr">` `_AppLayout.cshtml`'de
- Türkçe UTF-8 ("Düzenle", "İptal" — ASCII sadeleştirme yok)
- Open redirect: `Url.IsLocalUrl(returnUrl)` **yetmez**, ek `returnUrl.StartsWith("/") && !returnUrl.StartsWith("//")`

## Utility class kullanımı (mosaik-css-expert ile birlikte)

- `.btn` + `.primary/.ghost/.sm/.lg/.danger/.block/.icon-btn`
- `.field` + `.lab` + `.inp` + `.text-danger`
- `.form-section-card.wide/.narrow` + `.form-section-head__title/__sub` + `.form-section-body.stack`
- `.action-row.wide/.narrow` + `.with-hint`
- `.alert.success/.warn/.error` + `_AlertMessage` partial
- `.pill` + `.dot` + `.ok-tone/.warn/.danger/.info-tone/.urgent`
- `.crumbs`, `.hero`, `.mono`, `.ago`, `.status`
- `.checkbox-grid` + `.checkbox-row` + `.checkbox(.checkbox-lg)`
- `.toggle-row`, `.toggle-label`
- `.help-text(.error|.help-success)`, `.help-card`
- `.empty-state` + `.empty-state__icon`
- Tablo: `.dt` + `.mono` + `.ago` + `.status.ok/.warn/.err`
- Plan 25.1 ile **modüller-arası tek source-of-truth**: `components.css`. Modül-özel istisnai.

## Güvenlik checklist (her commit öncesi)

- [ ] POST action `[ValidateAntiForgeryToken]` var
- [ ] EF read query `.AsNoTracking()` var
- [ ] SP çağrısı `CommandType.StoredProcedure` + `SqlParameter` (string concat yok)
- [ ] Exception specific önce → generic sonra; user'a generic Türkçe mesaj
- [ ] `_auditLog.LogAsync(...)` kritik aksiyonda var
- [ ] ViewModel → Entity explicit mapping (mass assignment koruma)
- [ ] `[BindNever]` PK + sensitive alanlar
- [ ] Inline `style="..."` yok view'da
- [ ] `@Html.Raw` user-input ile değil
- [ ] Türkçe UTF-8 (Düzenle değil Duzenle)
- [ ] `[Authorize(Roles = "admin")]` admin controller class-level

## Bilinen anti-pattern'ler (yasak)

- `async void` (event handler hariç)
- `new HttpClient()` (yerine `IHttpClientFactory`)
- `DateTime.Now` (UTC yerine `DateTime.UtcNow`)
- `ex.Message` kullanıcıya gösterme
- Connection string `appsettings.json` plain-text (env var / User Secrets / Azure Key Vault)
- Property injection (constructor only)
- `Html.BeginForm` (raw `<form>` standart)
- `style="..."` HTML attribute (inline-style-guard sıfır tolerans)
- `[ReportType]` kolonuna kod bakması (`[Obsolete]`, ADR-009 tek path)
- `_DashboardHtml` DROP edildi (config-driven JSON tek source)

## Hızlı referans (sık bakılan)

- `Mosaik/Controllers/AdminController.*.cs` — partial split admin (8 dosya)
- `Mosaik/Services/AuditLogService.cs` — audit log impl
- `Mosaik/Services/DashboardRenderer.cs` — dashboard render
- `Mosaik/Models/MosaikContext.cs` (`Mosaik/Models/ReportPanelContext.cs`) — DbContext
- `.claude/rules/architecture.md` — mimari + tutarsızlık tablosu
- `.claude/rules/csharp-conventions.md` — C# detay
- `.claude/rules/razor-conventions.md` — Razor detay
- `.claude/rules/security-principles.md` — güvenlik
- `.claude/rules/turkish-ui.md` — UI dil + sözlük
- `.claude/rules/inline-style-guard.md` — inline yasak + istisna
- `.claude/rules/before-major-change.md` — silme/refactor öncesi 9 adım
- `docs/ARCHITECTURE_MAP.md` — V1/V2 deprecated tablosu + sidebar canonical map

## İlişkili skill'ler

- `mosaik-css-expert` — UI utility kullanım, generic vs modül-özel
- `mosaik-js-expert` — vanilla JS + AntiForgery + fetch + Alpine
- `sql-migration-writer` — SQL migration idempotent yazım
- `code-quality-checklist` — geniş kod kalite checklist
- `css-classify` — yeni utility yer kararı
