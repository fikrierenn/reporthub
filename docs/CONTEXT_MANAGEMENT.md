# ReportHub — Bağlam Yönetimi Anayasası
_22 Nisan 2026 — 2 paralel araştırma agent'ı + Claude Code resmi dokümanları sentezi_

## Problem Tanımı

Bu projede (ve benzer solo-dev, uzun-yaşam projelerinde) gözlemlenen bağlam çöküşü belirtileri:

| Belirti | Sebep (tespit edildi) |
|---|---|
| "Dün konuştuğumuz karar hatırlanmıyor" | Kararlar CLAUDE.md'de değil, konuşma geçmişinde kaldı — `/compact` yedi |
| "32 dosya commit'siz birikti" | Oturum sonu commit disiplini yok, 3 feature paralel ilerliyor |
| "CLAUDE.md her gün şişiyor" | Session journal (her oturumun ayrıntısı) CLAUDE.md'ye yazılıyor — yanlış yer |
| "Sub-agent proje kurallarını bilmiyor" | Sub-agent CLAUDE.md görüyor ama skill'leri görmüyor; path-scoped rule'lar onun cwd'sine denk gelmiyor |
| "Claude bir konuda başladığı anda başka şeyle uğraşmaya başlıyor" | Scope containment yok, kullanıcı anlık talep ediyor, plan atlanıyor |
| "`/compact` sonrası Türkçe UI kuralı unutuldu" | Turkish-ui rule'u `paths:` ile nested — compact sonrası kayıp |
| "Çoklu makine kullanırsam auto-memory kaybolur" | Auto-memory **machine-local**, cloud'a sync olmaz, git'e girmez |

## Tasarım İlkeleri (anayasa)

Aşağıdaki 7 kural **pazarlıksız** — CLAUDE.md'nin başına referanslanır, skill'lerde, hook'larda uygulanır.

### İlke 1 — Üç katman ayrımı

Her bilgi **tam olarak bir yerde** yaşar:

| Katman | Nerede | Ne yazılır | Örnek |
|---|---|---|---|
| **Kimlik** | `CLAUDE.md` | Proje tanımı, stack, değişmez kurallar, dosya konvansiyonları | "Net 10, EF Core, SP çağrıları ADO.NET" |
| **Kurallar** | `.claude/rules/*.md` | Davranış kuralları, konuya göre bölünmüş | SQL yazım kuralları, Razor kuralları, Türkçe UI |
| **Süreç** | `TODO.md` + `docs/ADR/` + `docs/journal/` | Planlar, geçmiş kararlar, oturum notları | "Sprint 1 planı", "SP mi EF mi kararı" |

**KURAL:** Aynı bilgi iki yerde durmaz. Yer seçiminde:
- Her oturumda mı gerekli? → CLAUDE.md
- Belli bir konuda iş yapılırken mi gerekli? → `.claude/rules/<konu>.md`
- Tarihli, geçici, referans mı? → `docs/journal/` veya `docs/ADR/`

### İlke 2 — 200 satır eşiği

- `CLAUDE.md` **her zaman 200 satır altında** kalmalı (hedef: 100-150).
- Şu an ~300 satır. Bu oturumda kırılacak.
- Bir `.claude/rules/*.md` dosyası da 200 satır altında — aşarsa konu bölünür.

### İlke 3 — Session journal CLAUDE.md'de yaşamaz

"Bu oturumda olanlar" bölümü **yanlış yer**. Alternatifler:
- `docs/journal/YYYY-MM-DD.md` (git'te, tarih bazlı)
- `~/.claude/projects/.../memory/` (auto-memory, makine yerel)
- `/export` ile Markdown dump (session kapatırken)

**KURAL:** CLAUDE.md'de geçmiş tarih yok. Sadece *bugün geçerli kurallar*.

### İlke 4 — 15 dosya eşiği

Uncommitted dosya sayısı 15'i aştığında **yeni iş başlamadan önce commit-split** zorunlu.
- Şu an: 32 (iki kat aşım). Bu oturumda kırılacak.

### İlke 5 — 3 paralel özellik eşiği

Aynı anda 3'ten fazla in-flight feature olmaz. Şu an:
- Dashboard builder UX
- SP Önizle (buggy)
- SP mimarisi refactor
- User management P1
- Mimari borç kapatma

Bu 5 eşiği aşıyor. Her oturumda **tek öncelik seçilir**, diğerleri pending.

### İlke 6 — Spec → Plan → Execute

Herhangi bir iş 3 dosyadan fazlasına dokunacaksa:
1. Kullanıcıya `AskUserQuestion` ile 2-4 soruda net tanım çıkart
2. TODO.md'ye kısa plan yaz
3. Sonra koda el at

"Hızlıca şunu yap" doğrudan koda başlamak → scope explosion, 32-dosyalık backlog.

### İlke 7 — Karar kalıcılığı

Her "hmm şöyle mi yapsak" tartışmasının sonu bir karar olmalı ve kalıcı yer bulmalı:
- Büyük mimari seçim → `docs/ADR/NNN-*.md`
- Küçük konvansiyon → `.claude/rules/<konu>.md`
- Bir seferlik iş → `TODO.md`

"Konuşmada kaldı" = `/compact`'ten sonra kayıp.

---

## Dosya Mimarisi (hedef yapı)

```
D:/Dev/reporthub/
├── CLAUDE.md                            # ~100 satır. Kimlik + kurallar indeksi
├── TODO.md                              # Aktif sprint + backlog. Tarih notu yok
├── .claude/
│   ├── settings.json                    # Hook kayıtları + permission
│   ├── settings.local.json              # .gitignore'd, makine yerel override
│   ├── rules/
│   │   ├── architecture.md              # Değişmez mimari kararlar (no paths, compact'te survive)
│   │   ├── security-principles.md       # XSS, SQL injection, audit log (no paths)
│   │   ├── turkish-ui.md                # Türkçe karakterler, metin kuralları (no paths)
│   │   ├── commit-discipline.md         # Git/commit stratejisi (no paths)
│   │   ├── sql-conventions.md           # paths: ReportPanel/Database/**/*.sql
│   │   ├── razor-conventions.md         # paths: **/*.cshtml
│   │   ├── js-conventions.md            # paths: wwwroot/assets/js/**
│   │   └── csharp-conventions.md        # paths: **/*.cs
│   ├── skills/
│   │   ├── session-handoff/SKILL.md     # /handoff — oturum sonu özeti üretir
│   │   ├── sp-preview/SKILL.md          # SP önizleme yardımcısı
│   │   ├── dashboard-validator/SKILL.md # Dashboard config güvenlik kontrolü
│   │   └── commit-split/SKILL.md        # 15+ dosya biriktiğinde çağrılır
│   ├── agents/
│   │   ├── sp-inventory-auditor.md      # SP katalog denetçisi
│   │   ├── razor-xss-auditor.md         # Razor XSS taraması
│   │   ├── user-data-filter-guard.md    # Multi-tenant veri sızıntısı koruması
│   │   └── commit-splitter.md           # 32-dosyalık backlog için bir defalık
│   └── hooks/
│       ├── session-start.sh             # git log + TODO kısa özet enjeksiyonu
│       ├── pre-commit-antipattern.sh    # DateTime.Now, async void, SA şifresi tespiti
│       ├── post-edit-format.sh          # dotnet format değişen dosyaya
│       └── stop-verify.sh               # "bitti" demeden önce build+JS parse
├── docs/
│   ├── ADR/
│   │   ├── 001-data-access.md           # SP kalıyor, metadata EF Core (bugün yazılacak)
│   │   ├── 002-dashboard-architecture.md
│   │   ├── 003-role-model.md
│   │   └── 004-sp-modularization.md     # sp_PdksPano → inline TVF kararı
│   ├── journal/
│   │   ├── 2026-04-21.md                # Dün olanlar (CLAUDE.md'den taşınacak)
│   │   └── 2026-04-22.md                # Bugün
│   ├── CLAUDE_TOOLING_PROPOSAL.md       # Dün yazıldı
│   └── CONTEXT_MANAGEMENT.md            # BU DOSYA
└── KULLANICI_KLAVUZU.md                 # (untracked, kalabilir)
```

---

## Oturum Disiplini (ritüeller)

### Oturum Başlangıcı (ilk 2 dakika)

**Otomatik** (SessionStart hook tarafından, arka planda):
```bash
# .claude/hooks/session-start.sh
#!/usr/bin/env bash
echo "## Son 3 gün ne değişti?"
git -C "$CLAUDE_PROJECT_DIR" log --since='3 days ago' --oneline
echo ""
echo "## Şu an aktif TODO'lar"
grep -A 1 '^### ' "$CLAUDE_PROJECT_DIR/TODO.md" | head -20
echo ""
echo "## Uncommitted dosya sayısı"
git -C "$CLAUDE_PROJECT_DIR" status --porcelain | wc -l
echo ""
echo "## En son journal"
ls -t "$CLAUDE_PROJECT_DIR/docs/journal/"*.md 2>/dev/null | head -1 | xargs -I{} tail -30 {}
```

Bu hook `additionalContext` olarak Claude'a enjekte edilir — her oturum başında kullanıcı sormadan Claude nerede kaldığını bilir.

**Elle** (kullanıcı yapsa iyi olur):
- `/memory` ile auto-memory'yi gözden geçir, stale olanı temizle
- Eğer uncommitted sayısı >15 ise önce `/commit-split` skill'i çağır

### Oturum Ortası (her 30-45 dk)

- `/context` ile kullanım kontrol — %60'ı geçtiyse `/compact` planı yap
- Task değiştiğinde → `/compact <önceki özet>` veya `/clear` (tam pivot ise)
- Yeni karar çıktıysa → ADR'ye veya rules/'a yaz (konuşma hafızasına bırakma)

### Oturum Sonu (son 5 dk)

**Zorunlu 3 adım:**

1. **`/handoff` skill çalıştır** — bugün yapılanları, açık kalanları, yarına başlangıcı `docs/journal/YYYY-MM-DD.md`'ye yazar
2. **Commit kontrol** — uncommitted varsa bu oturumun işiyle ilgili olanları commit et (commit-split skill'i yardımcı olur)
3. **TODO.md güncelle** — bu oturumda bittiyse çıkar, yeni açıldıysa ekle

---

## Bağlam Korunumu (compact/clear/resume)

### `/compact` ne zaman?

- `/context` %60+ gösteriyor ve aynı task'a devam ediliyor
- Uzun bir tool sequence bitti, detaylar gereksiz, özet yeter
- **Her zaman** focus instructions ekle: `/compact focus on the dashboard builder work; drop the Kaspersky debugging`

### `/clear` ne zaman?

- Task tamamen değişti (dashboard'dan user management'a)
- Claude "poisoned" — tekrar tekrar aynı yanlış varsayıma düşüyor
- Uzun oturum sonrası yeni session açıyorsun

### `/resume` ne zaman?

- Ara verildi ama aynı task devam edecek (aynı gün içinde)
- **Dikkat:** Session resume'da CLAUDE.md **yeniden okunmaz** — o yüzden "hatırlama" illüzyonu yapar ama disk'teki güncel kural değişti ise kaybolur. Büyük ara sonrası `/clear` daha güvenli.

### Compact sonrası hayatta kalan/kalmayan

| Veri | Compact sonrası |
|---|---|
| Root CLAUDE.md | ✅ Re-inject |
| `.claude/rules/*.md` (paths yok) | ✅ Re-inject |
| `.claude/rules/*.md` (paths: scoped) | ❌ Kayıp, dosya tekrar okunana kadar |
| Nested CLAUDE.md | ❌ Kayıp |
| Auto-memory (MEMORY.md) | ✅ Re-inject (ilk 200 satır) |
| Invoked skill body | ⚠️ 5K/skill, 25K toplam, eski dropped |
| Konuşma geçmişi | ❌ Özetlenir |

**Kural:** Kritik kurallar `paths:` KULLANMADAN yaz. Path-scoped kurallar yalnızca kod konvansiyonları için (Claude o dosyaya değinince zaten okunur).

---

## Agent Orkestrasyonu (Ralph pattern'i)

Geoffrey Huntley'in pattern'i ([ghuntley.com/ralph](https://ghuntley.com/ralph/)):

**Primary context = scheduler.** Büyük iş spawn edilir, özet alınır, bir sonrakine geçilir. Primary'nin context'i şişmez.

ReportHub için somut uygulama:

| İş | Primary mi, subagent mi? |
|---|---|
| Kod okuma, grep, file listing | Primary (ucuz, context'e almak değil) |
| Tüm SP'leri DB + kod cross-ref (büyük tool-result) | `sp-inventory-auditor` subagent |
| Razor XSS taraması (50 dosya) | `razor-xss-auditor` subagent |
| 32-dosyalık commit-split | `commit-splitter` subagent |
| Monolitik SP → inline TVF dönüşümü | `sp-to-tvf-refactorer` subagent |
| Hızlı edit, tek dosya fix | Primary |
| Araştırma (web fetch, çoklu kaynak) | general-purpose subagent |

**Prompt disiplini — subagent'a verirken:**

```
Görev: <net, tek paragraf>
Scope: <dosya listesi veya modül>
YAPMAYACAKLARIN: Bunların dışına çıkma. Fark ettiğin başka sorunları raporla, çözme.
Done tanımı: <ne döndürdüğünde iş bitmiş sayılır>
Raporla: <istediğin çıktı format>
```

Bu 5 satır subagent'ın kafasından uçmasını engelliyor.

---

## Git + Commit Disiplini

### Branch-per-ask

- Her kullanıcı talebi = bir branch
- İş bitince squash-merge
- Geri alma kolay: `git reset --hard main`

### Save-point commit

- Test yeşil = hemen commit (iş yarım olsa bile)
- "WIP: dashboard-builder drag-drop" tarzı commit mesajı
- Undo stack'i

### Commit eşiği

- 15+ uncommitted → yeni iş yasak, önce split

### 32-dosyalık backlog için

TODO.md'deki 7 fazlı plan + `commit-splitter` subagent. **Bu hafta yapılacak**:

1. `feat: rol sistemi` (Role.cs, UserRole.cs, ReportAllowedRole.cs, SQL 07-08)
2. `feat: rapor kategorileri` (ReportCategory*, SQL 07-08 katkısı)
3. `feat: rapor favorileri` (ReportFavorite, SQL 06)
4. `feat: AD user desteği` (SQL 09, User.IsAdUser, AuthController)
5. `feat: user data filter` (UserDataFilter, SQL 13, InjectUserDataFilters)
6. `feat: dashboard motoru` (DashboardConfig, DashboardRenderer, builder.js, SQL 10-12, 14, SP'ler)
7. `docs: kullanıcı kılavuzu + install notları`

---

## Eşikler (uyarı sinyalleri)

Aşağıdakiler gerçekleşirse "stop-the-world" temizlik zamanı:

| Sinyal | Aksiyon |
|---|---|
| CLAUDE.md > 200 satır | `.claude/rules/` bölümlerine split |
| Uncommitted > 15 dosya | `/commit-splitter` |
| Aynı hatayı 2. kez yapıyorum | Rule dosyasına veya skill'e yaz |
| 3+ paralel feature | Biri tamamlanana kadar yenisini başlatma |
| 30 gün önce yazılmış TODO | Ya yap ya sil |
| `/compact` sonrası kritik kural unutulmuş | O kuralı `.claude/rules/`'a taşı (`paths:` **yok**) |
| Auto-memory'de `debugging-XYZ.md` 90 gün önceki konudan | Sil |

---

## Bugün Uygulanacak (Acil 5 Aksiyon)

Bu 5 madde en çok ROI getirecek, 2-3 saat sürecek:

### Aksiyon 1 — CLAUDE.md split (30 dk)
- §1, §2, §4 kimlik kısmı kalsın → 100 satıra in
- §3 (Mimari Durum) → `.claude/rules/architecture.md`'e taşı
- §5 (Session log'u) → `docs/journal/2026-04-21.md`'ye taşı
- §5.1 (Kaspersky) → `.claude/rules/known-issues.md`'e taşı
- §6 (Hızlı Referans) → kalsın, faydalı

### Aksiyon 2 — `.claude/rules/` oluştur (30 dk)
- `architecture.md` (no paths)
- `security-principles.md` (no paths) — XSS, SQL injection, audit log
- `turkish-ui.md` (no paths) — Türkçe karakter kuralları
- `commit-discipline.md` (no paths) — bu dosyanın git bölümü
- `sql-conventions.md` (paths: `**/*.sql`)
- `razor-conventions.md` (paths: `**/*.cshtml`)
- `csharp-conventions.md` (paths: `**/*.cs`)
- `js-conventions.md` (paths: `wwwroot/assets/js/**`)

### Aksiyon 3 — SessionStart hook kur (15 dk)
- `.claude/hooks/session-start.sh` yaz (yukarıdaki snippet)
- `.claude/settings.json`'a kayıt et

### Aksiyon 4 — `session-handoff` skill yaz (30 dk)
- `.claude/skills/session-handoff/SKILL.md`
- `/handoff` ile çağrılır, `docs/journal/YYYY-MM-DD.md` üretir

### Aksiyon 5 — 32-dosyalık backlog'u böl (1-2 saat)
- `commit-splitter` subagent yaz
- 7 bucket'a böl
- Kullanıcı onayıyla commit et

---

## Sprint Planı (bu sistemi kurma)

### Sprint 0 — BUGÜN (2-3 saat)
1-5. acil aksiyon yukarıda.

### Sprint 1 — Bu hafta (5 gün)
- 3 ADR yazımı (data-access, dashboard-architecture, role-model)
- SP Önizle bug fix + default params
- Role CSV → UserRole migration (ADR-003 sonrası)
- `sp-inventory-auditor` ve `razor-xss-auditor` subagent'ları

### Sprint 2 — Bu ay (4 hafta)
- sp_PdksPano → inline TVF refactor (ADR-004)
- Serilog + basic observability
- GitHub Actions build workflow
- User P1 (admin listesi arama/filtre/son giriş)

### Sprint 3 — Çeyrek (3 ay)
- Test coverage %30
- Dashboard canlı önizleme iframe
- SP kolon auto-detect tam entegrasyon
- Kalan skill/agent paketi

---

## Mental Model

Claude'a **"kıdemli ama yönlendirme bekleyen bir yazılımcı"** gibi davran:
- Mimari kararı sen veriyorsun
- Uygulama detayını o çıkarıyor
- Her önemli değişiklik öncesi onay
- "Bitti" demeden önce verify (hook bunu zorluyor)

Burnout sinyalleri (dikkat):
- Diff okumadan accept ediyorum
- Son 5 commit'i açıklayamıyorum
- CLAUDE.md şişiyor ama TODO.md kısalmıyor → çöküş yakın

---

## Kaynaklar (araştırma temeli)

### Claude Code dokümanları (resmi)
- https://docs.claude.com/en/docs/claude-code/memory
- https://docs.claude.com/en/docs/claude-code/sub-agents
- https://docs.claude.com/en/docs/claude-code/skills
- https://docs.claude.com/en/docs/claude-code/hooks
- https://docs.claude.com/en/docs/claude-code/context-window

### Workflow makaleleri (2025-2026)
- [Addy Osmani — My LLM coding workflow going into 2026](https://addyosmani.com/blog/ai-coding-workflow/) — Spec → Plan → Save-point pattern
- [Geoffrey Huntley — Ralph as a software engineer](https://ghuntley.com/ralph/) — primary-as-scheduler
- [Armin Ronacher — Agentic Coding Recommendations](https://lucumr.pocoo.org/2025/6/12/agentic-coding/)
- [Rick Hightower — Stop Stuffing Everything into One CLAUDE.md](https://medium.com/@richardhightower/claude-code-rules-stop-stuffing-everything-into-one-claude-md-0b3732bca433)
- [claudefa.st — Rules Directory guide](https://claudefa.st/blog/guide/mechanics/rules-directory)
- [Chris Swan — ADRs with AI coding assistants](https://blog.thestateofme.com/2025/07/10/using-architecture-decision-records-adrs-with-ai-coding-assistants/)
- [Branch-per-Ask pattern](https://dev.to/novaelvaris/branch-per-ask-a-safer-git-workflow-for-ai-assisted-coding-130b)
- [MindwiredAI — 100-Line CLAUDE.md Workflow](https://mindwiredai.com/2026/03/25/claude-code-creator-workflow-claudemd/)
- [Anthropic — Session management and 1M context](https://claude.com/blog/using-claude-code-session-management-and-1m-context)

### Simon Willison rule
> "Learn to get them to prove their changes work" — test/verify is the skill, not prompting.
