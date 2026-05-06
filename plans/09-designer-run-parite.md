# Plan 09 — V2 Builder Designer ↔ Run Görsel Parite

**Tarih:** 2026-05-06
**Yazan:** Fikri / Claude
**Durum:** Onaylandı (2026-05-06, Fikri)

---

## 1. Problem

V2 builder canvas'ında widget'lar **simplified card** ile render ediliyor (`type-chip + w-head + w-body + bound-chip` iskelet, [builder-v2/builder-render.js](ReportPanel/wwwroot/assets/js/builder-v2/builder-render.js) `widgetInnerHtml(comp)`). Aynı dashboard'un Run sayfası ([Reports/Run/{id}](https://github.com/...)) **tam BKM brand styled** çıktıyı server-side render'dan ([Services/Rendering/KpiRenderer.cs](ReportPanel/Services/Rendering/KpiRenderer.cs), `ChartRenderer`, `TableRenderer`) üretiyor (`bg-white rounded-xl border shadow-sm + ikon kartı 9×9 + variant-spesifik DOM`).

Sonuç: Kullanıcı admin/EditReportV2'de simplified canvas görüyor, "Tam Önizle" tıklayıp iframe modal'da gerçek görseli ayrıca açıp karşılaştırmak zorunda. ADR-008'in "preview = Reports/Run tek renderer" kararı sadece "Tam Önizle" akışında uygulandı; **canvas içi WYSIWYG değil**.

Etki:
- KPI 4 variant (basic/delta/sparkline/progress) canvas'ta hep "basic" gibi görünür → kullanıcı variant değiştirir ama görsel aynı kalır → confusion.
- Chart canvas'ta inline SVG placeholder, gerçek Chart.js render Run'da → renk/font/eksen ayarları canvas'ta görünmez.
- Table conditional format (dataBar/colorScale/iconUpDown/negativeRed) canvas'ta atlanır.
- Color/Icon değişiklikleri kart başlığında göze çarpmaz çünkü iskelet farklı.

## 2. Scope

### Kapsam dahili
- **builder-render.js `widgetInnerHtml`** → KPI variant'ları (basic/delta/sparkline/progress) ile birebir BKM brand kart üretsin (server-side `KpiRenderer` HTML çıktısının client kopyası).
- **KPI variant'ları**: 4 variant ayrı render dalı, ikon kartı 9×9, uppercase başlık, value text-3xl font-bold, color text class, subtitle text-gray-400.
- **Chart**: canvas içinde gerçek Chart.js init (preview mode'da). Mevcut SVG placeholder yerine `<canvas>` element + `Chart.js` instance variant'a göre.
- **Table**: server-side `TableRenderer` HTML çıktısının client kopyası — header tipi badge, total row, stripe, sticky header, search, page size.
- **Edit-mode kontrolleri**: type-chip, w-head action buttons (dup/del), bound-chip selected/edit overlay olarak korunur. Mode='preview' iken bu overlay gizlenir, saf brand kart kalır.
- **Chart.js CDN**: builder-v2 sayfalarında zaten yüklü mü kontrol; yoksa script tag.

### Kapsam dışı
- **Server-side render değişikliği**: `KpiRenderer/ChartRenderer/TableRenderer` dokunulmaz; sadece client'a kopyalanır.
- **Razor view component / shared template pattern**: Plan §3.D olarak değerlendirildi, reddedildi (büyük refactor).
- **Mode='preview' iken canvas'ı iframe'le değiştirme**: Plan §3.B olarak değerlendirildi, reddedildi (network overhead, edit↔preview mode flicker).
- **builder-render.js'in tamamen yeniden yazımı**: incremental upgrade.

### Etkilenen dosyalar (tahmin)
- `builder-v2/builder-render.js` (326 → ~500 satır, +180) — KPI variant dalları, chart Chart.js init, table renderer
- `builder-v2/builder.js` (init: chart instance management) — destroy on remount
- `EditReportV2.cshtml` + `CreateReportV2.cshtml` — Chart.js CDN ekle (zaten varsa skip)
- `wwwroot/assets/css/dashboard-builder.css` (varsa) — edit overlay class'ları (preview mode'da hide)

**Tahmini boyut:** 3-4 dosya / +200 satır net / yeni dosya yok.
**Tahmini süre:** ~6-8h (1-2 oturum).

## 3. Alternatifler

### A: Inline iframe per widget (REDDEDİLDİ)
**Açıklama:** Her widget canvas'ta küçük iframe `/Reports/Run/{id}?widget=N&preview=1`. Server render full BKM brand.
**Reddetme sebebi:** N widget × N HTTP request, drag/resize sırasında her tetikte yenile, performans dramı. Edit overlay ekleme zorlaşır (iframe içine müdahale yok).

### B: Mode='preview' iken canvas'ı tam iframe'le değiştir (REDDEDİLDİ)
**Açıklama:** Edit mode'da simplified, preview mode'da tüm canvas'ı `<iframe srcdoc=DashboardRenderer.Render()>` ile değiştir.
**Reddetme sebebi:** Mode toggle'da flicker (iframe yeniden yükle), edit→preview→edit geçişlerinde state kaybı (Gridstack pozisyonları), drag/select edit kontrolleri preview'da yok ama ön/son geçişte bağ koparmak gerek.

### C: Razor view component / shared template (REDDEDİLDİ)
**Açıklama:** `KpiRenderer/ChartRenderer/TableRenderer`'ı Razor View Component'e refactor, hem server hem client tek HTML template.
**Reddetme sebebi:** Razor → JS template dönüştürme zor (T4 / source generator). Bakım karmaşası. Plan-First Tier 3 büyük refactor, M-11 kapsamı dışı.

### D (seçilen): builder-render.js incremental upgrade
**Açıklama:** Mevcut `widgetInnerHtml` fonksiyonunu KpiRenderer/ChartRenderer/TableRenderer çıktısına yakın HTML üretecek şekilde genişlet. Variant başına ayrı dal. Chart.js gerçek render canvas içinde. Edit overlay (drag handle, action buttons) absolute positioned ek katman, preview mode'da `display:none`.
**Sebep:** Server tarafı dokunulmaz (refactor risk yok). JS-only değişiklik (Razor cache sorun olmaz). Bakım yükü çift kat (server-client paralel) ama kullanıcı UX-WYSIWYG değer kazanır. Chart.js zaten Run sayfasında yüklü; CDN reuse.

## 4. Riskler

| Risk | Etki | Olasılık | Mitigation |
|---|---|---|---|
| **Bakım yükü çift kat** — server KpiRenderer değiştirilince client builder-render.js'in de senkronize edilmesi gerekir | orta | yüksek | Kısa vadede kabul; ileride view component pattern (Plan §3.C) tekrar değerlendirilir. Server-side renderer'lara değiştirmeden önce checklist'e "client paritesini koru" eklensin (CLAUDE.md). |
| **Chart.js bundle size** — canvas'a CDN script tag ekleme, edit sayfasında 70kb yük | düşük | yüksek | Run sayfası zaten yüklüyor; lighthouse impact ölçer, gerekirse `defer`. |
| **Chart instance leak** — widget destroy/remount sırasında Chart.js instance temizlenmezse memory leak | orta | orta | `widgetCharts` Map ile id→instance tut, removeWidget/refreshAllWidgets'ta `chart.destroy()`. |
| **Edit overlay z-index / pointer-events bug** — drag handle preview'da görünmemeli ama edit'te tıklanabilir olmalı | düşük | orta | CSS `.builder-v2.preview-mode .w-edit-overlay { display:none; }` net kural. |
| **Dragstart hover menu görünmüyor**, simplified canvas'ta net iken brand kart üzerinde grafik silinme aksiyonu zor erişilebilir | orta | orta | Hover'da overlay görünür, edit mode'da widget altına kbd hint "Çift-tık veri / Del sil". |
| **CSS sınıf çakışması** — `.kpi-value`, `.kpi-label` gibi class'lar mevcut canvas CSS'inde tanımlıysa server kart class'larıyla çatışır | düşük | düşük | Audit + namespace ekle (`.builder-v2 .kpi-value-card`) gerekirse. |

## 5. Done Criteria

- [ ] `builder-render.js widgetInnerHtml` KPI 4 variant ayrı dal, server `KpiRenderer` çıktısıyla **görsel olarak ayırt edilemez** (preview mode'da)
- [ ] Chart canvas'ta gerçek Chart.js render (line/bar/pie/doughnut/area/stacked/hbar/radar/polarArea/scatter)
- [ ] Table conditional format (dataBar/colorScale/iconUpDown/negativeRed) canvas'ta render
- [ ] Edit overlay (type-chip, w-head action, bound-chip) preview mode'da `display:none`
- [ ] Chart instance leak yok (widgetCharts Map + destroy on remove)
- [ ] Build yeşil, 228 test yeşil
- [ ] Smoke (browser /Admin/EditReportV2/13):
  - Edit mode: type-chip + actions görünür
  - Preview mode: brand kart, gerçek Chart.js, tam tablo
  - Tam Önizle iframe ile **görsel parite** (yan yana karşılaştırma kabul edilebilir)
- [ ] Screenshot before/after — docs/screenshots/m11-canvas-parite-{before,after}.png

## 6. Rollback Planı

Tek dosya `builder-render.js` (+ minor view edit'ler). Sorun olursa:
- `git revert <commit>` — tek commit ile geri al
- Eski simplified canvas behavior anlık restore
- Chart.js CDN script tag'ini view'lardan sil (varsa)
- DB schema dokunulmadığı için DB rollback gerekmez

## 7. Adımlar

### Faz 1 — KPI + edit overlay + Chart.js CDN ✅ 6 Mayıs 2026
1. [x] **F09.1** `widgetInnerHtml` KPI dalı 4 variant ayır — `renderKpiBasic/Delta/Sparkline/Progress`, server `KpiRenderer.Render*` paritesi (BKM brand kart: `bg-white rounded-xl border shadow-sm p-5` + ikon kartı 9×9 + uppercase başlık + text-3xl value)
2. [x] **F09.2** Edit overlay class'ı (`.w-edit-overlay`) — type-chip + w-head + w-actions oraya taşındı; CSS preview mode'da display:none (`builder-v2.css`)
3. [x] **F09.6** EditReportV2 + CreateReportV2 Chart.js CDN script tag (chart.js@4.4.0/chart.umd.min.js — Faz 2'de kullanılacak)
4. [x] Commit (Faz 1): `feat(m-11 f-09 faz-1): KPI 4 variant brand kart + edit overlay + Chart.js CDN (plan: 09)`

### Faz 2 — Chart.js entegrasyon + Table parite (BEKLEMEDE)
5. [ ] **F09.3** Chart.js entegrasyonu — `widgetCharts` Map, `mountChartWidget(comp, el)` her render sonrası init, `destroyWidgetChart(id)` cleanup; `refreshAllWidgets` öncesi tüm chart'ları destroy
6. [ ] **F09.4** Chart 10 variant Chart.js config — server `EmitChartInit` (`DashboardClientScripts.Chart.cs`) JS port
7. [ ] **F09.5** Table renderer dalı — conditional format (dataBar/colorScale/iconUpDown/negativeRed) + sticky header + total row + stripe (server `TableRenderer` paritesi)
8. [ ] **F09.7** Smoke: edit mode + preview mode iki ekran karşılaştır + screenshot
9. [ ] **F09.8** Faz 2 commit: `feat(m-11 f-09 faz-2): Chart.js + Table parite (plan: 09)`
10. [ ] **F09.9** Plan dosyası → `plans/archive/`

## 8. İlişkili

- ADR: `docs/ADR/008-dashboard-builder-v2.md` (preview tek renderer kuralı)
- Server renderer kaynak: `ReportPanel/Services/Rendering/KpiRenderer.cs`, `ChartRenderer.cs`, `TableRenderer.cs`
- Client renderer: `ReportPanel/wwwroot/assets/js/builder-v2/builder-render.js`
- Chart.js: zaten Run sayfasında yüklü, CDN URL `https://cdn.jsdelivr.net/npm/chart.js@4.x/dist/chart.umd.min.js`
- TODO: yeni satır eklenecek (M-11 Plan 09)
- Önceki plan: Plan 02 §F-7 alt-commit 3 (Gridstack CDN — Chart.js de eklenebilirdi ama atlanmış)

## 9. Onay

> Kullanıcı onay verene kadar implement edilmez.

- [x] Plan kullanıcıya gösterildi
- [x] Geri bildirim alındı — itiraz yok, "evet"
- [x] Onay alındı: 2026-05-06 Fikri
