---
name: wiki-keeper
description: Cross-project Obsidian Brain vault'unu (D:/Dev/brain) maintain eder. Karpathy LLM Wiki pattern'i — oturum sonu (handoff sonrası) journal'ı parse edip ilgili entity'leri günceller, log.md'ye satır ekler, çelişki tespit eder. Lint mode'da stale/orphan/broken-link tarar. Kullanıcı "vault güncelle", "brain'e yaz", "wiki sync", "/wiki-keeper", "vault lint" gibi ifadeler kullandığında veya session-handoff sonrası otomatik tetiklenir. **Commit etmez** — sadece staged değişiklik bırakır, kullanıcı review eder.
allowed-tools: Read, Write, Edit, Bash, Grep, Glob
user-invocable: true
model: inherit
---

# Wiki Keeper Skill — Brain Vault Maintainer

> Karpathy LLM Wiki pattern'i: <https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f>
> Brain vault: `D:/Dev/brain` (local git, GitHub push yok)
> Schema: `D:/Dev/brain/CLAUDE.md`

## Amaç

Brain vault'u "compounding" tutmak. Her oturum sonunda (veya manuel tetik) journal/değişiklikleri parse edip:

- İlgili entity'lere yeni gerçek + cross-link ekle
- Çelişki varsa `## Contradiction` başlığı altında flag'le (eski bilgiyi silme)
- `log.md`'ye 1 satır append
- Haftalık synthesis sayfasına (`synthesis/YYYY-Wnn.md`) commit referansları + 1-2 cümle özet
- **Commit etmez** — staged bırak, kullanıcı review

## Mod'lar

### 1. Update mode (default — handoff sonrası)

Tetik:
- `session-handoff` skill'i bittikten sonra (chain)
- Manuel: `/wiki-keeper`, "vault güncelle", "brain'e yaz", "wiki sync"

Workflow → bkz. Adım Adım (aşağıda).

### 2. Lint mode

Tetik:
- Manuel: "vault lint", "/wiki-keeper lint", "brain temizle", haftalık ritüel

Tarar:
- **Stale claim** — Update Log son satırı 60+ gün eski + sayfada "şu an X" iddiası
- **Orphan page** — başka sayfadan backlink alıyor mu (Grep `[[<filename>]]` vault genelinde)
- **Broken link** — `[[X]]` target dosya yok
- **Duplicate** — iki sayfada benzer içerik (manual review için flag)
- **Missing cross-ref** — entity A → company B referans veriyor ama B'de A backlink yok

Output: `D:/Dev/brain/raw/vault-lint-YYYY-MM-DD.md` raporu (commit etmez).

## Adım Adım — Update Mode

### Adım 1 — Vault context yükle

```bash
BRAIN="D:/Dev/brain"
[ -d "$BRAIN" ] || { echo "Brain vault yok: $BRAIN"; exit 1; }
```

`$BRAIN/CLAUDE.md` ilk satırlarını oku (schema hatırlatma). Kullanıcıya kısaca de: "Brain vault güncelleniyor (`$BRAIN`)".

### Adım 2 — Bugünün journal'ını oku

```bash
TODAY=$(date +%Y-%m-%d)
JOURNAL="docs/journal/$TODAY.md"
[ -f "$JOURNAL" ] || { echo "Journal yok, atlanıyor"; exit 0; }
```

Journal yoksa: skill çalıştırılmaz (handoff henüz çalışmamış).

### Adım 3 — Entity tespit

Journal'ı tara, mevcut entity isimleriyle eşleştir:

```bash
# Vault'taki entity isim listesi
ls $BRAIN/entities/{people,projects,companies,systems}/*.md 2>/dev/null \
  | xargs -I {} basename {} .md
```

Journal'da geçen isimleri match et (dosya adı veya frontmatter `aliases` ile). En az birinde geçen: **etkilenen entity**.

Yeni entity tespit edilirse (örn. journal'da "Belinza X projesi" geçiyor + `entities/projects/belinza-x.md` yok): yeni stub sayfa oluştur, kullanıcıya bildir.

### Adım 4 — Etkilenen entity'leri güncelle

Her etkilenen entity için:

1. **Sayfayı oku** (Read tool)
2. **Yeni gerçek/karar/durum** varsa ilgili bölüme ekle
3. **Çelişki** varsa: yeni `## Contradiction` başlığı altına eski + yeni iddia + tarih
4. **Update Log** sonuna satır ekle: `- YYYY-MM-DD: <kısa özet> ([[journal/YYYY-MM-DD]] / commit hash)`
5. **Frontmatter** `updated:` field'ı bugüne çek

**Yazma standardı (CLAUDE.md vault schema'ya göre):**
- Türkçe (UI/proje meta), kod terimleri İngilizce
- "Neden" yaz — sadece "ne" değil
- Tarih bilgisi mutlak (göreli ifade kullanma)
- Cross-link en az 1 ekle (mevcut entity'lere `[[]]` bağı)

### Adım 5 — Synthesis hafta dosyası

```bash
WEEK=$(date +%Y-W%V)
SYNTH="$BRAIN/synthesis/$WEEK.md"
```

Dosya yoksa: `templates/synthesis-week.md`'den kopyala (frontmatter + boş bölümler).

Bugün için 1-2 cümle özet append:
```markdown
## YYYY-MM-DD (Pazartesi/Salı/...)
- Tamamlanan: <kısa>
- Karar: <varsa>
- Etkilenen: [[entities/projects/X]], [[concepts/patterns/Y]]
- Commit: `<hash>` (repo: <repo-name>)
```

### Adım 6 — log.md append

```bash
LOG="$BRAIN/log.md"
```

Bugün için 1 satır:
```
- YYYY-MM-DD: <oturum ana konu> [[entities/projects/X]] / <commit hash>
```

`## YYYY-MM-DD` başlığı zaten varsa altına ekle, yoksa yeni başlık + satır.

### Adım 7 — Stage **(commit ETME)**

```bash
cd "$BRAIN"
git add -A
git status --short  # rapor için
```

Kullanıcıya özet:
```
Brain vault güncellendi (staged, commit beklemede):
- Etkilenen entity: 3 sayfa
- log.md: 1 satır append
- synthesis/2026-W18.md: 1 günlük blok
- Yeni stub: <varsa, örn. entities/projects/yeni-proje.md>

Review: cd D:/Dev/brain && git diff --cached
Commit (manuel): cd D:/Dev/brain && git commit -m "wiki: 2026-05-04 update"
```

**Önemli:** `commit-discipline.md` kuralı uygulanır — kullanıcı açıkça istemedikçe commit yok. Brain vault için bu kural devam ediyor (session-handoff istisnası burada **geçerli değil**, çünkü brain vault başka bir repo + Karpathy pattern review-first öneriyor).

## Adım Adım — Lint Mode

### Adım L1 — Vault tara

```bash
cd "$BRAIN"
ALL_PAGES=$(find entities concepts synthesis raw -name "*.md" -type f 2>/dev/null)
```

### Adım L2 — Stale claim tespit

Her sayfada:
- Frontmatter `updated:` field'ı oku
- Bugün - updated > 60 gün → "stale candidate"
- İçerikte "şu an", "bugün", "şu durumda" gibi şimdi-zaman ifadelerini tara
- Eşleşen: rapora ekle

### Adım L3 — Orphan tespit

Her sayfa için:
```bash
PAGENAME=$(basename "$page" .md)
BACKLINK_COUNT=$(grep -rl "\[\[.*$PAGENAME\]\]" --include="*.md" "$BRAIN" | wc -l)
```

`BACKLINK_COUNT == 0` → orphan flag.

### Adım L4 — Broken link tespit

Her sayfada `[[X]]` patterns ara:
```bash
grep -rEoh '\[\[[^]]+\]\]' "$BRAIN" --include="*.md" \
  | sed 's/\[\[\(.*\)\]\]/\1/' \
  | while read link; do
      # link'i dosya path'ine çevir, dosya yoksa "broken" olarak listele
      ...
    done
```

### Adım L5 — Rapor üret

```bash
REPORT="$BRAIN/raw/vault-lint-$(date +%Y-%m-%d).md"
```

Şablon:
```markdown
---
type: raw
tags: [lint, audit]
created: YYYY-MM-DD
---

# Vault Lint Report — YYYY-MM-DD

## Stale Claims (60+ gün)
- [[entities/X]] — son güncelleme YYYY-MM-DD, "şu an" iddiası: ...

## Orphans (backlink yok)
- [[entities/Y]] — ya bağla ya sil

## Broken Links
- [[entities/Z]] içinde [[nonexistent-page]] kırık

## Duplicate Candidates (manual review)
- [[A]] ve [[B]] benzer içerik (50%+ overlap)

## Missing Cross-References
- [[entities/projects/X]] [[entities/companies/Y]] referans veriyor ama Y'de X backlink yok
```

Stage et ama commit etme. Kullanıcı raporu okur, gereken aksiyonu alır.

## Tetikleme Örnekleri

- Kullanıcı `session-handoff`'tan sonra "ve brain'e de yaz" → bu skill çalış
- "Vault güncelle" / "wiki sync" → update mode
- "Vault lint" / "brain temizle" → lint mode
- "/wiki-keeper" → kullanıcı seçimi sor (update / lint)
- Otomatik chain: `session-handoff` skill'inin sonuna referans (manuel chain — kullanıcı isterse)

## Dikkat

1. **Commit etme** — staged bırak, kullanıcı review. Brain vault için "auto-commit" yok (NotebookLM senkron script'i ayrı, Faz 5).
2. **Brain vault yoksa** sessizce atla (Plan 08 implement edilmediyse).
3. **Sentez yaz, kopyala değil** — journal'ı brain'e kopyalama, özetle + link et.
4. **Çelişki silme, flag'le** — Karpathy pattern: çelişkiler `## Contradiction` altında bırakılır.
5. **Yeni stub için kullanıcı bilgilendir** — "Vault'a yeni sayfa eklendi: `entities/projects/X.md` (stub). Detay sen doldur."
6. **Repo değişikliklerini etkileme** — bu skill brain vault'a yazar, ReportHub repo'suna **dokunmaz**.
7. **Türkçe + UTF-8** — vault içeriği Türkçe (CLAUDE.md vault schema'ya göre).

## İlişkili

- Karpathy gist: <https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f>
- Plan 08: `D:/Dev/reporthub/plans/08-llm-wiki-obsidian-brain.md`
- Vault schema: `D:/Dev/brain/CLAUDE.md`
- Session-handoff skill: `.claude/skills/session-handoff/SKILL.md` (chain partner)
- NotebookLM senkron: Plan 08 Faz 5 (haftalık manuel script)
- Auto-memory: `reference_brain_obsidian.md`
- Commit-discipline: `.claude/rules/commit-discipline.md` (geçerli)
