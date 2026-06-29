---
name: consolidate-mosaik
description: Mosaik çatısının (plans/ + .claude/rules/ + .claude/skills/ + .claude/agents/ + docs/ADR/ + MEMORY.md + TODO.md) yaşam-döngüsü bakımı. Stale, çakışan, tamamlanmış, hiç-tetiklenmeyen kayıtları DRY-RUN ile raporlar (mutasyonsuz REPORT.md); kullanıcı onaylarsa ARCHIVE eder. ASLA silmez — archive-only (git history zaten korur). "çatı bakım", "curator", "bloat temizle", "stale göster", "pasif et", "consolidate", "MEMORY küçült", "/consolidate-mosaik" denildiğinde veya MEMORY.md limit aşınca / 15+ stale plan birikince tetiklenir.
allowed-tools: Read, Edit, Write, Bash, Grep, Glob
user-invocable: true
model: inherit
---

# consolidate-mosaik — Çatı Yaşam-Döngüsü Bakımı (curator)

Mosaik plan/rule/skill/agent/ADR/MEMORY zamanla birikir: tamamlanmış-ama-arşivlenmemiş plan, çakışan rule, hiç-tetiklenmeyen skill, limit-aşan MEMORY index. Bu skill onları **dry-run** ile rapor eder; kullanıcı onayıyla **archive** uygular. Pusula `consolidate-sema` (Hermes curator) uyarlaması. Kardeş kural: `footprint-ladder.md` (üretimi frenler, bu skill birikeni budar).

## Temel İlkeler (sert kurallar)

- **ASLA silme → archive.** Maksimum yıkıcı aksiyon = arşive taşıma. git history zaten korur.
- **Stale = bayrak, otomatik aksiyon DEĞİL.** Her archive/merge **kullanıcı onayı** ister.
- **Eskilik ≠ ölü.** Onaylanmış ama master-roadmap kuyruğunda bekleyen plan (örn. Plan 53 altındaki 44-51) 35 gün dokunulmamış olsa bile **CANLI**. Yaş tek başına archive gerekçesi değil — durum + referans + supersede birlikte bakılır.
- **Canonical MUAF.** ADR'ler (mimari karar kaydı), core rule'lar, mosaik-* expert skill'ler = pinned. Çelişki/supersede yoksa dokunma.
- **dry-run varsayılan.** Önce REPORT, sonra onay, sonra uygula.

## Kapsam ve Archive Hedefleri

| Yapı | Stale sinyali | Archive hedefi |
|---|---|---|
| **plans/*.md** | Tamamlandı + arşivlenmemiş · Taslak hiç onaylanmamış 30g+ · supersede edilmiş · master plan'a bağlı DEĞİL ve 30g+ | `git mv plans/archive/` |
| **.claude/rules/*.md** | %50+ içeriği tamamlanmış işi "aktif" gösteriyor · başka rule'la birebir dup · CLAUDE.md'de referans yok ve path-scoped değil | `.claude/_archive/rules/` + CLAUDE.md pointer kaldır |
| **.claude/skills/<ad>/** | Hiç tetiklenmiyor (grep CLAUDE.md+rules+skills 0 dış referans) · başka skill'le örtüşüyor · deferred teknoloji (ADR ile ertelenmiş) | `.claude/_archive/skills/` |
| **.claude/agents/*.md** | CLAUDE.md §2 listesinde yok (orphan) · scan rotasyonuna hiç girmiyor + on-demand çağrılmıyor | `.claude/_archive/agents/` |
| **docs/ADR/*.md** | Sadece supersede/contradiction varsa | `note: superseded by ADR-NN` (taşıma değil) |
| **MEMORY.md** | Entry >200 char · dup/superseded entry · tamamlanmış-plan entry'si | In-place TRIM/MERGE + detay topic file'a |
| **TODO.md** | `[ ]` madde 30g+ dokunulmamış | `## Arşiv` bölümüne taşı |

> `.claude/_archive/` dizini skill/agent auto-discovery'nin DIŞINDA olmalı — taşınan yapı pasifleşir ama git'te durur. (Skill discovery `.claude/skills/` altını tarar; `_archive` kardeş dizin, taranmaz.)

## Mod 1 — DRY-RUN (varsayılan)

### Adım 1 — Plan taraması
`ls -t plans/*.md` + her dosya için `git log -1 --format=%ci`. İçinde durum marker (TASLAK/ONAYLANDI/TAMAMLANDI). TODO.md + master plan (Plan 53 gibi) referansı var mı? Tamamlanmış-arşivlenmemiş + never-approved-Taslak-30g+ aday. **Master roadmap kuyruğundakini ARŞİVLEME** — sadece "VERIFY" işaretle.

### Adım 2 — Rule taraması
`.claude/rules/*.md` satır sayısı + CLAUDE.md referansı + dup tespiti (aynı fact iki dosyada). Tamamlanmış işi aktif gösteren bölümler (örn. "Plan X Faz 1 pending" ama done). Dup → canonical seç, diğerini one-line + link.

### Adım 3 — Skill/agent taraması
Her skill/agent için: `grep -rl "<ad>" CLAUDE.md .claude/rules .claude/skills` → 0 dış referans + on-demand değilse orphan. Örtüşen skill kümeleri (design yığını, çoklu css/security). Deferred teknoloji skill'i (ADR ile ertelenmiş).

### Adım 4 — MEMORY.md taraması
Boyut + index entry sayısı + >200 char entry listesi + dup/superseded entry + tamamlanmış-plan entry'leri. Hedef: index limit altına (one-line hook).

### Adım 5 — REPORT yaz (mutasyonsuz)
`docs/curator/REPORT-YYYY-MM-DD.md`:
```markdown
# Curator Dry-Run — YYYY-MM-DD
## Plans — ARCHIVE-now (N) / VERIFY (N) / KEEP
## Rules — TRIM / MERGE / ARCHIVE (dup haritası dahil)
## Skills+Agents — ARCHIVE / MERGE (referans-sayısı kanıtı)
## ADR — superseded işaret adayları
## MEMORY — TRIM/MERGE/DROP (tahmini KB kazanç)
## Önerilen aksiyonlar (onay bekliyor)
- [ ] ...
```
**Dosya yazar, hiçbir yapıya DOKUNMAZ.** Kullanıcıya özet göster.

## Mod 2 — UYGULA (yalnızca açık onay sonrası)

Kullanıcı REPORT'tan aksiyon seçince:
- **Plan archive:** `git mv plans/NN-*.md plans/archive/` + üstüne "Arşiv notu: <gerekçe> YYYY-MM-DD".
- **Rule:** `.claude/_archive/rules/`'a taşı + CLAUDE.md pointer'ı kaldır. TRIM ise stale bölümü sil/güncelle (silme değil, düzelt — `before-major-change`).
- **Skill/agent:** `.claude/_archive/skills|agents/`'a taşı.
- **ADR:** taşıma yok — `> note: superseded by ADR-NN (YYYY-MM-DD)` satırı ekle.
- **MEMORY:** entry'leri ≤150 char one-line'a indir, detay zaten topic file'da; dup'ları merge; tamamlanmış-plan entry'lerini index'ten düş (topic file diskte kalır).
- **Build/davranış etkisi yok** doğrula: archive sonrası `dotnet build` gerekmez (sadece .md) ama CLAUDE.md referans kırılmadığını grep'le kontrol et.
- Her uygulama tek commit: `chore(crossproject): çatı curator bakım`. Önce git-status temiz olsun (revert kolaylığı).

## Tetik
- "/consolidate-mosaik", "çatı bakım", "curator", "bloat temizle", "pasif et", "MEMORY küçült", "stale göster".
- MEMORY.md limit uyarısı · 15+ stale plan · session-handoff curator-check.

## Guardrails
- ❌ Onaysız taşıma/silme YOK.
- ❌ Master-roadmap kuyruğundaki onaylı planı archive etme (VERIFY işaretle, kullanıcıya sor).
- ❌ ADR taşıma (sadece supersede notu).
- ❌ Skill/agent'ı silme — `.claude/_archive/`'a taşı.
- ✅ archive-only, onay-gate'li, git-revert'lenebilir.

## İlişkili
- `.claude/rules/footprint-ladder.md` — üretim freni (kardeş; bu skill budar).
- `.claude/rules/before-major-change.md` — taşıma öncesi grep + referans tara.
- `.claude/rules/todo-verification.md` — "stale = hipotez, kanıtla" (plan durumu canlı kodla doğrula).
- `.claude/rules/session-memory.md` — katman ayrımı + eşikler.
- `.claude/skills/session-handoff/SKILL.md` — oturum sonu hafif curator-check.
