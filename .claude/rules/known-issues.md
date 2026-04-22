# Bilinen Sorunlar (blocker değil, bekliyor)

## Kaspersky EBADF — Claude Code token rename hatası

**Belirti:**
```
EBADF: bad file descriptor, rename
  'C:\Users\fikri.eren\AppData\Roaming\Claude\buddy-tokens.json.tmp-<hash>'
  -> 'C:\Users\fikri.eren\AppData\Roaming\Claude\buddy-tokens.json'
```

**Sebep:** Kaspersky Endpoint Security for Windows (BKM kurumsal AV) real-time scanning ile `.tmp-*` dosyalarını tararken file handle tutuyor — Claude rename edemiyor.

**Etki:** Kozmetik. Claude Code retry ile token'ı yenileyebiliyor, işlevsellik etkilenmiyor. Sadece konsol gürültüsü.

**Kalıcı çözüm:** BKM IT departmanından Kaspersky exclusion iste:
- Dizin: `C:\Users\fikri.eren\AppData\Roaming\Claude\`
- Ya da executable: `C:\Users\fikri.eren\AppData\Roaming\Claude\claude-code\*\claude.exe`

**User workaround (kurumsal policy izin verirse):**
- Kaspersky UI → Settings → Trusted Zone / Exclusions → klasör ekle.
- Policy kilitliyse: "Disabled by administrator" yazar → IT'ye git.

**Teşhis komutu:**
```powershell
Get-CimInstance -Namespace "root\SecurityCenter2" -ClassName AntiVirusProduct
# Kaspersky Endpoint Security + productState 266240 görünür
```

**Tarih:** 21 Nisan 2026'da teşhis edildi.

---

## SP Önizle click handler bağlanmıyor

**Belirti:** `/Admin/EditReport/{id}` sayfasında "SP Önizle" mor butonuna tıklanınca panel açılmıyor, hiçbir şey olmuyor.

**Teşhis (21 Nisan gecesi, canlı test):**
- Backend endpoint çalışıyor: `/Admin/SpPreview?dataSourceKey=PDKS&procName=sp_PdksPano` → HTTP 200 JSON.
- DOM elementleri tamam: `#spPreviewBtn`, `#spPreviewPanel`, `#ProcName` mevcut.
- Script yüklü: `/assets/js/admin-report-form.js` (19416 byte).
- Click sonrası panel hala `.hidden`, innerHTML=0 → **handler bağlı değil**.

**Muhtemel sebep:** `admin-report-form.js` içindeki outer IIFE `(() => { ... })()` içinde `if (!paramListEl || !paramSchemaEl) return;` erken çıkışı veya `initSpHelpers()` IIFE'si yanlış scope'ta.

**Çözüm planı (sabah):**
1. `preview_console_logs` ile tarayıcı console error kontrol.
2. `admin-report-form.js` son ~50 satır oku, IIFE kapanışlarını doğrula.
3. `initSpHelpers()`'i outer IIFE'nin **dışına** çıkar — paramList kontrolünden bağımsız top-level IIFE yap.

**Ek not:** `sp_PdksPano` parametresiz çağrılınca SQL hatası: "TOP veya OFFSET yan tümcesi geçersiz bir değer içeriyor" (@Tarih NULL kabul etmiyor). SpPreview endpoint'ine default parametre desteği eklenmeli (date→bugün, int→0, string→'', bool→0). TODO FAZ 1 madde 10.

---

## Uncommitted 32 Dosya

32 dosya `main` branch'te commit edilmemiş. Fazların kaynağı:
- Rol sistemi, kategori, favori, AD user, user data filter, dashboard motoru, SP önizleme.

Plan: 7 mantıklı commit'e bölme. Detay: `.claude/rules/commit-discipline.md` → "32-Dosyalık Backlog — Planlı Split".

Araç: `commit-splitter` subagent (TODO FAZ 1 madde 5).

---

## AGENT.md Yanıltıcı

`ReportPanel/Views/Auth/AGENT.md` içinde "Razor Pages + Dapper" manifesto var — proje MVC + EF Core. Gelecek Claude oturumları yanlış mimari varsayar.

**Çözüm:** Dosyayı sil veya başına `> ⚠️ DEPRECATED — reference only, project is MVC + EF Core` ekle. TODO F-04.

---

## Test Coverage <%10

Sadece `PasswordHasher` ve `AuditLogService` test edilmiş. Kritik yok:
- `DashboardRenderer` — XSS fix'leri regresyon testsiz.
- `UserDataFilter` — multi-tenant güvenlik noktası.
- `UserRole` sync.
- `ReportsController.Run` auth path'i.

Plan: TODO FAZ 1 madde 9 — DashboardRenderer + UserDataFilter + UserRole sync unit test'leri.
