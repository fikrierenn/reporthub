---
paths:
  - "ReportPanel/**/*.cs"
  - "ReportPanel.Tests/**/*.cs"
---

# C# Konvansiyonları

## Controller Action'ları

- **Async:** `public async Task<IActionResult> ActionName(...)`.
- **Authorize:** Admin action → class-level `[Authorize(Roles = "admin")]`. Auth action → method-level `[AllowAnonymous]` gerekiyorsa.
- **POST action:** `[HttpPost]` + `[ValidateAntiForgeryToken]` + route attribute (`[Route("Admin/EditUser/{id}")]`).
- **Dönüş:** ViewModel wrap (hiçbir action direkt Entity göndermez — TODO M-07 bekliyor).
- **Form binding:** `Request.Form["FieldName"]` kaçınılacak, model binding tercih.

## EF Core

- **DbContext lifetime:** `AddDbContext` (default scoped). Pool eklemek opsiyonel.
- **Read query:** Her zaman `.AsNoTracking()` (performans + track overhead yok).
- **Async:** `.ToListAsync()`, `.FirstOrDefaultAsync()`, `.AnyAsync()`. Sync `.ToList()` yok.
- **Include vs. Select projection:** Gerekli kolonlar için `.Select(x => new Dto { ... })`.
- **`SaveChangesAsync()`** (await).

## SP Çalıştırma

```csharp
using var connection = new SqlConnection(connString);
await connection.OpenAsync();
using var cmd = new SqlCommand(procName, connection)
{
    CommandType = CommandType.StoredProcedure,
    CommandTimeout = 120
};
cmd.Parameters.AddRange(parameters.ToArray());
using var reader = await cmd.ExecuteReaderAsync();
// ...
```

- `procName` asla user-input; admin'in onayladığı SP adı.
- Parametre tipi `SqlDbType` ile explicit ver (varsayım yapma).
- `CommandTimeout` büyük raporlar için 120, küçük için 30.
- Connection string **env var'dan** oku (TODO G-01 sonrası).

## Exception Handling

- **Spesifik exception önce:**
  ```csharp
  catch (SqlException sex) { _logger.LogError(sex, "..."); TempData["Message"] = "Veritabanı hatası."; }
  catch (Exception ex)     { _logger.LogError(ex, "...");  TempData["Message"] = "Beklenmedik hata."; }
  ```
- **User'a `ex.Message` GÖSTERME** — stack / connection string sızar.
- **Sessiz `catch {}` yasak** — en azından `_logger.LogWarning`.

## Nullability

- `<Nullable>enable</Nullable>` aktif. `?` ve `!` operator'lerini doğru kullan.
- Null-forgiving `!` sadece **gerçekten null olmayacak** biliyorsan.
- `string?` vs `string` tutarlı — default değer olarak `string.Empty` tercih.

## Async / await

- Controller action'ı → `async Task<IActionResult>`.
- Helper method → async gerekiyorsa async.
- **`async void` yasak** (event handler hariç).
- `ConfigureAwait(false)` library code'da, ASP.NET Core app code'da gerek yok.

## DI + Constructor Injection

```csharp
public class MyController : Controller
{
    private readonly IService _service;
    private readonly ILogger<MyController> _logger;

    public MyController(IService service, ILogger<MyController> logger)
    {
        _service = service;
        _logger = logger;
    }
}
```

- Property injection **yasak**.
- `IHttpClientFactory` — `new HttpClient()` asla.
- `DateTime.UtcNow` → `DateTime.Now` asla (timezone sorunları).

## Audit Logging

Admin/critical aksiyonlarda:
```csharp
await _auditLog.LogAsync(new AuditLogEntry
{
    EventType = "user_update",
    TargetType = "user",
    TargetKey = user.UserId.ToString(),
    Description = "User updated",
    NewValuesJson = AuditLogService.ToJson(new { user.UserId, user.Username, ... })
});
```

Login/logout, create/update/delete, export, dashboard config invalid hepsi log'lanır.

## ViewModel

- `ViewModels/` altında.
- Entity referansı **yasak** (TODO M-07 — mass assignment riski). DTO pattern: flat properties.
- `[BindNever]` kritik alanlarda (UserId, PasswordHash, ReportId).

## Naming

- **PascalCase:** class, method, property, public field.
- **camelCase:** local variable, parameter, private field.
- **_underscorePrefix:** private readonly field (`_context`, `_logger`).
- **Interface:** `I` prefix (`IStoredProcedureExecutor`).
- **Async method:** `Async` suffix (`GetUserAsync`, `LogAsync`).
