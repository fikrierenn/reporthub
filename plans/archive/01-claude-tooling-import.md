# Plan 01 — Claude Tooling Import (atlasops + sqlserver-mcp + claude-context-template)

> **NOT:** Bu retro plandır. İş başlandıktan sonra yazıldı (kullanıcı "hepsini yapmalısın" dedi → execution başladı → bu plan paralel yazılıyor). Plan-First kuralının kendi import'u plan-first ile yazılamadığı için (tavuk-yumurta) retro yazıldı; gelecek tüm Tier 3 işler bu şablonu sırayla takip eder.

**Tarih:** 2026-04-27
**Yazan:** Claude (Cowork oturumu, Fikri yönetiminde)
**Durum:** `Uygulamada → Tamamlandı` (commit'lere bölündüğünde kapanır)

---

## 1. Problem

Reporthub'ın `.claude/` ekosistemi atlasops + sqlserver-mcp-server + claude-context-template'e göre 2 katman geride. Eksikler:

- Tier sistemi yok (her iş plansız başlıyor, scope explosion riski).
- Bağlam yönetimi disiplini yazılı değil (session-memory.md yok).
- Multi-agent kod review/explore/architect aracı yok (sadece commit-splitter agent).
- feature-dev / review-pr komutları yok.
- Frontend tasarım skill'leri yok (M-11 F-7 dashboard builder UI fazlarında işe yarar).
- claude-context-template kendisi de eksik — yeni öğeler buraya da yansımalı (diğer projelerin `bootstrap.sh --update` ile alabilmesi için).

Kullanıcı verbatim:

> "d:\dev\atlasops içindeki session kuralları iş yapma kurallarını bu projeye uygulamamız lazım"
> "skiller md dosyaları ne varsa taramalısın aynı zamanda mcp tarafında da kurallar var onları da okumalısın D:\Dev\sqlserver-mcp-server"
> "D:\Dev\claude-context-template içine girip eksik olanları da eklemelisin bu genel olan kısım aslında"
> "hepsini yapmalısın"

## 2. Scope

### Kapsam dahili — Reporthub
- `.claude/rules/session-memory.md` (yeni, atlasops kopyası)
- `.claude/rules/plan-first.md` (yeni, sqlserver-mcp kopyası)
- `.claude/rules/commit-discipline.md` (Plan-First Referansı bölümü ekle)
- `.claude/agents/` — 8 yeni agent (code-architect, code-explorer, code-reviewer, code-simplifier, comment-analyzer, pr-test-analyzer, silent-failure-hunter, type-design-analyzer)
- `.claude/commands/feature-dev.md` + `review-pr.md` (yeni)
- `.claude/skills/` — 7 yeni frontend skill (accessibility-compliance, design-system-patterns, frontend-design, interaction-design, responsive-design, visual-design-foundations, web-component-design)
- `plans/` klasör + README + template + archive/ + bu retro plan
- `docs/PATTERNS.md` (claude-context-template kopyası — gerçek-dünya pattern'leri P-1..P-10)
- `docs/ADR/010-plan-first-tier-system.md` (sqlserver-mcp ADR-003'ten adapte)
- `CLAUDE.md` (yeni rule + agent + command + skill referansları)
- `docs/journal/2026-04-27.md` (oturum kaydı + handoff)

### Kapsam dahili — claude-context-template
- `templates/.claude/rules/_universal/plan-first.md` (yeni)
- `templates/.claude/rules/plan-first.md` (yeni — root duplikat, mevcut pattern)
- `templates/.claude/agents/` — 8 agent (yukarıdakiyle aynı set)
- `templates/.claude/commands/` — feature-dev + review-pr
- `templates/.claude/skills/` — 7 frontend skill
- `templates/plans/README.md` + `feature-template.md` + `archive/.gitkeep`
- `templates/docs/ADR/003-plan-first-tier-system.md` (template versiyon — proje-bağımsız)

### Kapsam dışı
- atlasops-spesifik skill'ler (atlasops-platform, atlasops-skill-consumer, podbul-api-patterns) — proje-bağımlı
- multi-project journal yapısı (sqlserver-mcp'de) — reporthub tek-projeli
- plugins/hookify — opsiyonel standalone plugin, ayrı iş
- session-protocol.md / session-memory.md / commit-discipline.md tam değiştirme — reporthub'daki sürümler proje-spesifik içerik (32-dosyalık backlog, 22 Nisan 2026 anekdotu) içeriyor, korundu
- bootstrap.sh güncellemesi (template'in installer'ı) — sadece içerik kopyalanıyor, install script aynı

## 3. Alternatifler

### A: Sadece kuralları (rules) port et, agent + skill ertele
**Reddetme sebebi:** Kullanıcı "hepsini yapmalısın" dedi — ertele kararı kullanıcıya geri sormalı, kullanıcı zaten kararı verdi.

### B: Tek mega-commit (40+ dosya)
**Reddetme sebebi:** 15-dosya eşiği ihlali. commit-discipline.md kuralı. Dört bucket'a ayrılmalı (rules/plans, agents+commands, skills, template).

### C: Seçilen — bucket'lı port
**Açıklama:** Reporthub için 4 commit + claude-context-template için 1-2 commit. Her bucket bir konu.
**Sebep:** 15-dosya eşiği, commit anlamlı kalır, geri alma kolay.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Frontend skill'ler React/Framer Motion odaklı, reporthub Razor+Vanilla JS | Düşük (skill'ler prensip kaynağı, kod birebir kullanılmaz) | Yüksek | Skill description'ları "when to use" doğru tetikleniyor; içerik referans olarak değerli |
| atlasops/sqlserver-mcp atıfları reporthub için yanlış | Orta (kafa karışıklığı) | Orta | Reporthub-uyumlu sürüm yazıldı (ADR-010, plans/README), generic dosyalar dokunulmadı |
| Template'e yansıtılmazsa diğer projeler eski kalır | Düşük (template'i sen yönetiyorsun) | Orta | Bu plana template port adımları dahil |
| 7 frontend skill description fazla geniş, false-positive tetikleyebilir | Düşük | Orta | Description'ları olduğu gibi kalsın, gerekirse ileride trim edilir |
| commit-discipline.md edit ile reporthub'a özgü "32-dosyalık backlog" kısmı bozulur | Düşük | Düşük | Append-only edit (Plan-First Referansı bölümü en sona) |

## 5. Done Criteria

- [x] Reporthub `.claude/agents/` 9 dosya (commit-splitter + 8 yeni)
- [x] Reporthub `.claude/commands/` 2 dosya
- [x] Reporthub `.claude/skills/` 10 klasör (mevcut 3 + 7 yeni frontend)
- [x] Reporthub `.claude/rules/` session-memory.md + plan-first.md eklendi
- [x] Reporthub `plans/` klasör + 3 dosya (README, template, retro 01)
- [x] Reporthub `docs/PATTERNS.md` mevcut
- [x] Reporthub `docs/ADR/010-plan-first-tier-system.md` mevcut
- [ ] Reporthub `commit-discipline.md` Plan-First Referansı bölümü ekli
- [ ] Reporthub `CLAUDE.md` agent + command + skill listesi güncel
- [ ] claude-context-template'e tüm yeni öğeler yansıtıldı
- [ ] 4-5 ayrı commit at (rules+plans / agents+commands / skills / context-template)
- [ ] Journal entry yazıldı (`docs/journal/2026-04-27.md`)

## 6. Rollback Planı

Bu commit'lerden herhangi birinde sorun çıkarsa:
- `git revert <commit-hash>` — her bucket bağımsız, tek revert kullanışlı kalır
- En riskli bucket: `CLAUDE.md` güncellemesi — referansları yanlış olursa session-start UX kırılabilir; revert hemen toparlar
- Frontend skill'ler safe (deferred load, sadece tetiklendiğinde okunur)

## 7. Adımlar

1. [x] atlasops/sqlserver-mcp/claude-context-template tara — eksiklik raporu çıkar
2. [x] Reporthub'a 8 agent kopyala (`cp` ile)
3. [x] Reporthub'a 2 command kopyala
4. [x] Reporthub'a 7 frontend skill kopyala
5. [x] Reporthub'a session-memory.md + plan-first.md + PATTERNS.md + ADR-010 kopyala
6. [x] plans/ klasör (README + template + archive/) reporthub-uyumlu yaz
7. [x] Retro plan yaz (BU dosya)
8. [ ] commit-discipline.md Plan-First Referansı ekle
9. [ ] CLAUDE.md güncelle
10. [ ] claude-context-template'e tüm yeni öğeleri port et
11. [ ] Bucket'lı commit (4-5 commit)
12. [ ] Journal entry yaz

## 8. İlişkili

- ADR: `docs/ADR/010-plan-first-tier-system.md`
- Kaynaklar:
  - atlasops: `D:/Dev/atlasops/.claude/{rules,agents,commands,skills}/`
  - sqlserver-mcp: `D:/Dev/sqlserver-mcp-server/.claude/rules/plan-first.md` + `plans/`
  - context-template: `D:/Dev/claude-context-template/{docs/PATTERNS.md,templates/}`
- Konuşma: `docs/journal/2026-04-27.md`

## 9. Onay

- [x] Plan kullanıcıya gösterildi (3-faz keşif raporu sunuldu)
- [x] Geri bildirim alındı: "hepsini yapmalısın" + "skill'ler de" + "context-template de"
- [x] Onay alındı: 2026-04-27, Fikri Eren
