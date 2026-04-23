# ADR-008 · Dashboard Builder v2 — UX redesign + chart expansion

- **Durum:** Kabul edildi (24 Nisan 2026)
- **Etkilenen:** `DashboardConfig` modeli, `DashboardRenderer`, `dashboard-builder.js` (SİLİNECEK, 6-7 modüle bölünecek), `EditReport.cshtml` + `CreateReport.cshtml` + `Reports/Run.cshtml` (split-pane + preview birleşimi), `ReportManagementService`, `DashboardConfigValidator`, migration 18, PDKS + Satış seed'leri, ADR-007 resolver.
- **İlgili TODO:** M-11 (13 faz, ~60h). Plan dosyası: `C:/Users/fikri.eren/.claude/plans/imdi-planlama-yap-bu-optimized-hippo.md`. Mockup: `ReportPanel/wwwroot/mockups/dashboard-builder-v3.html`.
- **İlgili ADR:** ADR-005 (config-driven), ADR-007 (named result contract — genişletiliyor), ADR-009 (rapor/dashboard tip birleşimi — ayrı doküman, aynı PR set'inde).

## Bağlam

Mevcut dashboard builder (22 Nisan itibarıyla) çalışıyor ama 4 yapısal sınırı var:

1. **Widget çeşidi dar** — `kpi` / `chart` / `table` ana tip, chart sub-type olarak sadece `line/bar/pie/doughnut`. PDKS + Satış canlı ama trend + hedef karşılaştırma + sparkline + koşullu format yok.
2. **UI tek sütun Razor** — `EditReport.cshtml:277-285` içinde `<textarea hidden>` + tek `<div>`. Split-pane yok, live preview yok, dirty/toast/validation banner yok, smart defaults yok, şablon yok, kısayol yok.
3. **Builder teknik borcu** — `dashboard-builder.js` 775 satır tek IIFE + string concat. `DashboardRenderer.cs` 422 satır `static class`. M-11 kapsamında 15+ yeni alt-tip eklenecek.
4. **Rapor/dashboard tipi ikiliği** (ADR-009'a bırakıldı) — `ReportCatalog.ReportType` 5+ yerde dallanma, iki farklı render yolu.

Kullanıcı 23 Nisan'da iki referans paylaştı:
- `D:/Downloads/dashboard-builder-design-v2.html` (1085 satır, görsel kit kaynağı)
- `D:/Downloads/ReportHub Dashboard Builder.html` (1118 satır, tam çalışan Gridstack + Chart.js mockup)

Hedef: Apache Superset'in Explore view UX prensiplerinden esinlenen, proje brand'ine uyumlu, 4 KPI variant + 10 chart tipi + koşullu format tablosu + live preview = Reports/Run destekli modern builder.

## Karar

### Superset'ten alınan 10 UX pattern'i (minimize edilmiş vanilla JS karşılığı)

| # | Pattern | Uygulama |
|---|---|---|
| 1 | Data/Customize drawer tab ayrımı | `builder-drawer.js`: binding/agregasyon "Veri", renk/eksen/ikon "Görünüm" |
| 2 | Kategorili chart gallery | Chip filter (Karşılaştırma/Trend/Pay/Dağılım/İleri) + arama + SVG thumbnail grid |
| 3 | Native Filters bar | `ParamSchemaJson` builder'da yaşar, chip'ler edit+preview+Run ortak |
| 4 | Inspect 3-tab preview | Çıktı / Sorgu (SP exec) / Ham JSON |
| 5 | Widget hover menu | Kopyala / Sorgu göster / Tam ekran / Sil |
| 6 | Loading/empty/error state | `data-state` attribute + CSS |
| 7 | Kısayol yardım modalı | `?` tuşu — Cmd+K fuzzy palette M-12'ye |
| 8 | Şablon market | KPI Trio / Trend Grafik / Detay Tablo preset'leri |
| 9 | Dark mode | **M-12'ye** — proje `color-scheme: light` zorunlu |
| 10 | Duplicate action | Widget kartta inline çoğaltma |

### Widget matrisi

**KPI (4 variant):** Basic / Delta / Sparkline / Progress.
**Chart (10 tip, hepsi Chart.js 4 native):** line / area / bar / hbar (`indexAxis:'y'`) / stacked (`stacked:true`) / pie / doughnut / radar / polarArea / scatter.
**Table:** Koşullu format (dataBar / colorScale / iconUpDown / negativeRed) + ayarlar (satır detay modal / toplam satırı / çizgili / sticky / arama / sayfalama).

**M-12'ye ertelendi:** Heatmap, Gauge, Combo (line+bar), Treemap, Sankey, Funnel, Bubble, Structure widget'ları (markdown / iframe / divider).

### Chart kütüphanesi: Chart.js 4 + **plugin YOK**

10 chart tipi Chart.js native ile karşılanıyor:
- `hbar` → `bar` + `indexAxis:'y'`
- `stacked` → `bar` + `scales.x/y.stacked:true`
- `area` → `line` + `fill:true`
- `polar` → `polarArea`
- `scatter` native

ECharts + ApexCharts reddedildi (migration 0 değer). Plugin'ler (datalabels, zoom, annotation, matrix, treemap, sankey, funnel) M-12'de değerlendirilir — M-11 scope dışı.

### Gridstack.js — **kabul**

Plan v1'de reddedilmişti ("v2 tasarım basit span/liste kullanıyor"). Kullanıcının yeni referansı Gridstack ile geldi ve drag-resize deneyimi çok daha güzel. Karar tersine:

- **Builder'da:** Gridstack 10.3 CDN — drag + resize + `{x,y,w,h}` JSON serialization.
- **Runtime (Reports/Run, dashboard view):** Gridstack **YOK** — CSS grid inline-style (`grid-column: X / span W; grid-row: Y / span H;`). Bundle tasarrufu + JS-less render.

Maliyet: +30KB Gridstack CDN (admin builder sayfasında, cache'li). Alternatif (span-only basit liste) admin UX'ini düşürdüğü için reddedildi.

### Preview = Reports/Run — tek renderer

Builder'daki "Önizle" modu = son kullanıcının göreceği Reports/Run ekranının birebir aynısı. Farklı render kodu yok. Mod toggle sadece:
- Widget chrome (w-head action'ları) visibility
- Gridstack interaction (enableMove / enableResize)
- Param bar edit-mode'da "varsayılan değerler"; preview+Run'da "canlı sorgu parametreleri"

Implementation patikası (F-7): **A (iframe)** — Builder'da Önizle = iframe `/Reports/Run/{id}?preview=1&configOverride=<draft>`. Save öncesi taslak config geçici param olarak geçer. B (DOM-level class toggle) reddedildi — SP re-run AJAX karmaşasına değer katmıyor.

### Param filter bar builder içinde yaşar

`ParamSchemaJson` (mevcut) builder drawer'ında düzenlenir. Chip'ler edit + preview + Reports/Run'da ortak. `Reports/Run.cshtml`'daki ayrı param input alanı kaldırılır (F-7 scope, ayrıntı ADR-009).

### Font: Inter + JetBrains Mono builder-only

Builder sayfasına özel CDN yüklenir. `_AppLayout.cshtml`'e eklenmez — proje geneli sistem font'uyla çalışmaya devam eder (kırma riski yok). Gerekirse F-12 smoke sonrası global'e geçiş ayrı karar.

### Türetilmiş Alanlar (Calculated Fields)

Client-side formül tabanlı yeni kolon. Admin `DeltaCiro = BugunCiro - GecenYilBugun` gibi ifade yazar, chart dataset / table kolonu / KPI valueField olarak kullanılır. Fonksiyonlar: `+ - * /`, `SUM/AVG/ROUND/IF/COALESCE/CONCAT`.

Güvenlik: `eval()` YASAK. Sandbox'lı expression parser (AST tabanlı, F-8 implementation) — `Function` constructor yok, sadece allowed AST node'ları. SP dokunulmaz, transform client-side.

### Tasarım uyum kuralı — **kritik**

Mockup dosyası (`dashboard-builder-v3.html`) **birebir kopyalanmayacak**. M-11 F-7 port sırasında:

1. **`_AppLayout.cshtml` header/footer içinde yaşayacak** — fixed header + max-w-7xl + fixed footer ile uyumlu, tam-ekran `body` değil.
2. **`wwwroot/assets/css/style.css` custom class'ları kullanacak** — `.btn-brand` / `.btn-brand-outline` / `.card-brand` / `.form-input-brand` / `.alert-warning` / `.card-header-brand` / `.nav-item-brand`. Mockup'taki ad-hoc `.btn`, `.inp`, `.palette-card` kaldırılır veya proje class'larına mapper'lar yapılır.
3. **Inter + JetBrains Mono** builder-only CDN — projenin geri kalanı default.
4. **Layout sınırı max-w-7xl** — proje geneli `max-w-7xl mx-auto px-6` pattern'ine uyar. Mockup'taki tam-ekran builder sadece EditReport sayfası için — ama `_AppLayout` kapsamında.
5. **Türkçe UI kuralı** (`.claude/rules/turkish-ui.md`) tam UTF-8 — mockup zaten uyumlu.
6. **Font Awesome 6** projede aktif — mockup'taki inline SVG'ler yerine `<i class="fas fa-*">` tercih edilebilir (ama SVG'ler de kalabilir, karar uygulama sırasında).

Mockup **tasarım referansı** — implementasyon proje kültürüne uyar.

## Alternatifler (atılanlar)

- **Apache ECharts / ApexCharts** — Chart.js'le aynı çeşitlilik daha büyük bundle. Geçiş değeri yok.
- **React / Vue dashboard builder** — proje framework-free; jQuery bile yok. Kabul edilemez.
- **Gridstack reddet** (v1'deki karar) — Kullanıcı referansı ve drag-resize deneyimi değerli, karar tersine.
- **Dark mode M-11'e** — proje `color-scheme: light` zorunlu, `_AppLayout`'ta yok. Ayrı karar lazım.
- **Cmd+K fuzzy palette** — tui-studio ilham yetmedi, sadece `?` shortcut modal (7 komut).
- **Structure widget'ları (markdown/iframe)** — XSS riski yüksek, solo-dev scope dışı.
- **Chart.js plugin'leri (heatmap/sankey/...)** — M-12'ye, M-11 scope sade tutar.

## Etki

### Dosya bazlı

**Yeni dosyalar (F-1..F-9):** `Rendering/` altında 8 C# dosyası (IWidgetRenderer + factory + Kpi/Chart/Table/Structure/Shell renderer'lar), 7 JS modülü (`builder-core/list/drawer/contract/preview/templates/commands`), CSS (`dashboard-builder.css`), 2 SQL migration (18 + 19), 2 ADR (008 + 009), schema JSON.

**Silinen:** `dashboard-builder.js` (775 satır). `DashboardRenderer.cs` (422 satır) orkestrasyona iner (~40 satır).

**Değişen:** `DashboardConfig.cs`, `DashboardConfigValidator.cs`, `ReportManagementService.cs`, `ReportsController.cs`, `AdminController.cs`, `Program.cs`, `EditReport.cshtml`, `CreateReport.cshtml`, `Reports/Run.cshtml` (ADR-009 scope'u), `appsettings.json`.

**Toplam:** ~18 yeni + ~18 değişen + 5 test/doküman = 40+ dosya, 13 faza dağılır, her commit <15.

### Migration (detay ADR-009'da)

`Migration 18`: PDKS + Satış v1 configleri v2'ye (`variant`/`numberFormat`/`axisOptions`/`tableOptions`/`calculatedFields`) + tüm `ReportType='table'` raporlar tek-table-widget dashboard'a idempotent çevrilir.

### Güvenlik

- **Calculated fields:** `eval()` yasak, AST-based parser (F-8).
- **Preview iframe:** `sandbox="allow-scripts"`, `allow-same-origin` YASAK (mevcut).
- **JSON save/load modalı:** admin-only, CSRF token zorunlu.
- **Param filter bar:** `ParamSchemaJson` validator'dan geçer, SP parametreleri `SqlParameter` ile bind.
- **Mockup CDN'leri** (Gridstack + Chart.js + Font Awesome + Google Fonts + Tailwind): SRI hash eklenir (F-7 PR).

## Revizyon tarihçesi

- **2026-04-23** — v1 plan (Plan agent çıktısı): Gridstack reddedildi, dark mode + Cmd+K + structure dahil.
- **2026-04-23 akşam** — kullanıcı yeni referans paylaştı (Gridstack + Chart.js canlı). Gridstack kabul, scope sadeleşti.
- **2026-04-23 akşam** — "preview = Reports/Run" kritik içgörüsü, tek renderer kararı.
- **2026-04-24 sabah** — bu ADR yazıldı, kararlar kristalleşti.
