# Güvenlik Denetimi Bulguları — 2026-05-12

**Kapsam:** `/security-check HEAD~4..HEAD` (3 paralel agent) + `/security-review` (tam branch, 189 commit)
**Durum:** Sabah gözden geçirilecek

---

## ACİL — Hemen Fix (HIGH)

### HIGH-1 · `AdminController.Reports.cs:66-82` — Exception discard, log yok
- **Kategori:** Silent failure / security-principles.md §7 ihlali
- **Kanıt:** `catch (Exception ex) { _ = ex; TempData["Message"] = "..."; }` — exception `_` ile discard ediliyor, `_logger.LogError` çağrısı yok
- **Etki:** SqlException, NullReferenceException, OperationCanceledException — hepsi yutulur, ops'a hiç trace ulaşmaz
- **Tahmini süre:** ~10dk
- **Fix:**
  ```csharp
  catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
  catch (SqlException sex) { _logger.LogError(sex, "CreateReport GET: DB error"); TempData["Message"] = "Veritabanı erişiminde sorun var."; TempData["MessageType"] = "error"; return RedirectToAction("Index", new { tab = "reports" }); }
  catch (Exception ex) { _logger.LogError(ex, "CreateReport GET: unexpected"); TempData["Message"] = "Beklenmedik bir hata oluştu."; TempData["MessageType"] = "error"; return RedirectToAction("Index", new { tab = "reports" }); }
  ```

### HIGH-2 · `SmtpEmailService.cs:55,60,70` — `ex.Message` public `ErrorDetail`'e yazılıyor
- **Kategori:** Exception leak / credential sızıntısı riski
- **Kanıt:** `return EmailSendResult.Failed("smtp_5xx", sex.Message)` — SMTP auth hata mesajı (`535 Authentication failed for user@smtp`) `ErrorDetail`'e giriyor
- **Etki:** `ErrorDetail` public property; ileride caller `TempData["Message"] = result.ErrorDetail` yaparsa SMTP credential sızar
- **Tahmini süre:** ~10dk
- **Fix:** `ErrorDetail = null` yap (mesaj zaten loglandı); veya property'yi `internal` yap

---

## BU SPRINT — Fix (HIGH devamı / MEDIUM)

### HIGH-3 · `AdminController.Reports.cs:88-100, 214-227` — POST action'larda try/catch yok
- **Kategori:** Silent failure
- **Kanıt:** `CreateReport` ve `EditReport` POST action'larında `BuildReportFormViewModel` EF sorgularını try/catch olmadan çağırıyor
- **Etki:** DB hiccup → YSOD, form input + hata bağlamı kaybolur
- **Tahmini süre:** ~15dk

### HIGH-4 · `SmtpEmailService.cs:82-85` — Bulk send SMTP disabled → belirsiz result, log yok
- **Kategori:** Silent failure
- **Kanıt:** `Sent=0, Skipped=N, Failures=[]` döner; `AllSucceeded==false` ama `Failures` boş; `_logger.LogWarning` yok
- **Etki:** Production'da SMTP yanlışlıkla disabled bırakılırsa hiç email gönderilmez, hiç uyarı yok
- **Tahmini süre:** ~10dk
- **Fix:**
  ```csharp
  if (!IsEnabled)
  {
      _logger.LogWarning("SmtpEmailService: SMTP disabled, bulk send skipped. Recipients={Count}", list.Count);
      return new EmailBulkResult(Sent: 0, Skipped: list.Count, Failures: Array.Empty<EmailBulkFailure>());
  }
  ```

### MEDIUM-A · `EmailSendResult.cs:25` — `AllSucceeded` semantiği yanlış
- **Kategori:** API design / silent failure
- **Kanıt:** `AllSucceeded => Failures.Count == 0 && Sent > 0` — 0 alıcı veya skipped = `false`
- **Etki:** Caller "hata var" sanır; audit log kirlenir, retry kuyruğu şişer
- **Tahmini süre:** ~5dk
- **Fix:** `=> Failures.Count == 0`

### MEDIUM-B · `_AppLayout.cshtml:66` — htmx CDN, SRI hash yok
- **Kategori:** Supply chain / integrity
- **Kanıt:** `<script src="https://cdn.jsdelivr.net/npm/htmx.org@@2.0.4/dist/htmx.min.js">` — `integrity` attribute yok; Tailwind/Alpine local-served ama htmx CDN'den
- **Etki:** CDN compromise → kötü JS inject edilebilir
- **Tahmini süre:** ~10dk
- **Fix:** `integrity="sha384-..."` ekle veya htmx'i `~/lib/htmx/` altına serve et

### MEDIUM-C · `org-chart-render.js:93-96` — Fixed 2.5s timeout (Promise-tied olmalı)
- **Kanıt:** `setTimeout(() => { exportBtn.disabled = false; }, 2500)` — export promise sonuçlanmadan buton re-enable olur
- **Etki:** Kullanıcı çift tıklayabilir, çift export + double failure
- **Tahmini süre:** ~10dk

---

## BACKLOG (LOW)

| # | Dosya | Not |
|---|---|---|
| L-1 | `org-chart-render.js:31` | `e.message` kullanıcıya textContent ile yazılıyor — XSS safe ama policy ihlali |
| L-2 | `app-shell.js:18,23,130` | `localStorage` sessiz catch'ler — `console.warn` yok, debug edilemiyor |
| L-3 | `AdminController.cs` overview catch | errorId/correlationId user'a sunulmuyor |
| L-4 | `EmailTemplates.cs:14-21` | `SafeUrl` `#` döndürünce log/warning yok |

---

## Doğrulanan FALSE POSITIVE'ler (takip gerekmiyor)

| Bulgu | Gerekçe |
|---|---|
| `_AdminUserDataFilterPanel.cshtml:54` `@Html.Raw(groupsJson)` | `System.Text.Json` default encoder `<`→`<` escape eder; `UnsafeRelaxedJsonEscaping` yok; veri admin-only |
| `ContractsController.cs:204` `ex.Message` | Exception kaynağı kendi domain validator'ı (`ContractObligationGenerator`), önceden yazılmış Türkçe UI mesajları |
| `DashboardController.cs:53` CSV auth | Authenticated kullanıcıya rapor ismi gösterebilir ama veri okuma bypass yok; pre-existing M-03 technical debt |

---

## Tam Branch Review (`/security-review`) Özeti

**189 commit, 0 exploitable yeni güvenlik açığı.**

Geçen denetimde eklenen hardening (a68378c, 598b8ef) temiz:
- `EmailTemplates.cs` — HtmlEncode + SafeUrl zinciri tam
- AntiForgery truthy cache fix doğru
- Overview try/catch 3 katman, generic Türkçe mesaj
- SmtpEmailService exception layering sırası doğru

---

## Sabah Öneri Sırası

1. **HIGH-1 + HIGH-2** (~20dk) — en kritik, commit-ready
2. **HIGH-3 + HIGH-4 + MEDIUM-A** (~30dk) — email result pattern tamamlanıyor
3. **MEDIUM-B** (~10dk) — htmx SRI (opsiyonel, CSP planıyla birleştirilebilir)
4. **MEDIUM-C + LOW'lar** — backlog'a

---

*Dosya: `docs/security-findings-2026-05-12.md`*
