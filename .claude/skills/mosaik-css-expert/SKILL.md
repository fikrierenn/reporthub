---
name: mosaik-css-expert
description: Mosaik projesi CSS uzman skill'i. Utility-first + tokens + BEM + namespace disiplini. Yeni view yazılırken, inline-style refactor edilirken, modül CSS değiştirilirken zorunlu tetiklenir. Plan 25.1 P1-P8 sonuçlarını uygular — tek source-of-truth components.css, semantic tokens.css, modül-özel istisnai. css-classify skill'i ile zincirleme çalışır (önce sınıflandırma akışı, sonra bu skill ile pattern uygula).
---

# Mosaik CSS Uzman Skill'i

## Ne zaman tetiklenir

- Yeni `.cshtml` view yazılırken
- Mevcut view inline-style refactor edilirken
- Modül CSS'inde sınıf eklerken/silerken
- `components.css` / `tokens.css` / `components-<modül>.css` / `org-chart.css` / `builder-v2.css` dokunulurken
- Class isimlendirme kararında (`css-classify` ile birlikte)

## Mosaik CSS mimari (Plan 25.1 P1-P8 sonrası)

```
tokens.css              (130 satır)  — Renk değişkenleri, semantic ton
components.css          (2096)       — TEK SOURCE-OF-TRUTH generic utility
app-shell.css                        — Sidebar + topbar + layout
notifications.css                    — Bildirim widget
utilities.css                        — Mini helper (mt-/mb-/text-*)
legacy.css                           — Geriye uyumluluk (silinmesi planlı)

components-tamim.css    (87)         — SADECE Tamim domain (envelope/quill/urgent-toggle/scope)
org-chart.css           (161)        — SADECE chart drag-drop tree + dabeng override
builder-v2.css          (1516)       — SADECE V2 Dashboard Builder canvas/drawer/widget
```

**Yükleme sırası (_AppLayout.cshtml):**
1. FontAwesome CDN
2. Inter + JetBrains Mono Google Fonts
3. `tokens.css`
4. `components.css`           ← canonical generic
5. `legacy.css`
6. `utilities.css`
7. `app-shell.css`
8. `components-tamim.css`     ← her sayfada yüklü, sadece tamim-özel
9. `notifications.css`

**Modül-özel CSS'ler view-level:** `builder-v2.css` sadece CreateReportV2/EditReportV2'de; `org-chart.css` sadece OrgChart sayfalarında.

## Kurallar (sıkı)

### 1. Inline style yasak (sıfır tolerans — Plan 25.1)

```html
<!-- YASAK -->
<div style="padding:14px; background:white;">

<!-- DOĞRU -->
<div class="form-section-card wide">
```

**İstisnalar (sadece bunlar):**
- `style="display:contents"` form-as-grid pattern (max 3 yerde)
- `style="--w: @pct%"` veri-driven CSS variable (trend bar)
- `Print.cshtml` view-local `<style>` block (Layout=null A4)
- Runtime/dynamic (GridStack drag-drop, Chart.js canvas width/height, Alpine `:style="..."` runtime eval)

### 2. Razor style expression yasak

```cshtml
<!-- YASAK -->
<span style="@(isActive ? "color:green" : "color:red")">

<!-- DOĞRU -->
<span class="pill @(isActive ? "ok-tone" : "danger")"><span class="dot"></span>@status</span>
```

### 3. css-classify skill kararı her CSS ekleme öncesi

3 adım (`.claude/skills/css-classify/SKILL.md`):
1. **Mevcut sınıfı ara** (`grep -nE "^\.<pattern>"` components.css → modül CSS)
2. **Reuse testi:** Başka modül kullanır mı? Evet → `components.css` (prefix YOK)
3. **Modül-özel:** Sadece bu modülün marka/domain kimliği mi? Evet → `components-<modül>.css` (prefix VAR)

**Şüphede generic varsay** (sonradan modül-özel olduğu anlaşılırsa taşı).

### 4. Token kullanımı zorunlu (hardcoded renk yasak)

```css
/* YASAK */
.foo { color: #1e40af; background: rgba(59, 130, 246, .12); }

/* DOĞRU */
.foo { color: var(--info-ink); background: var(--info-bg-badge); }
```

**Mevcut semantic değişkenler (`tokens.css`):**
- Renk paleti: `--brand-red(-light/-dark/-darker)`, `--brand-gray-50/100/200/300/400/500/600/700/800/900`, `--brand-white(-soft)`, `--brand-black`
- Status: `--success`, `--warning`, `--error`, `--info`, `--ok`, `--warn`, `--danger`
- Ink/paper: `--ink-0/1/2/3/4/5`, `--paper`, `--canvas`, `--chip`, `--line`, `--line-2`
- Accent: `--accent`, `--accent-soft`, `--accent-ink`
- Alias: `--border` → `--line`, `--muted` → `--ink-3`, `--ink-mute` → `--ink-3`, `--err` → `--danger`, `--bg-1` → `--canvas`
- Plan 25.1 semantic: `--info-ink/-bg-soft/-bg-softer/-bg-hover/-bg-chip/-bg-badge/-border-soft`
- Domain ton: `--karar-ink/-bg-soft`, `--bilgi-ink/-bg-soft`, `--uyari-ink/-bg-soft`, `--duyuru-ink/-bg-soft`
- AI: `--violet/-soft/-border`
- Soft bg/ink: `--danger-bg-soft`, `--danger-ink-soft`, `--success-bg-soft`, `--success-ink-soft`, `--warn-bg-soft`, `--warn-ink-soft`

Yeni renk gerekiyorsa: önce **tokens.css'e ekle**, sonra components'te `var(--xxx)` kullan.

## Utility class kataloğu (en sık)

### Layout
- `app-shell`, `sidebar`, `main`, `topbar`
- `side-group`, `side-section`, `side-group-head`, `nav-list`, `nav-subsection`, `nav-badge`
- `crumbs`, `hero` + `h1` + `sub` (`mono`, `pill`, `dot`)

### Form
- `form-section-card` + `.wide` (max 880px) / `.narrow` (max 720px) / `.split` / `.detail`
- `form-section-head` + `__title` + `__sub`
- `form-section-body` + `.stack` + `.detail-grid`
- `action-row` + `.wide` / `.narrow` / `.with-hint` + `.hint-text`
- `field` + `.error` + `.tight` + `.mt-12`
- `lab` + `.inline`
- `inp` + `.mono` + `.json` + `.disabled-mono` + `.inp-narrow` + `.inp-file` + `.inp-hex` + `.inp-code/-label/-order`
- `textarea-auto`
- `checkbox-grid` (`.auto-fit-140/-160`) + `checkbox-row` + `checkbox(.checkbox-lg)`
- `toggle-row`, `toggle-label`
- `help-text` (`.error/.help-success`) + `.help-card` + `field-warn-banner`
- `param-list-stack`, `param-builder-row`, `param-required-cell`
- `text-danger`, `req`, `pill-icon`

### Buton
- `btn` (34px) + `.sm` (28px) + `.lg` (40px) + `.block`
- `.btn.primary` (kırmızı CTA) + `.ghost` (kenarlıksız) + `.danger` + `.tertiary` (violet)
- `.icon-btn`
- `btn-group` (flex container 8px gap)
- `btn-row`
- `.btn-count` (sayım badge buton içinde)
- `btn.compact-sm` (24px) — series remove vs

### Tablo
- `card-paper`, `paper-card(.with-padding/.with-padding-lg)`, `paper-table`
- `dt` + `mono` + `ago` + `status.ok/.warn/.err`
- `dt td.cell-muted`, `cell-right`, `col-right`, `row-actions`
- `dt .trend-bar` (data-driven `--w: @pct%`)
- `dt th.w-100/.w-40p/.w-30`
- `table-card`, `table-card-head` + `__title` + `__count`
- `inline-form` (POST butonu wrap)

### Card / List / Modal
- `paper-card` + `.with-padding/.with-padding-lg`
- `content-card` + `__label/__body/__files/__files-list`
- `info-card`
- `dashed-card` + `.dc-icon`
- `qcard` + `.has-overlay/__date-icon/__time-icon/__num/__ago-mono`
- `list-stack` + `list-item` + `__head/__no/__body/__meta/__tip-row/__files` + `.urgent/.karar/.uyari/.bilgi/.duyuru`
- `list-grid-2`, `flat-list/__row/__row-code`
- `modal-backdrop`, `modal-overlay` (Alpine), `modal-card(.scrollable)`, `modal-head-block` + `__count`, `modal-title`, `modal-desc`, `modal-scroll`, `modal-list-wrap`, `modal-list/__item(.is-root/.is-selected)/__item-title/-code/-inactive/-meta`, `modal-list-empty`, `modal-actions(.between)`, `modal-form-grid`
- `side-card` + `__title/__desc/__empty`

### Pill / Badge / Status
- `pill` + `.dot(.ok/.warn/.err)` + `.ghost`
- `.pill.ok/.error/.danger/.warn` (renkli pill)
- `.pill.ok-tone/.danger/.warn` (dot tone variant)
- `.pill.info-tone` (mavi)
- `.pill.urgent` (kırmızı solid acil)
- `badge` (`.is-read/.is-new`)
- `type-chip` (`.type-warn/-karar/-uyari/-bilgi/-duyuru/-prosedur`) — Tamim blok tipi
- `status.ok/.warn/.err`

### File / Upload
- `file-pill` + `.fp-ext(.pdf/.xlsx/.docx)` + `.fp-size` + `.fp-ext-generic` + `.fp-size-dot`
- `file-chip` + `__ext` + `__size`
- `attached-file-list`, `attached-file` + `__main/__ext/__name/__size`, `attached-file-list-empty`
- `upload-zone(.lg)` + `upload-icon/__input/__title/__sub`

### Filter & Search
- `filter-bar` + `.tabs/.with-toggle`
- `filter-bar__field` + `.--grow`
- `filter-bar__actions`
- `fb-search-grow` + `fb-icon` + `fb-toggle-group`
- `search-input-wrap` + `__icon`

### Overview / Dashboard
- `overview-health` + `.health-card.ok/.warn` + `.health-icon/__title/__desc`
- `overview-kpis` + `kpi-tile` + `.kpi-tile-alert` + `__icon/__body/__label/__value/__total`
- `overview-grid` + `overview-aside` + `overview-panel` + `__head/__link`
- `overview-activity` + `.act-icon/__body/__title/__actor/__desc/__time` + `.tone-success/.warn/.danger/.neutral`
- `overview-actions` + `action-btn` + `.primary`
- `overview-list` + `.ovl-title/.ovl-meta`
- `overview-empty`

### Genel
- `empty-state` (dashed border + 48px ikon) + `__icon` + `__action` + `.solid/.paper-dashed`
- `alert` + `.success/.warn/.error/.danger` + `.alert-link` + `.alert-icon`
- `state-card` + `__icon(.danger/.warn/.ok)` + `state-meta`
- `code-chip` (mono identifier küçük chip)
- `code-block` + `.error` (`var(--err)`)

### İkon ve metin
- `icon-mr` (margin-right 4px), `icon-xs` (10px), `icon-lg`
- `text-right`, `text-center`, `text-muted`, `text-xs`, `text-danger`, `text-accent`, `text-dim`, `text-ink-1`
- `mono` (JetBrains Mono), `ago` (tarih)
- `mt-8/14/16/18`, `mb-8/18`, `ml-4/auto`

### State (JS toggled)
- `is-open`, `is-error`, `is-selected`, `is-active`, `is-root`
- `collapsed`, `mobile-open`, `oc-dragging`

## Pattern — yeni view yazımı

```cshtml
@model Mosaik.ViewModels.SozlesmeFormViewModel
@{
    Layout = "_AppLayout";
    ViewData["Title"] = "Sözleşme - Mosaik";
}

@section Breadcrumb {
    <nav class="crumbs" aria-label="Sayfa konumu">
        <a href="/Contracts">Sözleşmeler</a>
        <span class="sep" aria-hidden="true">/</span>
        <span aria-current="page">Yeni</span>
    </nav>
}

@section TopActions {
    <a href="/Contracts" class="btn">
        <i class="fas fa-xmark icon-xs"></i> İptal
    </a>
}

<div class="hero">
    <h1>Yeni Sözleşme</h1>
    <div class="sub">
        <span>Sözleşme tanımı oluştur</span>
        <span class="pill ok-tone"><span class="dot"></span>Aktif</span>
    </div>
</div>

@if (!string.IsNullOrEmpty(Model.Message))
{
    @await Html.PartialAsync("_AlertMessage", (Model.MessageType ?? "info", Model.Message))
}

<form method="post" action="/Contracts/Create" novalidate>
    @Html.AntiForgeryToken()

    <div class="form-section-card wide">
        <div class="form-section-head">
            <h2 class="form-section-head__title">
                <i class="fas fa-file-contract icon-mr"></i> Temel Bilgiler
            </h2>
        </div>
        <div class="form-section-body stack">
            <div class="field">
                <label class="lab" for="Title">Başlık <span class="text-danger">*</span></label>
                <input id="Title" name="Title" value="@Model.Title" class="inp" required maxlength="200" />
            </div>
            <div class="grid-2col">
                <div class="field">
                    <label class="lab" for="StartDate">Başlangıç</label>
                    <input id="StartDate" name="StartDate" type="date" class="inp" />
                </div>
                <div class="field">
                    <label class="lab" for="EndDate">Bitiş</label>
                    <input id="EndDate" name="EndDate" type="date" class="inp" />
                </div>
            </div>
        </div>
    </div>

    <div class="action-row wide">
        <a href="/Contracts" class="btn ghost">
            <i class="fas fa-arrow-left icon-xs"></i> Geri Dön
        </a>
        <button type="submit" class="btn primary">
            <i class="fas fa-save icon-xs"></i> Kaydet
        </button>
    </div>
</form>
```

## Anti-pattern (yasak)

- `style="..."` HTML attribute (Plan 25.1 sıfır tolerans)
- Razor `style="@(c ? "a" : "b")"` (class modifier ile)
- Hardcoded renk `#1e40af` / `rgba(...)` modül CSS'inde (token kullan)
- `.tamim-filterbar` / `.tcc-*` / `.bi-*` gibi generic ama prefix'li sınıf adı (anti-pattern, generic'e taşı)
- `Html.BeginForm` (raw `<form method="post">` + `@Html.AntiForgeryToken()`)
- Modül CSS'inde duplicate utility (zaten components.css'te var)
- Tek-kullanımlık inline `width:130px` → `.inp-code` (130px width utility)
- View-local `<style>` block (Print.cshtml istisna)

## Refactor stratejisi (var olan inline)

1. View'da `style="..."` say: `grep -c 'style="' <view>`
2. css-classify skill'i her inline için çağır
3. Mevcut utility varsa kullan (`.field/.lab/.inp/.btn/.pill`)
4. Yoksa generic mi modül-özel mi karar (Adım 2-3)
5. Build + preview smoke
6. `grep -c` sıfır olana kadar

## Hızlı referans

- `Mosaik/wwwroot/assets/css/components.css` — canonical generic
- `Mosaik/wwwroot/assets/css/tokens.css` — renk + semantic
- `.claude/rules/inline-style-guard.md` — kural + istisna + CSS dosyası seçimi tablosu
- `.claude/rules/ui-patterns.md` — pattern detayları
- `docs/MOSAIK_DESIGN_PROMPT.md` — tasarım üretim promptu
- `.claude/skills/css-classify/SKILL.md` — sınıflandırma akışı

## İlişkili skill'ler

- `css-classify` — bu skill'den önce çağrılır (sınıflandırma)
- `mosaik-csharp-razor` — view + form pattern
- `mosaik-js-expert` — interaktif state class'ları
- `frontend-design`, `responsive-design`, `accessibility-compliance` — genel UI standardı
- `ui-ux-pro-max` — WCAG + audit
