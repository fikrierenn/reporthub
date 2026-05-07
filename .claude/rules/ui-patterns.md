# UI Pattern Standardı (Mosaik)

_Yeni `.cshtml` view yazıyorsan / mevcut view'ı düzenliyorsan ÖNCE bu dosyayı oku. Tam HTML snippet'ler için: `.claude/rules/docs/UI_SNIPPETS.md`._

**Tetikleyici:** UI değişikliklerinde `frontend-design` + `ui-ux-pro-max` skill'leri otomatik devreye girer (CLAUDE.md §2.2). Bu rule onların output'unu tutarlı kılar.

## Referans dosyalar (en olgun)
- `Mosaik/Views/Admin/CreateUser.cshtml` — hero + section card + action row
- `Mosaik/Views/Admin/_AdminTabUsers.cshtml` — tablo
- `Mosaik/Views/Reports/Index.cshtml` — filter bar + empty state
- `Mosaik.Modules.Tamim/Areas/Tamim/Views/Blok/Index.cshtml` — filter butonu + subnav

---

## 1. Buton Standardı

| Class | Boyut | Kullanım |
|---|---|---|
| `.btn` | 34px h · 12.5px font · 14px pad | Default: TopActions iptal, ikincil |
| `.btn.primary` | 34px | Kırmızı CTA — Kaydet/Yeni/Onay/Filtrele |
| `.btn.ghost` | 34px | Kenarlıksız — Geri Dön/İptal/sekonder |
| `.btn.danger` | 34px | accent-ink renkli outline — silme |
| `.btn.sm` | 28px h · 11.5px font · 10px pad | Tablo satırı aksiyonları |
| `.btn.lg` | 40px h · 13.5px font · 18px pad | Hero CTA |
| `.btn.block` | width:100% | Form alt CTA (Login pattern) |
| `.icon-btn` | — | İkon-only — yardım, kapat |

**Yasak:** `style="font-size:11px; height:28px; ..."` gibi inline override. Yerine `.btn.sm` kullan.

**İkon boyutu:** btn/lg içinde `text-[10px]`, sm içinde `text-[9px]`.

---

## 2. Top Actions

`@section TopActions { ... }` — 3 kalıptan biri:

- **A) Sade İptal** (Create/Edit) → tek `<a class="btn">İptal</a>`
- **B) Primary CTA + Yardım** (Liste) → `<a class="btn primary">Yeni X</a>` + `<a class="icon-btn" title="Yardım">`
- **C) Sadece Yardım** → tek `<a class="icon-btn">`

Üçüncü buton ekleyeceksen → kullanıcıya sor.

---

## 3. Hero

`<div class="hero">` + `h1` + `.sub`. Sub içinde:
- `<span>düz metin</span>`
- `<span class="mono">@count kayıt</span>` (sayısal/teknik)
- `<span class="pill">metin</span>` (etiket)
- `<span class="pill"><span class="dot"></span>metin</span>` (dot=ok/warn/err)

**Emoji yok** — Dashboard'daki `👋` istisna.

---

## 4. Filter Bar

Sadece **liste sayfalarında**. Form sayfalarında YOK.

- Paper card kapsayıcı (border + radius)
- Search input + magnifying glass ikon (absolute)
- Submit `btn primary "Filtrele"`
- Ghost `btn ghost "Temizle"` (`x-show="q"`)
- Alpine `x-data="{ q: '' }"` + `x-model="q"`
- Tablo satırlarında `data-search` + `x-show="!q || $el.dataset.search.includes(q.toLowerCase())"`

Snippet için → `docs/UI_SNIPPETS.md` §4.

---

## 5. Empty State

Dashed border + 48px ikon + h3 + p. Solid border kullanma.

İkonlar: `fa-folder-open` (genel/rapor), `fa-history` (log), `fa-inbox` (mesaj/blok).

Snippet → `docs/UI_SNIPPETS.md` §5. Veya partial: `_EmptyState.cshtml` (model: `EmptyStateViewModel`).

---

## 6. Tablo

- Paper wrapper (`background:var(--paper); border + radius + overflow:hidden`)
- `<table class="dt">`
- Username/key → `<span class="mono">`
- Tarih → `<span class="ago">`
- Durum → `<span class="status ok|warn|err">`
- Aksiyonlar her zaman `btn ghost sm` (Düzenle) + `btn sm danger` (Sil)
- Aksiyon kolonu `style="text-align:right;"`

Snippet → `docs/UI_SNIPPETS.md` §6.

---

## 7. Form Section Card + Action Row

- **Section card:** paper + border + radius + `max-width:880px` (geniş) veya `720px` (dar) + `margin-bottom:18px`
  - Header `padding:14px 18px; border-bottom`
  - Body `padding:20px`
- **Action row:** ayrı paper card, `padding:14px 20px`, `space-between`
  - Sol `btn ghost` "Geri Dön"
  - Sağ `btn primary` "Kaydet"
  - `max-width` üst card ile **eşleşmeli**

Snippet → `docs/UI_SNIPPETS.md` §7. Veya partial: `_FormActionRow.cshtml`.

**Mini alternatif:** Section card içinde `padding-top:18px; border-top` ile entegre — küçük formlarda (Profile, EditRole).

---

## 8. Alert / Mesaj

`role="alert"` zorunlu. 3 renk: success (yeşil), warn (sarı), error (kırmızı).

Partial: `_AlertMessage.cshtml`:
```cshtml
@await Html.PartialAsync("_AlertMessage", ("success", "Kaydedildi"))
```

Snippet → `docs/UI_SNIPPETS.md` §8.

---

## 9. Subnav, Breadcrumb, FooterHint

- **Subnav:** `<nav class="subnav" aria-label="...">` — her modülün kendi `_<Module>Subnav.cshtml`'i
- **Breadcrumb:** `<nav class="crumbs" aria-label="Sayfa konumu">` (yeni standart). `<div class="crumbs">` legacy — kullanma
- **FooterHint:** `@await Html.PartialAsync("_FooterHint", (string?)$"{count} kayıt")` — sadece liste/dashboard, form'da yok. Cast `(string?)` zorunlu

---

## 10. Yeni Sayfa Kontrol Listesi

1. `Layout = "_AppLayout"` (ViewStart yoksa elle)
2. `@section Breadcrumb` — `<nav class="crumbs" aria-label>` + `aria-current="page"`
3. `@section TopActions` — Liste=B, Form=A, Sade=C
4. `<div class="hero">` + h1 + sub (mono/pill/dot)
5. Liste → filter bar + Alpine `x-model`
6. Form → section card + action row (sol ghost + sağ primary)
7. Tablo → paper wrapper + `.dt` + mono/ago/status + ghost sm/danger
8. Empty state → dashed + 48px ikon + h3 + p
9. Alert → `role="alert"` + 3 renk
10. Buton inline override **yok** — `.btn.sm/lg/danger/block`
11. İkon: btn `text-[10px]`, sm `text-[9px]`
12. FooterHint → liste/dashboard, cast `(string?)`
13. Türkçe UTF-8 (Düzenle/Bölüm/İçerik)
14. POST → `@Html.AntiForgeryToken()` + raw `<form method="post">` (Html.BeginForm yok)
15. A11y: aria-label, aria-current, aria-hidden, scope th'de

---

## 11. Önerilen Shared Partial'lar

- `Views/Shared/_AlertMessage.cshtml` — `(string type, string message)` tuple
- `Views/Shared/_EmptyState.cshtml` — `EmptyStateViewModel`
- `Views/Shared/_FormActionRow.cshtml` — `FormActionRowViewModel`
- `Views/Shared/_FilterBar.cshtml` — `FilterBarViewModel`
- `Views/Shared/_FooterHint.cshtml` — `string?`

ViewModel'ler: `Mosaik/ViewModels/Ui/EmptyStateViewModel.cs`.

---

## 12. Bilinen Sapmalar (dokunduğun sayfada düzelt)

- `Auth/Login.cshtml:49` — inline `width:100%; height:38px` → `class="btn primary block"`
- `Admin/BrandSettings.cshtml:43,93-99` — `Html.BeginForm` → raw form; action row pattern'a çek
- `Admin/Modules.cshtml:43,86-88` — Aynı düzeltme
- `Logs/Index.cshtml:123` — `<div class="activity">` wrapper kaldır, sadece paper + `.dt`
- `Admin/_AdminTabFilters.cshtml:29-34` — Empty state dashed/48px/ikon standardı
- `Admin/Index.cshtml:29-68` — Inline subnav `_AdminSubnav` partial'ına merge (count parametresi)
- `Shared/_FooterHint.cshtml:19` — "ReportHub" → "Mosaik"
- Mosaik core view'larda `<div class="crumbs">` → `<nav class="crumbs" aria-label>` (low priority)

---

## 13. İlişkili Skill ve Agent

Yeni pattern eklemeden önce:
1. Bu rule'a bak
2. Yetmezse `code-explorer` agent ile mevcut envanter
3. `code-architect` ile yeni pattern blueprint
4. Pattern eklenince bu rule'a yaz

UI değişikliklerinde otomatik tetik:
- `frontend-design` skill — visual design + interaction
- `ui-ux-pro-max` skill — WCAG/contrast/touch target audit
- `accessibility-compliance` skill — A11y checklist

**TL;DR:** Bu dosya = Mosaik UI'ın anayasası. Yeni view = bu listeden geçir. Tam HTML snippet'ler için `docs/UI_SNIPPETS.md`.
