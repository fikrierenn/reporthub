# Skill Index — Mosaik

Bu dosya `.claude/skills/` altındaki proje-level skill'lerin kategorize edilmiş envanteridir. Her skill'in **gerçek kullanım sıklığı** + **tetikleyici pattern** + **boyut** burada.

**Anthropic ekosistem skill'leri** (`anthropic-skills:*`, `claude-md-management:*`, vb.) burada listelenmez — bunlar Cowork bütünleşik, kullanıcı profil seviyesinde yönetilir.

**Disiplin:** Skill enflasyonu = context yükü. Yeni skill eklemeden önce "mevcut skill'lerden biri kapsıyor mu?" sorusu zorunlu. Kullanım kanıtı olmadan skill eklemek YASAK (CLAUDE.md §2 "Subagent + skill **default**" prensibinin alt-kuralı).

---

## A. Mosaik proje-spesifik (8 skill — KORUNUR, proaktif tetik)

Yeni kod yazılırken **otomatik** devreye girer. Bu skill'ler proje kuralları ile kod arasındaki köprü.

| Skill | Boyut | Tetikleyici |
|---|---|---|
| [mosaik-csharp-razor](mosaik-csharp-razor/SKILL.md) | 11 KB | Yeni controller/view/ViewModel/service/migration. AntiForgery + AsNoTracking + SqlCommand SP + form-section-card + Türkçe UI. |
| [mosaik-css-expert](mosaik-css-expert/SKILL.md) | 13 KB | Yeni view, inline-style refactor, modül CSS. Plan 25.1 P1-P8, components.css canonical. |
| [mosaik-js-expert](mosaik-js-expert/SKILL.md) | 11 KB | Yeni JS, fetch/POST, DOM manipülasyon, Alpine factory. IIFE + ES5 + XSS textContent. |
| [mosaik-security](mosaik-security/SKILL.md) | 16 KB | Yeni controller/POST/SQL/SP/email/file-upload/JS-fetch. security-principles.md 10 kuralın proaktif uygulayıcısı. |
| [css-classify](css-classify/SKILL.md) | 7 KB | Her CSS class ekleme kararı (zincirleme: mosaik-css-expert). |
| [sql-migration-writer](sql-migration-writer/SKILL.md) | 6 KB | Yeni `Database/NN_*.sql`. Idempotent + yedek-almadan-silme yasağı. |
| [code-quality-checklist](code-quality-checklist/SKILL.md) | 16 KB | Code Quality Checklist — Mosaik. |
| [vnext-entity-port](vnext-entity-port/SKILL.md) | 11 KB | Plan 17+ vNext modül üretimi (entity → migration → controller → view iskeleti). |

---

## B. Süreç / oturum disiplini (3 skill — KORUNUR)

| Skill | Boyut | Tetikleyici |
|---|---|---|
| [session-handoff](session-handoff/SKILL.md) | 9 KB | "iyi geceler" / "handoff" / "kaydet ve kapat" / `/handoff`. Journal yazar + auto-commit. |
| [plan-tracker](plan-tracker/SKILL.md) | 6 KB | 3+ adımlı iş planlandığında TodoWrite paralel. TODO.md'ye kalıcı yazım. |
| [wiki-keeper](wiki-keeper/SKILL.md) | 8 KB | Cross-project Obsidian Brain (`D:/Dev/brain`) maintain. Handoff sonrası otomatik. |

---

## C. Veri keşif / dış sistem (2 skill — KORUNUR)

| Skill | Boyut | Tetikleyici |
|---|---|---|
| [bkm-db-explorer](bkm-db-explorer/SKILL.md) | 6 KB | BKM kurumsal DB'lerinde tablo/SP keşfi (DerinSIS*, BKMDATA, EncoreMerkez, BKM). |
| [notebooklm](notebooklm/SKILL.md) | 7 KB | Google NotebookLM CLI — podcast/video/rapor/quiz üretimi. |

---

## D. Library reference (2 skill — KORUNUR, küçük)

CDN üzerinden kullanılan kütüphanelerin Mosaik entegrasyon pattern'i. Kendi başına tetiklenmez — mosaik-js-expert / mosaik-css-expert ile zincirleme.

| Skill | Boyut | Not |
|---|---|---|
| [alpine-js](alpine-js/SKILL.md) | 8 KB | _AppLayout.cshtml:65 yüklü. Reactive directive pattern'leri. |
| [htmx-expert](htmx-expert/SKILL.md) | 467 B | Üçüncü-parti repo clone (gitignored). Kurulum: `git clone --depth 1 https://github.com/ercan-er/htmx-claude-skill .claude/skills/htmx-expert`. |

---

## E. Karar / danışma (1 skill — KORUNUR)

| Skill | Boyut | Tetikleyici |
|---|---|---|
| [llm-council](llm-council/SKILL.md) | 4 KB | Mimari "hangi yol" belirsizliği. 5 bağımsız danışman + chairman sentezi. |

---

## F. Anthropic frontend tasarım (6 skill — DURUMSAL)

UI değişikliklerinde tetiklenir. Razor + Tailwind + vanilla JS context'inde "marjinal değerli" — toplam 50 KB context yükü. **Kalır ama gerçek tetik oranı kullanıcı izlemine açık.**

| Skill | Boyut | Razor + Tailwind uygunluğu |
|---|---|---|
| [accessibility-compliance](accessibility-compliance/SKILL.md) | 12 KB | ✅ Yüksek — WCAG 2.2, ARIA, screen reader. Her `.cshtml` edit'inde proaktif (CLAUDE.md §2). |
| [frontend-design](frontend-design/SKILL.md) | 4 KB | ✅ Orta — yeni page/layout için inspiration. |
| [design-system-patterns](design-system-patterns/SKILL.md) | 10 KB | ✅ Orta — tokens.css var, theming kurulu. |
| [interaction-design](interaction-design/SKILL.md) | 8 KB | ⚠️ Düşük — Mosaik az microinteraction kullanıyor. |
| [responsive-design](responsive-design/SKILL.md) | 13 KB | ⚠️ Düşük — Tailwind responsive native. |
| [visual-design-foundations](visual-design-foundations/SKILL.md) | 8 KB | ⚠️ Düşük — token sistemi mevcut. |

---

## G. GÖZDEN GEÇİRME ADAYI (1 skill)

| Skill | Boyut | Durum |
|---|---|---|
| [ui-ux-pro-max](ui-ux-pro-max/SKILL.md) | **44 KB / 658 satır** | Tek başına context yükünün %18'i. 161 renk paleti + 99 UX kuralı + 25 chart tipi — Mosaik iç portali için bu ölçek aşırı. **Karar bekliyor:** son 30 günde gerçekten kaç kez tetiklendi? Kullanıcı kararı (tut / küçült / sil) gerekli. |

---

## Silinen skill'ler (kayıt)

- **web-component-design** (2026-05-14) — SKILL.md'nin kendisi "SKIP for Razor + Vanilla JS projects (no JSX/SFC concept)" diyordu. React/Vue/Svelte ecosystem, Mosaik için tamamen alakasız. Süreç enflasyonu kanıtı.

---

## Toplam istatistik

- **Aktif:** 23 skill (silinen: 1)
- **Context yükü:** ~250 KB SKILL.md (ui-ux-pro-max %18, kalan 22 skill %82)
- **Proje-spesifik oranı:** 8/23 = %35 (Mosaik kuralları), 8/23 = %35 (workflow + dış sistem + library), 6/23 = %26 (Anthropic frontend), 1/23 = %4 (gözden geçirme)

---

## Skill ekleme kuralı

Yeni skill açmadan önce 3 soru:

1. **Mevcut bir skill bu konuyu kapsıyor mu?** Evet → mevcudu genişlet.
2. **Bu skill 3+ kez tetiklenecek mi?** Hayır → `docs/PATTERNS.md` veya `.claude/rules/` yeterli.
3. **Boyut 10 KB altında kalır mı?** Hayır → split veya azalt.

Üç soruya net "evet" olmadıkça skill ekleme. Skill silmeden skill eklemek = context enflasyonu.

---

## Üçüncü-parti skill kurulumu

`.gitignore` üçüncü-parti clone'ları dışlıyor. Yeni geliştirici / makinede kurulum:

```bash
# htmx-expert (ercan-er/htmx-claude-skill, MIT)
cd .claude/skills
git clone --depth 1 https://github.com/ercan-er/htmx-claude-skill htmx-expert
```
