# ADR-014 · Frontend stack katmanlama: Vanilla + Alpine birincil, htmx ertelenmiş

- **Durum:** Kabul edildi (14 Mayıs 2026 — [VISION.md](../VISION.md) §5 "htmx + Alpine + Vanilla net karar yok" işaretine yanıt)
- **Etkilenen:** `_AppLayout.cshtml` (script include'ları), `wwwroot/assets/js/`, yeni view yazımı, `.claude/skills/mosaik-js-expert/SKILL.md`, `.claude/rules/js-conventions.md`
- **İlgili dosyalar:** `.claude/skills/alpine-js/SKILL.md`, `.claude/skills/htmx-expert/SKILL.md`
- **İlgili ADR'ler:** ADR-002 (modular-monolith — frontend stack cross-modül zorunlu tutarlılık)

## Bağlam

Mosaik frontend'inde **3 stack yan yana** yaşıyor:

1. **Vanilla JS (IIFE)** — `wwwroot/assets/js/*.js` (4 dosya). `dashboard-builder.js`, `admin-report-form.js`, `org-chart-render.js`, `app-shell.js`. ES5 hedef, IIFE wrap. Mosaik'in tarihsel default'u.
2. **Alpine.js 3.x** — CDN-yüklü (Local `~/lib/alpinejs/alpine.min.js`). View'larda **25 ayrı `x-data` directive** tespit edildi. Form toggle, modal, drawer, filter UI için kullanılıyor.
3. **htmx 2.0.4** — CDN-yüklü (`_AppLayout.cshtml:66`). View'larda **0 (sıfır) `hx-*` attribute** tespit edildi (canlı tarama 2026-05-14). Yani **yüklü ama kullanılmıyor**.

CLAUDE.md tarihsel olarak "Vanilla IIFE" diyor (`.claude/rules/js-conventions.md`). Sonradan Alpine + htmx eklendi (M-13 Plan 04, 2026-04-28). Ama hangisinin **birincil**, hangisinin **istisna**, hangisi **hangi use case için** — yazılı kural yok. 3 skill dosyası (`mosaik-js-expert`, `alpine-js`, `htmx-expert`) ayrı ayrı doğru ama birlikte stratejisi yok.

**Sorun (VISION §5):** "1-2 yıl sonra burada Alpine, burada htmx, burada vanilla karmaşası kaçınılmaz." Yeni view yazarken hangisi seçilmeli sorusu her seferinde yeniden çözülüyor.

## Karar

**3 stack katmanlı + use case kuralı:**

### 1. Vanilla JS IIFE — Birincil seçenek (Default)

- **Kapsam:** Tüm karmaşık client-side state, drag-drop, canvas/chart render, multi-step form orchestration, dosya 250+ satır business logic.
- **Konum:** `wwwroot/assets/js/<feature>.js` (dosya hard-limit: 350 satır, csharp-conventions ile uyum).
- **Pattern:** IIFE wrap, ES5 target, addEventListener, DOM API (`createElement`+`textContent`), XSS güvenli.
- **Skill:** [`mosaik-js-expert`](../../.claude/skills/mosaik-js-expert/SKILL.md).
- **Örnek:** Dashboard builder, OrgChart render, SP preview.

### 2. Alpine.js 3.x — UI state için Default

- **Kapsam:** Form toggle, modal aç/kapa, drawer, dropdown, filter bar input → list filter, tab switcher, kısa accordion.
- **Konum:** `.cshtml` view içinde `<div x-data="{ open: false }">` inline. Karmaşıklaşırsa (`window.<X>Factory` global'ine ya da `wwwroot/assets/js/<feature>.js`'e taşı).
- **Kural:** `x-data` body **20 satırı geçmesin**. Geçerse Alpine factory'ye veya Vanilla'ya çık.
- **Skill:** [`alpine-js`](../../.claude/skills/alpine-js/SKILL.md).
- **Örnek:** Filter bar (`x-data="{ q: '' }"`), sidebar collapsible group, modal/drawer açma.

### 3. htmx 2.x — **ŞIMDILIK ERTELENMIŞ** (kullanım kanıtı yok)

- **Durum 2026-05-14:** `_AppLayout.cshtml` 7. satırda yüklü ama view'larda **sıfır `hx-*` kullanımı**. Yüklü ama kullanılmıyor.
- **Karar:** **Yeni view'da htmx kullanma.** Mevcut Vanilla + Alpine ikilisi yeterli.
- **htmx'e ne zaman geri dönülür:** Aşağıdaki spesifik 3 use case için **plan onayı + ADR ekleme** ile:
  - (a) Server-rendered partial swap (örn. dashboard widget refresh, log table pagination)
  - (b) Multi-stage form submission with partial response (admin SP preview tipi)
  - (c) Sidebar live update (Plan 31 SMTP caller notification badge)
- **Hangi durumda htmx kullanılmaz:**
  - Client-side state yönetimi (Alpine'in işi)
  - Karmaşık DOM manipulation (Vanilla'nın işi)
  - SPA navigation (Mosaik MVC, SPA değil)
- **Skill:** [`htmx-expert`](../../.claude/skills/htmx-expert/SKILL.md) — küçük stub, gerçek use case ortaya çıkana kadar küçük kalır.

### 4. Stack seçim kararı — Karar matrisi

```
Yeni UI parçası → ?
├─ Client state lazım mı? (toggle, modal, dropdown, kısa form)
│  └─ Evet → Alpine x-data (≤20 satır) veya factory
│
├─ Karmaşık DOM (drag-drop, canvas, chart, 250+ satır)?
│  └─ Evet → Vanilla IIFE (wwwroot/assets/js/)
│
├─ Server-rendered partial swap?
│  ├─ Mevcut feature → fetch + replaceChildren (Vanilla)
│  └─ Yeni feature → ADR ek + plan + htmx VEYA fetch+Vanilla
│
└─ Pure rendering, JS yok?
   └─ Server-side Razor + Tailwind utility
```

## Alternatifler

- **(A) htmx birincil, Alpine kaldır** — htmx'in client state desteği sınırlı (toggle/modal için `hx-on::` ile yapılır ama doğal değil). 25 mevcut Alpine kullanımı htmx'e çevirmek ciddi refactor. **Red.**
- **(B) Alpine birincil, htmx + Vanilla minimum** — Alpine drag-drop ve canvas için zayıf. Mevcut Vanilla kod (dashboard-builder.js, org-chart-render.js) yeniden yazılması maliyetli. **Red.**
- **(C) Vue.js veya React'e geç** — Mosaik MVC + Razor, SPA değil. Component model bütün view'ları yeniden yazma. Build pipeline (vite/webpack) ekler. Solo dev için ekstrem. **Red.**
- **(D) 3 stack katmanlı + use case kuralı (seçilen)** — bu ADR. Mevcut kod aynen kalır, yeni yazım için kural net. htmx erteleme net (kullanım kanıtı yok). **Kabul.**
- **(E) htmx CDN include'ını şimdilik kaldır** — yüklü ama kullanılmıyor, ölü kod. **Kısmi kabul** — bu ADR htmx CDN include'ını **bekletilir** statusuna sokar; ileride spesifik use case için geri açılır. Plan 32 (Scheduled Reports email notification sidebar update) htmx ilk gerçek use case olabilir.

## Sonuçlar

**Olumlu:**
- Yeni view yazarken stack seçimi belirsizliği bitti — karar matrisi var.
- htmx erteleme kararı net — yüklü olsa bile yeni view'da kullanılmaz, scope karmaşası kaybedilir.
- Mevcut kod intact — 25 Alpine kullanımı + 4 Vanilla dosya korunur.
- `mosaik-js-expert` + `alpine-js` + `htmx-expert` skill'leri farklı katmanları temsil eder, tutarlı.

**Olumsuz / dikkat:**
- htmx CDN include'ı görece ölü kod (network 1 download). **Hafifletme:** ilk gerçek use case (Plan 32 email notification sidebar update) için bekleme. 3 ay içinde htmx kullanım gelmezse CDN include kaldır, ADR-014.1 ile dokumante et.
- Alpine `x-data` 20 satır kuralı **enforce edilmiyor** (linter yok). Code review'da göz disiplini.
- Vanilla 350 satır hard-limit zaten var (`js-conventions.md`). ADR ile uyumlu.
- htmx'i 3 use case için açma kararı yeniden ADR gerektirir — proseslesini açıkça yazdı (ADR ek + plan onayı).

## Uygulama

### Etkilenen rule güncellemesi

`.claude/rules/js-conventions.md`'ye şu satır eklenir:

```markdown
## Stack seçimi (ADR-014)

- **Vanilla IIFE** — drag-drop, canvas, chart, 250+ satır business logic
- **Alpine x-data** — UI state (toggle/modal/drawer/filter), ≤20 satır inline
- **htmx** — şimdilik ertelenmiş; yeni view'da kullanma (kullanım kanıtı yok)
```

### Skill güncellemesi

`mosaik-js-expert` skill başlığına ADR-014 reference + karar matrisi.

## Referanslar

- [VISION.md](../VISION.md) §5 — frontend stack belirsizliği
- `_AppLayout.cshtml:48` (tailwindcss local), `:65` (alpine local), `:66` (htmx CDN)
- `.claude/rules/js-conventions.md` — dosya pattern + güvenlik
- `.claude/skills/mosaik-js-expert/SKILL.md` — Vanilla pattern
- `.claude/skills/alpine-js/SKILL.md` — Alpine pattern
- `.claude/skills/htmx-expert/SKILL.md` — htmx referans (şimdilik stub)
- Canlı tarama 2026-05-14: htmx 0, Alpine 25, Vanilla 4 dosya
