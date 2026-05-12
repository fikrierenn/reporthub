---
name: mosaik-security
description: Mosaik projesi güvenlik uzman skill'i. Yeni Controller action / POST endpoint / SQL execution / SP wrapper / email & notification gönderimi / file upload / JS fetch yazılırken **zorunlu** tetiklenir. AntiForgery + parameterized SP + HtmlEncode + secret-out-of-source + generic Türkçe exception + audit log + multi-tenant filter bypass koruma + open redirect kontrolü garanti eder. `security-principles.md` 10 kuralın proaktif uygulayıcısı (security-reviewer agent ise denetleyici).
---

# Mosaik Güvenlik Uzman Skill'i

## Ne zaman tetiklenir (otomatik)

- Yeni `[HttpPost]` / `[HttpDelete]` / `[HttpPut]` action eklenirken
- `SqlCommand`, `FromSqlRaw`, `ExecuteSqlRaw`, `Database.SqlQuery` kullanılırken
- `@Html.Raw`, `Html.Raw(`, `JsonSerializer.Serialize` Razor view'da yazılırken
- JS `fetch(`, `XMLHttpRequest`, `axios` (varsa) çağrısı eklenirken
- `IEmailService.SendAsync`, `INotificationService.Send` çağrısı eklenirken
- `IFormFile`, `FileStreamResult`, `Path.Combine(`, `File.OpenRead` ile dosya işlemi yazılırken
- `appsettings.json`, `appsettings.*.json` düzenlerken (secret risk)
- `cookieOptions`, `AddAuthentication`, `AddAuthorization` Program.cs düzenlemesi
- `[Authorize]`, `AllowAnonymous`, role check kodu değişirken
- `UserDataFilter`, `IUserDataScope`, `InjectUserDataFilters` çağrısı eklenirken
- `Redirect(`, `LocalRedirect(`, `Url.IsLocalUrl` kullanırken (open redirect)
- `eval(`, `new Function(`, `innerHTML +=`, `outerHTML =` JS'te (yasak)
- Yeni `migration` (`Database/NN_*.sql`) tabloya CHECK constraint / FK eklemeden

## Bağlam — `.claude/rules/security-principles.md` 10 mutlak kural

Aşağıdaki her bölüm o dosyadaki kurala karşılık gelir. Detaya gitmeden önce **o dosyayı oku**.

---

## Kural 1 — SQL injection (SP + SqlParameter zorunlu)

**Yasak:** `string.Concat`, `$"..."` interpolation, `+` ile SQL string oluşturma.

**Tek doğru pattern:**

```csharp
using var cmd = new SqlCommand(spName, conn)
{
    CommandType = CommandType.StoredProcedure,
    CommandTimeout = 120
};
cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.Int) { Value = companyId });
cmd.Parameters.Add(new SqlParameter("@FilterValue", SqlDbType.NVarChar, 100) { Value = filterValue ?? (object)DBNull.Value });
```

**Anti-pattern (tespit edersen — düzelt + raporla):**

```csharp
// YASAK
var cmd = new SqlCommand($"EXEC {spName} @p='{userInput}'", conn);     // SP adı + param concat
var sql = $"SELECT * FROM Users WHERE Username = '{user}'";              // raw concat
_context.Database.ExecuteSqlRaw($"DELETE FROM Logs WHERE Id={id}");      // SqlRaw + interpolation
```

**Dinamik liste:** SQL Server `STRING_SPLIT` + parametrized:
```sql
WHERE (@filter IS NULL OR col IN (SELECT value FROM STRING_SPLIT(@filter, ',')))
```

**`SqlDbType` explicit zorunlu.** ADO.NET çıkarımı string'lerde nvarchar(max) yapar → SP plan cache şişer + index seek miss.

---

## Kural 2 — XSS (HtmlEncode + DOM API)

**Razor view:**
- Default `@` syntax = otomatik HtmlEncode. **Kullan.**
- `@Html.Raw(x)` user input ile **yasak**. Admin-controlled içerik için yorum: `@* Raw: admin-only, sanitize edilmedi *@`
- `Json.Serialize` view içinde sadece `@Html.Raw(System.Text.Json.JsonSerializer.Serialize(model))` pattern'i, ardından client-side `JSON.parse` ile data attribute'a koy. `<script>` içine raw JSON gömme yasak.

**JS / DashboardRenderer / EmailTemplates:**
- `createElement` + `textContent` zorunlu.
- `innerHTML = "<div>" + userInput + "</div>"` **yasak**.
- `innerHTML` sabit string (kullanıcı input yok) OK ama tutarlılık için `createElement` tercih.

**Email HTML template (Plan 31 `EmailTemplates.cs` pattern):**
```csharp
private static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

public static string OverdueObligation(string title, string firmaName, string appUrl)
{
    var t = E(title);
    var f = E(firmaName);
    // appUrl config'den gelir ama yine de güvenli olsun:
    var u = Uri.IsWellFormedUriString(appUrl, UriKind.Absolute) ? E(appUrl) : "#";
    return $$"""<tr><td>{{t}} — {{f}}</td><td><a href="{{u}}/Contracts">Aç</a></td></tr>""";
}
```

C# `$$"""..."""` **HtmlEncode DEĞİL** — sadece raw string syntax. Her placeholder elle encode et.

**Server-generated HTML (DashboardRenderer):** XSS audit edildi — `createElement` + `textContent` pattern. Yeni renderer eklerken aynı pattern. `</script>` regex kaçırma (Renderer.cs içindeki örnek bak).

---

## Kural 3 — CSRF (AntiForgery zorunlu)

**Razor view (raw form):**
```cshtml
<form method="post" action="/Admin/EditUser/@Model.UserId" novalidate>
    @Html.AntiForgeryToken()
    ...
</form>
```

**Controller:**
```csharp
[HttpPost]
[Route("Admin/EditUser/{id:int}")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditUser(int id, EditUserViewModel input) { ... }
```

**JS fetch POST (Plan 25.2 global helper):**
```js
fetch('/Admin/SomeAction', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': window.getAntiForgeryToken()
    },
    body: JSON.stringify(payload)
})
.then(r => { if (!r.ok) throw new Error('HTTP ' + r.status); return r.json(); })
.catch(err => { console.error('Action failed', err); showToast('error', 'İşlem başarısız oldu.'); });
```

**`window.getAntiForgeryToken()` cache'i `null`/empty `''` ayrımı:**
- Boş string cache'e koyma — `if (__aftCache) return __aftCache;` (truthy kontrolü).
- `_AppLayout.cshtml`'de `@Html.AntiForgeryToken()` zaten render olmalı (global hidden input) — sadece form içinde değil.

**İstisna yok.** `TestController` POST'larında bile (dev mode + `#if DEBUG`) eklenir.

---

## Kural 4 — Open redirect

```csharp
public IActionResult LoginSuccess(string returnUrl)
{
    if (!string.IsNullOrEmpty(returnUrl)
        && Url.IsLocalUrl(returnUrl)
        && returnUrl.StartsWith("/")
        && !returnUrl.StartsWith("//"))
    {
        return LocalRedirect(returnUrl);
    }
    return RedirectToAction("Index", "Dashboard");
}
```

**`Url.IsLocalUrl` tek başına yetmez** — `//evil.com` veya `\\evil.com` bypass eder bazı ASP.NET sürümlerinde. İki ek check zorunlu.

**Legacy redirect (V1→V2) örneği:** id geçersizse generic Index'e dön, exception fırlatma:
```csharp
public async Task<IActionResult> EditReportLegacyRedirect(int id)
{
    var exists = await _context.ReportCatalog.AsNoTracking().AnyAsync(r => r.ReportId == id);
    if (!exists)
    {
        TempData["Message"] = "Aradığınız rapor bulunamadı.";
        TempData["MessageType"] = "warning";
        return RedirectToAction("Index", new { tab = "reports" });
    }
    return RedirectToAction(nameof(EditReportV2), new { id });
}
```

---

## Kural 5 — Secret yönetimi

**Yasak:**
- `appsettings.json` plain-text password, API key, connection string credential
- Git'e commitlenmiş `appsettings.Development.json` içinde gerçek SA şifresi
- `TestController` production'da açık

**Doğru:**
- `dotnet user-secrets set "SmtpSettings:Password" "..."` (dev local)
- Environment variable: `ASPNETCORE_SmtpSettings__Password` (prod)
- Azure Key Vault (cloud prod, gelecek)
- `appsettings.json` placeholder: `"Password": ""` + README/INSTALL.md notu

**Tetik kelimeler (commit öncesi tara):** `password`, `secret`, `apikey`, `token`, `connectionstring` → değer non-empty mi? Yorum/placeholder mi?

**`TestController.cs`:**
```csharp
#if DEBUG
[Route("Test/[action]")]
[Authorize(Roles = "admin")]                          // dev'de de auth
public class TestController : Controller { ... }
#endif
```

---

## Kural 6 — Cookie sertleştirme

```csharp
services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });
```

Mevcut `Program.cs` zaten doğru. **Bu blok elden geçirilirse** her flag tekrar doğrula.

---

## Kural 7 — Exception handling (generic Türkçe mesaj)

```csharp
try
{
    await _context.SaveChangesAsync();
    await _auditLog.LogAsync("user_update", "user", id.ToString(), description: "...");
    TempData["Message"] = "Kullanıcı güncellendi.";
    TempData["MessageType"] = "success";
    return RedirectToAction("Index");
}
catch (SqlException sex)
{
    _logger.LogError(sex, "EditUser DB error id={Id}", id);
    TempData["Message"] = "Veritabanı işleminde hata oluştu.";
    TempData["MessageType"] = "error";
}
catch (Exception ex)
{
    _logger.LogError(ex, "EditUser unexpected error id={Id}", id);
    TempData["Message"] = "Beklenmedik bir hata. Lütfen sistem yöneticisine bildirin.";
    TempData["MessageType"] = "error";
}
```

**`ex.Message` user'a YASAK.** SqlException → connection string sızar, EF exception → schema sızar.

**Silent failure de yasak** (silent-failure-hunter agent paralel scope):
- Boş `catch { }` yasak
- `catch (Ex) { return null; }` yasak — caller başarısızlığı göremez
- Email/notification gibi non-critical path bile **audit log `email_send_failed` event** yazılır
- `Task` dönen method exception yutuyorsa → `Task<Result<T>>` veya `Task<bool>` pattern

---

## Kural 8 — Multi-tenant `UserDataFilter`

**Whitelist regex:**
- `FilterKey`: `^[a-zA-Z_][a-zA-Z0-9_]*$`
- `FilterValue`: `^[a-zA-Z0-9,_\- ]+$`

**SP tarafı (NULL-guard zorunlu):**
```sql
WHERE (@p IS NULL OR col IN (SELECT value FROM STRING_SPLIT(@p, ',')))
```

**`InjectUserDataFilters` bypass yolu olmamalı:** her SP execution path'ı (Run, Export, Preview) `UserDataFilterInjector.InjectAsync()` çağırmalı. Yeni execution endpoint eklerken bunu kanıtla (kod referansı: `Services/UserDataFilterInjector.cs`).

---

## Kural 9 — Password hashing

- **PBKDF2** + 100k iteration (`PasswordHasher.CreateHash`)
- **`CryptographicOperations.FixedTimeEquals`** (timing-safe compare)
- BCrypt / Argon2 ekleme → ayrı ADR + migration planı
- Plain SHA / MD5 / no-salt → **YASAK**

---

## Kural 10 — Dashboard iframe sandbox

```cshtml
<iframe sandbox="allow-scripts"
        srcdoc="@Html.Raw(DashboardRenderer.Render(config, results))">
</iframe>
```

**`allow-same-origin` ASLA ekleme** — XSS izolasyonu çöker (iframe içindeki script parent DOM'a erişir).

---

## Audit log kapsamı (Kural ek)

Her kritik aksiyon `await _auditLog.LogAsync(eventType, entity, entityId, description)`:

- `user_create`, `user_update`, `user_delete`, `user_password_change`
- `role_create`, `role_update`, `role_delete`
- `report_create`, `report_update`, `report_delete`
- `datasource_create`, `datasource_update`, `datasource_delete`
- `category_create`, `category_update`, `category_delete`
- `login_success`, `login_failed`, `logout`
- `export_xlsx`, `export_csv`, `export_pdf`
- `dashboard_config_missing` (fallback yakalama)
- `email_send_failed`, `email_send_skipped` (Plan 31)
- `legacy_redirect_invalid_id` (Plan 33 V1→V2 redirect)

Audit kapsam değişikliği → `security-principles.md` "Audit Log Kapsamı" listesini güncelle.

---

## File upload (eklendiğinde)

```csharp
const long MaxBytes = 10 * 1024 * 1024;                                   // 10 MB
var allowed = new[] { ".pdf", ".docx", ".xlsx", ".png", ".jpg" };
var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

if (file.Length == 0 || file.Length > MaxBytes)
    return BadRequest("Dosya boyutu uygun değil.");
if (!allowed.Contains(ext))
    return BadRequest("Dosya tipi desteklenmiyor.");

// path traversal koruması — kullanıcı adı KULLANMA, GUID
var safeName = $"{Guid.NewGuid():N}{ext}";
var dest = Path.Combine(_uploadRoot, safeName);                            // _uploadRoot env'den
if (!Path.GetFullPath(dest).StartsWith(_uploadRoot, StringComparison.Ordinal))
    return BadRequest("Geçersiz yol.");

using var stream = System.IO.File.Create(dest);
await file.CopyToAsync(stream, ct);
```

**Yasak:** `Path.Combine(_uploadRoot, file.FileName)` (path traversal — `../../../../etc/passwd`).
**MIME sniffing:** Trust `Content-Type` header ETME — extension whitelist + magic byte check (PDF magic `%PDF`, PNG `89 50 4E 47`).

---

## CSP / inline-script

Mosaik şu an CSP header set etmiyor (gelecek hardening). Şimdiden hazırlık:
- Inline `<script>` minimum — external `.js` file tercih
- Inline `style="..."` yasak (`inline-style-guard.md` zaten zorluyor)
- `'unsafe-inline'`, `'unsafe-eval'` gelecek CSP'de OLMAYACAK — kod buna göre yazılsın

---

## Checklist (yeni POST endpoint için)

Action yazmadan önce zihinden geçir:

- [ ] `[HttpPost]` + `[ValidateAntiForgeryToken]` var
- [ ] `[Authorize(Roles = "...")]` class- veya action-level var
- [ ] Input `ViewModel` (Entity direkt değil) + `[BindNever]` kritik alanlarda
- [ ] `ModelState.IsValid` kontrol + invalid → View(input)
- [ ] EF mutation `try/catch (SqlException) → (Exception)` + generic Türkçe mesaj
- [ ] `_auditLog.LogAsync(...)` + ilgili event type
- [ ] Redirect target user-input ise open redirect 3-check
- [ ] Response asla `ex.Message` içermiyor
- [ ] SP çağrısı ise `CommandType.StoredProcedure` + `SqlParameter` + explicit `SqlDbType`
- [ ] User data filter inject ediliyor (multi-tenant ise)

## Checklist (yeni JS fetch POST için)

- [ ] `window.getAntiForgeryToken()` çağrılıyor
- [ ] Token boş ise erken çık + user feedback (sessiz fail yok)
- [ ] `.then(r => { if (!r.ok) throw ... })` + `.catch(...)`
- [ ] Error toast / inline mesaj Türkçe generic, `err.message` raw göstermiyor
- [ ] `console.error(err)` debug için (production'da log toplanır)

## Checklist (yeni view + form için)

- [ ] `Layout = "_AppLayout";`
- [ ] Raw `<form method="post">` + `@Html.AntiForgeryToken()` (Html.BeginForm yasak)
- [ ] `@Html.Raw(userInput)` yok
- [ ] Inline `style="..."` yok (whitelist istisnaları hariç)
- [ ] Türkçe UTF-8 mesajlar (`turkish-ui.md`)

## Checklist (secret / config / migration için)

- [ ] `appsettings.json` plain-text password yok
- [ ] Yeni migration tablo'sunda whitelist kolon için CHECK constraint
- [ ] Yeni FK'da ON DELETE davranışı belirtildi (CASCADE / NO ACTION / SET NULL)
- [ ] PII alan ekledin mi? → ADR şart (TC, telefon, email saklama kararı)

## Anti-pattern toplu tarama (commit öncesi)

```bash
# Bash / ripgrep ile hızlı tarama
grep -rn 'ex\.Message' Mosaik/ Mosaik.Core/ Mosaik.Modules.*/ --include='*.cs'
grep -rn 'Html\.Raw' Mosaik/Views Mosaik.Modules.*/Areas --include='*.cshtml'
grep -rn 'innerHTML\s*=\|innerHTML\s*+=' Mosaik/wwwroot/assets/js
grep -rn 'eval(\|new Function(' Mosaik/wwwroot/assets/js
grep -rn 'CommandText\s*=' Mosaik/ --include='*.cs'   # SP yerine ad-hoc SQL?
grep -rn '\[HttpPost\]' Mosaik/Controllers --include='*.cs' -A 3 | grep -B 1 -v 'ValidateAntiForgeryToken'
```

Bulgu varsa **düzelt** + `security-reviewer` agent çağır.

---

## İlişkili dosyalar

- `.claude/rules/security-principles.md` — 10 mutlak kural (anayasa)
- `.claude/rules/architecture.md` — bilinen tutarsızlıklar + `AsNoTracking` disiplin
- `.claude/rules/csharp-conventions.md` + `razor-conventions.md` + `js-conventions.md`
- `.claude/agents/security-reviewer.md` — denetleyici agent (sen proaktif, o denetleyici)
- `.claude/agents/silent-failure-hunter.md` — error handling silent fail (paralel scope)
- `.claude/skills/mosaik-csharp-razor/SKILL.md` — C# / Razor pattern (sen güvenlik katmanı)
- `.claude/skills/mosaik-js-expert/SKILL.md` — JS pattern (AntiForgery + XSS sertleştirme)
- `.claude/commands/security-check.md` — multi-agent security review slash command
- `Mosaik/Services/AuditLogService.cs` — audit impl
- `Mosaik/Services/UserDataFilterInjector.cs` — multi-tenant filter
- `Mosaik/Services/PasswordHasher.cs` — PBKDF2 + FixedTimeEquals
- `Mosaik/Program.cs` — cookie + auth config

## İlişkili skill / agent zinciri

- Önce: `mosaik-security` (bu skill) — yazarken proaktif uygulanır
- Yazılım sonrası: `security-reviewer` agent — denetler, file:line + attack path
- Paralel: `silent-failure-hunter` — error swallow ayrı scope
- Sonra: `/security-check` slash — hepsini paralel tetikler
