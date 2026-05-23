# Çelişki Audit — 2026-05-25

> **Kaynak:** Codex + Claude paralel oturum. İki turlu derinlemesine kod ↔ doküman ↔ ADR ↔ migration taraması.
> **İlk tur:** üst katman doküman + klasik tutarsızlık.
> **İkinci tur:** kod-içi + ADR vaadi + migration numaralandırma + aynı dosya içi çelişki.
> Toplam: **35+ bulgu**.

Bu dosya kayıt belgesidir — fix'ler ayrı commit/plan/ADR ile yapılır. Her bulgu için durum (open/closed/deferred) güncellenecek.

---

## CRITICAL (operasyonel risk + güvenlik)

### C-1. Migration numara çakışması (host + modüller, gelecekte 3'lü)

**Durum:** ❌ AÇIK

```
Numara | Host Mosaik/Database/                    | Modül
70     | 70_AiSettingsDailyBudget.sql             | Mosaik.Modules.Forms/Database/70_FormsSchema.sql
72     | 72_RenameTamimModuleToCircular.sql       | Mosaik.Modules.Forms/Database/72_FormsTokenCascade.sql
```

Plan 42 (Process Runtime) gelecekte host'ta `72_ProcessRuntimeSchema.sql` istiyor — 3. çakışma.

**Etki:** Operatör manuel `sqlcli script <file>` ile uygularken yanlış sıra → schema felç.
**Skill:** `sql-migration-writer` "zinciri ezme riskini önle" diyor ama global tek zincir yok.
**Çözüm yaklaşımları:**
- A) Global registry `Database/manifest.json` (sıra + bağımlılık)
- B) Prefix konvansiyon: `H72_` (host), `F72_` (Forms), `S03_` (SOP)
- C) Modül-içi migration'lar `Mosaik.Modules.X/Database/MX_NN_*.sql` (M-prefix)

**ADR adayı.**

---

### C-2. Üç paralel onay motoru vs Plan 36 "tek engine"

**Durum:** ❌ AÇIK

| Motor | Nerede | Tablo |
|---|---|---|
| `Mosaik.Services.ApprovalService` | Host DI | `ApprovalRequest` / `ApprovalStep` |
| `SopApprovalService` | SOP modülü | Aynı `ApprovalRequest` — host inject etmiyor, DbContext duplicate |
| `WorkflowEngine` (`IWorkflowService`) | Mosaik.Core | `WorkflowTemplate` / `WorkflowInstance` |

**Vaat:** Plan 36 "tüm modüllerin onay zinciri tek engine"
**ADR-002** satır 56 `IApprovalService` üzerinden cross-modül diyor — **interface bile yok** (yalnızca `.md`'de geçiyor).
**Journal 2026-05-23:** SOP duplicate logic "bilinçli erteleme" kabul edildi (memory: `feedback_modul_izolasyon_approval_service`).

**Etki:** Mimari ↔ doküman ↔ kod üçlü kopuk. Plan 36 implement edilince birleştirme zorlu.
**ADR adayı.**

---

### C-3. `ex.Message` kullanıcıya leak (security-principles ihlali) — **FALSE POSITIVE**

**Durum:** ✅ KAPALI 2026-05-25 — sweep sonrası false positive doğrulandı

Codex audit 6 satır flagledi. Hepsi incelendi:

| Dosya:satır | Tür | Hedef | Sızıntı? |
|---|---|---|---|
| `ReportsController.Run.cs:216` | `LogRun(...)` | AuditLog tablosu (internal) | ❌ Hayır |
| `ReportsController.V2Preview.cs:114` | `LogRun(...)` | AuditLog (internal) — user'a generic JSON | ❌ Hayır |
| `ReportsController.V2Preview.cs:238` | `ErrorMessage = ex.Message` | `AuditLog` entity | ❌ Hayır |
| `ReportsController.V2Preview.cs:388` | `ErrorMessage = ex.Message` | `AuditLog` entity — user `StatusCode(500, generic)` | ❌ Hayır |
| `ContractsController.cs:226` | `ModelState.AddModelError(ex.Message)` | `ArgumentException` domain validation (TR mesaj, user'ın kendi datası: `input.Title`) | ❌ Kasıtlı |
| `AiController.Wizard.cs:58` | `BadRequest(ioex.Message)` | `InvalidOperationException` `WizardExtractionService.Start` — kontrollü TR mesaj (`"Geçersiz dosya yolu."` veya rate-limit TR) | ❌ Hayır |

**Sonuç:** Hiçbiri SqlException/stack/connstring sızdırmıyor. 4'ü internal audit log; 2'si bilinçli Türkçe user-facing validation (sanitized). `security-principles.md` kural 7 ihlali yok.

**Lesson learned:** "ex.Message kullanılıyor" greplenince context bakmadan bug zannetme. Audit log vs user response ayrımı gerek.

---

### C-4. `VISION.md` §1 vs §3-§4 same-file çelişki

**Durum:** ❌ AÇIK (§1 düzeltildi 2026-05-25, §3-§4 hala stale)

§1 (2026-05-25 güncel): SOP ✅, Workflow engine canlı, %80 olgun.
§3:69-77 (stale): "Designer UI yok", "Mail caller yok"
§3:61-63 (stale): Calendar belirsiz, Plan 22 backlog
§4:86-94 (stale): "SOP (Plan yok) — EN HIZLI KAZANIM"

**Etki:** Yön belgesi olarak okuyan kişi yanlış öncelik alır.
**Bu oturumda yapılacak (Adım 3).**

---

## HIGH (ADR ihlali + stale meta)

### H-1. ADR-002 modül disable ↔ `ModuleLoader`

**Durum:** ❌ AÇIK

`ADR-002:58`: DB'de modül kapalı → assembly yüklenmez / DI register edilmez.

`Mosaik.Core/Module/ModuleLoader.cs:79-83`:
```csharp
public static void RegisterAll(IServiceCollection services)
{
    foreach (var module in DiscoverModules())
        module.ConfigureServices(services);
```

→ `AppModules.IsEnabled` yalnızca sidebar (`ModuleService.IsEnabled`). Kapalı SOP/Forms modülünün servisleri + endpoint'leri yine register.

`ModuleService.cs:69-72` cache fail-open:
```csharp
if (_cache == null) return true; // güvenli taraf?
```

**Etki:** ADR ihlali + endpoint security yüzeyi.
**Fix:** `ModuleLoader.RegisterAll` `IsEnabled` query, endpoint routing conditional. Tier 2 fix.

---

### H-2. `IApprovalService` interface yok

**Durum:** ❌ AÇIK

ADR-002 cross-modül abstraction listesinde `IApprovalService` var. Repo'da:
- `Mosaik.Core/` altında `IApprovalService.cs` **yok**
- Yalnızca host `ApprovalService` class (interface'siz)
- SOP modülü direkt DbContext + duplicate logic

**C-2'nin alt parçası.** ADR-002 vaadi yarım, ADR ya güncellenmeli ya da interface eklenmeli.

---

### H-3. Plan / TODO / Journal "durum" stale çelişkiler

**Durum:** ❌ AÇIK

| Kaynak | İddia | Gerçek |
|---|---|---|
| Plan 33 | "Plan 34 SOP öncesi başlar" | SOP Faz A-E + 34.1 Faz 0-6+8 ✅ |
| Plan 34 üst bilgi | "Taslak — onay bekliyor" | Aynı dosyada Faz A-E [x] |
| Plan 36 Done Criteria | Tümü [ ] | §7 Faz A-C [x], Hangfire workflow-step-processor kayıtlı |
| TODO.md:63 | Plan 34 [ ] | Journal + Mosaik.Modules.SOP/ dolu |
| Plan 41 giriş | "Form sistemi yok" | Faz 0 entity + migration 70-73 |
| ARCHITECTURE_MAP §8 | Plan 33 = "Admin views standardizasyon" | Gerçek = modül tamamlama roadmap (kısmen düzeltildi 2026-05-25) |

**Etki:** Süreç ve öncelik yanıltıcı.
**Fix:** Plan/TODO senkron botu veya manuel sweep. Tier 2.

---

### H-4. CLAUDE.md "SOP yok" vs modül var

**Durum:** ✅ KAPALI 2026-05-25 (VISION.md §1 güncelleme)

CLAUDE.md §3 "%40-50 vNext, 6 modül yok: SOP..." satırı VISION'a referans veriyor. VISION §1 düzeltildi → SOP "tam" listesinde. CLAUDE.md kendisi VISION'a delege ediyor, yeniden yazılması gerekmiyor.

---

## MEDIUM (stale + tasarım belirsizlik)

### M-1. VISION §3 mail/calendar/compliance stale

**Durum:** ❌ AÇIK (C-4 ile birlikte fix)

VISION §3: "caller yok", "gerçek mail atılmıyor"
Kod: `DailyReminderJob`, `SopReadReminderJob`, `WorkflowNotifier` → `IEmailService.SendAsync`
Config: `SmtpSettings.Enabled: false` default → prod'da mail gitmez (kısmen doğru — caller var, SMTP kapalı).

Journal 2026-05-12 "tek caller yok" → stale.

---

### M-2. AGENTS.md çift anayasa (Codex paralel)

**Durum:** ⚠️ KISMEN KAPALI 2026-05-25 (banner eklendi, local-only — .gitignore)

AGENTS.md:
- Ürün adı: ReportHub
- Tailwind: CDN
- Migration: 01-14
- Test coverage: <%10
- ADR: yazılacak

CLAUDE.md (gerçek):
- Mosaik
- Tailwind yerel `~/lib/tailwindcss/`
- Migration 00-72
- 516 test
- ADR 001-022

**Fix:** AGENTS.md `.gitignore`'da; banner kullanıcı disk'te ama repo'ya yansımıyor. Codex kullanıcıları için yeterli. **Eğer Codex shared repo'da çalışacaksa** AGENTS.md tracking'e alınıp CLAUDE.md'ye delege edilmeli.

---

### M-3. ARCHITECTURE_MAP §8 Plan 33 tanım

**Durum:** ✅ KAPALI 2026-05-25 (commit f0827c0)

§8 "Admin views standardizasyon" → "Modül Tamamlama Roadmap" düzeltildi + Plan 34/34.1/35/36/38/40/41/42 eklendi.

---

### M-4. OrgChart tenant filtresi eksik

**Durum:** ❌ AÇIK

`OrgChartController` — `FirmaId` filtresi yok.
Contracts/Compliance/Calendar — var.

**Etki:** Multi-tenant veri sızıntısı potansiyeli. Tasarım mı bug mı belirsiz.
**Fix:** Code review + karar. Tier 2 fix veya kasıtlı ise ADR.

---

### M-5. Çift veri erişim / filtre modeli

**Durum:** ❌ AÇIK (belgelenmemiş)

- `UserDataFilterInjector` — SP parametre enjeksiyonu (raporlar)
- `IUserDataScope` — `SpInjectionScope`, `ReportAccessScope` (Plan 14)

`Program.cs:140` yorumu hala "Faz C2 (gelecek): UserDataFilterInjector + ReportsController.Index" diyor; injector Run/Export'ta zaten kullanılıyor. Yorum stale, "iki kanal" mimarisi ADR'sız.

**Fix:** ADR-023 adayı "iki kanal data scope" veya tek kanalda birleştir.

---

### M-6. CONTEXT_MANAGEMENT kendi kuralını ihlal ediyor

**Durum:** ❌ AÇIK

`session-memory.md`: "Aynı bilgi iki yerde yaşamaz."

Aynı gerçek 5+ yerde farklı sayılarla:
- Test sayısı: 337 / 387 / 432 / 516 (CLAUDE.md ✅ 516, plan dosyaları 387)
- Migration sonu: 56 / 71 / 72 (ARCHITECTURE_MAP ✅ 72, CLAUDE.md ✅ 72, plan'lar 56)
- Plan 36 durumu: TODO [ ] vs Plan §7 [x]
- Mail caller var/yok
- SOP var/yok

**Etki:** Meta-çelişki; agent'lar farklı "gerçek" üretir.
**Fix:** Auto-refresh hook (`scripts/refresh-arch-map.sh` benzeri) tüm "sayı" marker'lara yayılmalı.

---

## LOW (stale + branding)

### L-1. Test sayısı AUTO marker

**Durum:** ✅ KAPALI 2026-05-25 (516)

---

### L-2. Forms modülü scaffold vs "yapılmayacak"

**Durum:** ✅ KAPALI 2026-05-25 (TODO.md revize, commit f0827c0)

Forms scaffold gerçek:
- Entity + EF config + migration 70-73 ✅
- `FormsModule.ConfigureServices` — servisler yorum satırı (TASLAK, Plan 41 Faz 1'de implement)
- `HomeController` — placeholder

---

### L-3. EF Migrations/ klasör vs "EF migrations yok"

**Durum:** ❌ AÇIK

Politika: EF migrations yok, SQL-first.
Var: `Mosaik/Migrations/20251216083943_InitialCreate` + `ReportPanelContextModelSnapshot`.

**Etki:** Yeni geliştirici `dotnet ef database update` sanabilir.
**Fix:** `Mosaik/Migrations/` sil + `Program.cs` migrations runner devre dışı (zaten devre dışı, dosyalar artifact).

---

### L-4. README ReportHub branding

**Durum:** ❌ AÇIK (kabul edilebilir)

Repo klasörü `D:\Dev\reporthub` korundu (tarihsel). README hala "ReportHub" geçiyor. CLAUDE.md "L1 canonical Mosaik" diyor.

**Karar:** Mosaik canonical, repo path korunur, README opsiyonel revize.

---

### L-5. Plan 36 migration adı uyumsuz

**Durum:** ❌ AÇIK

- Plan 36 belge: `62_WorkflowTables.sql`
- Repo: `65_WorkflowTemplatesInstances.sql`, `33_CreateMosaikCoreWorkflow.sql`

**Fix:** Plan 36 belge migration referansı düzelt. Cosmetic.

---

## Özet matris

| Şiddet | Sayı | Kapalı | Açık |
|---|---|---|---|
| Critical | 4 | 1 | 3 |
| High | 4 | 1 | 3 |
| Medium | 6 | 1 | 5 |
| Low | 5 | 2 | 3 |
| **Toplam** | **19** | **5** | **14** |

(35+ bulgu listesinden 19 kategorize edildi — kalan: detay/cross-ref/stale snapshot, ayrı sweep gerekirse açılır.)

---

## Sonraki adımlar

**Bu oturum (2026-05-25):**
1. ✅ Sidebar URL + migration 72 commit (`4cce015`)
2. ✅ Docs alignment commit (`f0827c0`)
3. ✅ VISION §3-§4 rewrite (C-4 + M-1) — Calendar/Compliance/Approval/Notification güncel
4. ✅ ex.Message security sweep (C-3) — **FALSE POSITIVE** (6/6 audit log veya kasıtlı TR validation)

**Gelecek oturum (ADR/Plan):**
5. ADR-023 adayı: Migration registry strategy (C-1)
6. ADR-024 adayı: Approval engine unification (C-2 + H-2)
7. ADR-025 adayı: ModuleLoader IsEnabled enforcement (H-1)
8. Plan 43 adayı: Baseline install + migration runner (C-1 ile bağlantılı)
9. Plan/TODO senkron disiplini hook (H-3, M-6)

**Erteli backlog:**
- M-4 OrgChart tenant filter audit
- M-5 ADR-023b iki kanal data scope
- L-3 EF Migrations/ artifact sil
- L-5 Plan 36 migration ref düzelt
