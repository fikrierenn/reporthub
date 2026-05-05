## Plan 08 — LLM Wiki / Obsidian Brain (Karpathy Pattern)

**Tarih:** 2026-05-04
**Yazan:** Claude (Fikri yönetiminde, Karpathy LLM Wiki gist referansıyla)
**Durum:** Uygulamada — Faz 1-6 ✓, Faz 7 (smoke test + arşivle) sonraki oturum
**Tier:** 3 (yeni klasör/sistem, multi-tool, kullanıcı-görünür workflow değişikliği, plan-first zorunlu)
**İlişkili:**
- Karpathy gist: <https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f>
- NotebookLM Brain (mevcut, ID `f2407372`) — paralel kalacak
- Memory: `feedback_session_start_hook.md`, `reference_brain_notebook.md`

---

### 1. Problem

Bilginin üç yerde dağılmış olması:

1. **Repo-içi markdown** (`.claude/rules/`, `docs/journal/`, `TODO.md`, `plans/`, `docs/ADR/`) — proje-spesifik, repo'ya bağlı, cross-project öğrenmeleri kapsayamıyor.
2. **NotebookLM Brain** (cloud, `f2407372`) — Q&A iyi ama statik kaynak listesi, LLM güncellemiyor, cross-link yok, mobil okuma için ideal ama günlük çalışma için değil.
3. **Auto-memory** (`C:\Users\fikri.eren\.claude\projects\D--Dev-reporthub\memory\`) — machine-local, oturuma özel, başka projelere taşınmıyor, görsel browse imkanı yok.

**Eksik:** Karpathy'nin "LLM Wiki" pattern'i — LLM'in sürekli maintain ettiği, cross-link'li, compounding (katlanarak büyüyen), local-first markdown vault. Hem proje-üstü öğrenmeler (sektör bilgisi, AI workflow, kişisel meta) hem proje sentezi tek yerde toplansın, Obsidian graph view ile görselleşsin.

**Hedef:** ReportHub repo'su gibi mevcut markdown ekosistemini bozmadan, **paralel** bir cross-project brain vault'u kur. NotebookLM Q&A endpoint olarak kalır, Obsidian günlük çalışma + LLM-maintained bilgi ağı olarak gelir.

---

### 2. Scope

#### Kapsam dahili

- **Vault** `D:/Dev/brain` — git-versioned (private), Karpathy şemasına göre `entities/`, `concepts/`, `synthesis/`, `raw/`, `log.md`, `index.md`, `CLAUDE.md` yapısı.
- **Obsidian** kurulum + vault aç + 4 plugin (Dataview, Templater, Periodic Notes, Web Clipper).
- **Schema** (`brain/CLAUDE.md`) — Claude için talimatlar: "her oturum sonu handoff'tan sonra brain'e ne yazılır, hangi entity güncellenir, lint nasıl çalışır".
- **Initial seed** — 5-7 entity/project sayfası (ReportHub, BKM Kitap, Belinza, Anthropic skills, kişisel profil, AI workflow, bağlam yönetimi).
- **Yeni skill** `wiki-keeper` — `session-handoff` skill'ini extend eder, oturum sonu brain'e auto-update yazar (commit etmez, manuel review).
- **NotebookLM senkron** — haftalık manuel script: `brain/synthesis/*.md` ve `entities/projects/*.md` NotebookLM Brain'e push.
- **Cross-link strateji** — repo CLAUDE.md → brain'deki `entities/projects/reporthub.md`'a referans (file:/// URL veya markdown link).

#### Kapsam dışı

- **Mobil sync** (Obsidian Sync $4/ay) — başlangıçta git yeterli; mobil ihtiyaç netleşince ayrı karar.
- **Multi-machine sync** — şimdilik tek makine (BT-FIKRI), git push/pull ile ileride genişler.
- **AI plugin'ler** (Smart Connections, Copilot for Obsidian) — başlangıçta yok; Claude Code zaten dışarıdan vault'u maintain ediyor. Sonradan değerlendirilir.
- **PARA / Zettelkasten gibi popüler metodolojiler** — Karpathy'nin daha minimal "entity/concept/synthesis" şeması seçildi, scope creep yok.
- **Repo'daki mevcut markdown'ı brain'e taşımak** — repo'da kalır, brain SADECE özet/synthesis/cross-project bilgi tutar. Kaynak repo'da, sentez brain'de.

#### Etkilenen dosyalar / dizinler

**Yeni (brain vault):**

```
D:/Dev/brain/                           ← yeni git repo (private)
├── .gitignore                          ← .obsidian/workspace.json vb. cache hariç
├── .obsidian/                          ← Obsidian config (git'e dahil)
│   ├── plugins/
│   ├── community-plugins.json
│   └── app.json
├── CLAUDE.md                           ← brain schema (Claude talimatları)
├── README.md                           ← human-readable kılavuz
├── log.md                              ← append-only changelog
├── index.md                            ← master katalog (Dataview ile)
├── entities/
│   ├── people/
│   │   └── fikri.md                    ← kişisel profil (skill'den seed)
│   ├── projects/
│   │   ├── reporthub.md                ← ReportHub project page
│   │   ├── bkm-kitap.md                ← BKM Kitap
│   │   └── belinza.md                  ← Belinza Tek Tuş / BelOps
│   ├── companies/
│   │   ├── bkm.md
│   │   └── anthropic.md
│   └── systems/
│       ├── claude-code.md
│       └── sql-server.md
├── concepts/
│   ├── architecture/
│   │   └── plan-first-tier-system.md   ← ADR-010'un özeti
│   ├── patterns/
│   │   └── llm-wiki.md                 ← Karpathy gist'inin özeti
│   └── principles/
│       └── context-management.md       ← CONTEXT_MANAGEMENT.md özeti
├── synthesis/
│   └── 2026-W18.md                     ← haftalık özet (Periodic Notes plugin)
└── raw/                                ← manuel eklenecek (Web Clipper output)
    └── 2026-05-04-karpathy-llm-wiki.md ← gist referans
```

**Yeni (ReportHub repo):**

- `.claude/skills/wiki-keeper/SKILL.md` — yeni skill, handoff sonrası brain auto-update.
- `.claude/skills/wiki-keeper/templates/` — entity/synthesis template'leri.

**Değişen (ReportHub repo):**

- `CLAUDE.md` — sonuna kısa bölüm: "Cross-project brain: D:/Dev/brain (Plan 08, ayrı vault)".
- `.claude/rules/session-protocol.md` — oturum sonu adımına "wiki-keeper skill (Plan 08 sonrası aktif)" notu.
- `MEMORY.md` (auto-memory) — `reference_brain_obsidian.md` yeni entry: vault path + workflow.

**Tahmini boyut:** ~25 yeni markdown (vault seed) + 2 ReportHub dosya değişiklik + 1 yeni skill (3-5 dosya).

---

### 3. Alternatifler

#### A: ReportHub repo'sunu doğrudan Obsidian vault olarak aç

**Açıklama:** Tek vault, ReportHub kök = vault. Mevcut markdown (journal, rules, plans) Obsidian graph view'a girer, ek setup yok.

**Reddetme sebebi:**
- Cross-project değil — sadece ReportHub bilgisi, BKM Kitap/Belinza ayrı kalır.
- `.git/`, `bin/`, `obj/`, `node_modules/` Obsidian indexer'i yorar.
- Repo collaborator'leri (gelecek olsa) brain'i görür — kişisel meta sızar.
- Kullanıcının asıl isteği "**hem proje hem ortak**" — A bunu vermez.

#### B: Tek mega-vault, repo'lar symlink olarak içeride

**Açıklama:** `D:/Dev/brain` ana vault, içinde `projects/reporthub` symlink ile `D:/Dev/reporthub` repo'suna bağlanır.

**Reddetme sebebi:**
- Windows'ta symlink admin gerektirir veya developer mode aktif olmalı, kırılgan.
- Git iki tarafta confused olur (vault git + repo git iç içe).
- Symlink Obsidian'da bazı plugin'lerle (Dataview indexing) sorunlu.
- Cross-platform taşıma zor.

#### C (SEÇİLEN): Çoklu vault, brain ayrı, repo'lara markdown linkle bağlı

**Açıklama:**
- `D:/Dev/brain` bağımsız git repo, Obsidian vault.
- ReportHub gibi proje repo'ları kendi başlarına kalır (mevcut markdown ekosistemi bozulmaz).
- Brain'de `entities/projects/reporthub.md` özet sayfası — kritik bilgilerin senteze + repo link.
- Cross-link: `[ReportHub repo](file:///D:/Dev/reporthub)` veya `obsidian://open?vault=reporthub-vault&file=...` (ileride repo da vault olursa).
- Wiki-keeper skill repo'da çalışır, handoff'tan sonra brain'e yazar.

**Sebep:** Kullanıcının "hem proje hem ortak" hayalini direkt karşılar. Mevcut workflow'u bozmaz. Git basit. Obsidian'ın switcher ile multi-vault zaten desteklenir. Ölçeklenir (BKM Kitap, Belinza vault'a eklenir, repo'lar bağımsız).

#### D: Sadece auto-memory genişlet, Obsidian yok

**Açıklama:** Mevcut `~/.claude/projects/*/memory/` sistemini büyüt, "global" memory ekle, görsel için VS Code markdown preview yeter.

**Reddetme sebebi:**
- Görsel graph yok, link traversal yok, plugin ekosistemi yok.
- Karpathy pattern'inin asıl gücü "LLM bookkeeping + insan görsel browse" — VS Code markdown bunu vermez.
- Mobil okuma yine yok (NotebookLM bu rolü dolduruyor — ona dokunmuyoruz).

---

### 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| Brain vault'u tutarsız büyür (entries duplicate, stale, contradictions) | Orta | Yüksek | `wiki-keeper` skill'inde "lint" mode — haftalık tarama, stale tespit |
| Claude oturumu vault'a yanlış yazar (bug, hallucination) | Yüksek | Düşük-Orta | Skill commit etmez, sadece staged change yazar; Fikri review eder, manuel commit |
| Mevcut workflow bozulur (handoff/journal kafası karışır) | Orta | Düşük | Repo journal **kaynak** olarak kalır, brain sadece **sentez** alır. Kural net: ham bilgi repo'da, sentez brain'de |
| Git private repo maliyeti / setup karmaşıklığı | Düşük | Düşük | GitHub Pro mevcut (varsayım), private repo ücretsiz; başlangıçta local-only git, push sonra |
| NotebookLM ile çakışma (hangisi primary?) | Düşük | Orta | Net rol ayrımı: Brain = primary maintained source, NotebookLM = haftalık snapshot Q&A endpoint |
| Cross-link kırılganlığı (file:/// path'ler taşınınca patlar) | Düşük | Düşük | Sadece relative path'ler vault içinde; cross-vault link `obsidian://` URI scheme |
| Vault git'in kişisel bilgi sızdırması (commit mesajları, .obsidian config) | Yüksek | Yok | Local-only git, GitHub push yok (kullanıcı kararı 2026-05-04) |
| Disk/makine kaybı brain'i sıfırlar | Yüksek | Düşük | Backup yok — kullanıcı kabul; ileride OneDrive/external disk opsiyonel |

---

### 5. Done Criteria

#### Faz 1 — Vault iskelet (~1.5h)

- [ ] `D:/Dev/brain` git repo init — **local-only** (GitHub'a push yok, kullanıcı kararı 2026-05-04)
- [ ] `.gitignore` (`.obsidian/workspace.json`, `.obsidian/cache/`, `*.tmp`)
- [ ] Klasör yapısı: `entities/{people,projects,companies,systems}`, `concepts/{architecture,patterns,principles}`, `synthesis/`, `raw/`
- [ ] `index.md` (Dataview master query)
- [ ] `log.md` (append-only changelog, ilk satır "2026-05-04 vault init")
- [ ] `CLAUDE.md` (brain schema — Claude için talimatlar)
- [ ] `README.md` (Fikri için kullanım kılavuzu, 1 sayfa)

#### Faz 2 — Obsidian setup (~30dk)

- [ ] Obsidian indir + kur (https://obsidian.md, Windows .exe)
- [ ] `D:/Dev/brain` vault olarak aç
- [ ] Plugin: Dataview, Templater, Periodic Notes, Web Clipper (browser uzantısı)
- [ ] Tema: default veya minimal (kullanıcı seçer)
- [ ] Hotkey: `Ctrl+Shift+O` brain switcher (Obsidian default)

#### Faz 3 — Initial seed (~3h)

- [ ] `entities/people/fikri.md` (skill `fikri-profil`'den özet)
- [ ] `entities/projects/reporthub.md` (CLAUDE.md + son 5 journal sentezi)
- [ ] `entities/projects/bkm-kitap.md` (skill bilgisinden)
- [ ] `entities/projects/belinza.md` (skill `belinza-baglan` özeti)
- [ ] `entities/companies/{bkm,anthropic}.md`
- [ ] `entities/systems/{claude-code,sql-server}.md`
- [ ] `concepts/patterns/llm-wiki.md` (Karpathy gist özeti)
- [ ] `concepts/principles/context-management.md` (`docs/CONTEXT_MANAGEMENT.md` özeti)
- [ ] `concepts/architecture/plan-first-tier-system.md` (ADR-010 özeti)
- [ ] Cross-link: her sayfa en az 2 backlink (graph görünür olsun)

#### Faz 4 — wiki-keeper skill (~3h)

- [ ] `.claude/skills/wiki-keeper/SKILL.md` (description, tetikler, workflow)
- [ ] Template'ler: `entity-template.md`, `synthesis-template.md`, `concept-template.md`
- [ ] Workflow: handoff sonrası → journal'ı parse et → ilgili entity güncelle / yeni concept ekle / synthesis week dosyasına satır ekle
- [ ] **Commit etmez** — sadece staged changes; kullanıcı review edip manuel commit
- [ ] Lint mode: `wiki-keeper lint` → stale claim, orphan, duplicate, broken link tespiti

#### Faz 5 — NotebookLM senkron (~1h)

- [ ] `brain/scripts/sync-notebooklm.sh` (manuel çalıştırılır, haftalık)
- [ ] `entities/projects/*.md` ve `synthesis/*.md` → NotebookLM Brain'e push (eski sürümler delete + yeni add, encoding fix)
- [ ] Test: bir entity güncelle → script çalıştır → NotebookLM'de yeni sürüm görünür

#### Faz 6 — ReportHub entegrasyonu (~30dk)

- [ ] `CLAUDE.md` (ReportHub) — yeni bölüm "Cross-project brain"
- [ ] `.claude/rules/session-protocol.md` — handoff sonrası wiki-keeper notu
- [ ] Auto-memory yeni entry: `reference_brain_obsidian.md` (path + workflow)
- [ ] `MEMORY.md` index güncelle

#### Faz 7 — Smoke + commit (~1h)

- [ ] Brain vault git push (lokalde commit sequence + opsiyonel GitHub push)
- [ ] ReportHub commit: `feat(brain): cross-project Obsidian brain (plan: 08)`
- [ ] Plan dosyası arşivle: `git mv plans/08-*.md plans/archive/`
- [ ] TODO.md kapanış
- [ ] Bir sonraki handoff'ta `wiki-keeper` skill tetikleyip vault güncellemesini Fikri review eder

**Tahmini toplam:** ~10 saat, 7 faz, 2 git repo etkilenir, 1 yeni skill, 1 yeni vault.

---

### 6. Rollback Planı

Bu plan rollback edilirse:

1. **Vault'u sil:** `rm -rf D:/Dev/brain` (lokal git repo, GitHub'a push edilmediyse iz kalmaz).
2. **ReportHub değişiklikleri revert:** `git revert <feat-brain-commit>` — CLAUDE.md + session-protocol + MEMORY.md geri döner.
3. **Skill kaldır:** `.claude/skills/wiki-keeper/` klasörü sil.
4. **NotebookLM Brain bozulmaz** — push edilen entries kalır, manuel temizlenmek istenirse `notebooklm source delete`.
5. **Veri kaybı:** 0 (vault tamamen yeni, kaynak data repo'da kalır).

Faz-bazlı kısmi rollback: Faz 4-5-6 implement edilmediyse, Faz 1-3 (sadece vault + seed) tek başına kullanılabilir, skill/senkron olmadan manuel olarak çalışır.

---

### 7. Adımlar

#### Faz 1 — Vault iskelet (~1.5h)

1. `mkdir D:/Dev/brain && cd D:/Dev/brain && git init`
2. `.gitignore`, `CLAUDE.md`, `README.md`, `log.md`, `index.md` yaz
3. Klasör yapısı oluştur (`mkdir -p entities/{people,projects,companies,systems}` vb.)
4. İlk commit: `init: brain vault iskeleti (plan: 08)`

#### Faz 2 — Obsidian (~30dk)

5. Obsidian indir + kur (kullanıcı tarafında)
6. Vault olarak `D:/Dev/brain` aç
7. Community plugin'leri etkinleştir + kur (Dataview, Templater, Periodic Notes)
8. Web Clipper browser extension kur (Chrome/Edge)

#### Faz 3 — Initial seed (~3h)

9. `entities/people/fikri.md` — `fikri-profil` skill'i oku, özetini yaz
10. `entities/projects/{reporthub,bkm-kitap,belinza}.md`
11. `entities/companies/{bkm,anthropic}.md`
12. `entities/systems/{claude-code,sql-server}.md`
13. `concepts/patterns/llm-wiki.md` — Karpathy gist tam özet
14. `concepts/principles/context-management.md` — `docs/CONTEXT_MANAGEMENT.md` özet
15. `concepts/architecture/plan-first-tier-system.md` — ADR-010 özet
16. Cross-link sweep — her sayfaya 2+ backlink ekle, graph görünür olsun
17. Commit: `seed: initial entities + concepts (plan: 08)`

#### Faz 4 — wiki-keeper skill (~3h)

18. `.claude/skills/wiki-keeper/SKILL.md` (frontmatter + workflow)
19. Template'ler: entity, synthesis, concept
20. Workflow: handoff parse → entity update → synthesis append → lint check
21. Test: dummy oturum sonu → skill çalıştır → vault'ta staged changes
22. Commit (ReportHub): `feat(skill): wiki-keeper for brain auto-update (plan: 08)`

#### Faz 5 — NotebookLM senkron (~1h)

23. `brain/scripts/sync-notebooklm.sh` yaz
24. Test: bir entity push → NotebookLM list'te görünür
25. Commit (brain): `feat: notebooklm sync script (plan: 08)`

#### Faz 6 — ReportHub entegrasyonu (~30dk)

26. ReportHub `CLAUDE.md` — Cross-project brain bölümü
27. `session-protocol.md` — wiki-keeper notu
28. Auto-memory `reference_brain_obsidian.md` + `MEMORY.md`
29. Commit (ReportHub): `docs(brain): cross-project Obsidian brain reference (plan: 08)`

#### Faz 7 — Smoke + arşivle (~1h)

30. End-to-end test: oturum sonu → handoff → wiki-keeper → brain staged → commit
31. Plan dosyasını arşivle: `git mv plans/08-llm-wiki-obsidian-brain.md plans/archive/`
32. TODO.md kapanış işareti
33. Journal'a smoke test sonucu

---

### 8. Açık sorular (implement öncesi netleşmeli)

- [x] **GitHub Pro?** Karar: GitHub'a push **yok**, brain local-only kalır (kullanıcı 2026-05-04). Backup stratejisi sonra: dış disk veya OneDrive optional.
- [ ] **Obsidian Sync ileride evet mi?** Mobil okuma için $4/ay; başlangıçta hayır, sonra eklenebilir.
- [x] **BKM Kitap repo'su nerede?** Karar: yok. Seed mevcut skill bilgisinden + Fikri'nin kafasından (kullanıcı 2026-05-04).
- [x] **Web Clipper hangi browser?** Karar: **Firefox** (kullanıcı 2026-05-04). Extension: <https://addons.mozilla.org/en-US/firefox/addon/obsidian-web-clipper/>.

---

### 9. İlişkili

- Karpathy gist: https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f
- Önceki plan: `plans/07-yetki-filter-revizyon.md` (paralel iş, blok değil)
- ADR adayı: `docs/ADR/008-cross-project-brain-strategy.md` (Plan 08 tamamlandığında yazılır)
- Auto-memory: `reference_brain_notebook.md` (NotebookLM, mevcut), `reference_brain_obsidian.md` (yeni, Faz 6)

---

### 10. Onay

> Kullanıcı onay verene kadar implement edilmez.

- [x] Plan kullanıcıya gösterildi: 2026-05-04
- [x] Açık sorular netleşti (madde 8): GitHub yok (local-only), Web Clipper Firefox, BKM Kitap repo yok
- [x] Onay alındı: 2026-05-04 (Fikri implicit — vault oluşturdu + Obsidian kurdu)
- [x] Implement edildi (Faz 1-6, ReportHub repo):
  - `56e1f23` docs(brain): CLAUDE.md cross-project brain reference
  - `7fedeb2` feat(skill): wiki-keeper Brain auto-update
  - `08e058a` (Plan 07) Migration 20 FilterDefinition + backfill — *Plan 07 ama paralel iş*
- [x] Implement edildi (Faz 1-5, Brain vault `D:/Dev/brain` lokal git):
  - `init: brain vault iskelet` (Faz 1)
  - `seed: 12 initial entities + concepts` (Faz 3)
  - `feat: notebooklm sync script + log.md Faz 4-6` (Faz 5+log)
- [ ] Faz 7 — Smoke test (wiki-keeper handoff'ta çalışsın, vault güncellenmesini doğrula) + plan arşivle
- [ ] Tamamlandı: <Faz 7 sonrası>
