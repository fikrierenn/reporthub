# Plan-First Disiplini (Tier Sistemi)

_Her projede aynen uygulanır. `paths:` yok — compact sonrası survive._

## Temel kural

**Tier 3 işlerde plan ZORUNLU.** Plan onaylanmadan kod yazılmaz, plan referansı olmadan Tier 3 commit atılmaz.

## Tier eşikleri

| Tier | Tanım | Plan? | Örnek |
|---|---|---|---|
| **1 — Trivial** | <30 satır, 1-2 dosya, sıfır yeni pattern, geri alınması kolay | **YOK** | Typo, version bump, config tarihi güncelle, comment ekle |
| **2 — Standard** | <5 dosya, mevcut pattern, küçük feature/fix | **TODO satırı yeterli** | send_mail.py UnicodeError fix, hook'a satır ekleme, dinamik tarih |
| **3 — Substantial** | 3+ dosya yeni pattern, schema/security/UX/harici bağımlılık, kullanıcı-görünür değişiklik | **TAM PLAN ZORUNLU** (`plans/NN-<slug>.md`) | Atlasops adaptasyonu, Mayıs %50 kampanya tahmini, Lock mekanizması, ADR yazımı |

## Tier 3 sinyalleri

Şu sinyallerden BİRİ varsa Tier 3 sayılır:

1. **3+ farklı klasöre dokunma** (örn. `src/` + `docs/` + `.claude/`)
2. **Yeni dosya tipi** (yeni schema migration, yeni servis modülü, yeni rule)
3. **Geri alınması zor** (DB migration, security policy, public API breaking change)
4. **Kullanıcı-görünür** UI/UX/CLI/API değişiklik
5. **Harici bağımlılık** (npm yeni paket, yeni env var zorunlu)
6. **Mimari karar** (yeni pattern, yeni teknoloji)

Şüphede kal? **Kullanıcıya sor:** "Bu Tier 2 mi Tier 3 mü, plan yazayım mı?"

## Plan-First Workflow

### 1. Tier tespiti
- 1-2 dosya, sade fix → Tier 1, planla zaman kaybetme
- 3+ dosya / yeni pattern → Tier 3, plan yaz

### 2. Plan yaz (Tier 3)

```bash
# Son plan ID'sini bul
ls plans/*.md | grep -E '^plans/[0-9]' | tail -1

# Yeni plan
cp plans/feature-template.md plans/NN-<slug>.md
```

Doldur: Problem, Scope, Alternatifler (en az 2 reddedilen), Riskler, Done criteria, Rollback, Adımlar.

**Alternatif değerlendirirken 5 lens (her biri için 1 cümle yeterli):**
- 🔴 **Contrarian:** Bu planın fatal flaw'u ne? Nerede yanılıyoruz?
- 🔵 **First Principles:** Yanlış soruyu mu soruyoruz? Gerçek problem ne?
- 🟢 **Expansionist:** Daha büyük fırsat kaçırılıyor mu? Scope çok dar mı?
- ⚪ **Outsider:** Koda yabancı biri baksaydı ne garip bulurdu?
- 🟡 **Executor:** İlk somut adım ne? Pazartesi sabahı ne yapılır?

Tüm beşi tek cümleyle plan "Alternatifler" bölümüne girer. Council detayı istiyorsan: `/council-this`.

### 3. Onay
Kullanıcıya göster, geri bildirim al, düzeltme yap. **Onay olmadan implement etme.**

### 4. Implementation
- Her commit message'da plan referansı: `feat(bkm): X (plan: 03)`
- TODO.md'de plan adımları (Faz X altında)

### 5. Tamamlanma
- Plan dosyasını arşive taşı: `git mv plans/NN-*.md plans/archive/`
- Done criteria'yı check'le
- Journal'da özet

### 6. Stale plan disiplini

**Plan ölüm tarihi (2026-05-14 kararı):** 14 gün dokunulmamış aktif plan ya **yeniden ısıt** ya **arşive taşı**.

- `session-start.sh` hook her oturum başında `find plans -mtime +14` ile uyarı verir.
- Karar 3 yoldan biri:
  - Plan hâlâ geçerli + iş başlayacak → bu oturumda Faz 1 adımına başla (touch ile mtime yenilemek yetmez — iş yapılmadan plan dosyası "ölü" sayılır).
  - Plan geçerli ama zamanlama uzak → `plans/archive/` (notla: "Faz 0 başlamadı, ileride yeniden açılacak").
  - Plan artık geçersiz (proje yönü değişti) → `plans/archive/` (notla: "Reddedildi: <gerekçe>").
- Soğukta tutulan plan = soğutulan iş. Aktif klasörü "yapılacak iş" listesi olarak temiz tut.

## İstisnalar

### Acil bug fix (production down)
Plan-first **bypass edilebilir** ama:
1. Kullanıcıya: "Bypass yapıyorum, retro plan yazacağım"
2. Commit: `fix(<proje>): bug X (plan: BYPASS-<tarih>)`
3. Sonradan retro plan: `plans/archive/BYPASS-<tarih>.md`

### Kullanıcı "hızlıca yap" derse
- Tier 3 sinyali varsa hâlâ uyarı: "Bu 3+ dosyayı etkiliyor, mini-plan yazayım mı (5 dakika) yoksa direkt mi?"
- "Direkt" derse: TODO'ya `[plan-skipped: <gerekçe>]` notu

## İlişkili

- `plans/README.md` — klasör yapısı + workflow
- `plans/feature-template.md` — şablon
- `.claude/rules/commit-discipline.md` — Tier 3 commit'lerde plan referansı
- `docs/ADR/003-plan-first-tier-system.md` — karar gerekçe + alternatifler
- `docs/CONTEXT_MANAGEMENT.md` § İlke 6 — Spec → Plan → Execute (3+ dosya)
