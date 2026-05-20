# SKILL_POOL — Dış Skill Havuzu (Mosaik için aday liste)

**Tarih:** 2026-05-21
**Kaynak:** [claudskills.com](https://claudskills.com) — 66,763+ Claude Code skill registry
**Amaç:** Mosaik projesi için işe yarayabilecek skill'leri kategorize et, mevcut skill/agent envanteriyle karşılaştır, port adaylarını işaretle.

> **Not:** Kullanıcı talebi 2026-05-21 — "bir ara tara işe yarayanları yarama ihtiyacı olanları bir havuz yap not al". Bu dosya **canlı backlog** — yeni skill keşfedildiğinde buraya eklenir. Sprint sonu skim → adaylardan birkaçı port edilir.

---

## 1. Site Bilgileri

| Alan | Değer |
|---|---|
| URL | https://claudskills.com/ |
| Skill sayısı | 66,763+ |
| Ücretsiz tier | gözat, ara, kendi skill'ini gönder |
| Pro tier | $9/ay veya **$149 ömür boyu** — kalite puanı + tek tık kurulum (`~/.claude/skills/`) + 6 tema paketi |
| Tema paketleri | Ads Mastery, Outbound Engine, Product Launch, ... |
| SKILL gönderme | Public GitHub repo + `SKILL.md` (YAML frontmatter: name/description/allowed-tools/argument-hint). 24h indeksleme. |

**$149 lifetime kararı:** YAGNI default. Tema paketleri Mosaik scope dışı (Ads/Sales/Marketing). Pro avantajı **tek tık kurulum** — manuel kurulumla aynı sonuç. Pro almak yerine: GitHub repo'ları doğrudan klonla.

---

## 2. Mosaik Mevcut Skill/Agent Envanteri (referans)

Karşılaştırma için — yeni keşif zaten yapılmış işi tekrarlamasın.

### Proje agent'ları (`.claude/agents/`)
- `code-reviewer` — confidence-scored CLAUDE.md compliance + bug detect
- `code-architect` — feature mimari blueprint (file:line, build sequence)
- `code-explorer` — feature trace (entry → data, layer mapping)
- `code-simplifier` — recently-modified clarity refactor
- `code-optimizer` — ASP.NET + EF perf (N+1, AsNoTracking)
- `comment-analyzer` — comment accuracy + rot
- `pr-test-analyzer` — behavioral test coverage
- `silent-failure-hunter` — error handling + catch block audit
- `type-design-analyzer` — invariant + encapsulation rating
- `commit-splitter` — uncommitted bucket split
- `security-reviewer` — security-principles.md 10 kural denetim

### Proje skill'leri (`.claude/skills/`)
- `mosaik-csharp-razor`, `mosaik-css-expert`, `mosaik-js-expert`, `mosaik-security` — domain-spesifik
- `session-handoff`, `plan-tracker`, `wiki-keeper`, `commit-splitter` — workflow
- `notebooklm` — AI Brain push
- `css-classify`, `inline-style-guard` — UI guard
- `frontend-design`, `ui-ux-pro-max`, `visual-design-foundations`, `design-system-patterns`, `interaction-design`, `responsive-design`, `accessibility-compliance` — frontend tasarım
- `llm-council` — multi-LLM danışma
- `vnext-entity-port` — modül çıkarma şablonu
- `sql-migration-writer` — DB migration yazımı
- `bkm-db-explorer` — BKM kurumsal DB
- `htmx-expert`, `alpine-js` — frontend lib uzmanlık
- `feature-dev`, `review-pr`, `security-check` — slash workflow

### Anthropic hazır skill'ler
- `anthropic-skills:docx`, `pptx`, `xlsx`, `pdf` — Office dosya işleme
- `claude-md-management:revise-claude-md`, `claude-md-improver`
- `update-config`, `keybindings-help`, `fewer-permission-prompts`
- `loop`, `schedule` — recurring
- `claude-api`, `init`, `simplify`, `review`, `security-review`

---

## 3. ClaudSkills'ten Aday Skill'ler — Kategorize

### 🔴 YÜKSEK Adaylar (Mosaik'e direkt değer)

| ClaudSkill | Açıklama | Mosaik karşılığı / hedef kullanım | Karar |
|---|---|---|---|
| `qa-security` | OWASP tabanlı güvenlik denetimi | Mevcut: `security-reviewer` agent + `mosaik-security` skill. OWASP-spesifik checklist eksik. | **Skim — OWASP kontrol kalemleri import edilebilir** |
| `write-prd` | 8 bölümlü PRD şablonu + kabul kriterleri | Mevcut: `plans/feature-template.md` (Plan-First Tier 3). PRD bölüm yapısı kıyaslanabilir. | **Skim — plans/feature-template.md zenginleştirilebilir** |
| `pm-discovery` | Ürün keşfi + otomatik PRD üretimi | Mevcut: `code-architect` agent + `llm-council`. PRD üretim adımı eksik. | **Skim — Tier 3 plan üretimini hızlandırır** |
| `research-brainstorm` | Araştırma fikri üretme + test | Mevcut: `llm-council` (5 lens). Brainstorm "kaba fikir" katmanı farklı. | **Düşük öncelik — llm-council yeterli** |
| `dev-api` | REST/GraphQL API geliştirme + doc | Mosaik MVC + cshtml. REST endpoint yazımı için doc skill faydalı (Plan 36 W-13 gibi). | **Düşük — MVC controller pattern zaten net** |

### 🟡 ORTA Adaylar (Bilgi için)

| ClaudSkill | Açıklama | Not |
|---|---|---|
| `code-reviewer` | Genel kod review | Mosaik'in kendi `code-reviewer` agent'ı daha derin (CLAUDE.md compliance). Genel olan değersiz. |
| `audit-contract` | Akıllı kontrat denetimi | Solidity/web3 — alakasız. |
| `supabase-security` | Supabase pentest | Mosaik SQL Server + .NET — alakasız. |
| `methodology-advisor` | Nicel/nitel araştırma yöntem | Akademik — Mosaik kapsam dışı. |
| `design-prototype` | Figma → React + Storybook | Mosaik Razor + Tailwind, React yok. Alakasız. |

### 🟢 DÜŞÜK / Alakasız (Skip)

- `dev-flutter` — Flutter
- `30x-seo-plan`, `seo-dataforseo` — SEO
- `ops-marketing` — Marketing
- `outreach`, `telegram-lead-finder` — Sales/Outreach
- `commit-flask` — Flask (Python)
- `lyric-writer`, `manuscript-drafter` — İçerik üretimi

---

## 4. Daha Derin Tarama Gerekenler (sonraki sprint)

Site arama kutusu üzerinden 66k içinden filtrelenecek anahtar kelimeler:

- `ef core`, `entity framework` — perf/migration örüntüleri
- `dotnet`, `asp.net`, `razor` — framework-spesifik uzman skill'ler
- `sql server`, `stored procedure` — DB optimizasyon
- `tailwind` — CSS framework
- `hangfire`, `background job` — scheduling
- `workflow`, `state machine`, `bpmn` — Plan 36 paralelleri
- `audit log`, `event sourcing` — Plan 38 yakın
- `multi-tenant`, `firma` (TR) — Plan 14 data scope
- `pdf extract`, `ocr` — Plan 27 sözleşme
- `obsidian`, `wiki`, `knowledge graph` — Plan 08 Brain

**Sonraki adım:** site search çalıştır (claudskills.com/search?q=...). 1 oturum, 5-10 kategori paralel tarama.

---

## 5. Çıkarımlar (genel)

1. **66k skill = gürültü oranı yüksek.** Çoğu çok dar/dar domain (akademik manuscript, Solidity audit, Telegram outreach). Mosaik için kullanılabilir ~%5'i.
2. **Mosaik kendi skill envanteri zaten zengin** (~30 skill + ~11 agent). Yeni skill **eklemekten önce mevcudu kullan** disiplini geçerli (kuzey yıldızı kuralı).
3. **Port adayları yazılmadan önce:** mevcut `code-reviewer`/`security-reviewer`/`code-architect` ile çakışıp çakışmadığını doğrula. Çakışıyorsa **mevcudunu güçlendir**, yeni skill açma.
4. **Pro tier ($149) ertelendi.** Tek tık kurulum lüks; manuel `git clone + mv ~/.claude/skills/` aynı sonucu verir.
5. **GitHub repo direkt klon paterni:** SKILL.md frontmatter Anthropic standardıyla uyumlu → `git clone <url> .claude/skills/<name>/` + restart yeterli (skill hot-reload var, agent yok).

---

## 6. Sonraki Sprint Aksiyonları

- [ ] **claudskills.com search** — yukarıdaki 12 anahtar kelime ile 1 oturum derin tarama. Bulguları bu dosyaya §3'e ekle.
- [ ] **qa-security skill** — OWASP checklist'i `.claude/rules/security-principles.md`'ye port karşılaştırması (eksik kalem var mı?). Yeni skill açma; mevcut rule'a satır ekle.
- [ ] **write-prd / pm-discovery** — `plans/feature-template.md`'yi 8-bölüm pattern ile karşılaştır, eksik bölüm varsa ekle.
- [ ] **workflow / event-sourcing / state-machine** etiketli skill'ler — Plan 36 referans/alternatif desen.

---

## 7. İlişkili

- `docs/LIBRARY_RADAR.md` — kütüphane radarı (paralel pattern)
- `CLAUDE.md` §2 — skill/agent/MCP ana çalışma prensibi
- `.claude/skills/` — proje skill envanteri
- `.claude/agents/` — proje agent envanteri
- `D:/Dev/brain/` — cross-project knowledge vault (gerekirse SKILL_POOL özeti push)
