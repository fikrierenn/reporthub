# Plan 02 — M-11 Dashboard Builder Redesign Finalize (F-7..F-12)

**Tarih:** 2026-04-27
**Yazan:** Claude (Cowork oturumu, Fikri yönetiminde)
**Durum:** `Onay bekliyor`
**Branch:** `feature/m-11-dashboard-builder-redesign`
**ADR'ler:** [008-dashboard-builder-v2](../docs/ADR/008-dashboard-builder-v2.md), [009-report-type-consolidation](../docs/ADR/009-report-type-consolidation.md)
**Mockup referans:** [dashboard-builder-v3.html](../ReportPanel/wwwroot/mockups/dashboard-builder-v3.html)

---

## 1. Problem

M-11 Dashboard Builder Redesign'ın ilk yarısı (F-0..F-6, 11 commit, ~30h) tamamlandı: Migration 18 + path consolidation + DashboardRenderer split + schema v2 + 10 chart tipi + 4 KPI variant + tablo conditional format. **Backend ve renderer hazır.** Ama:

- **Yeni variant'lar canlıda görünmüyor** — admin builder UI'da seçim yapılmadığı için. Migration 18 mevcut configleri `variant=basic` / `variant=<chartType>` ile yazdı; `delta`, `sparkline`, `progress`, `area`, `hbar`, `stacked`, `radar`, `polarArea`, `scatter` variant'ları sadece **builder UI yenilenince** seçilebilecek.
- **Conditional format + tableOptions de canlıda görünmüyor** — admin seçmeden aktifleşmez.
- **dashboard-builder.js 775 satır** monolitik — 500-satır kırmızı çizgi aşıldı, modüle bölme F-7 alt-commit 1.
- **EditReport/CreateReport.cshtml builder mount** v1 düzeninde — split-pane (palette + Gridstack canvas + Veri/Görünüm drawer + topbar) yok.
- **Live preview yok** — şu an admin builder kaydet → reload → görsem gerekiyor. Mockup'taki Edit↔Preview toggle (preview = `Reports/Run` iframe + configOverride) yok.

Plan: F-7..F-12 ile admin tarafı birinci-sınıf hale gelir, yeni variant'lar üretime girer, builder modern UX kazanır.

## 2. Scope

### Kapsam dahili
- **F-7** UI redesign (3 alt-commit): JS modül split + Razor split-pane + brand CSS + Gridstack
- **F-8** Drawer form UI: variant picker, chart gallery, swatch/icon grid, span toggle, suggest pills, calculated fields editör
- **F-9** Live preview + dirty/toast + validation banner + Geri Al
- **F-10** Şablon sistemi + kbd shortcuts
- **F-11** Smart defaults (SP preview → suggest pills + chart tipi öneri)
- **F-12** E2E smoke + screenshot + handoff journal
- **Opsiyonel öncesi:** F-1.5 alt-commit 3 (Migration 19 DROP + ReportType property sil) — Tier 1, kullanıcı onayında ayrı commit

### Kapsam dışı (M-12'ye)
- Dark mode · Cmd+K fuzzy palette · structure widgets (markdown/iframe/divider)
- heatmap/gauge/treemap/sankey/funnel/bubble/combo chart tipleri (Chart.js plugin gerekir)
- Tam undo/redo stack (Ctrl+Z/Y son N state)
- Cross-chart filter
- DashboardRenderer DI refactor (F-2'de atlandı, 9 XSS testi static API'ye bağımlı)
- Chart.js plugin'leri (datalabels/zoom/annotation/matrix)
- Pre-commit hook ile Tier 3 plan referansı zorlaması

### Etkilenen dosyalar (tahmin)

**F-7 alt-commit 1 — JS modül split:**
- YENİ: `wwwroot/assets/js/dashboard-builder/{builder-core.js, builder-list.js, builder-canvas.js, builder-drawer.js, builder-contract.js, builder-preview.js, builder-templates.js}` (~7 dosya, ~1200 satır toplam)
- SİL: `wwwroot/assets/js/dashboard-builder.js` (775 satır)
- Pattern: IIFE + `document.dispatchEvent(new CustomEvent('db:*', {detail}))` event bus

**F-7 alt-commit 2 — Razor split-pane:**
- M: `Views/Admin/EditReport.cshtml` + `CreateReport.cshtml` — yeni split-pane layout (sol 320px palette + contract bar + tab strip · orta Gridstack canvas · sağ 380px drawer Veri|Görünüm · topbar breadcrumb+dirty chip+Önizle+Geri Al+Kaydet)
- M: `Views/Shared/_AppLayout.cshtml` — sadece builder route'unda max-w-full override (admin sayfa wrap kuralı korunur)

**F-7 alt-commit 3 — CSS + CDN:**
- YENİ: `wwwroot/assets/css/dashboard-builder.css` (~420 satır, BKM brand: `.btn-brand`, `.card-brand`, `.form-input-brand` üstüne builder-spesifik)
- M: `Views/Admin/EditReport.cshtml` head'e Gridstack CSS + JS CDN (SRI ile) + Inter/JetBrains Mono font CDN — sadece builder sayfasında

**F-8 — Drawer form:**
- M: `wwwroot/assets/js/dashboard-builder/builder-drawer.js` — 4-kart KPI variant picker, 10-tip SVG thumbnail chart gallery (kategorili: Tümü/Karşılaştırma/Trend/Pay/Dağılım/İleri + arama), 7-renk swatch-grid, 16-icon grid, 1-4 span toggle, suggest pills (SP preview kolonlar), calculated fields editör (formul + fx badge + inline error)
- M: `Controllers/AdminController.cs` (calc field validation endpoint mu yoksa client-only mu — F-8 başında karar)

**F-9 — Live preview + dirty + toast + validation + Geri Al:**
- YENİ: `AdminController.DashboardPreview` (POST: `{configOverride, paramSchema}`, döner: srcdoc HTML) — preview iframe için
- YENİ: `AdminController.DashboardValidate` (POST: `{configJson}`, döner: `DashboardConfigValidator` sonucu)
- M: `builder-preview.js` — iframe srcdoc fetch + throttle (300ms)
- M: `builder-core.js` — dirty state tracker (`hasUnsavedChanges`), beforeunload warning, last-save snapshot (`Geri Al` butonu)
- M: `builder-drawer.js` — validation banner (red border + tooltip)

**F-10 — Şablon + kbd shortcuts:**
- M: `builder-templates.js` — KPI Trio (3 KPI yan yana) / Trend Grafik (line+area combo) / Detay Tablo (sortlu+sticky+pagination) preset'leri JSON olarak embed
- M: `builder-core.js` — keyboard listener (Ctrl+S=save, Ctrl+P=preview, Esc=cancel-edit, Delete=remove-selected, ?=shortcuts modal)
- YENİ küçük: `builder-shortcuts-modal.js` veya inline-HTML `?` modal

**F-11 — Smart defaults:**
- M: `builder-drawer.js` + `admin-report-form.js` interaction — SP preview kolonları geldiğinde:
  - suggest pills'i otomatik doldur
  - chart tipi öneri (örn. tarih + sayı kolonu varsa "line" öner, kategori + sayı varsa "bar")
- M: `Controllers/AdminController.SpPreview` — kolon metadata'ya tip bilgisi ekle (date/number/string/bool)

**F-12 — E2E smoke + screenshot + journal:**
- M: `tests/ReportPanel.Tests/Controllers/AdminController.DashboardPreviewTests.cs` (yeni dosya, 3-5 smoke test)
- M: `tests/ReportPanel.Tests/Services/DashboardConfigValidator_v2_FullCoverageTests.cs` (eksik kalan whitelist + edge case'ler)
- YENİ: `docs/screenshots/m11-builder-{edit,preview,drawer-veri,drawer-gorunum}.png` (4 screenshot)
- M: `docs/journal/2026-MM-DD.md` (M-11 finalize handoff entry)

**Tahmini boyut toplam:** ~25-30 dosya değişecek, ~3500-4500 satır eklenir/silinir (eski JS silinecek 775 satır netten düşer).

## 3. Alternatifler

### A: Tek mega-commit (F-7..F-12 birleşik)
**Açıklama:** Tüm UI redesign + form + preview + şablon + smart defaults bir commit'te.
**Reddetme sebebi:**
- 25-30 dosya · 4000+ satır → commit-discipline.md 15-eşik ihlali, hatta katlı.
- Bug bulunduğunda revert imkansız (her şey iç içe).
- Kod review yapılamaz.

### B: Faz başına tek commit (6 commit, F-7 dahil)
**Açıklama:** F-7 tek commit (~10h, 7+ JS modül + Razor + CSS) + F-8..F-12 ayrı.
**Reddetme sebebi:**
- F-7 tek başına 15-20 dosya — yine eşik ihlali.
- F-7'de bir alt-iş bozulursa (ör. CSS regresyonu) tüm UI değişikliği geri alınmalı.

### C: SEÇİLEN — F-7 alt-commit'lere bölünür (3 alt-commit), F-8..F-12 faz başına tek commit
**Açıklama:**
- F-7 alt-1: JS modül split (eski dosya sil + 7 modül ekle, fonksiyonel parite)
- F-7 alt-2: Razor split-pane + brand CSS dosyası iskelet
- F-7 alt-3: Gridstack CDN + brand CSS detay + font CDN + final touchup
- F-8: drawer form UI (1 commit, ~6h, ~5-8 dosya)
- F-9: live preview + dirty/toast + Geri Al (1 commit, ~5h, ~5 dosya)
- F-10: şablon + kbd (1 commit, ~3h, ~3 dosya)
- F-11: smart defaults (1 commit, ~3h, ~3 dosya)
- F-12: test + screenshot + journal (1 commit, ~3h, ~5 dosya)

**Toplam:** 3 + 5 = **8 commit**, ortalama 5-10 dosya/commit, 15-eşiği güvenli.

**Sebep:**
- Her alt-commit testable + revertable
- F-7 alt-1 (modül split) **fonksiyonel parite** olarak doğrulanabilir — eski dosya silinince builder hala çalışmalı (yeni özellik yok, sadece yeniden yapılanma)
- F-7 alt-2 (Razor) layout değişikliği — alt-1'i bozarsa belli olur
- F-7 alt-3 (CSS + Gridstack) görsel + interaction katmanı — alt-2 üzerine inşa
- F-8..F-12 faz başına commit zaten Tier 2 (mevcut pattern, küçük feature) ölçüsünde

### D: F-9'u F-8'in içine al
**Açıklama:** Drawer form UI + live preview birleşik commit.
**Reddetme sebebi:** F-9'un kapsamı (preview endpoint + dirty/toast + Geri Al snapshot + validation banner) F-8'den (drawer form) bağımsız — birleştirme commit'i 12+ dosyaya çıkarır.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| F-7 alt-1 modül split fonksiyonel parite kırar | Yüksek (canlı builder bozulur) | Orta | Manuel smoke önce: PDKS dashboard 4 KPI + chart + table render — alt-1 sonrası birebir görünmeli |
| Gridstack CDN yüklemezse builder yüklenmez (CDN downtime) | Yüksek | Düşük | SRI hash + opsiyonel local fallback (`wwwroot/lib/gridstack/`) — F-7 alt-3 |
| `_AppLayout` max-w-7xl override builder dışı sayfaları kırar | Orta | Düşük | Override sadece builder route'da (URL pattern check), test: admin index + reports index sayfaları sığmaya devam etmeli |
| Live preview iframe configOverride çok büyük → URL limit aşar | Orta | Orta | POST endpoint (configOverride request body'de, query string'de değil) — F-9 endpoint design |
| Calculated fields formula parser AST yok → eval() kaçar | YÜKSEK (XSS/RCE) | Orta | Sandbox AST parser zorunlu (F-8) — eval() yasağı kuralda var (`security-principles.md` §2). Whitelist: `+ - * /`, SUM, AVG, ROUND, IF, COALESCE, CONCAT |
| Şablon JSON'ları hardcode → schema v2 değişince güncelleme gerek | Düşük | Yüksek | Şablonları `DashboardConfigValidator` üzerinden geçir + `ConfigSchemaVersion: 2` set — F-10 |
| F-12 testleri F-9 endpoint'e bağımlı → endpoint değişirse test kırılır | Düşük | Orta | F-9 endpoint contract'ı F-9 commit'ten önce ADR-008 ekler bölümüne kayıtla |
| Inter/JetBrains Mono font CDN kurumsal proxy'de bloklu | Düşük | Düşük | Builder font fallback: `system-ui, sans-serif`. Font CDN failure UX'i bozmaz |
| Preview lock (dotnet build öncesi preview_stop gerekli) F-7..F-12 boyunca tekrarlar | Düşük (sadece zaman kaybı) | Yüksek | Build hook ile preview otomatik durdurma TODO (M-12). Şimdilik manuel disiplin |

## 5. Done Criteria

### F-7 (3 alt-commit)
- [ ] Eski `dashboard-builder.js` (775 satır) silindi, 7 modül yerinde
- [ ] Builder sayfa yüklenir, mevcut PDKS dashboard düzenlenebilir (KPI + chart + table render)
- [ ] Drag-resize Gridstack ile çalışır (eski drag-drop sıralama yerine)
- [ ] Sol palette + sağ drawer (Veri/Görünüm tab) + topbar görünür
- [ ] BKM brand renkleri (kırmızı `#dc2626` accent) builder'a uygulandı, max-w-7xl admin sayfa wrap'i bozmadı

### F-8
- [ ] KPI eklerken 4 variant kart görünüyor (basic/delta/sparkline/progress) + seçim renderer'a yansıyor
- [ ] Chart eklerken 10 tip SVG thumbnail gallery + arama + 6 kategori chip
- [ ] Swatch-grid 7-renk + icon-grid 16 + span-toggle 1-4 çalışıyor
- [ ] Calculated fields editör: formül girilince fx badge'li suggest pill, hatalı formül inline error, eval() **YOK** (AST parser)
- [ ] Suggest pills SP preview kolonlarından dolu

### F-9
- [ ] `Önizle` butonu → iframe srcdoc fetch → preview = `Reports/Run` çıktısı (configOverride ile)
- [ ] Form değiştirildiğinde dirty chip görünür + beforeunload warning
- [ ] Validation hatası → red border + drawer banner
- [ ] `Geri Al` → last-save snapshot'a döner (current oturumun kaydet'ten önceki state)

### F-10
- [ ] 3 şablon (KPI Trio / Trend Grafik / Detay Tablo) "Şablondan Seç" butonuyla yüklenebilir
- [ ] `?` modal kbd shortcuts gösterir
- [ ] Ctrl+S kaydet, Ctrl+P önizle, Esc edit-cancel, Delete seçili widget sil

### F-11
- [ ] SP preview tarih + sayı kolonu döndürürse default chart tipi `line` öner
- [ ] Kategori + sayı kolonu → `bar` öner
- [ ] Tek sayı (1 satır 1 kolon) → KPI öner
- [ ] Suggest pills SP preview sonrası otomatik dolu

### F-12
- [ ] 122/122 mevcut test yeşil + 5+ yeni F-9 endpoint smoke test
- [ ] 4 screenshot (`docs/screenshots/m11-builder-*.png`) eklendi
- [ ] Handoff journal entry (`docs/journal/<tarih>.md`) yazıldı
- [ ] M-11 main'e merge'e hazır (PR açılabilir)

## 6. Rollback Planı

### Faz başına revert
- Her commit bağımsız revertable. F-7 alt-1 bozarsa: `git revert <hash>` → eski dashboard-builder.js geri gelir, fonksiyonel parite olduğundan builder çalışmaya devam eder.
- F-9 endpoint bozarsa: F-9 revert + `Önizle` butonunu disable et (builder.js feature flag).
- F-7 alt-2 (Razor) bozarsa: revert, palette/drawer eski tek-form düzenine döner.

### Migration 19 (F-1.5 alt-3 ileride)
- DROP COLUMN ReportType — geri alma için DB backup zorunlu
- `Database/backup/before-migration-19-<timestamp>.sql` (Migration 18 örneğinde olduğu gibi)
- Rollback: backup'tan ReportType kolonunu geri yükle + tüm ilgili commit'leri revert (F-1.5 alt-1, alt-2, alt-3)

### Branch reset (acil)
- Tüm M-11 finalize'i geri al: `git reset --hard <pre-F-7-commit>` (mevcut durum: `b99ec33` öncesi `5135acc` veya M-11 alt-1 başına)
- **AÇIK ONAY GEREKİR** (destructive, commit-discipline.md zararlı komut)

## 7. Adımlar / İçerdiği TODO maddeleri

### Hazırlık (oturum başı)
1. [ ] Branch durumu kontrol: `feature/m-11-dashboard-builder-redesign` üzerinde 16 commit (11 M-11 + 4 Claude tooling + 1 journal)
2. [ ] `dotnet build --nologo` → 0w 0e doğrula
3. [ ] `dotnet test` → 122/122 doğrula
4. [ ] PDKS dashboard manuel smoke (browser): 4 KPI + chart + table render

### Opsiyonel önce: F-1.5 alt-commit 3 (Tier 1, ~30dk)
5. [ ] `Database/19_DropReportTypeColumn.sql` yaz (idempotent, backup'lı)
6. [ ] Dev DB'de dry-run + apply
7. [ ] `Models/ReportCatalog.cs` `[Obsolete] public string ReportType` field SİL
8. [ ] `ReportPanelContext.cs` HasDefaultValue("dashboard") + #pragma SİL
9. [ ] Tüm derleme yeşil + 122/122 test yeşil
10. [ ] Commit: `chore(m-11 f-1.5): migration 19 — drop ReportType column (plan: 02)`

### F-7 alt-commit 1 — JS modül split (~4h)
11. [ ] Yeni klasör: `wwwroot/assets/js/dashboard-builder/`
12. [ ] `builder-core.js` (~160 sat) — state + render orkestrasyon + event bus init
13. [ ] `builder-list.js` (~150 sat) — sol palette: widget list + drag handle + dup/delete
14. [ ] `builder-canvas.js` (~180 sat) — Gridstack wrap (init/destroy/save/load layout)
15. [ ] `builder-drawer.js` (~350 sat) — sağ panel Veri/Görünüm tab + form alanları (boş iskelet, F-8'de doldurulacak)
16. [ ] `builder-contract.js` (~110 sat) — ResultContract bar (sol palette üstü)
17. [ ] `builder-preview.js` (~130 sat) — Önizle butonu + iframe placeholder (F-9'da doldurulacak)
18. [ ] `builder-templates.js` (~120 sat) — şablon listesi placeholder (F-10'da doldurulacak)
19. [ ] `dashboard-builder.js` SİL (775 sat)
20. [ ] EditReport/CreateReport.cshtml `<script src>` → 7 modül listesi
21. [ ] **Smoke:** PDKS dashboard hala düzenlenebilir, fonksiyonel parite
22. [ ] Commit: `feat(m-11 f-7): dashboard-builder.js → 7 module split (plan: 02)`

### F-7 alt-commit 2 — Razor split-pane (~3h)
23. [ ] `EditReport.cshtml` yeni layout: `<div class="builder-shell grid-cols-[320px_1fr_380px]">`
24. [ ] Sol: palette + contract bar + tab strip (Bileşenler / Şablonlar)
25. [ ] Orta: Gridstack canvas (`<div class="grid-stack">`) + topbar (breadcrumb + dirty chip + Önizle + Geri Al + Kaydet butonları)
26. [ ] Sağ: drawer (`<div class="builder-drawer">`) Veri/Görünüm tab strip
27. [ ] `CreateReport.cshtml` aynı yapı
28. [ ] `_AppLayout.cshtml` builder route override: `@if (ViewContext.RouteData.Values["controller"] == "Admin" && action in {"EditReport","CreateReport"}) { max-w-full }`
29. [ ] **Smoke:** sayfa yüklenir, palette/canvas/drawer üç sütun görünür (içleri henüz boş, F-7 alt-3'te dolar)
30. [ ] Commit: `feat(m-11 f-7): EditReport/CreateReport split-pane Razor (plan: 02)`

### F-7 alt-commit 3 — CSS + Gridstack CDN (~3h)
31. [ ] `wwwroot/assets/css/dashboard-builder.css` (~420 sat): `.builder-shell`, `.builder-palette`, `.builder-canvas`, `.builder-drawer`, `.builder-topbar`, `.dirty-chip`, `.section-hd`, `.swatch-grid`, `.icon-grid`, `.tab-strip`, `.kbd-hint`
32. [ ] BKM brand: `--brand-red: #dc2626`, accent state'ler
33. [ ] Gridstack CSS + JS CDN (`https://cdn.jsdelivr.net/npm/gridstack@10.x/dist/...`) + SRI hash
34. [ ] Inter + JetBrains Mono font CDN (Google Fonts) — sadece builder sayfa
35. [ ] Builder canvas Gridstack init: `GridStack.init({column: 12, cellHeight: 80, ...})`
36. [ ] **Smoke:** PDKS dashboard widget'ları Gridstack'te görünür, drag-resize çalışır
37. [ ] Commit: `feat(m-11 f-7): dashboard-builder.css + Gridstack CDN + brand UI (plan: 02)`

### F-8 — Drawer form UI (~6h)
38. [ ] `builder-drawer.js` Veri tab: type switcher (KPI/Chart/Table) + variant picker (KPI 4 kart / Chart 10-tip gallery)
39. [ ] Chart gallery: 6 kategori chip + arama input + 10 SVG thumbnail (mockup'tan port)
40. [ ] Görünüm tab: swatch-grid 7-renk + icon-grid 16 + span toggle 1-4
41. [ ] Suggest pills (SP preview kolon listesi) — drag/click ile form alanına insert
42. [ ] Calculated fields editör: textarea + AST parser (whitelist: `+ - * /`, SUM, AVG, ROUND, IF, COALESCE, CONCAT) + fx badge'li pill + inline error
43. [ ] section-hd pattern (form bölüm başlıkları)
44. [ ] **Smoke:** mevcut PDKS KPI'lar variant=basic gözükür, delta'ya değiştirilince renderer'a yansır
45. [ ] Commit: `feat(m-11 f-8): drawer form UI + variant picker + chart gallery + calc fields (plan: 02)`

### F-9 — Live preview + dirty + toast + Geri Al (~5h)
46. [ ] `AdminController.DashboardPreview(int reportId, [FromBody] PreviewRequest req)` — döner: srcdoc HTML (configOverride uygulanmış)
47. [ ] `AdminController.DashboardValidate([FromBody] string configJson)` — `DashboardConfigValidator.Validate()` → JSON yanıt
48. [ ] `builder-preview.js` Önizle butonu → POST `/Admin/DashboardPreview` → iframe.srcdoc=response
49. [ ] Throttle 300ms (form değişikliği → preview re-render)
50. [ ] `builder-core.js` dirty state: `hasUnsavedChanges` flag, beforeunload warning
51. [ ] Geri Al butonu: `lastSaveSnapshot` (sayfa yüklendiğinde + her save'de güncellenir) → state restore
52. [ ] Validation banner: drawer'da kırmızı banner (`{errors}` listesi), submit disable
53. [ ] Toast (kaydet/önizle/hata) — sağ alt köşe, 3sn auto-dismiss
54. [ ] **Smoke:** edit → önizle → preview iframe yenilenir, hata varsa banner + toast
55. [ ] Commit: `feat(m-11 f-9): live preview iframe + validation banner + dirty/toast/Geri Al (plan: 02)`

### F-10 — Şablon + kbd shortcuts (~3h)
56. [ ] `builder-templates.js`: 3 şablon JSON (KPI Trio / Trend Grafik / Detay Tablo) — schemaVersion=2, validator'dan geçer
57. [ ] "Şablondan Seç" butonu modal → 3 kart → tıklayınca canvas'a yükle
58. [ ] `builder-core.js` keyboard listener: Ctrl+S=save, Ctrl+P=preview, Esc=cancel-edit, Delete=remove-selected
59. [ ] `?` tuşu → kbd shortcuts modal (5 kısayol listesi)
60. [ ] **Smoke:** boş canvas'a "Trend Grafik" şablonu yükle, render et
61. [ ] Commit: `feat(m-11 f-10): 3 sablon + 5 kbd shortcut + ? modal (plan: 02)`

### F-11 — Smart defaults (~3h)
62. [ ] `AdminController.SpPreview` kolon metadata'ya tip ekle (`{name, type: "date"|"number"|"string"|"bool"}`)
63. [ ] `builder-drawer.js` SP preview event listener: kolon listesi alındığında suggest pills doldur
64. [ ] Chart tipi öneri algoritması:
  - tarih kolonu + 1+ sayı kolonu → `line`
  - kategori (string) kolonu + sayı kolonu → `bar`
  - 2 sayı kolonu → `scatter`
  - 1 satır + 1 sayı → KPI
  - 3+ kategori + sayı → `pie` veya `doughnut`
65. [ ] **Smoke:** sp_PdksPano preview → KPI alanlarına auto-suggest dolu
66. [ ] Commit: `feat(m-11 f-11): smart defaults — kolon auto-detect + chart tipi onerisi (plan: 02)`

### F-12 — Test + screenshot + journal (~3h)
67. [ ] `AdminController.DashboardPreviewTests.cs` — 3 smoke (valid configOverride, invalid JSON, eksik widget)
68. [ ] `DashboardConfigValidator_v2_FullCoverageTests.cs` — calculated field formula whitelist, conditional mode kombinasyonları
69. [ ] 4 screenshot: builder edit / preview / drawer-veri / drawer-gorunum (`docs/screenshots/m11-builder-*.png`)
70. [ ] `docs/journal/<tarih>.md` M-11 finalize handoff entry
71. [ ] PR check: 122+8 = 130 test yeşil, 0 build error/warning
72. [ ] Commit: `test(m-11 f-12): smoke tests + screenshots + handoff journal (plan: 02)`
73. [ ] Plan dosyasını arşive taşı: `git mv plans/02-m11-dashboard-builder-finalize.md plans/archive/`

## 8. İlişkili

- ADR: [008-dashboard-builder-v2](../docs/ADR/008-dashboard-builder-v2.md), [009-report-type-consolidation](../docs/ADR/009-report-type-consolidation.md), [010-plan-first-tier-system](../docs/ADR/010-plan-first-tier-system.md)
- Mockup: [dashboard-builder-v3.html](../ReportPanel/wwwroot/mockups/dashboard-builder-v3.html)
- Önceki plan: `C:/Users/fikri.eren/.claude/plans/imdi-planlama-yap-bu-optimized-hippo.md` (M-11 13-faz tam plan, F-0..F-6 tamamlandı)
- TODO ID: madde 29.5 (TODO.md:129)
- Renderer kaynak: `ReportPanel/Services/Rendering/` (DashboardRenderer.cs + 6 yardımcı, F-2'de bölündü)
- Frontend skill'leri (CLAUDE.md §2 madde 2): `frontend-design`, `visual-design-foundations`, `interaction-design`, `responsive-design`, `web-component-design`, `design-system-patterns`, `accessibility-compliance` — F-7/F-8/F-10'da otomatik tetiklenecek
- Agent'lar: F-7 alt-commit'lerinde `code-architect` (split tasarımı) + `code-reviewer` (alt-commit sonrası), F-12'de `pr-test-analyzer` + `silent-failure-hunter`

## 9. Onay

- [ ] Plan kullanıcıya gösterildi
- [ ] Geri bildirim alındı (varsa düzeltildi)
- [ ] Onay alındı: <tarih, kullanıcı imzası>

---

**Önemli not:** F-7 alt-commit 1 (modül split) **fonksiyonel parite** olarak tamamlanır — yeni özellik eklemez, sadece kod organize eder. Smoke test: PDKS dashboard render birebir aynı kalmalı. Bu disiplin tüm sonraki alt-commit'lerin temelini oluşturur (eski koddan yenisine güvenli geçiş).
