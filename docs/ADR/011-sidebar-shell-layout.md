# ADR-011 — Sidebar Shell Layout (Project-wide)

## Durum

`Kabul edildi`

## Tarih

2026-04-27

## Bağlam

M-11 Dashboard Builder Redesign F-7 alt-3a sonrası **builder tek başına BKM brand** kazandı (commit `3a41847`). Ama proje geri kalanı (Dashboard, Reports, Admin, Profile, Auth, Logs) hala eski `_AppLayout.cshtml` topbar + `max-w-7xl mx-auto` center main pattern. Builder modern split-pane görünüyor; üç tık ötedeki rapor liste sayfası 2018 vibe veriyor — **görsel ada / kırılma**.

Kullanıcı verbatim:
> "tüm tasarımı bu builder ile uyumlu hale getirmek gerekiyor bence"
> "mockup renkleri değil tasarım bütünlüğü de uygulanmalı"

İki onaylı mockup hazır:
- [`app-shell-v1.html`](../../ReportPanel/wwwroot/mockups/app-shell-v1.html) — Genel Bakış (sidebar + ana içerik pattern)
- [`app-shell-builder-v1.html`](../../ReportPanel/wwwroot/mockups/app-shell-builder-v1.html) — Builder + sidebar shell birleşimi

Plan 02 A patikası: **Plan 02 önce** (M-11 builder finalize, alt-1+2+3a tamam) → **Plan 03 sonra** (project-wide harmonization).

## Karar

**Tüm proje sayfalarını sidebar shell pattern'ine taşı:**

- **`_AppLayout.cshtml`** rewrite: sol sidebar (240px, Ctrl+B → 60px collapse) + sticky topbar (56px, breadcrumb + search + actions slot) + main scroll area + mobile responsive overlay.
- **Inter sans + JetBrains Mono mono** projede default font (`<body>` font-family).
- **BKM kırmızı** (`--accent: #dc2626`) `_AppLayout` `style.css` token sisteminden geliyor (alt-3a'da eklendi).
- **Tek layout** — `_ShellLayout` paralel **yok** (alternatif B reddedildi, teknik borç).
- **Auth ayrı** — `_AuthLayout.cshtml` (sidebar'sız Login).
- **Aşamalı migration** — Faz A (skeleton) sonrası tüm sayfalar yeni layout altında çalışıyor (içerik ham), Faz B sayfa-by-sayfa pattern adaptation.

## Sebepler

- **Görsel bütünlük:** Kullanıcı sayfaları arası gezerken aynı dil. Builder'ın brand vurgusu izole ada değil; her admin/raporlama sayfasında aynı navigasyon ve tipografi.
- **Modern admin panel pattern:** Linear/Notion/Slack/Vercel dashboard pattern'i — sidebar + main. Kullanıcı tanıdık, öğrenme eğrisi düşük.
- **Sidebar nav scaling:** Topbar nav 4 link sınırlı. Sidebar'da 8-10+ nav item rahat (3 section: Ana / Sistem / Yönetim). Yeni eklenen feature'lar (Favoriler, Veri Kaynakları, Roller) sidebar'da yer bulur, topbar tıklamasız aşmaz.
- **Builder topbar consolidation:** M-11 F-7 alt-2/3a Razor topbar (breadcrumb + dirty chip + Önizle/Geri Al/Kaydet) `_AppLayout` topbar slot'una taşınır → çift-bar yok, tek source of truth.
- **Mockup onaylı:** Pattern referansı net, tahmin yok.

## Alternatifler (Reddedilenler)

### A: Topbar nav'ı genişlet (sidebar olmadan)
**Reddetme sebebi:** Topbar 4-5 link sınırlı, dikey scroll yok. Yeni eklemeler topbar'ı gergin yapar veya overflow menüye düşer. Sidebar pattern modern admin standart, yatay yer kaybı kabul edilir.

### B: Paralel layout (`_AppLayout` + `_ShellLayout`)
**Reddetme sebebi:** İki layout = iki nav = code duplication + UX kafa karışıklığı (kullanıcı bir sayfada sidebar, diğerinde topbar görür). Plan 03'te detaylı tartışıldı, "geçici çözüm uzun yaşayan teknik borç olur" gerekçesiyle red.

### C: Sidebar opsiyonel (sadece admin)
**Reddetme sebebi:** Kullanıcı isteği "tasarım bütünlüğü" — kısmi bütünlük bütünlük değildir. User-facing Dashboard/Reports/Run da sidebar pattern'i hak eder.

### D: Tailwind utility-first hibrit (custom class minimum)
**Reddetme sebebi:** Mockup'ta `.btn`, `.field`, `.lab`, `.inp`, `.dt`, `.qcard`, `.kpi-card` custom class'lar zaten kullanıldı, kullanıcı onayladı. Tailwind utility ile pattern bağ kurmak satır şişirir, mockup birebir port engellenir. Token sistemi (CSS variables) hibrit — Tailwind config'e gitmeye gerek yok.

## Sonuçlar

### Olumlu
- **Tek dil:** Tüm proje aynı görsel sistem.
- **Sidebar nav genişler:** 8-10+ nav item rahat. Yeni feature'lar yer bulur.
- **Builder consolidation:** Çift-bar değil tek-bar (Plan 03 Faz D).
- **Mockup birebir port:** Tahmin/uyarlama gerek yok, mockup-driven implementation.
- **Plan 02 alt-3b paralel bitirilebilir:** Builder canvas Gridstack consolidation Plan 03 Faz D ile birlikte.

### Olumsuz / Risk
- **`_AppLayout` rewrite yüksek riskli:** Tüm sayfaları etkiler. Mitigation: backup branch + Faz A statik smoke + aşamalı migration + Plan 03 risk tablosu (11 risk).
- **Tailwind utility class'larıyla custom class çakışma:** `.field/.lab/.inp` Tailwind `text-sm/p-2/border` gibi class'larla aynı element'te. Mitigation: scope (`.app-shell .inp`), tek tek view migration sırasında utility temizlenir.
- **Inter font tüm projede yüklendiğinde Tailwind text-* boyutları küçük metrikteki kayma:** Inter ve default sans-serif baseline yakın, ama 0.5-1px farklar olabilir. Mitigation: Faz A smoke ile font kayma kontrol, gerekirse `.app-shell` scope.
- **Mobile responsive (<1024px) sidebar overlay JS karmaşık:** Faz A'da minimum (position:fixed + backdrop). Detay polish Faz F sonu.
- **Solo-dev efor tahmin altı:** ~13-16h tahmin, gerçekçi 18-22h. Mitigation: P0/P1 ayrımı, P1 sayfaları sonraya itilebilir.

### Bilinmeyen
- **Custom Tailwind config (PostCSS pipeline) ne zaman?** Şu an CDN, `style.css` extension yeterli. JS bundle 5+ dosyaya çıkarsa esbuild + Tailwind PostCSS düşünülür (M-12+ veya ileride).
- **i18n (multi-language) ileride:** Sidebar text'ler Türkçe hardcoded — i18n eklenirse `@Localizer["..."]` pattern.

## Uygulama

- [x] [Plan 03](../../plans/03-project-wide-design-system-harmonization.md) yazıldı (commit `5f912c3`)
- [x] [Mockup app-shell-v1.html](../../ReportPanel/wwwroot/mockups/app-shell-v1.html) onaylandı
- [x] [Mockup app-shell-builder-v1.html](../../ReportPanel/wwwroot/mockups/app-shell-builder-v1.html) onaylandı
- [x] Bu ADR yazıldı
- [ ] Faz A skeleton (`_AppLayout` rewrite + app-shell.css/.js + style.css extend)
- [ ] Faz B P0 (Dashboard/Reports/EditReport)
- [ ] Faz B P1 (9 sayfa)
- [ ] Faz C component pattern
- [ ] Faz D builder topbar consolidation + Plan 02 alt-3b Gridstack
- [ ] Faz E `_AuthLayout` + Login
- [ ] Faz F test + screenshot + journal

## İlişkili Dosyalar

- Plan: [plans/03-project-wide-design-system-harmonization.md](../../plans/03-project-wide-design-system-harmonization.md)
- Mockup'lar: `wwwroot/mockups/app-shell-v1.html`, `app-shell-builder-v1.html`, `dashboard-builder-v3.html`
- Token kaynağı: `wwwroot/assets/css/style.css` (alt-3a'da eklendi, --ink-*, --accent, --paper, --canvas, --line, --chip)
- Builder shell (alt-3a): `wwwroot/assets/css/dashboard-builder.css` — Plan 03 Faz D'de genel pattern'ler `style.css` veya `app-shell.css`'e devir
- ADR-008: dashboard builder v2 (M-11)
- ADR-009: report-type consolidation
- ADR-010: plan-first tier sistemi

## Referanslar

- Konuşma: [docs/journal/2026-04-27.md](../journal/2026-04-27.md)
- Kullanıcı verbatim: "evet onaylıyorum mockuplar tamamdır" + "öncelik sırasına göre yap ana sayfayı yapalım mockup'tan canlıya"
- Mockup tasarım kaynak: `dashboard-builder-v3.html` token sistemi + Linear/Notion/Slack admin pattern'leri
