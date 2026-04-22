# Güvenlik İlkeleri

_Kapsam: Her değişiklikte uygulanacak defansif ilkeler. `paths:` yok — compact sonrası bile kalmalı._

## Mutlak Kurallar

1. **SQL injection:** Her `SqlCommand` → `CommandType.StoredProcedure` + `SqlParameter`. String concat **yasak**. Dinamik SQL gerekirse `STRING_SPLIT` + parametreli.

2. **XSS:** Razor view'larda `@Html.Raw` **minimum**. Server-generated HTML'de (DashboardRenderer) DOM API kullan: `createElement` + `textContent`. `innerHTML` + string concat yasak. `eval()` yasak.

3. **CSRF:** POST action'larda `[ValidateAntiForgeryToken]` + view'da `@Html.AntiForgeryToken()`. İstisnasız.

4. **Open redirect:** `Url.IsLocalUrl(returnUrl)` yeterli değil. Ek kontrol: `returnUrl.StartsWith("/") && !returnUrl.StartsWith("//")`.

5. **Sır/şifre yönetimi:**
   - Connection string → **env var** veya User Secrets veya Azure Key Vault.
   - `appsettings.json` içinde plain-text şifre YASAK. (TestController.cs:50, appsettings.json:3 → TODO G-01.)
   - `TestController` production'da kapalı (`#if DEBUG` sarılı).

6. **Cookie güvenlik flag'leri:**
   ```csharp
   options.Cookie.HttpOnly = true;
   options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
   options.Cookie.SameSite = SameSiteMode.Strict;
   options.ExpireTimeSpan = TimeSpan.FromHours(8);
   ```

7. **Exception handling:** User'a `ex.Message` GÖSTERME. `SqlException` → connection string sızabilir. Pattern:
   ```csharp
   catch (SqlException sex) {
       _logger.LogError(sex, "DB error");
       TempData["Message"] = "Veritabanı işleminde hata oluştu.";
   }
   catch (Exception ex) {
       _logger.LogError(ex, "Unexpected");
       TempData["Message"] = "Beklenmedik bir hata. Lütfen sistem yöneticisine bildirin.";
   }
   ```

8. **User data filter (multi-tenant güvenlik):**
   - `FilterKey` whitelist regex: `^[a-zA-Z_][a-zA-Z0-9_]*$`.
   - `FilterValue` regex: `^[a-zA-Z0-9,_\- ]+$`.
   - SP tarafında: `WHERE (@p IS NULL OR col IN (SELECT value FROM STRING_SPLIT(@p, ',')))`.
   - `InjectUserDataFilters` bypass edilemez olmalı — her SP execution path'ı geçer.

9. **Password hashing:**
   - **PBKDF2** (`PasswordHasher.CreateHash`). 100000 iteration.
   - Timing-safe compare: `CryptographicOperations.FixedTimeEquals` ✓ (zaten uygulanmış).

10. **Dashboard iframe:** `sandbox="allow-scripts"` — `allow-same-origin` **ekleme** (XSS izolasyonu kalksın).

## Security Review Ritüeli

- Her büyük değişiklik **öncesi** `security-review` skill'i çalıştır.
- Her büyük değişiklik **sonrası** tekrar çalıştır.
- Bulgulara göre düzelt, commit'le.

## Audit Log Kapsamı

Her kritik aksiyon log'lanır:
- User create/update/delete
- Role create/update/delete
- DataSource create/update/delete
- Report create/update/delete
- Category create/update/delete
- Login/logout (başarılı + başarısız)
- Password change
- Export
- Dashboard config invalid (fallback durumu)

Log'lanmazsa: bilmeyiz → bir şey olmuş gibi davranılır → audit gap.
