# CLAUDE.md — ReportHub / ReportPanel

Bu dosya her Claude oturumunun başında okunur. **Değişmez kurallar + kimlik.** Tarihli notlar, geçmiş kararlar ve detay kurallar ayrı dosyalarda:

- `.claude/rules/*.md` — davranış kuralları (konuya göre)
- `.claude/agents/*.md` — proje agent'ları (8 kod review + commit-splitter)
- `.claude/commands/*.md` — slash komutlar (`/feature-dev`, `/review-pr`)
- `.claude/skills/*/SKILL.md` — proje + frontend tasarım skill'leri
- `docs/ADR/*.md` — mimari karar kayıtları
- `docs/journal/YYYY-MM-DD.md` — oturum günlükleri
- `docs/PATTERNS.md` — gerçek-dünya pattern'leri (P-1..P-10)
- `TODO.md` — aktif sprint + backlog
- `plans/NN-<slug>.md` — Tier 3 iş planları (zorunlu, ADR-010)
- `docs/CONTEXT_MANAGEMENT.md` — bağlam yönetimi anayasası (ilkeler)
- `docs/CLAUDE_TOOLING_PROPOSAL.md` — Claude araç setinin tam önerisi

---

## 0. OTURUM BAŞI RİTÜELİ — İLK YANITTAN ÖNCE ZORUNLU

**Bu blok her oturumun ilk işidir. Atlamak yasak. Varsayım yasak.**

**Koşulsuz kural:** Context'te hook çıktısı görünse bile, her oturumun başında şu 4 adımı **elle** ve **sessizce** yap (kullanıcıya raporlamadan). Hook çıktısının context'te olması `bash` çalıştırmayı atlama gerekçesi **DEĞİLDİR** — fresh çıktı farklı olabilir, context stale olabilir, varsayım yapma.

1. `bash .claude/hooks/session-start.sh` çalıştır — git log + uncommitted + TODO özeti + son journal. **HER oturumda, istisnasız.**
2. `docs/journal/` altındaki **son 2 dosyayı** oku (özellikle yarım kalan işler bölümü).
3. `TODO.md` → "BIRLESIK ONCELIK SIRASI" → Faz 0 + aktif bug'ları gözden geçir.
4. **Uncommitted backlog'unu hatırla** (`git status --porcelain | wc -l`, 15 eşiği).

Ritüel sonrasında kullanıcının sorusuna cevap ver. "Nerede kaldık?" / "devam" / "günaydın" tetiklerinde cevap **bu okumalara dayanır**, hafıza tahminine değil, context'teki hook çıktısına değil.

**Ritüeli atladıysan:** kullanıcı fark ettiği anda yanlışı kabul et, eksiği anında kapat, önlemini `.claude/rules/session-protocol.md`'ye ekle. Tekrarı kabul edilmez.

Detay: [`.claude/rules/session-protocol.md`](.claude/rules/session-protocol.md).

---

## 1. Proje Kimliği

**ReportHub** (iç ad: **ReportPanel**) — SQL Server stored procedure'ları üzerinde çalışan rapor + dashboard portalı. Kullanıcılar rapor çalıştırır, parametrelerle filtreler, sonuçları tablo veya dashboard olarak görüntüler, Excel'e export eder. Admin tarafında rapor/data source/kullanıcı/rol/kategori yönetimi var.

### Tech stack
- **.NET 10.0** (`ReportPanel.csproj` → `net10.0`). `net8.0`'a DÖNME — "8 için destek bitecek" uyarısı var. NuGet paketleri 10.0.1.
- **ASP.NET Core MVC** (Controller + Razor Views). Razor Pages **DEĞİL**. (`Views/Auth/AGENT.md` yanıltıcı, siliniyor — TODO F-04.)
- **Entity Framework Core 10** (`ReportPanelContext`). Metadata CRUD için. SP çağrıları ADO.NET/`SqlCommand` ile. Dapper yok.
- **SQL Server** — `mcp__sqlserver__*` ve `mcp__sqlserver-express__*` MCP'leri mevcut.
- **Tailwind CSS** (CDN, utility-first) + `wwwroot/assets/css/style.css` custom sınıflar (`btn-brand`, `form-input-brand`).
- **Chart.js 4** + **Font Awesome 6** CDN (dashboard render).
- **Frontend JS:** Vanilla, IIFE pattern, `wwwroot/assets/js/`. jQuery yok.
- **Testler:** xUnit (`ReportPanel.Tests/`). Mevcut: `PasswordHasher`, `AuditLogService`. Coverage <%10.

### Ana klasörler
- `ReportPanel/Controllers/` — `Admin`, `Auth`, `Reports`, `Dashboard`, `Profile`, `Home`, `Test`, `Logs`
- `ReportPanel/Models/` — EF entities
- `ReportPanel/ViewModels/` — view-model wrapper'ları
- `ReportPanel/Views/` — Razor views, `_AppLayout.cshtml` ana layout
- `ReportPanel/Services/` — `PasswordHasher`, `AuditLogService`, `DashboardRenderer`
- `ReportPanel/Database/` — SQL migration + seed + SP scriptleri (01_ → 14_)
- `ReportPanel/wwwroot/assets/{js,css}/` — static assets

---

## 2. Çalışma Prensipleri (kullanıcı direktifleri)

Ayrıntılı kurallar `.claude/rules/` altında — burada sadece değişmez prensipler.

1. **Sistematik çalış.** Her karar + kural + talimat dosyaya yazılır (CLAUDE.md, TODO.md, `.claude/rules/`, `docs/ADR/`, `docs/journal/`). Konuşma hafızasında kalmaz. Detay: [`docs/CONTEXT_MANAGEMENT.md`](docs/CONTEXT_MANAGEMENT.md).

2. **Skill + agent + MCP aktif kullan.**
   - Built-in agent: `Explore` (keşif, audit), `Plan` (tasarım), `general-purpose` (araştırma). Paralel 2-3'e kadar.
   - **Proje agent'ları (`.claude/agents/`):**
     - `code-architect` — feature mimari blueprint (file:line referanslı, build sequence)
     - `code-explorer` — feature trace (entry → data, layer mapping)
     - `code-reviewer` — confidence-scored review (CLAUDE.md compliance + bug detect)
     - `code-simplifier` — recently-modified code clarity refactor
     - `comment-analyzer` — comment accuracy + rot detection
     - `pr-test-analyzer` — behavioral test coverage analysis
     - `silent-failure-hunter` — error handling + catch block audit
     - `type-design-analyzer` — invariant strength + encapsulation rating
     - `commit-splitter` — uncommitted'i bucket'lara böl
   - **Slash commands (`.claude/commands/`):**
     - `/feature-dev` — 7 fazlı guided feature development (discovery → exploration → clarify → architect → implement → review → summary)
     - `/review-pr` — multi-agent comprehensive PR review
   - **Proje skill'leri:** `session-handoff` (oturum sonu journal + auto-commit), `plan-tracker` (TodoWrite ↔ TODO.md senkron).
   - **Frontend tasarım skill'leri** (M-11 F-7+ dashboard builder UI + M-13 Plan 03 sayfa migration fazlarında tetiklenir): `frontend-design`, `visual-design-foundations`, `design-system-patterns`, `interaction-design`, `responsive-design`, `web-component-design` (Razor projesi için SKIP), `accessibility-compliance`.
   - **`ui-ux-pro-max`** (M-13 sırasında en güçlü audit aracı) — 161 color palette, 99 UX guideline, 25 chart type, 10 priority-ranked rule kategorisi (accessibility CRITICAL → charts LOW). UI değişikliği yaparken otomatik tetiklenir, WCAG contrast/touch target/anti-pattern checklist uygular.
   - Hazır skill'ler: `security-review`, `review`, `simplify`, `init`, `consolidate-memory`, `schedule`, `loop`, `claude-api`.
   - Gereken skill yoksa: `WebFetch`/`WebSearch` araştır, veya `.claude/skills/` altına yarat.
   - MCP'ler: `mcp__sqlserver__*` (DB), `mcp__Claude_in_Chrome__tabs_context_mcp` (browser test), `mcp__Claude_Preview__*` (live preview).

3. **Dil kuralı.** Kod + SQL table/column = İngilizce. UI metni + dokümantasyon = Türkçe (UTF-8, "Düzenle"/"Bileşen" — ASCII sadeleştirme yok). Detay: [`.claude/rules/turkish-ui.md`](.claude/rules/turkish-ui.md).

4. **Güvenlik öncelikli.** Büyük değişiklik öncesi/sonrası `security-review` skill'i çalıştır. Detay: [`.claude/rules/security-principles.md`](.claude/rules/security-principles.md).

5. **Commit disiplini.** Kullanıcı açıkça istemeden commit etme. Uncommitted dosya sayısı 15'i aşarsa yeni iş başlamadan önce commit-split zorunlu. Detay: [`.claude/rules/commit-discipline.md`](.claude/rules/commit-discipline.md).

6. **Plan-First (Tier sistemi).** 3+ klasöre dokunan / schema-security-UX / kullanıcı-görünür / harici dep işler **Tier 3** → `plans/NN-<slug>.md` tam plan zorunlu, kullanıcı onaylamadan implement etme. Tier 1 (typo) plansız, Tier 2 (küçük feature) TODO satırı yeterli. Detay: [`.claude/rules/plan-first.md`](.claude/rules/plan-first.md), [`docs/ADR/010-plan-first-tier-system.md`](docs/ADR/010-plan-first-tier-system.md).

7. **Bağlam yönetimi disiplini.** Aynı bilgi iki yerde yaşamaz (CLAUDE.md kimlik, `.claude/rules/` kural, `TODO.md` plan, `docs/ADR/` karar, `docs/journal/` günlük). Detay: [`.claude/rules/session-memory.md`](.claude/rules/session-memory.md).

8. **200 satır eşiği.** CLAUDE.md ve her `.claude/rules/*.md` dosyası 200 satır altında. Aşarsa konu bölünür.

---

## 3. Mimari Durumu

**Olgunluk:** ~%75. Core auth + reports + user CRUD + audit log çalışıyor. 32 dosyalık uncommitted dev block var.

Detaylı mimari notlar ve bilinen tutarsızlıklar: [`.claude/rules/architecture.md`](.claude/rules/architecture.md).

Aktif mimari tartışmalar ve yol haritası: [`TODO.md`](TODO.md) — "BIRLESIK ONCELIK SIRASI" bölümü.

Karar kayıtları: [`docs/ADR/`](docs/ADR/) (yazılması bekleniyor — ADR-001 data-access, ADR-002 dashboard-architecture, ADR-003 role-model, ADR-004 sp-modularization).

---

## 4. Geliştirme Workflow

### Build + run + test
```bash
# Build
cd D:/Dev/reporthub/ReportPanel && dotnet build --nologo

# Run (dev)
cd D:/Dev/reporthub/ReportPanel && dotnet run
# URL: http://localhost:5197

# Test
cd D:/Dev/reporthub && dotnet test
```

### Smoke test
- Tarayıcı: `http://localhost:5197/` → login → rapor çalıştır → dashboard aç → export et.
- DB: `mcp__sqlserver__sql_query`.
- JS parse kontrolü: `node -e "new Function(require('fs').readFileSync('path/to.js'))"`.

### Dosya konvansiyonları (özet — detay `.claude/rules/`)
- C#: [`.claude/rules/csharp-conventions.md`](.claude/rules/csharp-conventions.md)
- Razor: [`.claude/rules/razor-conventions.md`](.claude/rules/razor-conventions.md)
- SQL: [`.claude/rules/sql-conventions.md`](.claude/rules/sql-conventions.md)
- JS: [`.claude/rules/js-conventions.md`](.claude/rules/js-conventions.md)

---

## 5. Bilinen Sorunlar (referans)

- **Kaspersky EBADF** — Claude Code token rename hatası. BKM kurumsal AV `.tmp-*` kilitliyor. Kozmetik, işlevsellik etkilenmiyor. Kalıcı çözüm IT'den exclusion. Detay: [`.claude/rules/known-issues.md`](.claude/rules/known-issues.md).

---

## 6. Hızlı Referans — Sık Bakılan Dosyalar

| Amaç | Dosya |
|---|---|
| Rapor çalıştırma logic | `ReportPanel/Controllers/ReportsController.cs` |
| Admin user CRUD | `ReportPanel/Controllers/AdminController.cs:937-1183` |
| Dashboard render motoru | `ReportPanel/Services/DashboardRenderer.cs` |
| Dashboard builder JS | `ReportPanel/wwwroot/assets/js/dashboard-builder.js` |
| User data filter enjeksiyon | `ReportPanel/Controllers/ReportsController.cs:875` |
| SP önizleme endpoint | `ReportPanel/Controllers/AdminController.cs` → `SpList`, `SpPreview` |
| User modeli | `ReportPanel/Models/User.cs` |
| DB context | `ReportPanel/Models/ReportPanelContext.cs` |
| Audit log servisi | `ReportPanel/Services/AuditLogService.cs` |
| Şifreleme | `ReportPanel/Services/PasswordHasher.cs` |
| Ana layout | `ReportPanel/Views/Shared/_AppLayout.cshtml` |
| Uygulama başlatma | `ReportPanel/Program.cs` |
| Proje hedef framework | `ReportPanel/ReportPanel.csproj` |
