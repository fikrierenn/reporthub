---
paths:
  - "Mosaik/**/*.cs"
  - "Mosaik.Tests/**/*.cs"
---

# C# Konvansiyonları

## Dosya Boyutu Disiplini

**Kural:** Yeni yazılan/düzenlenen C# dosyaları **300 satırın altında** kalmalı. 500 satır **kırmızı çizgi** — bir sonraki PR'da split zorunlu.

**Neden:** Tek dosyada iç içe 7 feature (AdminController 1736 satır örneği) merge conflict, test zorluğu, yeni geliştirici onboarding yükü üretir. Solo dev için bile context switch maliyeti.

**Uygulama:**
- Yeni controller: endpoint çoksa servis/service layer'a bölme (örn. `UserManagementService`, `ReportManagementService`).
- Yeni model: ilgili olmayan alt modeller ayrı dosyaya.
- Util/helper: 5+ public method varsa scope bazlı ayır.
- **Mevcut büyük dosyalar** (legacy): TODO maddesi + ADR "Known debt" + faz planla. Touch ettikçe azaltmaya çalış.

**Snapshot (22 Nisan 2026):**
- `AdminController.cs` 1736 satır — **anti-pattern**. TODO M-01 (Faz 2) service extraction.

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

## Modern C# 14 / .NET 10 Özellikleri (cherry-pick)

_Kaynak: codewithmukesh/dotnet-claude-kit `modern-csharp` skill, 2026-05-27 manuel adapte._
_Hedef: Mosaik `net10.0` + C# 14. Yeni kod modern pattern, mevcut legacy refactor touch-ettikçe._

### Tercih edilen modern pattern'ler

| Eski pattern | Yeni pattern (C# 14) | Ne zaman |
|---|---|---|
| Constructor + `private readonly _x = x` boilerplate | **Primary constructor** `public class OrderService(IRepo repo, ILogger<OrderService> log)` | Her DI'lı class, alan body'de kullanılıyorsa |
| `new List<string>() { "a", "b" }` | **Collection expression** `List<string> names = ["a", "b"];` | List/array/dict init |
| Manuel `private string _name; public string Name { get => _name; set => _name = value?.Trim() ?? ""; }` | **`field` keyword** `public string Name { get; set => field = value?.Trim() ?? ""; }` | Property validation/normalize |
| DTO için class + manual equality | **`record`** (immutable DTO) veya **`readonly record struct`** (küçük value) | Flat data taşıma, value semantics |
| `static class StringExtensions { static string Trim2(this string s) }` | **Extension members** (C# 14) | Cleaner sözdizimi |
| `"line1\nline2 \" quote"` string escape | **Raw string** `"""..."""` | SQL/JSON/HTML literal |

### Karar matrisi
- **DTO / ViewModel** → `record`
- **Küçük value object** (≤ 16 byte, immutable) → `readonly record struct`
- **DI'lı service / controller** → primary constructor
- **Performans-kritik byte/char slice** → `Span<T>` / `ReadOnlySpan<T>` zero-alloc
- **Default**: `sealed` ekle (inheritance açıkça gerekli değilse)
- **Immutability default**: `record`, `readonly`, `init`, `required` ile invalid state'i imkânsız yap

### Anti-pattern
- Manuel backing field (`field` kw varken)
- `var` belirsizken (`var x = GetThing()` — `Thing x` tercih)
- Deeply nested pattern match (>2 seviye) — ayrı method'a çıkar
- `record` yerine tuple ile domain type taşıma
- `new HttpClient()` (zaten yasak — `IHttpClientFactory`)

### Mosaik için pratik
- **Mevcut legacy** dokunmadan refactor YOK ([coding-discipline.md](coding-discipline.md) surgical changes).
- **Yeni dosya** = modern pattern default.
- **Touch ettikçe** = constructor → primary ctor, `new List<>(){}` → `[]`, manuel backing field → `field`.
- Roslyn MCP (`cwm-roslyn-navigator`) `detect_antipatterns` ile sync-over-async + async void tara — pre-commit hook bash regex'inden daha güvenilir.
