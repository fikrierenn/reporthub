# ADR-010 — Plan-First Tier Sistemi

## Durum

`Kabul edildi`

## Tarih

2026-04-27

## Bağlam

Reporthub büyürken (M-11 Dashboard Builder Redesign 12+ faz, F-7 alt-commit'lere bölünmüş ~10h tek faz) plan disiplini kritik hale geldi. Atlasops adaptasyonu ve sqlserver-mcp-server projelerinde gözlemlenen pattern: net plan olmadan başlayan büyük işler scope explosion + düzeltme döngüsüne giriyor.

Mevcut sistem (TODO.md + ADR + journal) "3+ dosya öncesi spec yaz" ilkesini koymuş (`docs/CONTEXT_MANAGEMENT.md` İlke 6) ama operasyonel kural eksik — sadece soyut bir prensip.

İki gerilim:

1. **Disiplin ihtiyacı**: M-11 paralel fazlarda ve commit-split anlarında scope kayması yaşandı. Plan-first bunun çözümü.

2. **Overengineering riski**: Her typo veya version bump için plan yazmak disiplinin dead letter olmasıyla sonuçlanır.

## Karar

**3-Tier sistemi:**

| Tier | Plan zorunluluğu | Koşul |
|---|---|---|
| **1 — Trivial** | YOK | <30 satır, 1-2 dosya, sıfır yeni pattern |
| **2 — Standard** | TODO satırı yeterli | <5 dosya, mevcut pattern |
| **3 — Substantial** | TAM PLAN (`plans/NN-<slug>.md`) | 3+ klasör / yeni pattern / schema-security-UX / harici dep / kullanıcı-görünür |

Tier 3 commit'lerde plan referansı zorunlu (`(plan: NN)` mesajda).

## Sebepler

- Küçük işlere plan zaman kaybı — Tier 1 dışında tutulur, plan-fatigue yok.
- Büyük işlerde (Tier 3) plan **gerçek scope explosion'ı önler** — M-11 alt-fazları (F-7 dashboard builder UI redesign, ~10h, 6+ JS modül + Razor split-pane + Gridstack + brand CSS) plan olmadan dağılırdı.
- Tier 2 zaten mevcut TODO disiplinini takip ediyor — yeni yük gelmiyor.
- "3+ klasör/dosya" sezgisel sinyali pratikte iyi çalışıyor.

## Alternatifler (Reddedilenler)

### A: Blanket plan-first (her commit öncesi plan)
**Reddetme sebebi:** "Küçük şeye plan mantıksız" prensibi haklı. Disiplin dead letter olur, plan yorgunluğu, kullanıcı sistemi bypass eder.

### B: Vazgeç — TODO + ADR yeterli
**Reddetme sebebi:** M-11 gibi büyük işler scope explosion üretti — F-7 alt-commit ayrımı kullanıcı uyarısıyla son anda kuruldu. Mevcut sistem "sezgisel" ilke koymuş ama operasyonel disiplin yok.

### C: İki-tier (plan vs no-plan, eşik 5 dosya)
**Reddetme sebebi:** Tier 2 (küçük feature) ile Tier 1 (typo) farklı operasyonel ihtiyaç. Tier 2'de TODO satırı yeterli; Tier 1'de TODO bile yok. İki-tier'da bu nüans kaybolur.

### D: Plan zorunlu ama eşik = 10+ dosya
**Reddetme sebebi:** Çok yüksek eşik. 5-10 dosya aralığında gerçek mimari karar olabilir. Eşik 3+ klasör pratikte daha iyi çalışır.

## Sonuçlar

### Olumlu
- Tier 3 disiplinli, scope explosion önlenir.
- Tier 1/2'de plan-fatigue yok.
- Tier sinyalleri net (3+ klasör, schema vb.).
- Mevcut TODO + ADR + journal entegrasyonu kolay (plan referansı commit mesajında).

### Olumsuz / Risk
- Tier tespiti sübjektif. Mitigation: şüphede kullanıcıya sor.
- Plan archive'a taşınmazsa `plans/` şişer. Mitigation: handoff skill plan tamamlanma kontrolü ileride.
- Pre-commit hook ile zorlanmıyor — manuel disiplin. Mitigation: gelecekte hook (TODO).

### Bilinmeyen
- 1-2 ay sonra plans/ ne kadar dolacak? Tier sınırları sertleştirilmesi gerekirse karara dön.

## Uygulama

- [x] `plans/` + `plans/archive/` klasörü
- [x] `plans/README.md` (klasör yapısı + workflow)
- [x] `plans/feature-template.md` (Tier 3 şablonu)
- [x] `.claude/rules/plan-first.md` (Tier eşikleri + workflow + istisnalar)
- [x] `.claude/rules/commit-discipline.md` güncellendi (Tier 3 plan referansı)
- [x] `plans/01-claude-tooling-import.md` (retro plan, BU işin kendisi)
- [x] Bu ADR yazıldı
- [ ] Pre-commit hook (gelecekte) — Tier 3 sinyali varsa plan referansı yoksa uyarı (block değil, fail-soft)
- [ ] Handoff skill plan tamamlanma kontrolü
- [ ] Test: M-11'in kalan fazları (F-7..F-12) bu disiplinle yazılsın

## İlişkili Dosyalar

- `plans/README.md`
- `plans/feature-template.md`
- `.claude/rules/plan-first.md`
- `.claude/rules/commit-discipline.md` (Plan-First Referansı bölümü)
- `docs/CONTEXT_MANAGEMENT.md` (§ İlke 6 — Spec → Plan → Execute)

## Referanslar

- Kaynak: sqlserver-mcp-server / atlasops projesinden adapte edildi (ADR-003 → 010 yeniden numara)
- Konuşma: `docs/journal/2026-04-27.md`
- Kullanıcı talebi: "atlasops içindeki session kuralları iş yapma kurallarını bu projeye uygulamamız lazım"
