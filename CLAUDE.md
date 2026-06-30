# CLAUDE.md — Mosaik

Bu dosya her Claude oturumunun başında okunur. **Değişmez kurallar + kimlik.** Tarihli notlar, geçmiş kararlar ve detay kurallar ayrı dosyalarda:

- `.claude/rules/*.md` — davranış kuralları (konuya göre — yeni view yazmadan önce **`ui-patterns.md`** zorunlu)
- `.claude/agents/*.md` — proje agent'ları (8 kod review + commit-splitter)
- `.claude/commands/*.md` — slash komutlar (`/feature-dev`, `/review-pr`)
- `.claude/skills/*/SKILL.md` — proje + frontend tasarım skill'leri
- `docs/ADR/*.md` — mimari karar kayıtları
- `docs/journal/YYYY-MM-DD.md` — oturum günlükleri
- `docs/PATTERNS.md` — gerçek-dünya pattern'leri (P-1..P-10)
- `docs/VISION.md` — ürün özellik vizyonu + vNext değer sıralı modül listesi (yön belgesi)
- `TODO.md` — aktif sprint + backlog
- `plans/NN-<slug>.md` — Tier 3 iş planları (zorunlu, ADR-010)
- `docs/CONTEXT_MANAGEMENT.md` — bağlam yönetimi anayasası (ilkeler)
- `docs/CLAUDE_TOOLING_PROPOSAL.md` — Claude araç setinin tam önerisi
- **Cross-project brain:** `D:/Dev/brain` (Obsidian vault, Plan 08, Karpathy LLM Wiki). Schema: `D:/Dev/brain/CLAUDE.md`. NotebookLM (`f2407372`) ile paralel. Bu repo'daki ham bilgi (journal, plan, ADR) burada kalır; **sentez** brain'de yaşar.

---

## 0. OTURUM BAŞI RİTÜELİ — İLK YANITTAN ÖNCE ZORUNLU

**Bu blok her oturumun ilk işidir. Atlamak yasak. Varsayım yasak.**

**Koşulsuz kural:** Context'te hook çıktısı görünse bile, her oturumun başında şu 4 adımı **elle** ve **sessizce** yap (kullanıcıya raporlamadan). Hook çıktısının context'te olması `bash` çalıştırmayı atlama gerekçesi **DEĞİLDİR** — fresh çıktı farklı olabilir, context stale olabilir, varsayım yapma.

1. `bash .claude/hooks/session-start.sh` çalıştır — git log + uncommitted + TODO özeti + son journal. **HER oturumda, istisnasız.**
2. `docs/journal/` altındaki **son 2 dosyayı** oku (özellikle yarım kalan işler bölümü).
3. `TODO.md` → "BIRLESIK ONCELIK SIRASI" → Faz 0 + aktif bug'ları gözden geçir.
4. **Uncommitted backlog'unu hatırla** (`git status --porcelain | wc -l`, 15 eşiği).
5. **`docs/ARCHITECTURE_MAP.md` oku.** Sidebar → route → view canonical map, V1/V2 deprecated tablosu, refactor öncesi zorunlu çek-liste. Silme/rename öncesi **mutlaka** bu dosyayı kontrol et — "V1 var sandım, V2 zaten varmış" tarzı kaybı bu engeller.

Ritüel sonrasında kullanıcının sorusuna cevap ver. "Nerede kaldık?" / "devam" / "günaydın" tetiklerinde cevap **bu okumalara dayanır**, hafıza tahminine değil, context'teki hook çıktısına değil.

**Ritüeli atladıysan:** kullanıcı fark ettiği anda yanlışı kabul et, eksiği anında kapat, önlemini `.claude/rules/session-protocol.md`'ye ekle. Tekrarı kabul edilmez.

Detay: [`.claude/rules/session-protocol.md`](.claude/rules/session-protocol.md).

---

## 1. Proje Kimliği

**Mosaik** — modüler şirket içi portal. Şu an aktif modül: rapor + dashboard (SQL Server stored procedure'ları üzerinde çalışan). vNext modüller (TODO.md:298): tamim/sirküler, duyurular, departman dizini, doküman paylaşımı, mesajlaşma, form/anket, SOP, takvim, KPI/OKR, onay akışları. Brand metaforu: her modül bir taş, birlikte mozaiği oluşturur.

**Canonical kimlik (sabit):** `Mosaik` namespace + `Mosaik.csproj` + `Mosaik.sln`.
**Repo klasörü:** `D:\Dev\reporthub` (tarihsel, kod-içi rebrand kapsam dışı bırakıldı; iç klasörler `Mosaik/` + `Mosaik.Tests/` adlarında kalır).
**Brand görünümü (parametric):** UI marka adı/logo/renk DB-driven (Plan 11 Faz 3 BrandSettings — bekliyor).
**Modüller (switchable):** Sidebar'da hangi modüller görünür, DB-driven (Plan 11 Faz 4 Modules — bekliyor).

### Tech stack
- **.NET 10.0** (`Mosaik.csproj` → `net10.0`). `net8.0`'a DÖNME — "8 için destek bitecek" uyarısı var. NuGet paketleri 10.0.1.
- **ASP.NET Core MVC** (Controller + Razor Views). Razor Pages **DEĞİL**.
- **Entity Framework Core 10** (`MosaikContext`). Metadata CRUD için. SP çağrıları ADO.NET/`SqlCommand` ile. Dapper yok.
- **SQL Server** — DB adı `Mosaik` (Plan 11 Faz 6 reset 2026-05-06'da tamamlandı). `mcp__sqlserver__*` MCP (canonical, BKM kurumsal DB'leri için). `mcp__portalhub__*` ölü — eski DB adı, kullanma. **MCP allowlist:** master, DerinSISBkm, DerinSISBkmCrm, DerinSISBkmWeb, BKMDATA, EncoreMerkez, BKM. **Mosaik DB allowlist'te yok** — uygulama içi DB sorgusu için MCP değil, dotnet run + SSMS/sqlcmd kullan.
- **Tailwind CSS** (CDN, utility-first) + `wwwroot/assets/css/style.css` custom sınıflar (`btn-brand`, `form-input-brand`).
- **Chart.js 4** + **Font Awesome 6** CDN (dashboard render).
- **Frontend JS:** Vanilla, IIFE pattern, `wwwroot/assets/js/`. jQuery yok.
- **Testler:** xUnit (`Mosaik.Tests/Mosaik.Tests.csproj`). <!-- AUTO:TEST_COUNT -->480<!-- /AUTO:TEST_COUNT --> test geçiyor.

### Ana klasörler
- `Mosaik/Controllers/` — <!-- AUTO:CONTROLLERS -->`Admin`, `Ai`, `Auth`, `Calendar`, `Comments`, `Compliance`, `Contracts`, `Dashboard`, `Documents`, `Escalation`, `Home`, `Inbox`, `Logs`, `Notifications`, `Obligations`, `OrgChart`, `Profile`, `Reports`, `Search`, `Test`, `Workflow`<!-- /AUTO:CONTROLLERS -->
- `Mosaik/Models/` — EF entities
- `Mosaik/ViewModels/` — view-model wrapper'ları
- `Mosaik/Views/` — Razor views, `_AppLayout.cshtml` ana layout
- `Mosaik/Services/` — `PasswordHasher`, `AuditLogService`, `DashboardRenderer`
- `Mosaik/Database/` — SQL migration + seed + SP scriptleri (<!-- AUTO:MIGRATION_RANGE -->00_ → 79_<!-- /AUTO:MIGRATION_RANGE -->, <!-- AUTO:MIGRATION_COUNT -->80<!-- /AUTO:MIGRATION_COUNT --> dosya)
- `Mosaik/wwwroot/assets/{js,css}/` — static assets

---

## 2. Çalışma Prensipleri (kullanıcı direktifleri)

Ayrıntılı kurallar `.claude/rules/` altında — burada sadece değişmez prensipler.

1. **Sistematik çalış.** Her karar + kural + talimat dosyaya yazılır (CLAUDE.md, TODO.md, `.claude/rules/`, `docs/ADR/`, `docs/journal/`). Konuşma hafızasında kalmaz. Detay: [`docs/CONTEXT_MANAGEMENT.md`](docs/CONTEXT_MANAGEMENT.md).

2. **Skill + agent + MCP — ANA ÇALIŞMA PRENSİBİ.** Kullanıcı kararı (2026-05-07): "İşleri mutlaka subagent ve skill kullanarak yapmalısın senin ana çalışma prensibin olmalı." Subagent + skill **default**, manuel iş **istisnai**. Hatırlatma bekleme. Detay: [`.claude/rules/session-protocol.md`](.claude/rules/session-protocol.md) → "Skill/Agent/MCP proaktif kullanım — ANA ÇALIŞMA PRENSİBİ" + [`memory/feedback_subagent_skill_ana_prensip.md`].
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
     - `security-reviewer` — `security-principles.md` 10 kural denetimi (file:line + attack path + fix)
     - `mosaik-portal-danismani` — portal/süreç MODELLEME salt-okuma danışmanı (status enum→lookup, EntityRelations, modül izolasyon, Process/aspect). Karar tablosu + confidence + file:line. Kod yazmaz. Router: `.claude/rules/advisor-skills.md`. **Restart sonrası aktif (agent hot-reload yok).**
   - **Slash commands (`.claude/commands/`):**
     - `/feature-dev` — 7 fazlı guided feature development (discovery → exploration → clarify → architect → implement → review → summary)
     - `/security-check [range]` — 3 paralel agent (security-reviewer + silent-failure-hunter + OWASP sweep) güvenlik denetimi
     - `/review-pr` — multi-agent comprehensive PR review
   - **Proje skill'leri:** `session-handoff` (oturum sonu journal + auto-commit + memory kaydet + NotebookLM Brain push), `plan-tracker` (TodoWrite ↔ TODO.md senkron), `notebooklm` (Google NotebookLM CLI — podcast/video/rapor/quiz üret), `mosaik-csharp-razor` + `mosaik-css-expert` + `mosaik-js-expert` + `mosaik-security` (proje-spesifik kod yazım uzman skill'leri — yeni controller/view/JS/POST/SQL yazarken otomatik tetiklenir), `css-classify` (CSS ekleme öncesi 3 adım karar), `consolidate-mosaik` (çatı curator — plans/rules/skills/ADR/MEMORY stale+dup archive-only budama, footprint-ladder kardeşi).
   - **Frontend tasarım skill'leri:** `ui-ux-pro-max` (ana UI/UX audit) + `accessibility-compliance` (WCAG). _Not (2026-06-29): generic Anthropic design yığını (frontend-design, visual-design-foundations, design-system-patterns, interaction-design, responsive-design) `.claude/_archive/skills/`'e taşındı — Tailwind+tokens.css zaten karşılıyor, ui-ux-pro-max yeterli. Gerekirse geri alınır (footprint-ladder)._
   - **`ui-ux-pro-max`** (M-13 sırasında en güçlü audit aracı) — 161 color palette, 99 UX guideline, 25 chart type, 10 priority-ranked rule kategorisi (accessibility CRITICAL → charts LOW). UI değişikliği yaparken otomatik tetiklenir, WCAG contrast/touch target/anti-pattern checklist uygular.
   - **`llm-council`** — 5 bağımsız danışman + peer review + chairman sentezi. Tetikleyici: "council this" / "war room this" / "pressure-test this". Mimari seçim, önceliklendirme, scope kararı gibi gerçek tradeoff'larda kullan. Plan sistemi onaylanmış kararlar için değil, "hangi yol" belirsizliği için.
   - Hazır skill'ler: `security-review`, `review`, `simplify`, `init`, `consolidate-memory`, `schedule`, `loop`, `claude-api`.
   - Gereken skill yoksa: `WebFetch`/`WebSearch` araştır, veya `.claude/skills/` altına yarat.
   - MCP'ler: `mcp__sqlserver__*` (DB), `mcp__Claude_in_Chrome__tabs_context_mcp` (browser test), `mcp__Claude_Preview__*` (live preview).

3. **Dil kuralı.** Kod + SQL table/column = İngilizce. UI metni + dokümantasyon = Türkçe (UTF-8, "Düzenle"/"Bileşen" — ASCII sadeleştirme yok). Detay: [`.claude/rules/turkish-ui.md`](.claude/rules/turkish-ui.md).

4. **Güvenlik öncelikli.** Büyük değişiklik öncesi/sonrası `security-review` skill'i çalıştır. Detay: [`.claude/rules/security-principles.md`](.claude/rules/security-principles.md).

5. **Commit disiplini.** Kullanıcı açıkça istemeden commit etme. Uncommitted dosya sayısı 15'i aşarsa yeni iş başlamadan önce commit-split zorunlu. Detay: [`.claude/rules/commit-discipline.md`](.claude/rules/commit-discipline.md).

6. **Plan-First (Tier sistemi).** 3+ klasöre dokunan / schema-security-UX / kullanıcı-görünür / harici dep işler **Tier 3** → `plans/NN-<slug>.md` tam plan zorunlu, kullanıcı onaylamadan implement etme. Tier 1 (typo) plansız, Tier 2 (küçük feature) TODO satırı yeterli. Detay: [`.claude/rules/plan-first.md`](.claude/rules/plan-first.md), [`docs/ADR/010-plan-first-tier-system.md`](docs/ADR/010-plan-first-tier-system.md).

7. **Bağlam yönetimi disiplini.** Aynı bilgi iki yerde yaşamaz (CLAUDE.md kimlik, `.claude/rules/` kural, `TODO.md` plan, `docs/ADR/` karar, `docs/journal/` günlük). Detay: [`.claude/rules/session-memory.md`](.claude/rules/session-memory.md).

8. **200 satır eşiği.** CLAUDE.md ve her `.claude/rules/*.md` dosyası 200 satır altında. Aşarsa konu bölünür.

9. **Kodlama disiplini.** Spekülatif feature/abstraction ekleme, drive-by refactoring yapma. Detay: [`.claude/rules/coding-discipline.md`](.claude/rules/coding-discipline.md).

10. **Yanıt özlülüğü.** Uzun açıklama yasak. Yap, bir cümleyle bildir, devam et. Özet / iltifat / adım duyurusu yok. Detay: [`.claude/rules/response-style.md`](.claude/rules/response-style.md).

11. **Büyük değişiklik öncesi çek-liste.** Silme / rename / refactor / route kaldırma öncesi `docs/ARCHITECTURE_MAP.md` oku + referans tara + V1/V2 deprecated tablosu kontrol et. Tahmin etme — doğrula. Detay: [`.claude/rules/before-major-change.md`](.claude/rules/before-major-change.md).

12. **TODO doğrulama disiplini.** `TODO.md` / journal / memory'deki "HIGH X açık" listesi kanıt değil **hipotez**. Action almadan önce her madde için file:line ile `Read`+`Grep` doğrulama zorunlu (paralel, tek mesajda). "Post-review hardening" commit'i son 7 günde varsa backlog muhtemelen stale. Detay: [`.claude/rules/todo-verification.md`](.claude/rules/todo-verification.md).

13. **Test disiplini — testleri kapatmadan yap (2026-05-14 kullanıcı kararı).** Yeni feature/bug fix/refactor `dotnet test` yeşil olmadan kapatılmaz. **Build yeşil ≠ test yeşil.** Yeni test eklendiğinde en az 1 koşum yapılır, failure varsa fix et veya scaffolding'i geri al — yarım test commit'leme. Detay: [`.claude/rules/test-discipline.md`](.claude/rules/test-discipline.md).

14. **Footprint ladder — yeni yetenek en dar basamakta (2026-06-29).** Yeni ihtiyaç → mevcut rule/skill/view'i genişlet < yeni skill < rule < agent < ADR < plan < modül (son çare). Yeni dosya açmadan mevcut listeyi kontrol et; dup yaratma. Biriken yapıyı `consolidate-mosaik` skill ile **archive-only** buda. Detay: [`.claude/rules/footprint-ladder.md`](.claude/rules/footprint-ladder.md).

---

## 3. Mimari Durumu

**Olgunluk (iki ölçü — karıştırma):**
- **Mevcut özellik seti:** ~%75. Reports + Dashboard + Contracts/Obligations + AI extraction + Tamim + OrgChart canlı kullanıma hazır.
- **vNext "iç portal" vaadi:** ~%40-50. 10 modülden 3 tam (Reports, Dashboard, Tamim), 1 yarım (Documents), 6 yok (SOP, Comment/Mention, Form/Anket, Workflow Designer, Duyuru, KPI/OKR).
- **Yön belgesi:** [`docs/VISION.md`](docs/VISION.md) — vNext kalbi **SOP+Comment+Workflow Designer üçlüsü** (~6-9 hafta).

Detaylı mimari notlar ve bilinen tutarsızlıklar: [`.claude/rules/architecture.md`](.claude/rules/architecture.md).

Aktif mimari tartışmalar ve yol haritası: [`TODO.md`](TODO.md) — "EN ÜST ÖNCELİK" bölümü + [`docs/VISION.md`](docs/VISION.md).

Karar kayıtları: [`docs/ADR/`](docs/ADR/) — ADR-001 (data-access), ADR-002 (modular-monolith), ADR-003 (role-model), ADR-004 (skill-design-principles), ADR-005 (dashboard-architecture), ADR-006 (datetime-utc), ADR-007 (named-result-contract), ADR-008 (dashboard-builder-v2), ADR-009 (report-type-consolidation), ADR-010 (plan-first-tier), ADR-011 (sidebar-shell-layout), ADR-012 (pk-scheduler-firma-filter), ADR-013 (multi-db-topology), ADR-014 (frontend-stack-layering), ADR-015 (new-modules-separate-assembly).

---

## 4. Geliştirme Workflow

### Build + run + test
```bash
# Build
cd D:/Dev/reporthub/Mosaik && dotnet build --nologo

# Run (dev)
cd D:/Dev/reporthub/Mosaik && dotnet run
# URL: http://localhost:5197

# Test
cd D:/Dev/reporthub && dotnet test
```

### Smoke test
- Tarayıcı: `http://localhost:5197/` → login → rapor çalıştır → dashboard aç → export et.
- DB: `mcp__sqlserver__sql_query`.
- JS parse kontrolü: `node -e "new Function(require('fs').readFileSync('path/to.js'))"`.

### Dosya konvansiyonları (özet — detay `.claude/rules/`)
- C#: [`.claude/rules/csharp-conventions.md`](.claude/rules/csharp-conventions.md) (+ C# 14 modern features section)
- Hata yönetimi: [`.claude/rules/error-handling.md`](.claude/rules/error-handling.md) (Result pattern + exception disiplini)
- Razor: [`.claude/rules/razor-conventions.md`](.claude/rules/razor-conventions.md)
- SQL: [`.claude/rules/sql-conventions.md`](.claude/rules/sql-conventions.md)
- JS: [`.claude/rules/js-conventions.md`](.claude/rules/js-conventions.md)

### Roslyn MCP (2026-05-27)
- `cwm-roslyn-navigator` user-scope MCP — 15 semantic tool (find_symbol/references/callers, detect_antipatterns, find_dead_code, get_diagnostics). Token kazancı ~10x vs file scan. **Claude Code restart sonrası `mcp__cwm-roslyn-navigator__*` aktif.**

---

## 5. Bilinen Sorunlar (referans)

- **Kaspersky EBADF** — Claude Code token rename hatası. BKM kurumsal AV `.tmp-*` kilitliyor. Kozmetik, işlevsellik etkilenmiyor. Kalıcı çözüm IT'den exclusion. Detay: [`.claude/rules/known-issues.md`](.claude/rules/known-issues.md).

---

## 6. Hızlı Referans — Sık Bakılan Dosyalar

| Amaç | Dosya |
|---|---|
| Rapor çalıştırma logic | `Mosaik/Controllers/ReportsController.cs` |
| Admin user CRUD | `Mosaik/Controllers/AdminController.cs:937-1183` |
| Dashboard render motoru | `Mosaik/Services/DashboardRenderer.cs` |
| Dashboard builder JS | `Mosaik/wwwroot/assets/js/dashboard-builder.js` |
| User data filter enjeksiyon | `Mosaik/Controllers/ReportsController.cs:875` |
| SP önizleme endpoint | `Mosaik/Controllers/AdminController.cs` → `SpList`, `SpPreview` |
| User modeli | `Mosaik/Models/User.cs` |
| DB context | `Mosaik/Models/MosaikContext.cs` |
| Audit log servisi | `Mosaik/Services/AuditLogService.cs` |
| Şifreleme | `Mosaik/Services/PasswordHasher.cs` |
| Ana layout | `Mosaik/Views/Shared/_AppLayout.cshtml` |
| Uygulama başlatma | `Mosaik/Program.cs` |
| Proje hedef framework | `Mosaik/Mosaik.csproj` |
