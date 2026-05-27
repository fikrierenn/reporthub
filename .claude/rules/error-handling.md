# Hata Yönetimi (Result Pattern + Exception Disiplini)

_Kaynak: codewithmukesh/dotnet-claude-kit `error-handling` skill manuel adapte + Mosaik Plan 16.5 `ServiceResult` pattern._
_2026-05-27 cherry-pick. Mosaik MVC + Razor için ProblemDetails opsiyonel (zaten TempData + view kullanılıyor)._

## Temel Ayrım

**Beklenen sonuç (business outcome)** ≠ **gerçek exception (system failure)**.

| Durum | Tip | Mekanizma |
|---|---|---|
| "Sözleşme bulunamadı", "validation hatası", "yetki yok", "tarih çakışması" | Beklenen | **Result pattern** (`ServiceResult<T>` / null + audit) |
| DB connection loss, network timeout, JSON parse, file I/O fail | Gerçek exception | `try/catch` + log + generic kullanıcı mesajı |
| Bug / impossible state (null check fail, off-by-one) | Programming error | Fırlat, üst handler yakalasın |

**Anti-pattern:** Beklenen iş sonucu için `throw new Exception("Bulunamadı")` → exception flow control = expensive + readability ↓.

## Result Pattern (Mosaik bağlamı)

Mosaik'te canonical: **`Mosaik.Core.Domain.ServiceResult<T>`** (Plan 16.5 Faz A).

### Kullanım — controller / service

```csharp
public async Task<ServiceResult<Sop>> CreateAsync(SopCreateDto dto, int userId, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(dto.Title))
        return ServiceResult<Sop>.Fail("Başlık boş olamaz.");

    var exists = await _ctx.Sops.AsNoTracking()
        .AnyAsync(x => x.FirmaId == dto.FirmaId && x.Code == dto.Code, ct);
    if (exists)
        return ServiceResult<Sop>.Fail("Bu kod ile SOP zaten var.");

    var sop = new Sop { /* … */ };
    _ctx.Sops.Add(sop);
    await _ctx.SaveChangesAsync(ct);
    return ServiceResult<Sop>.Ok(sop);
}
```

### Controller — Result tüketme

```csharp
var r = await _sopService.CreateAsync(dto, User.GetUserId(), ct);
if (!r.IsSuccess)
{
    TempData["Message"] = r.Error;
    TempData["MessageType"] = "error";
    return RedirectToAction(nameof(Index));
}
TempData["Message"] = "SOP oluşturuldu.";
TempData["MessageType"] = "success";
return RedirectToAction(nameof(Details), new { id = r.Value!.Id });
```

## Exception Handling (gerçek arızalar)

```csharp
catch (SqlException sex)
{
    _logger.LogError(sex, "DB error: {Op}", "SopCreate");
    TempData["Message"] = "Veritabanı işleminde hata oluştu.";
    TempData["MessageType"] = "error";
}
catch (OperationCanceledException) when (ct.IsCancellationRequested)
{
    throw; // propagate, handler 499/clean shutdown
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected: {Op}", "SopCreate");
    TempData["Message"] = "Beklenmedik bir hata. Sistem yöneticisine bildirin.";
    TempData["MessageType"] = "error";
}
```

### Mutlak kurallar

1. **Boş catch yasak.** `catch (Exception) {}` veya `catch { _ = ex; }` → silent failure. Asgari `_logger.LogWarning(ex, ...)`.
2. **`ex.Message` kullanıcıya gösterme.** SqlException → connection string sızar. Generic Türkçe mesaj. Detay log'a.
3. **`Exception` yakalamadan önce spesifik.** `SqlException`, `JsonException`, `HttpRequestException` → ayrı catch. En son `Exception`.
4. **`OperationCanceledException` rethrow.** `when (ct.IsCancellationRequested)` filter ile gerçek iptal'i propagate et — iş hatası olarak yutma.
5. **Async + CancellationToken.** Tüm `async Task` method'lar `CancellationToken ct = default` parametresi alır + downstream'e geçir.

## ProblemDetails (opsiyonel)

Mosaik MVC + view-based, ProblemDetails primary değil. Ama AJAX endpoint'lerde (örn. `/Admin/SpPreview`, AI Wizard JSON endpoint'leri) **RFC 9457 ProblemDetails JSON** dön:

```csharp
return Problem(
    type: "https://mosaik/errors/sp-not-found",
    title: "SP bulunamadı",
    statusCode: 404,
    detail: $"DataSource '{key}' için '{procName}' tanımlı değil.");
```

`builder.Services.AddProblemDetails()` `Program.cs`'te aktif olmalı (kontrol edilecek).

## Anti-pattern Listesi

| Anti-pattern | Doğrusu |
|---|---|
| `throw new Exception("Not found")` business case | `return ServiceResult<T>.Fail("Bulunamadı")` |
| `catch (Exception) { return null; }` | Spesifik catch + log + result |
| `catch { /* sessiz */ }` | Minimum `_logger.LogWarning` |
| `TempData["Message"] = ex.Message` | Generic + log detayı |
| `if (!await SaveChangesAsync()) throw` | `SaveChangesAsync` zaten throw eder — gereksiz |
| Result + Exception karışık (`return Result.Fail` üst, `throw` alt method) | Tek strateji seç, layer içinde tutarlı |

## İlişkili

- [`.claude/rules/csharp-conventions.md`](csharp-conventions.md) Exception Handling bölümü (canonical)
- [`.claude/rules/security-principles.md`](security-principles.md) `ex.Message` gizleme kuralı (#7)
- `Mosaik.Core/Domain/ServiceResult.cs` (Plan 16.5 Faz A)
- `silent-failure-hunter` agent — `_ = ex;` + sessiz catch tarayıcı
